using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using DArvis.IO;
using DArvis.Models;
using DArvis.Services.Logging;
using Newtonsoft.Json;

namespace DArvis.Services.SideQuest;

public class SideQuest : ISideQuest
{
    private readonly ILogger _logger;
    private Process? _sideQuestProcess;
    
    public bool IsRunning => _sideQuestProcess != null && !_sideQuestProcess.HasExited;
    public string ServiceName => "SideQuest";
    
    private const string PipeName = "SideQuestPipe";
    public const string ReplyPipeName = "SideQuestReplyPipe";
    private CancellationTokenSource? _replyListenerCts;
    
    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();
    
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

    public void ShowBackgroundToast(Player player, ToastMessage toast)
    {
        if (GetForegroundWindow() == player.Process.WindowHandle)
            return;

        try
        {
            using var client = new NamedPipeClientStream(".", PipeName, PipeDirection.Out);
            client.Connect(1000); // Avoid indefinite blocking

            using var writer = new StreamWriter(client) { AutoFlush = true };
            toast.PlayerName = player.Name;
            toast.ClientPipe = ReplyPipeName;
            string json = JsonConvert.SerializeObject(toast);
            writer.WriteLine(json);
        }
        catch (TimeoutException)
        {
            _logger.LogWarn("Toast agent is not running or not accepting pipe connections.");
        }
        catch (IOException ex)
        {
            _logger.LogWarn($"IO error sending toast: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Unexpected error sending toast: {ex.Message}");
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
                _logger.LogInfo("SideQuest is already running.");
                StartReplyListener();
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
                _logger.LogInfo($"Started SideQuest process with ID: {_sideQuestProcess?.Id}");
                StartReplyListener();
                _logger.LogInfo($"Reply listener started for SideQuest with pipe name: {ReplyPipeName}");
            }
            else
            {
                _logger.LogInfo("SideQuest.exe not found in application directory. Skipping SideQuest startup.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"Failed to start SideQuest: {ex.Message}");
        }
    }

    private void StartReplyListener()
    {
        _replyListenerCts = new CancellationTokenSource();

        Task.Run(async () =>
        {
            while (!_replyListenerCts.IsCancellationRequested)
            {
                try
                {
                    using var server = new NamedPipeServerStream(
                        ReplyPipeName,
                        PipeDirection.In,
                        1,
                        PipeTransmissionMode.Message,
                        PipeOptions.Asynchronous);

                    await server.WaitForConnectionAsync(_replyListenerCts.Token);

                    using var reader = new StreamReader(server);
                    string? json = await reader.ReadLineAsync();

                    if (!string.IsNullOrWhiteSpace(json))
                    {
                        var toast = JsonConvert.DeserializeObject<ToastMessage>(json);
                        var player = PlayerManager.Instance.GetPlayerByName(toast.PlayerName);
                        GameActions.Whisper(player, toast.ReplyTo, toast.Content);
                    }
                }
                catch (OperationCanceledException)
                {
                    // Graceful shutdown
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogWarn($"Error in SideQuest reply listener: {ex.Message}");
                }
            }
        }, _replyListenerCts.Token);
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

                        _logger.LogInfo($"Stopped additional SideQuest process with ID: {process.Id}");
                    }
                }
                catch (Exception ex)
                {
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
            _logger.LogError($"Error while stopping SideQuest: {ex.Message}");
        }
        finally
        {
            StopReplyListener();
        }
    }
    
    private void StopReplyListener()
    {
        try
        {
            _replyListenerCts?.Cancel();
            _replyListenerCts?.Dispose();
            _replyListenerCts = null;
        }
        catch (Exception ex)
        {
            _logger.LogWarn($"Failed to stop reply listener: {ex.Message}");
        }
    }
    
}