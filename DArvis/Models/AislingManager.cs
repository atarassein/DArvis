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
                    // Update existing hidden aisling
                    existingAisling.X = newAisling.X;
                    existingAisling.Y = newAisling.Y;
                    existingAisling.Direction = newAisling.Direction;
                    existingAisling.IsHidden = true;
                    existingAisling.IsVisible = true;
                    existingAisling.LastSeen = DateTime.UtcNow;
                    UpdateAislings();
                    return;
                }
            }

            if (newAisling.Name == "") return;
            
            // Find existing aisling by serial
            var existing = _aislings.FirstOrDefault(a => a.Serial == newAisling.Serial);
            if (existing != null)
            {
                // Update existing aisling properties
                existing.Name = newAisling.Name;
                existing.X = newAisling.X;
                existing.Y = newAisling.Y;
                existing.Direction = newAisling.Direction;
                existing.IsVisible = newAisling.IsVisible;
                existing.IsHidden = newAisling.IsHidden;
                existing.LastSeen = newAisling.LastSeen;
            }
            else
            {
                // Add new aisling
                _aislings.Add(newAisling);
                UpdateAislings();
            }
        });
    }
    
    /// <summary>
    /// Returns false if the aisling was not found.
    /// </summary>
    /// <param name="aislingEntity"></param>
    /// <returns></returns>
    public bool UpdateAisling(MapEntity aislingEntity)
    {
        bool found = false;
        
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            var aisling = _aislings.FirstOrDefault(a => a.Serial == aislingEntity.Serial);
            if (aisling != null)
            {
                aisling.X = aislingEntity.X;
                aisling.Y = aislingEntity.Y;
                aisling.Direction = aislingEntity.Direction;
                aisling.IsVisible = true;
                aisling.IsHidden = aislingEntity.Name == "";
                aisling.LastSeen = DateTime.UtcNow;
                found = true;
                UpdateAislings();
            }
        });

        return found;
    }

    /// <summary>
    /// We never really remove Aislings, we just consider them non-visible.
    /// </summary>
    /// <param name="serial"></param>
    public void HideAisling(int serial)
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

    /// <summary>
    /// This is called upon map refresh. Any aislings which are visible will be re-added
    /// when AislingManager.AddAisling is called.
    /// </summary>
    public void HideEveryoneForRefresh()
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
    
    private void RemoveInactiveAislings(object? state)
    {
        var cutoffTime = DateTime.UtcNow - TimeSpan.FromMinutes(10);

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
    
    /// <summary>
    /// Updates and reorders the UI list of aislings
    /// </summary>
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
