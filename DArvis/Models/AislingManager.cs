using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading;

namespace DArvis.Models;

public class AislingManager : INotifyPropertyChanged
{
    // Single collection for both internal tracking and UI binding
    private readonly ObservableCollection<Aisling> _aislings = new();
    private readonly object _aislingLock = new object();

    public ObservableCollection<Aisling> Aislings => _aislings;

    public event PropertyChangedEventHandler? PropertyChanged;

    private readonly Timer _cleanupTimer;

    public AislingManager(Player Owner)
    {
        _cleanupTimer = new Timer(RemoveInactiveAislings, null, TimeSpan.Zero, TimeSpan.FromMinutes(1));
    }

    public void AddAisling(MapEntity? aislingEntity)
    {
        if (aislingEntity == null || aislingEntity.Serial == 0)
            return; // not an aisling

        lock (_aislingLock)
        {
            // Marshal to UI thread for all collection operations
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
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
                    UpdateAction = UpdateAislings // Set the update action so checkbox changes trigger reordering
                };

                if (newAisling.Name == "")
                {
                    // Handle hidden aislings by serial lookup
                    var existingAisling = _aislings.FirstOrDefault(a => a.Serial == newAisling.Serial && a.Name != "");
                    if (existingAisling != null && existingAisling.LastSeen > DateTime.UtcNow - TimeSpan.FromHours(4))
                    {
                        var existingHidden = existingAisling.IsHidden;
                        // Update existing hidden aisling - position/direction changes don't affect sort order
                        existingAisling.X = newAisling.X;
                        existingAisling.Y = newAisling.Y;
                        existingAisling.Direction = newAisling.Direction;
                        existingAisling.IsHidden = true;
                        existingAisling.IsVisible = true;
                        existingAisling.LastSeen = DateTime.UtcNow;
                        if (!existingHidden)
                        {
                            UpdateAislings();
                        }
                        return;
                    }
                }

                if (newAisling.Name == "") return; // no point tracking nameless aislings

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
                    UpdateAislings();
                }
            });
        }
    }

    public bool UpdateAisling(MapEntity aislingEntity)
    {
        bool found = false;

        lock (_aislingLock)
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                var aisling = _aislings.FirstOrDefault(a => a.Serial == aislingEntity.Serial);
                if (aisling != null)
                {
                    // Position and direction changes don't affect sort order, no need to call UpdateAislings
                    var changed = false;
                    if (aislingEntity.Name == "" && aislingEntity.Name != aisling.Name)
                    {
                        changed = true;
                        aisling.IsHidden = true;
                    } else if (aisling.Name == "" && aislingEntity.Name != aisling.Name)
                    {
                        changed = true;
                        aisling.Name = aislingEntity.Name;
                        aisling.IsHidden = false;
                    }
                    aisling.X = aislingEntity.X;
                    aisling.Y = aislingEntity.Y;
                    aisling.Direction = aislingEntity.Direction;
                    aisling.IsVisible = true;
                    aisling.LastSeen = DateTime.UtcNow;
                    found = true;
                    
                    if (changed)
                    {
                        UpdateAislings();
                    }
                }
            });
        }

        return found;
    }

    public void HideAisling(int serial)
    {
        lock (_aislingLock)
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                var aisling = _aislings.FirstOrDefault(a => a.Serial == serial);
                if (aisling != null)
                {
                    aisling.IsVisible = false;
                    UpdateAislings();
                }
            });
        }
    }

    public void HideEveryoneForRefresh()
    {
        lock (_aislingLock)
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                foreach (var aisling in _aislings)
                {
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
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
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
        Console.WriteLine("Updating Aislings...");
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
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