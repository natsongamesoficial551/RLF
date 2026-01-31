using RLF.Core.Events.EventArgs;

namespace RLF.Core.Needs.Events
{
    public class NeedCriticalEvent : RLFEventArgs
    {
        public NeedType Type { get; }
        public float Value { get; }

        public NeedCriticalEvent(NeedType type, float value)
        {
            Type = type;
            Value = value;
        }
    }
}
