using System;
using System.Linq;
using SmartX.API.Models;
using SmartX.API.Services;
using Xunit;

namespace SmartX.Tests
{
    public class DataStructureTests
    {
        [Fact]
        public void Dictionary_ProvidesInstantaneousO1LookupsByMacAddress()
        {
            // Arrange
            var service = new DeviceStateService();
            string mac = "AA:BB:CC:DD:EE:01";
            var device = new LiveDevice(mac, "NODE-TEST-01", "Transformer Monitor", "Power", "Sector-7 / Sub-A", 220.0, "V");

            // Act
            service.RegisterDevice(device);
            var retrieved = service.GetDevice(mac);

            // Assert
            Assert.NotNull(retrieved);
            Assert.Equal(mac, retrieved.MacAddress);
            Assert.Equal("Transformer Monitor", retrieved.DeviceName);
            Assert.Equal("Power", retrieved.Category);
        }

        [Fact]
        public void MessageQueue_StandardTelemetryEnqueuedAndProcessedFIFO()
        {
            // Arrange
            var service = new DeviceStateService();
            var packet1 = new TelemetryPacket<double>("00:1A:2C:3F:4E:56", "Sector-7", "Environmental", 25.0);
            var packet2 = new TelemetryPacket<double>("00:1A:2C:3F:4E:56", "Sector-7", "Environmental", 30.0);

            // Act
            var resp1 = service.EnqueueTelemetry(packet1);
            var resp2 = service.EnqueueTelemetry(packet2);
            var status = service.GetQueueStatus();

            // Assert
            Assert.Equal("GREEN_NORMAL", resp1.VisualState);
            Assert.Equal("GREEN_NORMAL", resp2.VisualState);
            Assert.True(status.TotalPacketsProcessed >= 2);
        }

        [Fact]
        public void PriorityQueue_CriticalPowerSpikeBypassesStandardQueue()
        {
            // Arrange
            var service = new DeviceStateService();
            var spikePacket = new TelemetryPacket<double>("12:34:56:78:9A:BC", "Substation-B", "Power", 4200.0);

            // Act
            var response = service.EnqueueTelemetry(spikePacket);
            var status = service.GetQueueStatus();

            // Assert
            Assert.Equal("RED_ALERT", response.VisualState);
            Assert.Contains("HIGH PRIORITY QUEUE BYPASS", response.Message);
            Assert.True(status.TotalPriorityBypasses > 0);
        }

        [Fact]
        public void PriorityQueue_CriticalMoistureDropBypassesStandardQueue()
        {
            // Arrange
            var service = new DeviceStateService();
            var moisturePlunge = new TelemetryPacket<double>("00:1A:2C:3F:4E:56", "Sector-7", "Hydroponics", 12.5);

            // Act
            var response = service.EnqueueTelemetry(moisturePlunge);
            var status = service.GetQueueStatus();

            // Assert
            Assert.Equal("RED_ALERT", response.VisualState);
            Assert.Contains("HIGH PRIORITY QUEUE BYPASS", response.Message);
            Assert.True(status.TotalPriorityBypasses > 0);
        }

        [Fact]
        public void Stack_CommandHistoryAndUndoRevertsLastIssuedCommand()
        {
            // Arrange
            var service = new DeviceStateService();
            string pumpMac = "00:1A:2C:3F:4E:57"; // Irrigation Water Pump Alpha
            var pumpDevice = service.GetDevice(pumpMac);
            Assert.NotNull(pumpDevice);
            string originalState = pumpDevice.PumpState; // Initially OFF

            // Step 1: Issue manual override command (Push to Stack)
            var cmd = new DeviceCommand(pumpMac, "Irrigation Water Pump Alpha", "TRIGGER_PUMP", "ON", originalState, "ON");
            service.ExecuteCommand(cmd);

            // Verify device updated to ON
            Assert.Equal("ON", pumpDevice.PumpState);
            Assert.True(service.GetCommandHistory().Count() >= 1);

            // Step 2: Call UndoLastCommand (Pop from Stack)
            var undone = service.UndoLastCommand();

            // Assert
            Assert.NotNull(undone);
            Assert.Equal(pumpMac, undone.MacAddress);
            Assert.Equal("OFF", pumpDevice.PumpState); // State successfully reverted!
            Assert.True(undone.IsUndone);
        }

        [Fact]
        public void SortedDictionary_OrdersHistoricalLogsChronologicallyByTimestamp()
        {
            // Arrange
            var service = new DeviceStateService();
            var t1 = DateTime.UtcNow.AddMinutes(-10);
            var t2 = DateTime.UtcNow.AddMinutes(-5);
            var t3 = DateTime.UtcNow;

            service.EnqueueTelemetry(new TelemetryPacket<double>("3C:71:BF:8A:22:90", "Tunnel", "Environmental", 22.0, t1));
            service.EnqueueTelemetry(new TelemetryPacket<double>("3C:71:BF:8A:22:90", "Tunnel", "Environmental", 23.0, t2));
            service.EnqueueTelemetry(new TelemetryPacket<double>("3C:71:BF:8A:22:90", "Tunnel", "Environmental", 24.0, t3));

            // Act
            var timeline = service.GetTimelineLogs().ToList();

            // Assert
            Assert.NotEmpty(timeline);
            for (int i = 0; i < timeline.Count - 1; i++)
            {
                // Descending for UI timeline display (latest first)
                Assert.True(timeline[i].Timestamp >= timeline[i + 1].Timestamp);
            }
        }

        [Fact]
        public void HashSet_PreventsDuplicateErrorStatesForSameSensor()
        {
            // Arrange
            var service = new DeviceStateService();
            string mac = "12:34:56:78:9A:BC";

            // Act: Inject two power spikes for the same sensor
            service.SimulateHighPriorityAlert(mac, "POWER_SPIKE", 4500.0, "Spike 1");
            service.SimulateHighPriorityAlert(mac, "POWER_SPIKE", 4700.0, "Spike 2");

            var activeErrors = service.GetActiveErrors().ToList();

            // Assert: HashSet should only contain 1 unique entry for the error state
            int occurrences = activeErrors.Count(e => e == $"{mac}:POWER_SPIKE");
            Assert.Equal(1, occurrences);
        }

        [Fact]
        public void PredictiveEngine_SuggestsPumpTriggerWhenMoisturePlunges()
        {
            // Arrange
            var deviceService = new DeviceStateService();
            var predictiveService = new PredictiveEngineService();

            // Plunge moisture on hydroponic sensor below 20%
            deviceService.EnqueueTelemetry(new TelemetryPacket<double>("00:1A:2C:3F:4E:56", "Greenhouse", "Hydroponics", 13.5));

            // Act
            var insights = predictiveService.GetCurrentInsights(deviceService).ToList();

            // Assert
            Assert.NotEmpty(insights);
            var hydroInsight = insights.FirstOrDefault(i => i.SuggestedAction == "TRIGGER_PUMP");
            Assert.NotNull(hydroInsight);
            Assert.Equal("CRITICAL", hydroInsight.Severity);
            Assert.True(hydroInsight.ConfidenceScore >= 0.85);

            // Execute suggestion directly
            var executed = predictiveService.ExecuteSuggestion(hydroInsight.InsightId, deviceService);
            Assert.NotNull(executed);
            Assert.Equal("TRIGGER_PUMP", executed.ActionType);
            Assert.Equal("ON", executed.Parameter);

            // Verify Undo stack captured the predictive execution
            var history = deviceService.GetCommandHistory().ToList();
            Assert.Contains(history, c => c.ActionType == "TRIGGER_PUMP");
        }
    }
}
