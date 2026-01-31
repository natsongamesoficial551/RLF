using RLF.Core.Events.EventArgs;
using RLF.Core.Jobs.Enums;
using RLF.Core.Jobs.Shift;

namespace RLF.Core.Jobs.Events
{
    public sealed class ShiftCompletedEvent : RLFEventArgs
    {
        public JobType JobType { get; }
        public ShiftType ShiftType { get; }
        public int TasksCompleted { get; }
        public decimal Payment { get; }

        public ShiftCompletedEvent(
            JobType jobType,
            ShiftType shiftType,
            int tasksCompleted,
            decimal payment)
        {
            JobType = jobType;
            ShiftType = shiftType;
            TasksCompleted = tasksCompleted;
            Payment = payment;
        }
    }
}