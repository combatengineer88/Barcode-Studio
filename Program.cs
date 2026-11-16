using System;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace BarcodeStudio;

internal static class Program
{
    internal static readonly string LogPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "BarcodeStudio", "logs", "startup.log");

    internal static void Log(string message)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(LogPath)!);
            File.AppendAllText(LogPath, $"{DateTime.Now:O} [PID {Environment.ProcessId}] {message}{Environment.NewLine}");
        }
        catch { /* Never prevent the app from opening because logging failed. */ }
    }

    [STAThread]
    private static void Main()
    {
        Log("Process entered Main.");
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        Application.ThreadException += (_, e) =>
        {
            Log("UI exception: " + e.Exception);
            MessageBox.Show(e.Exception.ToString(), "Barcode Studio error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        };
        AppDomain.CurrentDomain.UnhandledException += (_, e) => Log("Unhandled exception: " + e.ExceptionObject);
        TaskScheduler.UnobservedTaskException += (_, e) => { Log("Task exception: " + e.Exception); e.SetObserved(); };
        try
        {
            ApplicationConfiguration.Initialize();
            Log("WinForms initialized.");
            using var form = new MainForm();
            Log("MainForm constructed; starting message loop.");
            Application.Run(form);
            Log("Message loop exited.");
        }
        catch (Exception ex)
        {
            Log("Fatal startup exception: " + ex);
            MessageBox.Show($"Barcode Studio could not start.\n\n{ex}\n\nLog: {LogPath}",
                "Barcode Studio startup error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
