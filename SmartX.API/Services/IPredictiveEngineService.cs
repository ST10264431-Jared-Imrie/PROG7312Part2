using System.Collections.Generic;
using SmartX.API.Models;

namespace SmartX.API.Services
{
    public interface IPredictiveEngineService
    {
        void RecordUserSearch(string searchTerm, string category, string alertState);
        void RecordCommandExecution(DeviceCommand command);
        IEnumerable<PredictiveInsight> GetCurrentInsights(IDeviceStateService deviceService);
        DeviceCommand? ExecuteSuggestion(string insightId, IDeviceStateService deviceService);
    }
}
