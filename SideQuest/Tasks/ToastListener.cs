using System.IO.Pipes;
using Microsoft.Toolkit.Uwp.Notifications;
using Newtonsoft.Json;
using SideQuest.Messages;

namespace SideQuest.Tasks;

public static class ToastListener
{
    private const string PipeName = "SideQuestPipe";
    private static CancellationTokenSource _cancellationTokenSource = new();

    public static Task ListenForMessagesAsync(CancellationToken cancellationToken)
    {
        return Task.Run(async () =>
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    using var server = new NamedPipeServerStream(PipeName, PipeDirection.In, NamedPipeServerStream.MaxAllowedServerInstances,
                        PipeTransmissionMode.Message, PipeOptions.Asynchronous);

                    await server.WaitForConnectionAsync(cancellationToken);

                    using var reader = new StreamReader(server);
                    string? json = await reader.ReadLineAsync();

                    if (!string.IsNullOrWhiteSpace(json))
                    {
                        var toast = JsonConvert.DeserializeObject<ToastMessage>(json);
                        if (toast != null)
                        {
                            switch (toast.Type)
                            {
                                case "mention":
                                    ShowMentionToast(toast);
                                    break;
                                case "whisper":
                                    ShowWhisperToast(toast);
                                    break;
                            }
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    // Graceful cancellation
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                }
            }
        }, cancellationToken);
    }

    public static void Stop()
    {
        _cancellationTokenSource.Cancel();
    }

    private static void ShowMentionToast(ToastMessage toast)
    {
        new ToastContentBuilder()
            .AddArgument("action", "focus")
            .AddArgument("process", toast.Process ?? "")
            .AddText("Mention")
            .AddText(toast.Content)
            .Show();
    }

    private static void ShowWhisperToast(ToastMessage toast)
    {
        new ToastContentBuilder()
            .AddArgument("action", "reply")
            .AddArgument("pipe", toast.ClientPipe ?? "")
            .AddText(toast.Title)
            .AddText(toast.Content)
            .AddInputTextBox("replyBox", placeHolderContent: "Type a reply...")
            .AddButton(new ToastButton("Send", $"action=sendReply&pipe={toast.ClientPipe ?? ""}&inputId=replyBox")
            {
                ActivationType = ToastActivationType.Background
            })
            .Show();
    }
}