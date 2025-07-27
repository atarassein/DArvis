namespace SideQuest.Messages;

public class ToastMessage
{
    public string Type { get; set; } = "";
    public string PlayerName { get; set; } = "";
    public string Title { get; set; } = "";
    public string Content { get; set; } = "";
    public string? Process { get; set; } // For "mention"
    public string? ClientPipe { get; set; } // For "whisper"
    
    public string? ReplyTo { get; set; } // For "reply" action

    public override string ToString()
    {
        return $"Type: {Type}, PlayerName: {PlayerName}, Title: {Title}, Content: {Content}, Process: {Process}, ClientPipe: {ClientPipe}, ReplyTo: {ReplyTo}";
    }
}