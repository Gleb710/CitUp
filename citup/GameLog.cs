using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using CitUp.Sim;
using Godot;

namespace CitUp.Game;

/// <summary>
/// Журнал: каждая строка идёт в консоль Godot и в файл user://logs/citup-дата-режим-pid.log.
/// Режим (solo, host, client) и номер процесса в имени файла разводят два окна на одном компьютере.
/// </summary>
public static class GameLog
{
    private const int MaxBufferedLines = 500;

    private static readonly object Gate = new();
    private static readonly List<string> Buffer = new();   // строки до выбора режима
    private static StreamWriter? _writer;
    private static string _role = "menu";
    private static bool _handlersInstalled;

    /// <summary>Папка с журналами (на Windows: %APPDATA%/Godot/app_userdata/имя проекта/logs).</summary>
    public static string LogsFolder => ProjectSettings.GlobalizePath("user://logs");

    public static string? CurrentPath { get; private set; }

    /// <summary>Открывает файл журнала для режима. Тот же режим повторно: продолжаем писать в тот же файл.</summary>
    public static void Start(string role)
    {
        lock (Gate)
        {
            if (_writer is not null && _role == role)
                return;

            CloseWriter();
            _role = role;

            try
            {
                Directory.CreateDirectory(LogsFolder);
                string fileName = $"citup-{DateTime.Now:yyyyMMdd-HHmmss}-{role}-pid{OS.GetProcessId()}.log";
                CurrentPath = System.IO.Path.Combine(LogsFolder, fileName);
                _writer = new StreamWriter(CurrentPath, append: false) { AutoFlush = true };
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                _writer = null;
                CurrentPath = null;
                GD.PushWarning($"GameLog: не удалось открыть файл журнала: {ex.Message}");
            }

            WriteToFile($"=== CitUp, режим {role}, протокол {NetProtocol.Version}, ОС {OS.GetName()} ===");
            foreach (var line in Buffer)
                WriteToFile(line);
            Buffer.Clear();
        }
    }

    public static void Info(string text) => Write("INFO", text);

    public static void Warn(string text) => Write("WARN", text);

    public static void Error(string text) => Write("ERROR", text);

    /// <summary>Открывает папку с журналами в Проводнике.</summary>
    public static void OpenFolder()
    {
        try
        {
            Directory.CreateDirectory(LogsFolder);
            OS.ShellOpen(LogsFolder);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Warn($"не удалось открыть папку журналов: {ex.Message}");
        }
    }

    /// <summary>Записывает в журнал необработанные исключения других потоков. Вызывается один раз при старте.</summary>
    public static void InstallCrashHandlers()
    {
        if (_handlersInstalled)
            return;

        _handlersInstalled = true;
        AppDomain.CurrentDomain.UnhandledException += (_, e) => Error($"НЕОБРАБОТАННОЕ ИСКЛЮЧЕНИЕ: {e.ExceptionObject}");
        TaskScheduler.UnobservedTaskException += (_, e) => Error($"ИСКЛЮЧЕНИЕ В ЗАДАЧЕ: {e.Exception}");
    }

    public static void Shutdown()
    {
        Info("игра закрыта");
        lock (Gate)
        {
            CloseWriter();
        }
    }

    private static void Write(string level, string text)
    {
        string line = $"{DateTime.Now:HH:mm:ss.fff} [{level}] [{_role}] {text}";

        lock (Gate)
        {
            if (_writer is null && CurrentPath is null)
            {
                // Режим ещё не выбран: держим строки в памяти до Start()
                Buffer.Add(line);
                if (Buffer.Count > MaxBufferedLines)
                    Buffer.RemoveAt(0);
            }
            else
            {
                WriteToFile(line);
            }
        }

        switch (level)
        {
            case "ERROR":
                GD.PushError(line);
                break;
            case "WARN":
                GD.PushWarning(line);
                break;
            default:
                GD.Print(line);
                break;
        }
    }

    private static void WriteToFile(string line)
    {
        try
        {
            _writer?.WriteLine(line);
        }
        catch (IOException)
        {
            // Диск полон или файл недоступен: игре из-за журнала падать нельзя
        }
    }

    private static void CloseWriter()
    {
        _writer?.Dispose();
        _writer = null;
    }
}