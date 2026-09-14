using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Win32;

namespace SmartX.Dashboard
{
    public partial class MainWindow : Window
    {
        private static readonly HttpClient HttpClient = new HttpClient
        {
            BaseAddress = new Uri("http://localhost:5000")
        };

        private string? _selectedFilePath;

        public MainWindow()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Handles OpenFileDialog event for selecting diagnostic log/photo files.
        /// </summary>
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

        /// <summary>
        /// Submits telemetry data and attached log file to backend Minimal API.
        /// </summary>
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
                // Step 1: Post Telemetry Packet to /api/telemetry
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
                    AppendConsoleLog($"[MESSAGE] {message}");

                    // Update Dynamic Engagement Heatmap UI Component
                    UpdateHeatmapVisualState(visualState, telemetryValue, macAddress);
                }
                else
                {
                    AppendConsoleLog($"[API ERROR] HTTP {(int)response.StatusCode}: {responseBody}");
                    UpdateHeatmapVisualState("RED_ALERT", telemetryValue, macAddress, $"API Call Failed: {(int)response.StatusCode}");
                }

                // Step 2: Upload Attached File if selected
                if (!string.IsNullOrEmpty(_selectedFilePath) && File.Exists(_selectedFilePath))
                {
                    await UploadAttachedLogFileAsync(macAddress, _selectedFilePath);
                }
            }
            catch (HttpRequestException httpEx)
            {
                AppendConsoleLog($"[CONNECTION ERROR] Could not connect to API at {HttpClient.BaseAddress}: {httpEx.Message}");
                MessageBox.Show($"Unable to connect to SmartX.API at {HttpClient.BaseAddress}.\nPlease ensure the API project is running.", 
                                "API Connection Failed", MessageBoxButton.OK, MessageBoxImage.Error);
                UpdateHeatmapVisualState("RED_ALERT", telemetryValue, macAddress, "Backend API Connection Refused");
            }
            catch (Exception ex)
            {
                AppendConsoleLog($"[ERROR] Unexpected exception: {ex.Message}");
                MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                SubmitButton.IsEnabled = true;
                SubmitButton.Content = "🚀 Submit Telemetry Data";
            }
        }

        /// <summary>
        /// Handles multipart file upload dispatch to /api/sensors/{mac}/upload-log
        /// </summary>
        private async Task UploadAttachedLogFileAsync(string macAddress, string filePath)
        {
            try
            {
                AppendConsoleLog($"[FILE UPLOAD] Preparing multipart upload for file: {Path.GetFileName(filePath)}");

                using var form = new MultipartFormDataContent();
                await using var fileStream = File.OpenRead(filePath);
                using var streamContent = new StreamContent(fileStream);

                string contentType = GetContentType(filePath);
                streamContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
                form.Add(streamContent, "file", Path.GetFileName(filePath));

                string requestUri = $"/api/sensors/{Uri.EscapeDataString(macAddress)}/upload-log";
                HttpResponseMessage response = await HttpClient.PostAsync(requestUri, form);
                string responseBody = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    AppendConsoleLog($"[FILE UPLOAD SUCCESS] Saved on Gateway API: {responseBody}");
                    // Reset file attachment after successful upload
                    _selectedFilePath = null;
                    AttachedFileTextBlock.Text = "No file attached";
                    AttachedFileTextBlock.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#94A3B8"));
                }
                else
                {
                    AppendConsoleLog($"[FILE UPLOAD FAILED] HTTP {(int)response.StatusCode}: {responseBody}");
                }
            }
            catch (Exception ex)
            {
                AppendConsoleLog($"[FILE UPLOAD ERROR] {ex.Message}");
            }
        }

        /// <summary>
        /// Dynamic Engagement Heatmap Component visual state updater.
        /// Transitions Fill background color between Green (#10B981) and Red (#EF4444).
        /// </summary>
        private void UpdateHeatmapVisualState(string visualState, double value, string macAddress, string? customDetail = null)
        {
            if (visualState == "RED_ALERT")
            {
                // Red Accent (#EF4444)
                Color redColor = (Color)ColorConverter.ConvertFromString("#EF4444");
                StatusHeatmapBorder.Background = new SolidColorBrush(redColor);
                StatusIconTextBlock.Text = "⚠";
                StatusHeaderTextBlock.Text = $"RED_ALERT (THRESHOLD EXCEEDED: {value:F2})";
                StatusDetailTextBlock.Text = customDetail ?? $"Node {macAddress} registered reading {value:F2} exceeding critical limit of 80.0.";
            }
            else
            {
                // Green Accent (#10B981)
                Color greenColor = (Color)ColorConverter.ConvertFromString("#10B981");
                StatusHeatmapBorder.Background = new SolidColorBrush(greenColor);
                StatusIconTextBlock.Text = "✓";
                StatusHeaderTextBlock.Text = $"GREEN_NORMAL (NOMINAL: {value:F2})";
                StatusDetailTextBlock.Text = customDetail ?? $"Node {macAddress} operating within safe boundaries. Value below 80.0 threshold.";
            }
        }

        private void AppendConsoleLog(string message)
        {
            string timestamp = DateTime.Now.ToString("HH:mm:ss");
            ConsoleLogTextBox.AppendText($"[{timestamp}] {message}\n");
            ConsoleLogTextBox.ScrollToEnd();
        }

        private void ClearConsole_Click(object sender, RoutedEventArgs e)
        {
            ConsoleLogTextBox.Text = $"[{DateTime.Now:HH:mm:ss}] Console cleared. Awaiting telemetry dispatch...\n";
        }

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            LoginWindow loginWindow = new LoginWindow();
            loginWindow.Show();
            this.Close();
        }

        private static string GetContentType(string fileName)
        {
            string ext = Path.GetExtension(fileName).ToLowerInvariant();
            return ext switch
            {
                ".txt" => "text/plain",
                ".log" => "text/plain",
                ".csv" => "text/csv",
                ".png" => "image/png",
                ".jpg" or ".jpeg" => "image/jpeg",
                _ => "application/octet-stream"
            };
        }
    }
}
