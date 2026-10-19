using System;
using System.Collections.Generic;
using System.Linq;
using SmartX.API.Models;

namespace SmartX.API.Services
{
    /// <summary>
    /// Predictive Action and Recommendation Engine.
    /// Analyzes historical sensor queries, search trends, and device telemetry to proactively
    /// suggest actions and highlight anomalous devices before operators manually search for them.
    /// </summary>
    public class PredictiveEngineService : IPredictiveEngineService
    {
        private readonly object _engineLock = new();

        // Historical query & search pattern logs
        private readonly List<UserSearchEvent> _searchEvents = new();
        private readonly List<DeviceCommand> _commandHistory = new();

        // Pattern frequency map: Key = (ConditionTrigger, SuggestedAction) -> Frequency Count
        private readonly Dictionary<string, int> _patternFrequencyMap = new(StringComparer.OrdinalIgnoreCase);

        // Active generated insights cache
        private readonly Dictionary<string, PredictiveInsight> _activeInsights = new(StringComparer.OrdinalIgnoreCase);

        public PredictiveEngineService()
        {
            // Seed baseline learned patterns representing expert operational knowledge
            _patternFrequencyMap["MOISTURE_LOW->TRIGGER_PUMP"] = 5;
            _patternFrequencyMap["POWER_SPIKE->TOGGLE_RELAY"] = 4;
            _patternFrequencyMap["TEMP_HIGH->OPEN_VALVE"] = 3;
        }

        /// <summary>
        /// Records user frontend searches and updates correlation weights between active alerts and queries.
        /// </summary>
        public void RecordUserSearch(string searchTerm, string category, string alertState)
        {
            if (string.IsNullOrWhiteSpace(searchTerm)) return;

            lock (_engineLock)
            {
                var evt = new UserSearchEvent(searchTerm, category, alertState);
                _searchEvents.Add(evt);

                // Pattern Learning: If operator searches for "pump" while moisture alert is active, increment correlation
                if (searchTerm.Contains("pump", StringComparison.OrdinalIgnoreCase))
                {
                    _patternFrequencyMap["MOISTURE_LOW->TRIGGER_PUMP"] = _patternFrequencyMap.GetValueOrDefault("MOISTURE_LOW->TRIGGER_PUMP", 0) + 1;
                }
                else if (searchTerm.Contains("relay", StringComparison.OrdinalIgnoreCase) || searchTerm.Contains("breaker", StringComparison.OrdinalIgnoreCase))
                {
                    _patternFrequencyMap["POWER_SPIKE->TOGGLE_RELAY"] = _patternFrequencyMap.GetValueOrDefault("POWER_SPIKE->TOGGLE_RELAY", 0) + 1;
                }
            }
        }

        public void RecordCommandExecution(DeviceCommand command)
        {
            if (command == null) return;

            lock (_engineLock)
            {
                _commandHistory.Add(command);

                if (command.ActionType.Equals("TRIGGER_PUMP", StringComparison.OrdinalIgnoreCase))
                {
                    _patternFrequencyMap["MOISTURE_LOW->TRIGGER_PUMP"] = _patternFrequencyMap.GetValueOrDefault("MOISTURE_LOW->TRIGGER_PUMP", 0) + 1;
                }
                else if (command.ActionType.Equals("TOGGLE_RELAY", StringComparison.OrdinalIgnoreCase))
                {
                    _patternFrequencyMap["POWER_SPIKE->TOGGLE_RELAY"] = _patternFrequencyMap.GetValueOrDefault("POWER_SPIKE->TOGGLE_RELAY", 0) + 1;
                }
            }
        }

