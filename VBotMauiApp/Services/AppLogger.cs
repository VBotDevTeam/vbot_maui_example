using System;
using System.Text;
using Microsoft.Maui.ApplicationModel;

namespace VBotMauiApp.Services;

public static class AppLogger
{
    private static readonly StringBuilder _logBuffer = new();
    private static readonly object _lock = new();

    public static event Action<string>? LogAdded;

    public static void Log(string tag, string message)
    {
        var timestamp = DateTime.Now.ToString("HH:mm:ss");
        var formatted = $"[{timestamp}] [{tag}] {message}";

        Console.WriteLine(formatted);

        lock (_lock)
        {
            _logBuffer.AppendLine(formatted);
            if (_logBuffer.Length > 20000)
            {
                var text = _logBuffer.ToString();
                var lines = text.Split('\n');
                if (lines.Length > 100)
                {
                    _logBuffer.Clear();
                    for (int i = lines.Length - 100; i < lines.Length; i++)
                    {
                        if (!string.IsNullOrEmpty(lines[i]))
                            _logBuffer.AppendLine(lines[i]);
                    }
                }
            }
        }

        try
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                LogAdded?.Invoke(formatted);
            });
        }
        catch
        {
            // Background thread fallback
        }
    }

    public static string GetAllLogs()
    {
        lock (_lock)
        {
            return _logBuffer.ToString();
        }
    }

    public static void ClearLogs()
    {
        lock (_lock)
        {
            _logBuffer.Clear();
        }
    }
}
