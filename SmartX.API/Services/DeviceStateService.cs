using System;
using System.Collections.Generic;
using System.Linq;
using SmartX.API.Models;

namespace SmartX.API.Services
{
    /// <summary>
    /// Thread-safe singleton service implementing the 5 core Advanced Data Structures:
    /// 1. Queue&lt;T&gt; - Standard FIFO Message Queue for incoming telemetry packets
    /// 2. PriorityQueue&lt;TElement, TPriority&gt; - High-urgency alert bypass queue
    /// 3. Stack&lt;T&gt; - Manual override command history with Undo capability
    /// 4. Dictionary&lt;TKey, TValue&gt; - Live registered device registry for O(1) lookups
    /// 5. SortedDictionary&lt;TKey, TValue&gt; - Historical logs sorted sequentially by DateTime
    /// 6. HashSet&lt;T&gt; - Unique active error states &amp; disconnected nodes (prevents duplicate alerts)
    /// </summary>
    public class DeviceStateService : IDeviceStateService
    {
        private readonly object _syncLock = new();

        // 1. Message Queue: FIFO processing of standard telemetry packets
        private readonly Queue<TelemetryPacket<double>> _fifoTelemetryQueue = new();

        // 2. Priority Queue: Critical alerts bypass standard FIFO queue (Priority 1 = Highest)
        private readonly PriorityQueue<TelemetryAlert, int> _priorityAlertQueue = new();

        // 3. Stack: History of manual override commands for "Undo" functionality
        private readonly Stack<DeviceCommand> _commandUndoStack = new();
        private readonly List<DeviceCommand> _executedCommandsLog = new();

        // 4. Dictionary: Live registered devices for instantaneous O(1) lookup by MAC address
        private readonly Dictionary<string, LiveDevice> _registeredDevices = new(StringComparer.OrdinalIgnoreCase);

        // 5. Sorted Dictionary: Historical sensor logs organized sequentially by timestamp
        private readonly SortedDictionary<DateTime, HistoricalLog> _historicalTimeline = new();

