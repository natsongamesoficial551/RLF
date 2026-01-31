using RLF.Core.Events.EventArgs;
using RLF.Core.Jobs.Enums;
using RLF.Core.Jobs.Shift;

namespace RLF.Core.Jobs.Events
{
    public sealed class ShiftStartedEvent : RLFEventArgs
    {
        public JobType JobType { get; }
        public ShiftType ShiftType { get; }
        public int TasksTotal { get; }

        public ShiftStartedEvent(
            JobType jobType,
            ShiftType shiftType,
            int tasksTotal)
        {
            JobType = jobType;
            ShiftType = shiftType;
            TasksTotal = tasksTotal;
        }
    }
}