using System;

namespace RLF.Core.Jobs.Shift
{
    public sealed class ShiftSchedule
    {
        public ShiftType Type { get; }
        public TimeSpan StartTime { get; }
        public TimeSpan EndTime { get; }
        public int MinTasks { get; }
        public int MaxTasks { get; }

        public ShiftSchedule(
            ShiftType type,
            TimeSpan startTime,
            TimeSpan endTime,
            int minTasks,
            int maxTasks)
        {
            Type = type;
            StartTime = startTime;
            EndTime = endTime;
            MinTasks = minTasks;
            MaxTasks = maxTasks;
        }

        public bool IsAvailable(DateTime currentTime)
        {
            TimeSpan now = currentTime.TimeOfDay;
            return now >= StartTime && now < EndTime;
        }

        public int GenerateTaskCount(Random rng)
        {
            return rng.Next(MinTasks, MaxTasks + 1);
        }
    }
}