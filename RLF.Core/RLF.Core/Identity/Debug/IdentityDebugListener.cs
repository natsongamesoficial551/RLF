using System;
using RLF.Core.Debug;
using RLF.Core.Events;
using RLF.Core.Events.EventArgs;
using RLF.Core.Identity.Events;

namespace RLF.Core.Identity.Debug
{
    /// <summary>
    /// Listener de debug para eventos de identidade.
    /// Apenas LOG – nunca altera estado.
    /// </summary>
    public sealed class IdentityDebugListener
    {
        private readonly EventManager _eventManager;
        private bool _isListening;

        public IdentityDebugListener(EventManager eventManager)
        {
            _eventManager = eventManager ?? throw new ArgumentNullException(nameof(eventManager));
        }

        public void StartListening()
        {
            if (_isListening)
                return;

            _eventManager.Subscribe("identity:violation_detected", OnViolationDetected);
            _eventManager.Subscribe("identity:document_status_changed", OnDocumentStatusChanged);

            _isListening = true;

            SafeInfo("IdentityDebugListener: escuta ativa");
        }

        public void StopListening()
        {
            if (!_isListening)
                return;

            _eventManager.Unsubscribe("identity:violation_detected", OnViolationDetected);
            _eventManager.Unsubscribe("identity:document_status_changed", OnDocumentStatusChanged);

            _isListening = false;

            SafeInfo("IdentityDebugListener: escuta desativada");
        }

        private void OnViolationDetected(object sender, RLFEventArgs e)
        {
            var violation = e as ViolationDetectedEvent;
            if (violation == null)
                return;

            SafeWarning($"[IDENTITY] VIOLAÇÃO: {violation.Type} | Severidade: {violation.Severity} | Contexto: {violation.Context}");

            if (violation.Severity == Enums.ViolationSeverity.Critical)
            {
                SafeCritical($"[IDENTITY] VIOLAÇÃO CRÍTICA: {violation.Type} | Contexto: {violation.Context}");
            }
        }

        private void OnDocumentStatusChanged(object sender, RLFEventArgs e)
        {
            var doc = e as DocumentChangedEvent;
            if (doc == null)
                return;

            SafeInfo($"[IDENTITY] DOCUMENTO [{doc.DocumentKey}] {doc.OldStatus} -> {doc.NewStatus} | Razão: {doc.Reason}");
        }

        // =========================
        // 🔒 SAFE DEBUG (ANTI-CRASH)
        // =========================

        private static void SafeInfo(string msg)
        {
            try { RLFDebug.Info(DebugChannel.System, msg); } catch { }
        }

        private static void SafeWarning(string msg)
        {
            try { RLFDebug.Warning(DebugChannel.System, msg); } catch { }
        }

        private static void SafeCritical(string msg)
        {
            try { RLFDebug.Critical(DebugChannel.System, msg); } catch { }
        }
    }
}
