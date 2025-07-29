using System;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Windows;

namespace DArvis.Models;

public class AislingManager : INotifyPropertyChanged
{
    // Single collection for both internal tracking and UI binding
    private readonly ObservableCollection<Aisling> _aislings = new();
    private readonly object _aislingLock = new object();
    public ObservableCollection<Aisling> Aislings => _aislings;
    public ConcurrentDictionary<int, Aisling> BuffTargets { get; } = new();
    
    private bool _buffAllVisibleAislings = false;
    public bool BuffAllVisibleAislings
    {
        get => _buffAllVisibleAislings;
        set
        {
            foreach (var aisling in _aislings.Where(a => a.IsBuffTarget != value))
            {
                if (value && !aisling.IsVisible)
                    continue;
                
                aisling.IsBuffTarget = value;
            }
            _buffAllVisibleAislings = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(BuffAllVisibleAislings)));
        }
    }
    public event PropertyChangedEventHandler? PropertyChanged;

    private readonly Timer _cleanupTimer;

    
    public AislingManager(Player Owner)
    {
        _cleanupTimer = new Timer(RemoveInactiveAislings, null, TimeSpan.Zero, TimeSpan.FromMinutes(1));
    }

    private void OnBuffTargetChanged(Aisling aisling, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(Aisling.IsBuffTarget))
            return;
        
        if (aisling.Name == "")
            return; // we can't really do anything with this

        if (aisling.IsBuffTarget && !BuffTargets.ContainsKey(aisling.Serial))
            BuffTargets.TryAdd(aisling.Serial, aisling);
        else if (!aisling.IsBuffTarget && BuffTargets.ContainsKey(aisling.Serial))
            BuffTargets.TryRemove(aisling.Serial, out _);
    }
    
    public void AddAisling(MapEntity? aislingEntity)
    {
        if (aislingEntity == null || aislingEntity.Serial == 0 || aislingEntity.Name == "")
            return; // not an aisling

        lock (_aislingLock)
        {
            // Marshal to UI thread for all collection operations
            Application.Current.Dispatcher.Invoke(() =>
            {
                var newAisling = new Aisling
                {
                    Name = aislingEntity.Name,
                    Serial = aislingEntity.Serial,
                    X = aislingEntity.X,
                    Y = aislingEntity.Y,
                    Direction = aislingEntity.Direction,
                    IsVisible = true,
                    IsHidden = false,
                    LastSeen = DateTime.UtcNow,
                };
                newAisling.IsBuffTargetChanged += (sender, e) => OnBuffTargetChanged((Aisling)sender!, e);
                
                if (BuffTargets.TryGetValue(newAisling.Serial, out _))
                    BuffTargets[newAisling.Serial] = newAisling;

                // Find existing aisling by serial
                var existing = _aislings.FirstOrDefault(a => a.Serial == newAisling.Serial);
                if (existing != null)
                {
                    // Update existing aisling properties - only update if visibility changes
                    var visibilityChanged = existing.IsVisible != newAisling.IsVisible;
                    existing.Name = newAisling.Name;
                    existing.X = newAisling.X;
                    existing.Y = newAisling.Y;
                    existing.Direction = newAisling.Direction;
                    existing.IsVisible = newAisling.IsVisible;
                    existing.IsHidden = newAisling.IsHidden;
                    existing.LastSeen = newAisling.LastSeen;
                    
                    if (BuffAllVisibleAislings)
                        existing.IsBuffTarget = true;
                    
                    // Only update sort if visibility changed
                    if (visibilityChanged)
                    {
                        UpdateAislings();
                    }
                }
                else
                {
                    // Add new aisling - this affects sort order
                    _aislings.Add(newAisling);
                    
                    if (BuffAllVisibleAislings)
                        newAisling.IsBuffTarget = true;
                    
                    UpdateAislings();
                }
            });
        }
    }

    public void UpdateAisling(MapEntity aislingEntity)
    {
        lock (_aislingLock)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                var aisling = _aislings.FirstOrDefault(a => a.Serial == aislingEntity.Serial);
                if (aisling == null)
                    return;
                
                // Update position coordinates first
                aisling.X = aislingEntity.X;
                aisling.Y = aislingEntity.Y;
                aisling.Direction = aislingEntity.Direction;
                
                // Position and direction changes don't affect sort order, no need to call UpdateAislings
                var changed = false;
                if (aislingEntity.Name == "" && !aisling.IsHidden)
                {
                    changed = true;
                    aisling.IsHidden = true;
                } else if (aislingEntity.Name != "" && aisling.IsHidden)
                {
                    changed = true;
                    aisling.IsHidden = false;
                }

                if (!aisling.IsVisible)
                {
                    changed = true;
                    aisling.IsVisible = true;
                }
                
                aisling.LastSeen = DateTime.UtcNow;
                   
                // Update BuffTargets to point to the same object reference
                if (aisling.IsBuffTarget)
                {
                    BuffTargets.AddOrUpdate(aisling.Serial, aisling, (key, existing) => aisling);
                }
                
                if (changed)
                {
                    UpdateAislings();
                }
            });
        }
    }

    public void RemoveAisling(int serial)
    {
        if (BuffTargets.TryGetValue(serial, out var buffTarget))
            buffTarget.IsVisible = false;
        
        lock (_aislingLock)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                var aisling = _aislings.FirstOrDefault(a => a.Serial == serial);
                if (aisling != null)
                {
                    aisling.IsVisible = false;
                    if (BuffAllVisibleAislings)
                        aisling.IsBuffTarget = false;
                    UpdateAislings();
                }
            });
        }
    }

    public void HideEveryoneForRefresh()
    {
        var buffTargetsSnapshot = BuffTargets.Values.ToList();
        foreach (var buffTarget in buffTargetsSnapshot)
        {
            buffTarget.IsVisible = false;
        }
        
        lock (_aislingLock)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                foreach (var aisling in _aislings)
                {
                    if (BuffAllVisibleAislings)
                        aisling.IsBuffTarget = false;
                    aisling.IsVisible = false;
                }

                UpdateAislings();
            });
        }
    }

    private void RemoveInactiveAislings(object? state)
    {
        var cutoffTime = DateTime.UtcNow - TimeSpan.FromMinutes(10);

        lock (_aislingLock)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                var toRemove = _aislings
                    .Where(a => !a.IsBuffTarget && a.LastSeen < cutoffTime)
                    .ToList();

                foreach (var aisling in toRemove)
                {
                    _aislings.Remove(aisling);
                }

                // Reorder after removing inactive aislings
                if (toRemove.Count > 0)
                {
                    UpdateAislings();
                }
            });
        }
    }

    public void UpdateAislings()
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            // Create a sorted list of aislings
            var sortedAislings = _aislings
                .OrderByDescending(a => a.IsBuffTarget) // 1. Checked aislings first
                .ThenBy(a => a.IsBuffTarget ? a.Name : (a.IsVisible ? $"1{a.Name}" : $"2{a.Name}")) // 2. For checked: alphabetical, for unchecked: visible then invisible, both alphabetical
                .ToList();

            // Clear and repopulate to trigger UI update with new order
            _aislings.Clear();
            foreach (var aisling in sortedAislings)
            {
                _aislings.Add(aisling);
            }
        });
    }
}