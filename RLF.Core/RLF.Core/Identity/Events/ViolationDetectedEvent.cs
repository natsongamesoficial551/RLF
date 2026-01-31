using RLF.Core.Events.EventArgs;
using RLF.Core.Identity.Enums;

namespace RLF.Core.Identity.Events
{
    public class ViolationDetectedEvent : RLFEventArgs
    {
        public ViolationType Type { get; }
        public ViolationSeverity Severity { get; }
        public string Context { get; }

        public ViolationDetectedEvent(
            ViolationType type,
            ViolationSeverity severity,
            string context)
        {
            Type = type;
            Severity = severity;
            Context = context;
        }
    }
}
