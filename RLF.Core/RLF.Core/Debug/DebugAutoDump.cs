using System;

namespace RLF.Core.Debug
{
    public static class DebugAutoDump
    {
        private static DebugSnapshot _lastSnapshot;

        public static void UpdateSnapshot(DebugSnapshot snapshot)
        {
            _lastSnapshot = snapshot;
        }

        public static void Dump(string reason, Exception ex = null)
        {
            if (_lastSnapshot != null)
            {
                RLFDebug.Critical(
                    DebugChannel.Crash,
                    $"AUTO-DUMP: {reason}",
                    ex,
                    _lastSnapshot.ToString()
                );
            }
            else
            {
                RLFDebug.Critical(
                    DebugChannel.Crash,
                    $"AUTO-DUMP (sem snapshot): {reason}",
                    ex
                );
            }
        }
    }
}
