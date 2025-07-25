using System;
using DArvis.Models;

namespace DArvis.Services.SideQuest;

public class ToastMessage
{
    public string Type { get; set; } = "";
    public string Title { get; set; } = "";
    public string Content { get; set; } = "";
    public string? Process { get; set; } = null;
    public string? ClientPipe { get; set; } = null;
}

public interface ISideQuest : IDisposable
{
    
    void ShowBackgroundToast(Player player, ToastMessage toast);
    
    /// <summary>
    /// Starts the SideQuest service.
    /// </summary>
    void Start();

    /// <summary>
    /// Stops the SideQuest service.
    /// </summary>
    void Stop();

    /// <summary>
    /// Checks if the SideQuest service is running.
    /// </summary>
    bool IsRunning { get; }

    /// <summary>
    /// Gets the name of the SideQuest service.
    /// </summary>
    string ServiceName { get; }
}