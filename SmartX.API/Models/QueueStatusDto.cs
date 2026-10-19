using System;
using System.Collections.Generic;

namespace SmartX.API.Models
{
    /// <summary>
    /// Encapsulates real-time runtime metrics of all 5 required advanced data structures:
    /// 1. FIFO Queue&lt;T&gt; (Standard Telemetry Ingestion)
    /// 2. PriorityQueue&lt;T, P&gt; (Critical Alert Ingestion)
    /// 3. Stack&lt;T&gt; (Command History &amp; Undo Reversals)
    /// 4. Dictionary&lt;TKey, TValue&gt; (Live Device Registry O(1))
    /// 5. SortedDictionary&lt;DateTime, TValue&gt; (Sequential Timeline History)
    /// 6. HashSet&lt;T&gt; (Unique Active Error States)
    /// </summary>
    public class QueueStatusDto
    {
        public int StandardFifoQueueCount { get; set; }
        public int PriorityAlertQueueCount { get; set; }
        public int CommandUndoStackCount { get; set; }
        public int RegisteredDevicesCount { get; set; }
        public int HistoricalTimelineCount { get; set; }
        public int ActiveErrorCount { get; set; }
        public List<string> ActiveErrorList { get; set; } = new();
        public List<string> RecentCommands { get; set; } = new();
        public long TotalPacketsProcessed { get; set; }
        public long TotalPriorityBypasses { get; set; }
        public DateTime LastProcessedTimestamp { get; set; } = DateTime.UtcNow;
    }
}
