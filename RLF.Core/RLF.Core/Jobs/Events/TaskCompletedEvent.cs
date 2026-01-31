using RLF.Core.Events.EventArgs;
using RLF.Core.Jobs.Enums;

namespace RLF.Core.Jobs.Events
{
    public sealed class TaskCompletedEvent : RLFEventArgs
    {
        public JobType JobType { get; }
        public int TaskNumber { get; }
        public int TasksRemaining { get; }

        public TaskCompletedEvent(
            JobType jobType,
            int taskNumber,
            int tasksRemaining)
        {
            JobType = jobType;
            TaskNumber = taskNumber;
            TasksRemaining = tasksRemaining;
        }
    }
}