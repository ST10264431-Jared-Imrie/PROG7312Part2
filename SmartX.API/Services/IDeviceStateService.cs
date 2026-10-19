using System;
using System.Collections.Generic;
using SmartX.API.Models;

namespace SmartX.API.Services
{
    public interface IDeviceStateService
    {
        void RegisterDevice(LiveDevice device);
        LiveDevice? GetDevice(string macAddress);
        IEnumerable<LiveDevice> GetAllDevices(string? category = null, string? status = null, string? search = null);
        
        // Advanced Data Structure Operations
        TelemetryResponse EnqueueTelemetry(TelemetryPacket<double> packet, bool forcePriority = false);
        void SimulateHighPriorityAlert(string mac, string alertType, double value, string message);
        
        DeviceCommand ExecuteCommand(DeviceCommand command);
        DeviceCommand? UndoLastCommand();
        IEnumerable<DeviceCommand> GetCommandHistory();
        
        QueueStatusDto GetQueueStatus();
        IEnumerable<HistoricalLog> GetTimelineLogs(int limit = 100);
        IEnumerable<string> GetActiveErrors();
        bool ResolveError(string errorKey);
    }
}
