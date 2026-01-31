using System;
using RLF.Core.Debug;
using RLF.Core.Events;
using RLF.Core.Events.EventArgs;
using RLF.Core.Identity.Events;
using RLF.Core.Identity.Enums;

namespace RLF.Core.Identity.Debug
{
    /// <summary>
    /// Listener de debug para eventos do DocumentSystem.
    /// Apenas loga informações — NÃO altera gameplay.
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

            RLFDebug.Info(
                DebugChannel.System,
                "IdentityDebugListener: escuta ativa"
            );
        }

        public void StopListening()
        {
            if (!_isListening)
                return;

            _eventManager.Unsubscribe("identity:violation_detected", OnViolationDetected);
            _eventManager.Unsubscribe("identity:document_status_changed", OnDocumentStatusChanged);

            _isListening = false;

            RLFDebug.Info(
                DebugChannel.System,
                "IdentityDebugListener: escuta desativada"
            );
        }

        private void OnViolationDetected(object sender, RLFEventArgs e)
        {
            if (e is ViolationDetectedEvent violation)
            {
                RLFDebug.Warning(
                    DebugChannel.System,
                    $"VIOLAÇÃO: {violation.Type} | Severidade: {violation.Severity} | Contexto: {violation.Context}"
                );

                if (violation.Severity == ViolationSeverity.Critical)
                {
                    RLFDebug.Critical(
                        DebugChannel.System,
                        $"VIOLAÇÃO CRÍTICA: {violation.Type} | Contexto: {violation.Context}"
                    );
                }
            }
        }

        private void OnDocumentStatusChanged(object sender, RLFEventArgs e)
        {
            if (e is DocumentChangedEvent doc)
            {
                RLFDebug.Info(
                    DebugChannel.System,
                    $"DOCUMENTO [{doc.DocumentKey}] {doc.OldStatus} → {doc.NewStatus} | Razão: {doc.Reason}"
                );
            }
        }
    }
}
