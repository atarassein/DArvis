using System;
using System.Collections.Concurrent;
using System.ComponentModel;

namespace DArvis.Models;

public class Aisling : INotifyPropertyChanged
{
    public string Name { get; set; }
    public int Serial;
    public int X;
    public int Y;
    public Direction Direction;
    public bool IsHidden;
    public DateTime LastSeen = DateTime.UtcNow;
    
    private bool _isBuffTarget;
    private bool _isVisible;
    
    public ConcurrentDictionary<string, DateTime> BuffExpirationTimes { get; set; } = new();
    public bool IsBuffTarget
    {
        get => _isBuffTarget;
        set
        {
            _isBuffTarget = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsBuffTarget)));
            UpdateAction?.Invoke(); // Call UpdateAislingCheckboxes when IsBuffTarget changes
        }
    }
    
    public bool IsVisible
    {
        get => _isVisible;
        set
        {
            _isVisible = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsVisible)));
        }
    }
    
    public Action UpdateAction { get; set; }

    public event PropertyChangedEventHandler PropertyChanged;
}

public class AislingCheckboxViewModel : INotifyPropertyChanged
{
    private bool _isChecked;
    private bool _isVisible;

    public string Name { get; set; }

    public bool IsChecked
    {
        get => _isChecked;
        set
        {
            _isChecked = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsChecked)));
            UpdateAction?.Invoke(); // Call UpdateAislingCheckboxes when IsChecked changes
        }
    }

    public bool IsVisible
    {
        get => _isVisible;
        set
        {
            _isVisible = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsVisible)));
        }
    }
    
    public Action UpdateAction { get; set; }

    public event PropertyChangedEventHandler PropertyChanged;
}