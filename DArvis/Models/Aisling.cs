using System;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Windows;

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
    
    private Point _point;
    private bool _isBuffTarget;
    private bool _isVisible;
    
    public ConcurrentDictionary<string, DateTime> BuffExpirationTimes { get; set; } = new();
    
    public Point Point {
        get => new(X, Y);
        private set {}
    }
    
    public bool IsBuffTarget
    {
        get => _isBuffTarget;
        set
        {
            _isBuffTarget = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsBuffTarget)));
            IsBuffTargetChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsBuffTarget)));
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
    
    public event PropertyChangedEventHandler? PropertyChanged;
    public event PropertyChangedEventHandler? IsBuffTargetChanged;
}