        // 6. Hash Set: Unique active error states & disconnected nodes to eliminate duplicate alerts
        private readonly HashSet<string> _activeErrorStates = new(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _disconnectedNodes = new(StringComparer.OrdinalIgnoreCase);

        // Metrics tracking
        private long _totalPacketsProcessed = 0;
        private long _totalPriorityBypasses = 0;

        public DeviceStateService()
        {
            SeedInitialDevices();
        }

        /// <summary>
        /// Populates default IoT hardware nodes representing South African smart agricultural and power grid deployments.
        /// </summary>
        private void SeedInitialDevices()
        {
            var seedList = new List<LiveDevice>
            {
                new LiveDevice("00:1A:2C:3F:4E:56", "NODE-ESP32-HYDRO-01", "ESP32 Hydroponic Sensor A", "Hydroponics", "Sector-7 / Greenhouse-A", 45.2, "%")
                {
                    PumpState = "OFF",
                    RelayState = "CLOSED",
                    PowerDrawWatts = 3.2
                },
                new LiveDevice("00:1A:2C:3F:4E:57", "ACTUATOR-PUMP-ALPHA", "Irrigation Water Pump Alpha", "Actuator", "Sector-7 / Greenhouse-A", 0.0, "L/min")
                {
                    PumpState = "OFF",
                    RelayState = "CLOSED",
                    PowerDrawWatts = 450.0
                },
                new LiveDevice("12:34:56:78:9A:BC", "NODE-GRID-METER-02", "Substation Main Smart Meter", "Power", "Sector-7 / Substation-B", 240.5, "V")
                {
                    PumpState = "N/A",
                    RelayState = "CLOSED",
                    PowerDrawWatts = 3200.0
                },
                new LiveDevice("24:0A:C4:5B:3F:11", "ACTUATOR-RELAY-MAIN", "Substation High-Power Relay", "Actuator", "Sector-7 / Substation-B", 1.0, "State")
                {
                    RelayState = "CLOSED",
                    PumpState = "N/A",
                    PowerDrawWatts = 15.0
                },
                new LiveDevice("3C:71:BF:8A:22:90", "NODE-ENV-CLIMATE-03", "Tunnel Ambient Climate Node", "Environmental", "Sector-4 / Agro-Tunnel", 24.8, "°C")
                {
                    RelayState = "CLOSED",
                    PumpState = "OFF",
                    PowerDrawWatts = 2.1
                },
                new LiveDevice("48:E7:29:10:44:02", "ACTUATOR-VALVE-SOLENOID", "Hydroponic Nutrient Solenoid Valve", "Actuator", "Sector-7 / Greenhouse-A", 100.0, "% Open")
                {
                    ValveState = "NORMAL",
                    RelayState = "CLOSED",
                    PowerDrawWatts = 24.0
                }
            };

            foreach (var dev in seedList)
            {
                _registeredDevices[dev.MacAddress] = dev;
                
                // Add initial historical log in SortedDictionary
                var initialLog = new HistoricalLog(
                    dev.LastSeen.AddMinutes(-5),
                    dev.MacAddress,
                    dev.DeviceName,
                    dev.Category,
                    dev.LastReading,
                    dev.Unit,
                    "Normal",
                    "StandardFIFO"
                );
                _historicalTimeline[initialLog.Timestamp] = initialLog;
            }
        }

        #region Dictionary (O(1) Device Lookups)
        public void RegisterDevice(LiveDevice device)
        {
            if (device == null || string.IsNullOrWhiteSpace(device.MacAddress))
                throw new ArgumentException("Device and MAC address cannot be null.");

            lock (_syncLock)
            {
                _registeredDevices[device.MacAddress] = device;
            }
        }

        public LiveDevice? GetDevice(string macAddress)
        {
            if (string.IsNullOrWhiteSpace(macAddress)) return null;

            lock (_syncLock)
            {
                // Instantaneous O(1) hash lookup
                return _registeredDevices.TryGetValue(macAddress, out var dev) ? dev : null;
            }
        }

        public IEnumerable<LiveDevice> GetAllDevices(string? category = null, string? status = null, string? search = null)
        {
            lock (_syncLock)
            {
                var query = _registeredDevices.Values.AsEnumerable();

                if (!string.IsNullOrWhiteSpace(category) && !category.Equals("All", StringComparison.OrdinalIgnoreCase))
                {
                    query = query.Where(d => d.Category.Equals(category, StringComparison.OrdinalIgnoreCase));
                }

                if (!string.IsNullOrWhiteSpace(status) && !status.Equals("All", StringComparison.OrdinalIgnoreCase))
                {
                    query = query.Where(d => d.Status.Equals(status, StringComparison.OrdinalIgnoreCase));
                }

                if (!string.IsNullOrWhiteSpace(search))
                {
                    string s = search.Trim();
                    query = query.Where(d =>
                        d.MacAddress.Contains(s, StringComparison.OrdinalIgnoreCase) ||
                        d.DeviceName.Contains(s, StringComparison.OrdinalIgnoreCase) ||
                        d.Location.Contains(s, StringComparison.OrdinalIgnoreCase) ||
                        d.Category.Contains(s, StringComparison.OrdinalIgnoreCase));
                }

                return query.ToList();
            }
        }
        #endregion

        #region Telemetry Ingestion (FIFO Queue & Priority Queue Bypass)
        public TelemetryResponse EnqueueTelemetry(TelemetryPacket<double> packet, bool forcePriority = false)
        {
            if (packet == null)
                throw new ArgumentNullException(nameof(packet));

            lock (_syncLock)
            {
                // 1. Identify device via instantaneous O(1) Dictionary lookup
                if (!_registeredDevices.TryGetValue(packet.MacAddress, out var device))
                {
                    // Auto-register new node if not present
                    device = new LiveDevice(
                        packet.MacAddress,
                        $"NODE-{packet.MacAddress.Replace(":", "").ToUpper()}",
                        $"IoT Node {packet.MacAddress}",
                        string.IsNullOrWhiteSpace(packet.Category) ? "Environmental" : packet.Category,
                        string.IsNullOrWhiteSpace(packet.Location) ? "Sector-7 / Mesh-Gateway" : packet.Location,
                        packet.PayloadValue,
                        "units"
                    );
                    _registeredDevices[packet.MacAddress] = device;
                }

                // 2. Determine if packet is a Critical Alert requiring Priority Queue bypass:
                // Rule A: Value > 80.0 (Severe power spike or high temperature)
                // Rule B: Hydroponics moisture drop < 20.0%
                // Rule C: Explicit forcePriority flag
                bool isCriticalPowerSpike = (device.Category.Equals("Power", StringComparison.OrdinalIgnoreCase) || packet.Category.Equals("Power", StringComparison.OrdinalIgnoreCase)) && packet.PayloadValue > 80.0;
                bool isCriticalMoistureDrop = (device.Category.Equals("Hydroponics", StringComparison.OrdinalIgnoreCase) || packet.Category.Equals("Hydroponics", StringComparison.OrdinalIgnoreCase)) && packet.PayloadValue < 20.0;
                bool isSevereThreshold = packet.PayloadValue > 80.0;

                bool isHighPriority = forcePriority || isCriticalPowerSpike || isCriticalMoistureDrop || isSevereThreshold;

                string visualState = (packet.PayloadValue > 80.0 || isCriticalMoistureDrop) ? "RED_ALERT" : "GREEN_NORMAL";
                string queueTypeUsed = isHighPriority ? "PriorityAlertQueue" : "StandardFIFO";
                string alertKey = $"{packet.MacAddress}:{visualState}";

                if (isHighPriority)
                {
                    // PRIORITY QUEUE PATH: Bypasses standard FIFO queue!
                    _totalPriorityBypasses++;
                    int priorityRank = (isCriticalMoistureDrop || isCriticalPowerSpike) ? 1 : 2;

                    string alertReason = isCriticalMoistureDrop
                        ? $"CRITICAL MOISTURE DROP: Level plunged to {packet.PayloadValue:F1}% (<20.0% threshold) on hydroponics node {device.DeviceName}!"
                        : $"CRITICAL THRESHOLD SPIKE: Surge value {packet.PayloadValue:F2} exceeded safety limit on {device.DeviceName}!";

                    var alert = new TelemetryAlert(
                        packet.MacAddress,
                        device.DeviceName,
                        isCriticalMoistureDrop ? "MOISTURE_CRITICAL" : "POWER_SPIKE",
                        priorityRank,
                        packet.PayloadValue,
                        isCriticalMoistureDrop ? 20.0 : 80.0,
                        device.Unit,
                        alertReason
                    );

                    _priorityAlertQueue.Enqueue(alert, priorityRank);

                    // HASH SET: Add unique error state (prevents duplicate disconnect/alert records)
                    _activeErrorStates.Add(alertKey);

                    // Update live device state immediately
                    device.UpdateTelemetry(packet.PayloadValue, "RED_ALERT");
                }
                else
                {
                    // STANDARD FIFO QUEUE PATH
                    _fifoTelemetryQueue.Enqueue(packet);

                    // If previously in error, resolve
                    _activeErrorStates.Remove(alertKey);
                    _activeErrorStates.Remove($"{packet.MacAddress}:RED_ALERT");

                    device.UpdateTelemetry(packet.PayloadValue, "GREEN_NORMAL");
                }

                // 3. Process immediate batch to keep timeline updated
                _totalPacketsProcessed++;

                // Ensure unique DateTime key for SortedDictionary
                DateTime logTimestamp = DateTime.UtcNow;
                while (_historicalTimeline.ContainsKey(logTimestamp))
                {
                    logTimestamp = logTimestamp.AddTicks(10);
                }

                var historyEntry = new HistoricalLog(
                    logTimestamp,
                    packet.MacAddress,
                    device.DeviceName,
                    device.Category,
                    packet.PayloadValue,
                    device.Unit,
                    device.Status,
                    queueTypeUsed
                );
                _historicalTimeline[logTimestamp] = historyEntry;

                // Build response
                string responseMessage = isHighPriority
                    ? $"[HIGH PRIORITY QUEUE BYPASS] Critical packet priority-routed: {packet.PayloadValue:F2} on {device.DeviceName}. Alert registered."
                    : $"[STANDARD FIFO QUEUE] Packet queued and processed: {packet.PayloadValue:F2} on {device.DeviceName}.";

                return new TelemetryResponse
                {
                    Status = "SUCCESS",
                    VisualState = visualState,
                    MacAddress = packet.MacAddress,
                    Location = device.Location,
                    Category = device.Category,
                    Value = packet.PayloadValue,
                    ProcessedAt = logTimestamp,
                    Message = responseMessage
                };
            }
        }

        public void SimulateHighPriorityAlert(string mac, string alertType, double value, string message)
        {
            lock (_syncLock)
            {
                if (!_registeredDevices.TryGetValue(mac, out var device))
                {
                    device = new LiveDevice(mac, "NODE-SIM-01", "Simulated Critical Node", "Power", "Sector-7 / Substation-B", value, "W");
                    _registeredDevices[mac] = device;
                }

                _totalPriorityBypasses++;
                _totalPacketsProcessed++;

                var alert = new TelemetryAlert(mac, device.DeviceName, alertType, 1, value, 80.0, device.Unit, message);
                _priorityAlertQueue.Enqueue(alert, 1);

                // Add to HashSet
                string errorKey = $"{mac}:{alertType}";
                _activeErrorStates.Add(errorKey);

                // Update device status
                device.UpdateTelemetry(value, "RED_ALERT");

                // Record in SortedDictionary
                DateTime now = DateTime.UtcNow;
                while (_historicalTimeline.ContainsKey(now))
                {
                    now = now.AddTicks(10);
                }

                _historicalTimeline[now] = new HistoricalLog(
                    now, mac, device.DeviceName, device.Category, value, device.Unit, "Critical", "PriorityAlertQueue"
                );
            }
        }
        #endregion

        #region Stack (Manual Override Commands & Undo Feature)
        public DeviceCommand ExecuteCommand(DeviceCommand command)
        {
            if (command == null) throw new ArgumentNullException(nameof(command));

            lock (_syncLock)
            {
                if (_registeredDevices.TryGetValue(command.MacAddress, out var device))
                {
                    command.TargetDeviceName = device.DeviceName;

                    // Capture previous state for precise reversal
                    switch (command.ActionType.ToUpperInvariant())
                    {
                        case "TRIGGER_PUMP":
                            command.PreviousState = device.PumpState;
                            device.PumpState = command.Parameter.ToUpperInvariant();
                            command.ResultState = device.PumpState;
                            break;

                        case "TOGGLE_RELAY":
                            command.PreviousState = device.RelayState;
                            device.RelayState = command.Parameter.ToUpperInvariant();
                            command.ResultState = device.RelayState;
                            break;

                        case "SHUTDOWN_VALVES":
                        case "SET_VALVE":
                            command.PreviousState = device.ValveState;
                            device.ValveState = command.Parameter.ToUpperInvariant();
                            command.ResultState = device.ValveState;
                            break;

                        case "EMERGENCY_SHUTDOWN":
                            command.PreviousState = $"Relay:{device.RelayState}|Pump:{device.PumpState}";
                            device.RelayState = "OPEN";
                            device.PumpState = "OFF";
                            device.ValveState = "SHUTDOWN";
                            command.ResultState = "ALL_SYSTEMS_ISOLATED";
                            break;

                        default:
                            command.PreviousState = "UNKNOWN";
                            command.ResultState = command.Parameter;
                            break;
                    }

                    device.LastSeen = DateTime.UtcNow;
                }

                // PUSH to Stack<DeviceCommand> for Undo capability
                _commandUndoStack.Push(command);
                _executedCommandsLog.Add(command);

                // Log to SortedDictionary timeline
                DateTime now = DateTime.UtcNow;
                while (_historicalTimeline.ContainsKey(now)) now = now.AddTicks(10);

                _historicalTimeline[now] = new HistoricalLog(
                    now,
                    command.MacAddress,
                    command.TargetDeviceName,
                    "CommandOverride",
                    1.0,
                    "CMD",
                    $"{command.ActionType}->{command.Parameter}",
                    "ManualOverrideStack"
                );

                return command;
            }
        }

        public DeviceCommand? UndoLastCommand()
        {
            lock (_syncLock)
            {
                if (_commandUndoStack.Count == 0)
                {
                    return null; // Stack is empty, nothing to undo
                }

                // POP the last issued command from the LIFO Stack
                DeviceCommand lastCommand = _commandUndoStack.Pop();

                // Compute and apply the inverse command
                DeviceCommand inverse = lastCommand.GenerateInverseCommand();

                if (_registeredDevices.TryGetValue(inverse.MacAddress, out var device))
                {
                    switch (lastCommand.ActionType.ToUpperInvariant())
                    {
                        case "TRIGGER_PUMP":
                            device.PumpState = inverse.Parameter.ToUpperInvariant();
                            break;

                        case "TOGGLE_RELAY":
                            device.RelayState = inverse.Parameter.ToUpperInvariant();
                            break;

                        case "SHUTDOWN_VALVES":
                        case "REOPEN_VALVES":
                        case "SET_VALVE":
                            device.ValveState = inverse.Parameter.ToUpperInvariant();
                            break;

                        case "EMERGENCY_SHUTDOWN":
                            // Restore components
                            device.RelayState = "CLOSED";
                            device.PumpState = "OFF";
                            device.ValveState = "NORMAL";
                            break;
                    }
                    device.LastSeen = DateTime.UtcNow;
                }

                _executedCommandsLog.Add(inverse);

                // Log reversal in SortedDictionary
                DateTime now = DateTime.UtcNow;
                while (_historicalTimeline.ContainsKey(now)) now = now.AddTicks(10);

                _historicalTimeline[now] = new HistoricalLog(
                    now,
                    inverse.MacAddress,
                    device?.DeviceName ?? inverse.TargetDeviceName,
                    "CommandUndo",
                    0.0,
                    "UNDO",
                    $"REVERTED: {lastCommand.ActionType}:{lastCommand.Parameter} -> {inverse.Parameter}",
                    "StackUndoReversal"
                );

                return inverse;
            }
        }

        public IEnumerable<DeviceCommand> GetCommandHistory()
        {
            lock (_syncLock)
            {
                // Return copy of stack entries in LIFO order
                return _commandUndoStack.ToList();
            }
        }
        #endregion

        #region Hash Sets (Unique Errors & Disconnected Nodes)
        public IEnumerable<string> GetActiveErrors()
        {
            lock (_syncLock)
            {
                return _activeErrorStates.ToList();
            }
        }

        public bool ResolveError(string errorKey)
        {
            lock (_syncLock)
            {
                return _activeErrorStates.Remove(errorKey);
            }
        }
        #endregion

        #region Sorted Dictionary (Timeline Logs)
        public IEnumerable<HistoricalLog> GetTimelineLogs(int limit = 100)
        {
            lock (_syncLock)
            {
                // SortedDictionary orders naturally by DateTime ascending
                return _historicalTimeline.Values.TakeLast(limit).Reverse().ToList();
            }
        }
        #endregion

        #region Queue & Data Structures Status Metric
        public QueueStatusDto GetQueueStatus()
        {
            lock (_syncLock)
            {
                return new QueueStatusDto
                {
                    StandardFifoQueueCount = _fifoTelemetryQueue.Count,
                    PriorityAlertQueueCount = _priorityAlertQueue.Count,
                    CommandUndoStackCount = _commandUndoStack.Count,
                    RegisteredDevicesCount = _registeredDevices.Count,
                    HistoricalTimelineCount = _historicalTimeline.Count,
                    ActiveErrorCount = _activeErrorStates.Count,
                    ActiveErrorList = _activeErrorStates.ToList(),
                    RecentCommands = _commandUndoStack.Take(5).Select(c => c.ToString()).ToList(),
                    TotalPacketsProcessed = _totalPacketsProcessed,
                    TotalPriorityBypasses = _totalPriorityBypasses,
                    LastProcessedTimestamp = DateTime.UtcNow
                };
            }
        }
        #endregion
    }
}
