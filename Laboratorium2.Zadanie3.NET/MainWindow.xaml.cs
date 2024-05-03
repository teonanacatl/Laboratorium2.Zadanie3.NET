using System.Diagnostics;
using System.IO;
using System.Windows.Threading;
using System.Xml.Serialization;

namespace Laboratorium2.Zadanie3.NET;

public partial class MainWindow
{
    private PerformanceCounter? _cpuCounter;
    private PerformanceCounter? _ramCounter;
    private PerformanceCounter? _diskCounter;
    private PerformanceCounter? _threadCounter;
    private readonly DispatcherTimer _timer;
    private readonly Configuration _config;

    public MainWindow()
    {
        InitializeComponent();

        _config = LoadConfiguration();
        
        Console.WriteLine($"Loaded configuration parameters: CpuThreshold: {_config.CpuThreshold}, RamThreshold: {_config.RamThreshold}, DiskThreshold: {_config.DiskThreshold}, ThreadThreshold: {_config.ThreadThreshold}, LogFilePath: {_config.LogFilePath}, RefreshInterval: {_config.RefreshInterval}");

        InitializePerformanceCounters();

        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(_config.RefreshInterval ?? 1)
        };
        _timer.Tick += Timer_Tick;
        _timer.Start();
    }

    private void InitializePerformanceCounters()
    {
        _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
        _ramCounter = new PerformanceCounter("Memory", "Available MBytes");
        _diskCounter = new PerformanceCounter("PhysicalDisk", "% Disk Time", "_Total");
        _threadCounter = new PerformanceCounter("Process", "Thread Count", "_Total");
    }

    private void Timer_Tick(object? sender, EventArgs e)
    {
        UpdatePerformanceCounters();

        if (_cpuCounter?.NextValue() <= _config.CpuThreshold &&
            _ramCounter?.NextValue() >= _config.RamThreshold &&
            _diskCounter?.NextValue() <= _config.DiskThreshold &&
            _threadCounter?.NextValue() <= _config.ThreadThreshold) return;

        using var writer = new StreamWriter(_config.LogFilePath ?? "log.txt", true);
        var logMessage = $"{DateTime.Now}: {cpuLabel.Content}, {ramLabel.Content}, {diskLabel.Content}, {threadLabel.Content}";
        writer.WriteLine(logMessage);

        using var eventLog = new EventLog("Application");
        eventLog.Source = "My Application";
        eventLog.WriteEntry(logMessage, EventLogEntryType.Information);
    }

    private void UpdatePerformanceCounters()
    {
        cpuLabel.Content = $"CPU Usage: {_cpuCounter?.NextValue() ?? 0}%";
        ramLabel.Content = $"RAM Available: {_ramCounter?.NextValue() ?? 0}MB";
        diskLabel.Content = $"Disk Usage: {_diskCounter?.NextValue() ?? 0}%";
        threadLabel.Content = $"Thread Count: {_threadCounter?.NextValue() ?? 0}";
    }

    private static Configuration LoadConfiguration()
    {
        try
        {
            using var reader = new StreamReader("config.xml");
            var serializer = new XmlSerializer(typeof(Configuration));
            var config = (Configuration?)serializer.Deserialize(reader);
            if (config != null)
            {
                Console.WriteLine($"Loaded configuration: {config}");
                return config;
            }
        }
        catch (FileNotFoundException)
        {
            Console.WriteLine("Configuration file not found. Using default configuration.");
        }

        var defaultConfig = GetDefaultConfiguration();
        Console.WriteLine($"Loaded default configuration: {defaultConfig}");
        return defaultConfig;
    }
    
    private static Configuration GetDefaultConfiguration()
    {
        return new Configuration()
        {
            CpuThreshold = 10.0f,
            RamThreshold = 5000.0f,
            DiskThreshold = 10.0f,
            ThreadThreshold = 1000.0f,
            LogFilePath = "log.txt",
            RefreshInterval = 1
        };
    }
}

public class Configuration
{
    public float? CpuThreshold { get; init; }
    public float? RamThreshold { get; init; }
    public float? DiskThreshold { get; init; }
    public float? ThreadThreshold { get; init; }
    public string? LogFilePath { get; init; }
    public int? RefreshInterval { get; init;  }
}