        /// <summary>
        /// Evaluates live devices and learned patterns to proactively generate algorithmic suggestions.
        /// </summary>
        public IEnumerable<PredictiveInsight> GetCurrentInsights(IDeviceStateService deviceService)
        {
            lock (_engineLock)
            {
                var insights = new List<PredictiveInsight>();
                var allDevices = deviceService.GetAllDevices().ToList();

                // 1. Hydroponics Pattern Analysis: Moisture Drop & Pump Automation
                var hydroSensors = allDevices.Where(d => d.Category.Equals("Hydroponics", StringComparison.OrdinalIgnoreCase)).ToList();
                var pumps = allDevices.Where(d => d.Category.Equals("Actuator", StringComparison.OrdinalIgnoreCase) && d.DeviceName.Contains("Pump", StringComparison.OrdinalIgnoreCase)).ToList();

                foreach (var sensor in hydroSensors)
                {
                    // If moisture drops below 20% or sensor is in Critical/Warning status
                    if (sensor.LastReading < 20.0 || sensor.Status == "Critical" || sensor.Status == "Warning")
                    {
                        var targetPump = pumps.FirstOrDefault(p => p.PumpState != "ON") ?? pumps.FirstOrDefault();
                        if (targetPump != null)
                        {
                            int pumpFreq = _patternFrequencyMap.GetValueOrDefault("MOISTURE_LOW->TRIGGER_PUMP", 1);
                            double confidence = Math.Min(0.99, 0.85 + (pumpFreq * 0.02));

                            var insight = new PredictiveInsight(
                                targetPump.MacAddress,
                                targetPump.DeviceName,
                                "TRIGGER_PUMP",
                                "ON",
                                $"Moisture level at {sensor.LastReading:F1}% on {sensor.DeviceName} (below critical 20% limit). Historical pattern indicates immediate irrigation needed.",
                                confidence,
                                "CRITICAL",
                                $"Frequent Query & Override Match (Learned frequency count: {pumpFreq})"
                            );
                            insights.Add(insight);
                            _activeInsights[insight.InsightId] = insight;
                        }
                    }
                }

                // 2. Power Grid Surge Pattern Analysis: Power Spike & Relay Isolation
                var powerMeters = allDevices.Where(d => d.Category.Equals("Power", StringComparison.OrdinalIgnoreCase)).ToList();
                var relays = allDevices.Where(d => d.Category.Equals("Actuator", StringComparison.OrdinalIgnoreCase) && d.DeviceName.Contains("Relay", StringComparison.OrdinalIgnoreCase)).ToList();

                foreach (var meter in powerMeters)
                {
                    // Power surge spike (reading > 80.0 W or 3000 W)
                    if (meter.LastReading > 80.0 || meter.Status == "Critical")
                    {
                        var targetRelay = relays.FirstOrDefault(r => r.RelayState != "OPEN") ?? relays.FirstOrDefault();
                        if (targetRelay != null)
                        {
                            int relayFreq = _patternFrequencyMap.GetValueOrDefault("POWER_SPIKE->TOGGLE_RELAY", 1);
                            double confidence = Math.Min(0.98, 0.88 + (relayFreq * 0.02));

                            var insight = new PredictiveInsight(
                                targetRelay.MacAddress,
                                targetRelay.DeviceName,
                                "TOGGLE_RELAY",
                                "OPEN",
                                $"Power surge of {meter.LastReading:F1} W detected on {meter.DeviceName}. Automated action engine suggests opening relay to isolate substation.",
                                confidence,
                                "CRITICAL",
                                $"Overload Mitigation Rule (Learned frequency count: {relayFreq})"
                            );
                            insights.Add(insight);
                            _activeInsights[insight.InsightId] = insight;
                        }
                    }
                }

                // 3. Proactive Disconnect / Stale Node Insight
                var activeErrors = deviceService.GetActiveErrors().ToList();
                foreach (var error in activeErrors)
                {
                    if (error.Contains("DISCONNECT", StringComparison.OrdinalIgnoreCase))
                    {
                        var parts = error.Split(':');
                        string mac = parts[0];
                        var device = deviceService.GetDevice(mac);

                        var insight = new PredictiveInsight(
                            mac,
                            device?.DeviceName ?? mac,
                            "PING_AND_REROUTE",
                            "DIAGNOSTIC",
                            $"Node {device?.DeviceName ?? mac} has reported a disconnect state. Suggested action: Send mesh ping to re-route via adjacent node.",
                            0.90,
                            "HIGH",
                            "Mesh Self-Healing Protocol"
                        );
                        insights.Add(insight);
                        _activeInsights[insight.InsightId] = insight;
                    }
                }

                // 4. Default preventative maintenance insight if no critical emergency
                if (insights.Count == 0)
                {
                    var firstPump = pumps.FirstOrDefault();
                    if (firstPump != null)
                    {
                        insights.Add(new PredictiveInsight(
                            firstPump.MacAddress,
                            firstPump.DeviceName,
                            "CYCLE_TEST",
                            "VERIFY",
                            "All operational metrics nominal. Scheduled preventative micro-cycle test suggested for irrigation system.",
                            0.75,
                            "LOW",
                            "Preventative Maintenance Schedule"
                        ));
                    }
                }

                return insights;
            }
        }

        /// <summary>
        /// Executes a suggested action directly with one click and registers the command on the Undo stack.
        /// </summary>
        public DeviceCommand? ExecuteSuggestion(string insightId, IDeviceStateService deviceService)
        {
            lock (_engineLock)
            {
                if (!_activeInsights.TryGetValue(insightId, out var insight))
                {
                    return null;
                }

                var cmd = new DeviceCommand(
                    insight.TargetMac,
                    insight.TargetDeviceName,
                    insight.SuggestedAction,
                    insight.ActionParameter,
                    "PRE_SUGGESTION",
                    insight.ActionParameter,
                    "PredictiveEngine:AutomatedAction"
                );

                DeviceCommand executed = deviceService.ExecuteCommand(cmd);
                RecordCommandExecution(executed);

                _activeInsights.Remove(insightId);
                return executed;
            }
        }
    }
}
