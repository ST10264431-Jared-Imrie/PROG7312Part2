using System;

namespace SmartX.API.Models
{
    /// <summary>
    /// Represents an algorithmic recommendation produced by the Predictive Action & Recommendation Engine.
    /// Derived from historical sensor patterns, frequent user queries, and critical anomaly thresholds.
    /// </summary>
    public class PredictiveInsight
    {
        public string InsightId { get; set; } = Guid.NewGuid().ToString("N");
        public string TargetMac { get; set; } = string.Empty;
        public string TargetDeviceName { get; set; } = string.Empty;
        public string SuggestedAction { get; set; } = string.Empty; // e.g. "TRIGGER_PUMP", "TOGGLE_RELAY", "SHUTDOWN_VALVES"
        public string ActionParameter { get; set; } = "ON";
        public string Reason { get; set; } = string.Empty;
        public double ConfidenceScore { get; set; } = 0.95; // e.g. 0.0 - 1.0 (95% confidence)
        public string Severity { get; set; } = "HIGH"; // CRITICAL, HIGH, MEDIUM, LOW
        public string PatternCorrelation { get; set; } = string.Empty; // e.g. "Frequent query pattern (Pump A after Moisture < 20%)"
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
        public bool IsDismissed { get; set; } = false;

        public PredictiveInsight()
        {
        }

        public PredictiveInsight(string mac, string deviceName, string action, string param, string reason, double confidence, string severity, string pattern)
        {
            TargetMac = mac;
            TargetDeviceName = deviceName;
            SuggestedAction = action;
            ActionParameter = param;
            Reason = reason;
            ConfidenceScore = confidence;
            Severity = severity;
            PatternCorrelation = pattern;
            GeneratedAt = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Captures a user search or filter interaction on the frontend to train the pattern analyzer.
    /// </summary>
    public class UserSearchEvent
    {
        public string SearchTerm { get; set; } = string.Empty;
        public string SelectedCategory { get; set; } = string.Empty;
        public string SelectedAlertState { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        public UserSearchEvent()
        {
        }

        public UserSearchEvent(string term, string cat, string state)
        {
            SearchTerm = term;
            SelectedCategory = cat;
            SelectedAlertState = state;
            Timestamp = DateTime.UtcNow;
        }
    }
}
