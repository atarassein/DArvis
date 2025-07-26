using System.Diagnostics;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using SideQuest.Tasks;
using CommunityToolkit.WinUI.Notifications;
using Newtonsoft.Json;
using SideQuest.Messages;

namespace SideQuest;

internal static class NativeMethods
{
    [DllImport("user32.dll")]
    public static extern bool SetForegroundWindow(IntPtr hWnd);
}

class Program
{
    private const string MutexName = "Global\\SideQuestSingleInstance";

    private const string AppId = "com.yourcompany.sidequest"; // choose any unique ID

    public static string AppUserModelId => AppId;
    
    [STAThread]
    static void Main(string[] args)
    {
        // Console.WriteLine("Running...");
        using var mutex = new Mutex(true, MutexName, out bool isNewInstance);
        if (!isNewInstance)
        {
            // Console.WriteLine("Another instance of the application is already running.");
            return;
        }

        var cancellationTokenSource = new CancellationTokenSource();

        Console.CancelKeyPress += (sender, eventArgs) =>
        {
            // Console.WriteLine("Termination requested...");
            eventArgs.Cancel = true;
            cancellationTokenSource.Cancel();
        };

        try
        {
            ToastNotificationManagerCompat.OnActivated += toastArgs =>
            {
                var parsedArgs = ToastArguments.Parse(toastArgs.Argument);
                var userInput = toastArgs.UserInput;

                if (parsedArgs.TryGetValue("type", out var type))
                {
                    switch (type)
                    {
                        case "mention":
                            string targetProc = parsedArgs["process"];
                            FocusProcess(targetProc);
                            break;
                        case "whisper":
                            string reply = userInput["replyBox"]?.ToString() ?? "";
                            string pipeName = parsedArgs["pipe"];
                            string playerName = parsedArgs["playerName"];
                            string replyTo = parsedArgs["replyTo"];
                            var responseMessage = new ToastMessage()
                            {
                                Type = "reply",
                                PlayerName = playerName,
                                ReplyTo = replyTo,
                                Content = reply,
                                ClientPipe = pipeName
                            };
                            SendReplyToClient(responseMessage);
                            break;
                    }
                }
            };

            ToastListener.ListenForMessagesAsync(cancellationTokenSource.Token).Wait();
        }
        catch (OperationCanceledException)
        {
            // Console.WriteLine("Application terminated.");
        }
    }
    
    private static void FocusProcess(string processName)
    {
        try
        {
            var proc = Process.GetProcessesByName(processName).FirstOrDefault();
            if (proc != null)
            {
                NativeMethods.SetForegroundWindow(proc.MainWindowHandle);
            }
        }
        catch (Exception ex)
        {
            // Console.WriteLine($"Failed to focus process: {ex.Message}");
        }
    }
    
    private static void SendReplyToClient(ToastMessage reply)
    {
        if (string.IsNullOrWhiteSpace(reply.ClientPipe))
            return;
        
        try
        {
            using var client = new NamedPipeClientStream(".", reply.ClientPipe, PipeDirection.Out);
            client.Connect(2000);
            using var writer = new StreamWriter(client);
            writer.AutoFlush = true;
            var json = JsonConvert.SerializeObject(reply);
            writer.WriteLine(json);
        }
        catch (Exception ex)
        {
            // Console.WriteLine($"Failed to send reply: {ex.Message}");
        }
    }
}