using RLF.Core.Events.EventArgs;

namespace RLF.Core.Needs.Events
{
    public class NeedChangedEvent : RLFEventArgs
    {
        public NeedType Type { get; }
        public float OldValue { get; }
        public float NewValue { get; }

        public NeedChangedEvent(NeedType type, float oldValue, float newValue)
        {
            Type = type;
            OldValue = oldValue;
            NewValue = newValue;
        }
    }
}
