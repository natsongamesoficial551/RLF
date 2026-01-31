using System;

namespace RLF.Core.Debug
{
    public sealed class DebugScope : IDisposable
    {
        private readonly string _name;
        private bool _ended;

        public DebugScope(string name)
        {
            _name = name;
            RLFDebug.Trace(DebugChannel.Performance, $"ENTER {_name}");
        }

        public void Dispose()
        {
            if (_ended) return;
            _ended = true;
            RLFDebug.Trace(DebugChannel.Performance, $"EXIT {_name}");
        }
    }
}
