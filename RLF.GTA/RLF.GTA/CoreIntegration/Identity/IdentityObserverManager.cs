using System;
using GTA;
using RLF.Core.Debug;

namespace RLF.GTA.CoreIntegration.Identity
{
    /// <summary>
    /// Manager central dos observers de identidade.
    /// ✅ Anti-duplicado real
    /// ⚠️ NÃO instancia observers manualmente (SHVDN já faz isso).
    /// </summary>
    public sealed class IdentityObserverManager : Script
    {
        private static bool _isActiveInstance;

        public IdentityObserverManager()
        {
            if (_isActiveInstance)
            {
                try { RLFDebug.Warning(DebugChannel.System, "IdentityObserverManager duplicado detectado -> Abort()"); }
                catch { }
                Abort();
                return;
            }

            _isActiveInstance = true;

            Aborted += OnAborted;

            RLFDebug.Info(DebugChannel.System, "IdentityObserverManager iniciado (modo passivo / anti-dup)");
        }

        private void OnAborted(object sender, EventArgs e)
        {
            _isActiveInstance = false;
        }
    }
}
