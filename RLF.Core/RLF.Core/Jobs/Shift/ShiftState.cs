using System;

namespace RLF.Core.Jobs.Shift
{
    public sealed class ShiftState
    {
        public ShiftType Type { get; internal set; }
        public bool IsActive { get; internal set; }
        public DateTime StartedAt { get; internal set; }
        public int TasksTotal { get; internal set; }
        public int TasksCompleted { get; internal set; }
        public int TasksRemaining => TasksTotal - TasksCompleted;
        public bool IsCompleted => TasksCompleted >= TasksTotal;

        public ShiftState()
        {
            Type = ShiftType.Morning;
            IsActive = false;
            TasksTotal = 0;
            TasksCompleted = 0;
        }
    }
}