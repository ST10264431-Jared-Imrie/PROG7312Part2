using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.Win32;
using SmartX.Dashboard.Models;

namespace SmartX.Dashboard
{
    public partial class MainWindow : Window
    {
        private static readonly HttpClient HttpClient = new HttpClient
        {
            BaseAddress = new Uri("http://localhost:5000"),
            Timeout = TimeSpan.FromSeconds(5)
        };

        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        // UI Observable Collections
        public ObservableCollection<LiveDeviceDto> DevicesList { get; } = new();
        public ObservableCollection<DeviceCommandDto> CommandHistoryList { get; } = new();
        public ObservableCollection<HistoricalLogDto> TimelineLogsList { get; } = new();

        // Active State
        private string? _selectedFilePath;
        private PredictiveInsightDto? _currentInsight;
        private readonly DispatcherTimer _autoRefreshTimer;

        public MainWindow()
        {
            InitializeComponent();

            // Bind UI collections
            DevicesItemsControl.ItemsSource = DevicesList;
            CommandHistoryListBox.ItemsSource = CommandHistoryList;
            TimelineListBox.ItemsSource = TimelineLogsList;

            // Setup auto-refresh polling timer (every 4 seconds for live data sync)
            _autoRefreshTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(4)
            };
            _autoRefreshTimer.Tick += async (s, e) =>
            {
                if (Pillar2CommandStreamGrid.Visibility == Visibility.Visible)
                {
                    await RefreshStreamAsync(silent: true);
                }
            };
            _autoRefreshTimer.Start();

            // Initial load
            Loaded += async (s, e) =>
            {
                await RefreshStreamAsync();
            };
        }

        #region 3-Pillar Navigation Logic (Preserves Application State)
        private void NavPillar1_Click(object sender, RoutedEventArgs e)
        {
            Pillar1IngestionGrid.Visibility = Visibility.Visible;
            Pillar2CommandStreamGrid.Visibility = Visibility.Collapsed;
            Pillar3TopologyGrid.Visibility = Visibility.Collapsed;

            NavPillar1Button.Style = (Style)FindResource("ActivePillarButtonStyle");
            NavPillar2Button.Style = (Style)FindResource("PillarNavButtonStyle");
            NavPillar3Button.Style = (Style)FindResource("PillarNavButtonStyle");
        }

        private async void NavPillar2_Click(object sender, RoutedEventArgs e)
        {
            Pillar1IngestionGrid.Visibility = Visibility.Collapsed;
            Pillar2CommandStreamGrid.Visibility = Visibility.Visible;
            Pillar3TopologyGrid.Visibility = Visibility.Collapsed;

            NavPillar1Button.Style = (Style)FindResource("PillarNavButtonStyle");
            NavPillar2Button.Style = (Style)FindResource("ActivePillarButtonStyle");
            NavPillar3Button.Style = (Style)FindResource("PillarNavButtonStyle");

            await RefreshStreamAsync();
        }

