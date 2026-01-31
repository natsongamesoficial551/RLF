using RLF.Core.Events;
using RLF.Core.Events.EventArgs;

namespace RLF.Core.Identity.Events
{
    public class DocumentChangedEvent : RLFEventArgs
    {
        public string DocumentKey { get; }
        public string OldStatus { get; }
        public string NewStatus { get; }
        public string Reason { get; }

        public DocumentChangedEvent(string key, string oldStatus, string newStatus, string reason)
        {
            DocumentKey = key;
            OldStatus = oldStatus;
            NewStatus = newStatus;
            Reason = reason;
        }
    }
}
