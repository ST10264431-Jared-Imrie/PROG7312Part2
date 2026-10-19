using System;
using System.IO;
using System.Windows;
using System.Windows.Threading;

namespace SmartX.Dashboard
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            AppDomain.CurrentDomain.UnhandledException += (s, args) =>
            {
                File.WriteAllText("crash_log.txt", $"[AppDomain Crash] {args.ExceptionObject}");
            };

            DispatcherUnhandledException += (s, args) =>
            {
                File.WriteAllText("crash_log.txt", $"[Dispatcher Crash] {args.Exception}");
                args.Handled = true;
                MessageBox.Show($"Application Error: {args.Exception.Message}\n{args.Exception.StackTrace}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            };
        }
    }
}
