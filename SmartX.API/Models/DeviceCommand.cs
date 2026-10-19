using System;

namespace SmartX.API.Models
{
    /// <summary>
    /// Represents a manual override command issued to an IoT device.
    /// Managed via Stack<DeviceCommand> for instant LIFO "Undo" / reversal operations.
    /// </summary>
    public class DeviceCommand
    {
        public string CommandId { get; set; } = Guid.NewGuid().ToString("N");
        public string MacAddress { get; set; } = string.Empty;
        public string TargetDeviceName { get; set; } = string.Empty;
        public string ActionType { get; set; } = string.Empty; // e.g. "TRIGGER_PUMP", "TOGGLE_RELAY", "SHUTDOWN_VALVES", "SET_THRESHOLD"
        public string Parameter { get; set; } = string.Empty;  // e.g. "ON", "OFF", "LEVEL_3"
        public string PreviousState { get; set; } = string.Empty; // State before this command, needed for Undo
        public string ResultState { get; set; } = string.Empty;
        public DateTime IssuedAt { get; set; } = DateTime.UtcNow;
        public string Operator { get; set; } = "admin@smartx.io";
        public bool IsReversible { get; set; } = true;
        public bool IsUndone { get; set; } = false;

        public DeviceCommand()
        {
        }

        public DeviceCommand(string macAddress, string deviceName, string actionType, string parameter, string previousState, string resultState, string op = "admin@smartx.io")
        {
            MacAddress = macAddress;
            TargetDeviceName = deviceName;
            ActionType = actionType;
            Parameter = parameter;
            PreviousState = previousState;
            ResultState = resultState;
            Operator = op;
            IssuedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Computes the inverse command necessary to undo this action.
        /// </summary>
        public DeviceCommand GenerateInverseCommand()
        {
            string inverseAction = ActionType;
            string inverseParameter = PreviousState;

            if (ActionType == "TRIGGER_PUMP")
            {
                inverseParameter = (Parameter == "ON" || Parameter == "START") ? "OFF" : "ON";
            }
            else if (ActionType == "TOGGLE_RELAY")
            {
                inverseParameter = (Parameter == "CLOSED" || Parameter == "ON") ? "OPEN" : "CLOSED";
            }
            else if (ActionType == "SHUTDOWN_VALVES")
            {
                inverseAction = "REOPEN_VALVES";
                inverseParameter = "NORMAL";
            }
            else if (ActionType == "EMERGENCY_STOP")
            {
                inverseAction = "RESUME_OPERATION";
                inverseParameter = PreviousState;
            }

            return new DeviceCommand(MacAddress, TargetDeviceName, inverseAction, inverseParameter, ResultState, PreviousState, Operator)
            {
                IsUndone = true
            };
        }

        public override string ToString()
        {
            return $"[{IssuedAt:HH:mm:ss}] {TargetDeviceName} ({MacAddress}) -> {ActionType}:{Parameter} by {Operator}";
        }
    }
}
