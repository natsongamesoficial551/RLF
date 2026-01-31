using RLF.Core.Events;
using RLF.Core.Identity.Documents;
using RLF.Core.Identity.Enums;
using RLF.Core.Identity.Events;
using RLF.Core.Identity.Storage;
using RLF.Core.Logging;
using RLF.Core.Systems;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RLF.Core.Identity
{
    public class DocumentSystem : SystemBase
    {
        private readonly List<IdentityDocument> _documents;
        private readonly IIdentityStore _store;
        private readonly EventManager _eventManager;

        public DocumentSystem(
            Logger logger,
            EventManager eventManager)
            : base("DocumentSystem", logger, eventManager)
        {
            _eventManager = eventManager;
            _store = new IniIdentityStore("scripts/RLF/identity.ini");
            _documents = _store.Load().ToList();
        }

        protected override void OnStart()
        {
            Logger.Info($"{Name}: iniciado com {_documents.Count} documentos");
        }

        protected override void OnStop()
        {
            _store.Save(_documents);
            Logger.Info($"{Name}: estado salvo");
        }

        protected override void OnTick()
        {
            CheckExpirations();
        }

        // ===============================
        // 🔍 VERIFICA EXPIRAÇÕES
        // ===============================
        private void CheckExpirations()
        {
            foreach (var doc in _documents)
            {
                if (doc.ExpiresAt.HasValue &&
                    doc.Status == DocumentStatus.Valid &&
                    DateTime.UtcNow > doc.ExpiresAt.Value)
                {
                    ChangeStatus(doc, DocumentStatus.Expired, "Documento expirado");
                }
            }
        }

        // ===============================
        // ✅ CONSULTAS
        // ===============================
        public bool HasValidLicense(LicenseType type)
        {
            return _documents.Any(d =>
                d.Metadata.TryGetValue("LicenseType", out var v) &&
                v == type.ToString() &&
                d.Status == DocumentStatus.Valid);
        }

        // ===============================
        // ✅ EMISSÃO DE LICENÇA (CORRIGIDO)
        // ===============================
        public bool GrantLicense(LicenseType type, int validityDays = 365, string reason = "Emitida via teste")
        {
            try
            {
                // 🔍 Procura licença existente desse tipo
                IdentityLicense existing = _documents
                    .OfType<IdentityLicense>()
                    .FirstOrDefault(lic => lic.LicenseType == type);

                if (existing == null)
                {
                    // ✅ CRIA NOVA LICENÇA
                    var lic = new IdentityLicense(type)
                    {
                        Status = DocumentStatus.Valid,
                        IssuedAt = DateTime.UtcNow,
                        ExpiresAt = DateTime.UtcNow.AddDays(Math.Max(1, validityDays)),
                        Reason = reason
                    };

                    lic.LastStatusChangeAt = lic.IssuedAt;

                    _documents.Add(lic);

                    // 🔔 EVENTO
                    _eventManager.Raise(
                        "identity:document_status_changed",
                        new DocumentChangedEvent(
                            "CNH:" + type.ToString(),
                            DocumentStatus.Missing.ToString(),
                            DocumentStatus.Valid.ToString(),
                            reason
                        )
                    );

                    // 💾 SALVA IMEDIATAMENTE
                    _store.Save(_documents);

                    Logger.Info($"[DocumentSystem] CNH {type} emitida e salva com sucesso");

                    return true;
                }

                // ✅ REVALIDA LICENÇA EXISTENTE
                var old = existing.Status;

                existing.Status = DocumentStatus.Valid;
                existing.IssuedAt = DateTime.UtcNow;
                existing.ExpiresAt = DateTime.UtcNow.AddDays(Math.Max(1, validityDays));
                existing.LastStatusChangeAt = existing.IssuedAt;
                existing.Reason = reason;

                // 🔔 EVENTO
                _eventManager.Raise(
                    "identity:document_status_changed",
                    new DocumentChangedEvent(
                        "CNH:" + type.ToString(),
                        old.ToString(),
                        DocumentStatus.Valid.ToString(),
                        reason
                    )
                );

                // 💾 SALVA IMEDIATAMENTE
                _store.Save(_documents);

                Logger.Info($"[DocumentSystem] CNH {type} renovada e salva com sucesso");

                return true;
            }
            catch (Exception ex)
            {
                Logger.Error($"[DocumentSystem] Falha ao emitir CNH {type}", ex);
                return false;
            }
        }

        // ===============================
        // 🚨 VIOLAÇÕES (NÃO PUNE)
        // ===============================
        public void DetectViolation(
            ViolationType type,
            ViolationSeverity severity,
            string context)
        {
            _eventManager.Raise(
                "identity:violation_detected",
                new ViolationDetectedEvent(type, severity, context)
            );
        }

        // ===============================
        // 🔄 MUDANÇA DE STATUS
        // ===============================
        private void ChangeStatus(
            IdentityDocument doc,
            DocumentStatus newStatus,
            string reason)
        {
            var oldStatus = doc.Status;

            doc.Status = newStatus;
            doc.LastStatusChangeAt = DateTime.UtcNow;
            doc.Reason = reason;

            _eventManager.Raise(
                "identity:document_status_changed",
                new DocumentChangedEvent(
                    doc.Type.ToString(),
                    oldStatus.ToString(),
                    newStatus.ToString(),
                    reason
                )
            );
        }
    }
}
