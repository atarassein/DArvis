using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using DArvis.Services.Logging;
using Newtonsoft.Json;

namespace DArvis.Services.SideQuest;

public class SideQuest : ISideQuest
{
    private readonly ILogger _logger;
    private Process? _sideQuestProcess;
    
    public bool IsRunning => _sideQuestProcess != null && !_sideQuestProcess.HasExited;
    public string ServiceName => "SideQuest";

    public SideQuest(ILogger logger)
    {
        _logger = logger;
    }

    public void Dispose()
    {
        Stop();
        _sideQuestProcess?.Dispose();
        _sideQuestProcess = null;
    }

    public void ShowToast(ToastMessage toast)
    {
        using (var client = new NamedPipeClientStream(".", "SideQuestPipe", PipeDirection.Out))
        {
            client.Connect();
            using (var writer = new StreamWriter(client) { AutoFlush = true })
            {
                string json = JsonConvert.SerializeObject(toast);
                writer.AutoFlush = true; // Ensure data is sent immediately
                writer.WriteLine(json);
            }
        }
    }

    public void Start()
    {
        try
        {
            // Check if SideQuest is already running
            var existingProcesses = Process.GetProcessesByName("SideQuest");
            if (existingProcesses.Length > 0)
            {
                Console.WriteLine("Side quest process already running");
                _logger.LogInfo("SideQuest is already running.");
                return;
            }

            // Get the path to the SideQuest executable in the same directory as DArvis
            var currentDirectory = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            if (currentDirectory == null) return;
            
            var sideQuestPath = Path.Combine(currentDirectory, "SideQuest", "SideQuest.exe");

            if (File.Exists(sideQuestPath))
            {
                _sideQuestProcess = Process.Start(new ProcessStartInfo
                {
                    FileName = sideQuestPath,
                    UseShellExecute = false,
                    CreateNoWindow = false
                });

                Console.WriteLine($"Started SideQuest process with ID: {_sideQuestProcess?.Id}");
                _logger.LogInfo($"Started SideQuest process with ID: {_sideQuestProcess?.Id}");
            }
            else
            {
                Console.WriteLine("SideQuest.exe not found in application directory. Skipping SideQuest startup.");
                _logger.LogInfo("SideQuest.exe not found in application directory. Skipping SideQuest startup.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to start SideQuest: {ex.Message}");
            _logger.LogError($"Failed to start SideQuest: {ex.Message}");
        }
    }

    public void Stop()
    {
        try
        {
            // Stop the process we started
            if (_sideQuestProcess != null && !_sideQuestProcess.HasExited)
            {
                _sideQuestProcess.CloseMainWindow();
                
                // Give it a moment to close gracefully
                if (!_sideQuestProcess.WaitForExit(5000))
                {
                    _sideQuestProcess.Kill();
                }
                
                _sideQuestProcess.Dispose();
                _sideQuestProcess = null;
                
                Console.WriteLine("Stopped SideQuest process.");
                _logger.LogInfo("Stopped SideQuest process.");
            }

            // Also check for any other SideQuest processes that might be running
            var sideQuestProcesses = Process.GetProcessesByName("SideQuest");
            foreach (var process in sideQuestProcesses)
            {
                try
                {
                    if (!process.HasExited)
                    {
                        process.CloseMainWindow();
                        
                        if (!process.WaitForExit(5000))
                        {
                            process.Kill();
                        }
                        Console.WriteLine($"Stopped additional SideQuest process with ID: {process.Id}");
                        _logger.LogInfo($"Stopped additional SideQuest process with ID: {process.Id}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to stop SideQuest process {process.Id}: {ex.Message}");
                    _logger.LogWarn($"Failed to stop SideQuest process {process.Id}: {ex.Message}");
                }
                finally
                {
                    process.Dispose();
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error while stopping SideQuest: {ex.Message}");
            _logger.LogError($"Error while stopping SideQuest: {ex.Message}");
        }
    }
}