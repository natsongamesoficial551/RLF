// ===============================
// CancellationTracker.cs
// ===============================
using System;
using System.Collections.Generic;

namespace RLF.GTA.Jobs.Uber.Penalty
{
    public sealed class CancellationTracker
    {
        private readonly List<DateTime> _cancellations;
        private readonly TimeSpan _trackingWindow;

        public CancellationTracker()
        {
            _cancellations = new List<DateTime>();
            _trackingWindow = TimeSpan.FromHours(1);
        }

        public void RecordCancellation()
        {
            _cancellations.Add(DateTime.UtcNow);
            CleanOldRecords();
        }

        public int GetRecentCancellations()
        {
            CleanOldRecords();
            return _cancellations.Count;
        }

        private void CleanOldRecords()
        {
            DateTime threshold = DateTime.UtcNow - _trackingWindow;
            _cancellations.RemoveAll(dt => dt < threshold);
        }
    }
}