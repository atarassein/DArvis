namespace SideQuest.Messages;

public class ToastMessage
{
    public string Type { get; set; } = "";
    public string Title { get; set; } = "";
    public string Content { get; set; } = "";
    public string? Process { get; set; } // For "mention"
    public string? ClientPipe { get; set; } // For "whisper"
}