        private void NavPillar3_Click(object sender, RoutedEventArgs e)
        {
            Pillar1IngestionGrid.Visibility = Visibility.Collapsed;
            Pillar2CommandStreamGrid.Visibility = Visibility.Collapsed;
            Pillar3TopologyGrid.Visibility = Visibility.Visible;

            NavPillar1Button.Style = (Style)FindResource("PillarNavButtonStyle");
            NavPillar2Button.Style = (Style)FindResource("PillarNavButtonStyle");
            NavPillar3Button.Style = (Style)FindResource("ActivePillarButtonStyle");
        }

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            _autoRefreshTimer.Stop();
            var loginWindow = new LoginWindow();
            loginWindow.Show();
            Close();
        }
        #endregion

        #region Pillar 1: Telemetry Dispatch & Heatmap (Part 1 Preserved)
        private void AttachLogButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                OpenFileDialog openFileDialog = new OpenFileDialog
                {
                    Title = "Select Diagnostic Log or Sensor Image",
                    Filter = "All Supported Files (*.log;*.txt;*.png;*.jpg;*.jpeg;*.csv)|*.log;*.txt;*.png;*.jpg;*.jpeg;*.csv|Log Files (*.log;*.txt)|*.log;*.txt|Images (*.png;*.jpg;*.jpeg)|*.png;*.jpg;*.jpeg|All Files (*.*)|*.*",
                    Multiselect = false
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    _selectedFilePath = openFileDialog.FileName;
                    AttachedFileTextBlock.Text = Path.GetFileName(_selectedFilePath);
                    AttachedFileTextBlock.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#60A5FA"));
                    AppendConsoleLog($"[FILE ATTACHED] Selected file: {_selectedFilePath}");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error selecting file: {ex.Message}", "File Attachment Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void SubmitButton_Click(object sender, RoutedEventArgs e)
        {
            string macAddress = MacAddressTextBox.Text.Trim();
            string location = LocationTextBox.Text.Trim();
            string category = (CategoryComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Environmental";
            string rawValueText = TelemetryValueTextBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(macAddress))
            {
                MessageBox.Show("Please enter a valid MAC Address.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!double.TryParse(rawValueText, out double telemetryValue))
            {
                MessageBox.Show("Please enter a valid numeric Telemetry Value.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            SubmitButton.IsEnabled = false;
            SubmitButton.Content = "⏳ Transmitting Payload...";

            try
            {
                var packetPayload = new
                {
                    MacAddress = macAddress,
                    Location = location,
                    Category = category,
                    PayloadValue = telemetryValue,
                    Timestamp = DateTime.UtcNow
                };

                string jsonContent = JsonSerializer.Serialize(packetPayload);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                AppendConsoleLog($"[DISPATCH] Sending TelemetryPacket to /api/telemetry | MAC: {macAddress} | Val: {telemetryValue}");

                HttpResponseMessage response = await HttpClient.PostAsync("/api/telemetry", content);
                string responseBody = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    using JsonDocument doc = JsonDocument.Parse(responseBody);
                    JsonElement root = doc.RootElement;

                    string status = root.GetProperty("status").GetString() ?? "SUCCESS";
                    string visualState = root.GetProperty("visualState").GetString() ?? "GREEN_NORMAL";
                    string message = root.GetProperty("message").GetString() ?? string.Empty;

                    AppendConsoleLog($"[API RESPONSE] Status: {status} | VisualState: {visualState}");
                    AppendConsoleLog($"[QUEUE ROUTING] {message}");

                    UpdateHeatmapVisualState(visualState, telemetryValue, macAddress);
                }
                else
                {
                    AppendConsoleLog($"[API ERROR] HTTP {(int)response.StatusCode}: {responseBody}");
                    UpdateHeatmapVisualState("RED_ALERT", telemetryValue, macAddress, $"API Call Failed: {(int)response.StatusCode}");
                }

                if (!string.IsNullOrEmpty(_selectedFilePath) && File.Exists(_selectedFilePath))
                {
                    await UploadAttachedLogFileAsync(macAddress, _selectedFilePath);
                }
            }
            catch (HttpRequestException httpEx)
            {
                AppendConsoleLog($"[CONNECTION ERROR] Could not connect to API at {HttpClient.BaseAddress}: {httpEx.Message}");
                UpdateHeatmapVisualState("RED_ALERT", telemetryValue, macAddress, "Backend API Connection Refused");
            }
            catch (Exception ex)
            {
                AppendConsoleLog($"[ERROR] Unexpected exception: {ex.Message}");
            }
            finally
            {
                SubmitButton.IsEnabled = true;
                SubmitButton.Content = "🚀 Submit Telemetry Data";
            }
        }

        private async Task UploadAttachedLogFileAsync(string macAddress, string filePath)
        {
            try
            {
                string safeMac = Uri.EscapeDataString(macAddress);
                string uploadEndpoint = $"/api/sensors/{safeMac}/upload-log";

                using var formContent = new MultipartFormDataContent();
                byte[] fileBytes = await File.ReadAllBytesAsync(filePath);
                var fileContent = new ByteArrayContent(fileBytes);
                fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");

                string fileName = Path.GetFileName(filePath);
                formContent.Add(fileContent, "file", fileName);

                AppendConsoleLog($"[FILE UPLOAD] Uploading {fileName} ({fileBytes.Length} bytes) to {uploadEndpoint}");

                HttpResponseMessage uploadResponse = await HttpClient.PostAsync(uploadEndpoint, formContent);
                string responseBody = await uploadResponse.Content.ReadAsStringAsync();

                if (uploadResponse.IsSuccessStatusCode)
                {
                    AppendConsoleLog($"[FILE SUCCESS] Log uploaded and persisted on server. Details: {responseBody}");
                }
                else
                {
                    AppendConsoleLog($"[FILE ERROR] Server rejected upload with HTTP {(int)uploadResponse.StatusCode}: {responseBody}");
                }
            }
            catch (Exception ex)
            {
                AppendConsoleLog($"[FILE EXCEPTION] Could not upload file: {ex.Message}");
            }
        }

        private void UpdateHeatmapVisualState(string visualState, double value, string macAddress, string? customMessage = null)
        {
            if (visualState == "RED_ALERT")
            {
                StatusHeatmapBorder.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EF4444"));
                StatusIconTextBlock.Text = "⚠️";
                StatusHeaderTextBlock.Text = "RED_ALERT (CRITICAL THRESHOLD EXCEEDED)";
                StatusDetailTextBlock.Text = customMessage ?? $"Node {macAddress} reported value of {value:F2}, exceeding safe threshold limit!";
            }
            else
            {
                StatusHeatmapBorder.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#10B981"));
                StatusIconTextBlock.Text = "✓";
                StatusHeaderTextBlock.Text = "GREEN_NORMAL (NOMINAL OPERATIONAL RANGE)";
                StatusDetailTextBlock.Text = customMessage ?? $"Node {macAddress} nominal at {value:F2}. Normal boundary verified.";
            }
        }

        private void AppendConsoleLog(string message)
        {
            string timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
            ConsoleLogTextBox.AppendText($"[{timestamp}] {message}\n");
            ConsoleLogTextBox.ScrollToEnd();
        }

        private void ClearConsole_Click(object sender, RoutedEventArgs e)
        {
            ConsoleLogTextBox.Clear();
        }
        #endregion

        #region Pillar 2: Real-Time Command Stream, Data Structures & Predictive Engine
        /// <summary>
        /// Asynchronously fetches live devices, data structures metrics, timeline, and suggestions from API.
        /// </summary>
        private async Task RefreshStreamAsync(bool silent = false)
        {
            try
            {
                // 1. Fetch Live Registered Devices from Dictionary<string, LiveDevice> (O(1))
                string category = (CategoryFilterComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "All Categories";
                string status = (StatusFilterComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "All States";
                string search = DeviceSearchTextBox.Text.Trim();

                string devicesUrl = $"/api/devices?category={Uri.EscapeDataString(category)}&status={Uri.EscapeDataString(status)}&search={Uri.EscapeDataString(search)}";
                var devicesResponse = await HttpClient.GetAsync(devicesUrl);

                if (devicesResponse.IsSuccessStatusCode)
                {
                    string json = await devicesResponse.Content.ReadAsStringAsync();
                    var devices = JsonSerializer.Deserialize<List<LiveDeviceDto>>(json, JsonOptions);

                    DevicesList.Clear();
                    if (devices != null)
                    {
                        foreach (var d in devices) DevicesList.Add(d);
                    }
                    DeviceCountBadgeTextBlock.Text = $"Showing {DevicesList.Count} Devices";
                }

                // 2. Fetch Data Structures Metrics (Queue<T>, PriorityQueue, Stack<T>, HashSet<T>, Dictionary)
                var queueResponse = await HttpClient.GetAsync("/api/telemetry/queue-status");
                if (queueResponse.IsSuccessStatusCode)
                {
                    string json = await queueResponse.Content.ReadAsStringAsync();
                    var qStatus = JsonSerializer.Deserialize<QueueStatusDto>(json, JsonOptions);

                    if (qStatus != null)
                    {
                        MetricFifoQueueTextBlock.Text = $"{qStatus.StandardFifoQueueCount} pkts";
                        MetricPriorityQueueTextBlock.Text = $"{qStatus.TotalPriorityBypasses} bypasses";
                        MetricUndoStackTextBlock.Text = $"{qStatus.CommandUndoStackCount} cmds";
                        UndoStackDepthBadge.Text = $"Stack: {qStatus.CommandUndoStackCount}";
                        MetricActiveErrorsTextBlock.Text = $"{qStatus.ActiveErrorCount} unique";
                        MetricRegisteredDevicesTextBlock.Text = $"{qStatus.RegisteredDevicesCount} nodes";
                    }
                }

                // 3. Fetch Command History (Stack<DeviceCommand>)
                var historyResponse = await HttpClient.GetAsync("/api/commands/history");
                if (historyResponse.IsSuccessStatusCode)
                {
                    string json = await historyResponse.Content.ReadAsStringAsync();
                    var history = JsonSerializer.Deserialize<List<DeviceCommandDto>>(json, JsonOptions);

                    CommandHistoryList.Clear();
                    if (history != null)
                    {
                        foreach (var cmd in history) CommandHistoryList.Add(cmd);
                    }
                }

                // 4. Fetch Chronological Timeline Logs (SortedDictionary<DateTime, HistoricalLog>)
                var timelineResponse = await HttpClient.GetAsync("/api/telemetry/timeline?limit=25");
                if (timelineResponse.IsSuccessStatusCode)
                {
                    string json = await timelineResponse.Content.ReadAsStringAsync();
                    var timeline = JsonSerializer.Deserialize<List<HistoricalLogDto>>(json, JsonOptions);

                    TimelineLogsList.Clear();
                    if (timeline != null)
                    {
                        foreach (var log in timeline) TimelineLogsList.Add(log);
                    }
                }

                // 5. Fetch Predictive Insights & Automated Recommendations
                var insightsResponse = await HttpClient.GetAsync("/api/predictive/insights");
                if (insightsResponse.IsSuccessStatusCode)
                {
                    string json = await insightsResponse.Content.ReadAsStringAsync();
                    var insights = JsonSerializer.Deserialize<List<PredictiveInsightDto>>(json, JsonOptions);

                    if (insights != null && insights.Count > 0)
                    {
                        _currentInsight = insights[0];
                        InsightTitleTextBlock.Text = $"Pattern Match: {_currentInsight.Reason}";
                        InsightReasonTextBlock.Text = $"{_currentInsight.PatternCorrelation} | Target: {_currentInsight.TargetDeviceName}";
                        InsightConfidenceTextBlock.Text = $"Confidence: {_currentInsight.ConfidencePercentage}";
                        ExecuteInsightButton.IsEnabled = true;
                        ExecuteInsightButton.Content = $"⚡ Execute {_currentInsight.SuggestedAction}";
                    }
                    else
                    {
                        _currentInsight = null;
                        InsightTitleTextBlock.Text = "System operating optimally. Predictive engine monitoring historical patterns.";
                        InsightReasonTextBlock.Text = "No critical anomalies or frequent query triggers detected.";
                        InsightConfidenceTextBlock.Text = "Nominal State";
                        ExecuteInsightButton.IsEnabled = false;
                        ExecuteInsightButton.Content = "All Nominal";
                    }
                }

                // API Status Indicator
                ApiStatusIndicator.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#10B981"));
                ApiStatusTextBlock.Text = "API: Connected (http://localhost:5000)";
            }
            catch (Exception)
            {
                if (!silent)
                {
                    ApiStatusIndicator.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EF4444"));
                    ApiStatusTextBlock.Text = "API: Offline / Connection Failed";
                }
            }
        }

        private async void FilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IsLoaded)
            {
                await RefreshStreamAsync();
            }
        }

        private async void DeviceSearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!IsLoaded) return;

            string query = DeviceSearchTextBox.Text.Trim();
            await RefreshStreamAsync();

            // Record search query on API for pattern correlation learning
            if (query.Length >= 3)
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        var req = new { SearchTerm = query, Category = "All", AlertState = "All" };
                        var content = new StringContent(JsonSerializer.Serialize(req), Encoding.UTF8, "application/json");
                        await HttpClient.PostAsync("/api/predictive/record-search", content);
                    }
                    catch { }
                });
            }
        }

        private async void RefreshStreamButton_Click(object sender, RoutedEventArgs e)
        {
            await RefreshStreamAsync();
        }

        #region Manual Override Commands (Stack Implementation)
        private async void DeviceToggleRelay_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string mac)
            {
                // Find device in current list
                var device = DevicesList.FirstOrDefault(d => d.MacAddress.Equals(mac, StringComparison.OrdinalIgnoreCase));
                string newParam = (device?.RelayState == "CLOSED") ? "OPEN" : "CLOSED";

                await SendOverrideCommandAsync(mac, "TOGGLE_RELAY", newParam, $"Toggling Relay to {newParam}");
            }
        }

        private async void DeviceTogglePump_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string mac)
            {
                var device = DevicesList.FirstOrDefault(d => d.MacAddress.Equals(mac, StringComparison.OrdinalIgnoreCase));
                string newParam = (device?.PumpState == "ON") ? "OFF" : "ON";

                await SendOverrideCommandAsync(mac, "TRIGGER_PUMP", newParam, $"Triggering Pump to {newParam}");
            }
        }

        private async void DeviceEmergencyStop_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string mac)
            {
                await SendOverrideCommandAsync(mac, "EMERGENCY_SHUTDOWN", "ISOLATE", "Emergency Isolation Triggered");
            }
        }

        private async Task SendOverrideCommandAsync(string mac, string actionType, string param, string description)
        {
            try
            {
                var cmd = new
                {
                    MacAddress = mac,
                    ActionType = actionType,
                    Parameter = param,
                    Operator = "admin@smartx.io"
                };

                string json = JsonSerializer.Serialize(cmd);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await HttpClient.PostAsync("/api/commands", content);
                if (response.IsSuccessStatusCode)
                {
                    LastActionStatusTextBlock.Text = $"Command Sent: {actionType} -> {param} (Pushed to Undo Stack)";
                    await RefreshStreamAsync();
                }
                else
                {
                    MessageBox.Show("Could not dispatch override command to device.", "Command Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Command dispatch failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// "Undo" Feature Implementation: Pops the last command from Stack<DeviceCommand>
        /// and applies the reverse command to revert the device state.
        /// </summary>
        private async void UndoButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                UndoCommandButton.IsEnabled = false;

                var response = await HttpClient.PostAsync("/api/commands/undo", null);
                string responseBody = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    using JsonDocument doc = JsonDocument.Parse(responseBody);
                    string msg = doc.RootElement.GetProperty("message").GetString() ?? "Command successfully undone.";

                    LastActionStatusTextBlock.Text = $"↺ {msg}";
                    MessageBox.Show(msg, "Command Undone (Stack Pop)", MessageBoxButton.OK, MessageBoxImage.Information);

                    await RefreshStreamAsync();
                }
                else
                {
                    MessageBox.Show("No previous commands on the undo stack to revert.", "Undo Stack Empty", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Undo operation failed: {ex.Message}", "Undo Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                UndoCommandButton.IsEnabled = true;
            }
        }

        /// <summary>
        /// Simulates a Critical High-Priority Alert (Power Surge / Moisture Drop)
        /// that bypasses standard FIFO Queue via PriorityQueue<TelemetryAlert, int>.
        /// </summary>
        private async void SimulateAlertButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var req = new
                {
                    MacAddress = "12:34:56:78:9A:BC",
                    AlertType = "POWER_SPIKE",
                    Value = 4850.0,
                    Message = "SIMULATED CRITICAL POWER SURGE: 4850.0 W detected. Priority Queue bypass activated!"
                };

                string json = JsonSerializer.Serialize(req);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await HttpClient.PostAsync("/api/telemetry/simulate-alert", content);
                if (response.IsSuccessStatusCode)
                {
                    LastActionStatusTextBlock.Text = "🚨 Critical Power Surge Alert Injected via Priority Queue!";
                    MessageBox.Show("High-Priority Alert injected!\n\nNotice that this packet bypassed the standard FIFO Queue and was immediately priority-routed into the Priority Alert Queue and registered in the Unique Error HashSet.", "Priority Queue Test Successful", MessageBoxButton.OK, MessageBoxImage.Exclamation);

                    await RefreshStreamAsync();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Simulation failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Executes the suggested predictive action with one click, automatically pushing to the Undo stack.
        /// </summary>
        private async void ExecuteInsightButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentInsight == null) return;

            try
            {
                ExecuteInsightButton.IsEnabled = false;

                string url = $"/api/predictive/execute-suggestion/{_currentInsight.InsightId}";
                var response = await HttpClient.PostAsync(url, null);

                if (response.IsSuccessStatusCode)
                {
                    LastActionStatusTextBlock.Text = $"⚡ Executed Automated Insight: {_currentInsight.SuggestedAction} on {_currentInsight.TargetDeviceName}";
                    MessageBox.Show($"Executed Automated Recommendation:\n\n{_currentInsight.SuggestedAction} on {_currentInsight.TargetDeviceName}\n\nThis action was automatically pushed onto the Undo Stack and can be reverted at any time.", "Predictive Action Executed", MessageBoxButton.OK, MessageBoxImage.Information);

                    await RefreshStreamAsync();
                }
                else
                {
                    MessageBox.Show("Suggestion has expired or already been executed.", "Notice", MessageBoxButton.OK, MessageBoxImage.Information);
                    await RefreshStreamAsync();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to execute suggestion: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                ExecuteInsightButton.IsEnabled = true;
            }
        }
        #endregion
        #endregion
    }
}
