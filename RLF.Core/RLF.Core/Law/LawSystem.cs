using RLF.Core.Debug;
using RLF.Core.Events;
using RLF.Core.Events.EventArgs;
using RLF.Core.Identity.Events;
using RLF.Core.Logging;
using RLF.Core.Systems;
using System;

namespace RLF.Core.Law
{
    /// <summary>
    /// Sistema legal central.
    /// Decide ações legais com base em violações.
    /// NÃO executa punições (isso é papel do GTA / controllers).
    /// </summary>
    public sealed class LawSystem : SystemBase
    {
        private readonly EventManager _eventManager;

        public LawSystem(Logger logger, EventManager eventManager)
            : base("LawSystem", logger, eventManager, tickRate: 1)
        {
            _eventManager = eventManager;
        }

        protected override void OnStart()
        {
            // 🔴 CRÍTICO: escuta direto no EventManager real do Core
            _eventManager.Subscribe(
                "identity:violation_detected",
                OnViolationDetected
            );

            Logger.Info("LawSystem iniciado");
            SafeDebugInfo("LawSystem iniciado");
        }

        protected override void OnStop()
        {
            _eventManager.Unsubscribe(
                "identity:violation_detected",
                OnViolationDetected
            );

            Logger.Info("LawSystem parado");
            SafeDebugInfo("LawSystem parado");
        }

        protected override void OnTick()
        {
            // Nenhuma lógica por tick nesta fase
        }

        private void OnViolationDetected(object sender, RLFEventArgs args)
        {
            if (!(args is ViolationDetectedEvent violation))
                return;

            LawRule rule;
            try
            {
                rule = LawRulesConfig.GetRule(violation.Type);
            }
            catch (Exception ex)
            {
                Logger.Error($"[LAW] Falha ao obter regra para {violation.Type}", ex);
                SafeDebugError("[LAW] Erro ao buscar regra", ex);
                return;
            }

            if (rule == null)
            {
                Logger.Warning($"[LAW] Nenhuma regra encontrada para {violation.Type}");
                SafeDebugWarning($"[LAW] Regra inexistente para {violation.Type}");
                return;
            }

            // 📜 LOG DE DECISÃO (ESSE LOG É O SEU SINAL DE VIDA)
            Logger.Warning(
                $"[LAW] Violação recebida | Tipo={violation.Type} | Severidade={violation.Severity} | Ação={rule.Action} | Multa={rule.FineAmount}"
            );

            SafeDebugWarning(
                $"[LAW] Ação={rule.Action} | Multa={rule.FineAmount} | Contexto={violation.Context}"
            );

            // 🚨 EVENTO OFICIAL → GTA
            _eventManager.Raise(
                "law:decision_made",
                new RLFEventArgs<LawRule>(rule)
                {
                    CustomData = violation
                }
            );
        }

        // =========================
        // 🔒 DEBUG SEGURO (ANTI-CRASH)
        // =========================

        private static void SafeDebugInfo(string msg)
        {
            try { RLFDebug.Info(DebugChannel.System, msg); } catch { }
        }

        private static void SafeDebugWarning(string msg)
        {
            try { RLFDebug.Warning(DebugChannel.System, msg); } catch { }
        }

        private static void SafeDebugError(string msg, Exception ex)
        {
            try { RLFDebug.Error(DebugChannel.System, msg, ex); } catch { }
        }
    }
}
