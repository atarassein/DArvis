using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading;

namespace DArvis.Models;

public class AislingManager : INotifyPropertyChanged
{
    private class Aisling
    {
        public string Name;
        public int Serial;
        public int X;
        public int Y;
        public Direction Direction;
        public bool IsVisible;
        public bool IsHidden;
        public DateTime LastSeen = DateTime.UtcNow;
    }
    
    // use this class to track aisling entities seen, whether they are logged in or not, visible on the map or not, etc
    private readonly ConcurrentDictionary<string, Aisling> _aislings = new();
    private readonly ObservableCollection<AislingCheckboxViewModel> _aislingCheckboxes = new();
    
    public ObservableCollection<AislingCheckboxViewModel> AislingCheckboxes => _aislingCheckboxes;
    
    public event PropertyChangedEventHandler PropertyChanged;

    private readonly Timer _cleanupTimer;
    
    public AislingManager(Player Owner)
    {
        _cleanupTimer = new Timer(RemoveInactiveAislings, null, TimeSpan.Zero, TimeSpan.FromMinutes(1));
    }
    
    public void AddAisling(MapEntity? aislingEntity)
    {
        if (aislingEntity == null || aislingEntity.Serial == 0)
            return; // not an aisling

        var newAisling = new Aisling
        {
            Name = aislingEntity.Name,
            Serial = aislingEntity.Serial,
            X = aislingEntity.X,
            Y = aislingEntity.Y,
            Direction = aislingEntity.Direction,
            IsVisible = true,
            IsHidden = false, // TODO
            LastSeen = DateTime.UtcNow
        };
        
        if (newAisling.Name == "")
        {
            foreach (var aisling in _aislings.Values)
            {
                if (aisling.Name != "" || aisling.Serial != newAisling.Serial)
                    continue;
                
                if (aisling.LastSeen < DateTime.UtcNow - TimeSpan.FromHours(4))
                    continue; // TODO: configure memory time for aislings
                
                // we found a hidden aisling
                aisling.X = newAisling.X;
                aisling.Y = newAisling.Y;
                aisling.Direction = newAisling.Direction;
                aisling.IsHidden = true;
                aisling.IsVisible = true;
                // Console.WriteLine($"~{newAisling.Name} @ ({newAisling.X},{newAisling.Y})");
                return;
            }
        }

        if (newAisling.Name == "") return;
        // Console.WriteLine($" {newAisling.Name} @ {newAisling.Direction.ToString()[0]}({newAisling.X},{newAisling.Y})");
        _aislings.AddOrUpdate(newAisling.Name.ToLower(), newAisling, (k, v) => newAisling);
        UpdateAislingCheckboxes();
    }
    
    /// <summary>
    /// Returns false if the aisling was not found.
    /// </summary>
    /// <param name="aislingEntity"></param>
    /// <returns></returns>
    public bool UpdateAisling(MapEntity aislingEntity)
    {
        var serial = aislingEntity.Serial;
        foreach (var aisling in _aislings.Values)
        {
            if (aisling.Serial != serial) continue;
            
            aisling.X = aislingEntity.X;
            aisling.Y = aislingEntity.Y;
            aisling.Direction = aislingEntity.Direction;
            aisling.IsVisible = true;
            aisling.IsHidden = aislingEntity.Name == "";
            aisling.LastSeen = DateTime.UtcNow;
            // Console.WriteLine($" {aisling.Name} -> {aisling.Direction.ToString()[0]}({aisling.X},{aisling.Y})");
            UpdateAislingCheckboxes();
            return true;
        }

        return false;
    }

    /// <summary>
    /// We never really remove Aislings, we just consider them non-visible.
    /// </summary>
    /// <param name="serial"></param>
    public void HideAisling(int serial)
    {
        foreach (var aisling in _aislings.Values)
        {
            if (aisling.Serial != serial) continue;
            
            aisling.IsVisible = false;
            // Console.WriteLine($" {aisling.Name} left view");
            UpdateAislingCheckboxes();
            return;
        }
    }

    /// <summary>
    /// This is called upon map refresh. Any aislings which are visible will be re-added
    /// when AislingManager.AddAisling is called.
    /// </summary>
    public void HideEveryoneForRefresh()
    {
        foreach (var aisling in _aislings.Values)
        {
            aisling.IsHidden = true;
        }
        
        foreach (var aislingCheckbox in _aislingCheckboxes)
        {
            aislingCheckbox.IsVisible = false;
        }
    }
    
    private void UpdateAislingCheckboxes()
    {
        // Marshal to UI thread
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            var sortedAislings = _aislings.Values
                .OrderBy(a => _aislingCheckboxes.FirstOrDefault(x => x.Name == a.Name)?.IsChecked ?? false)
                .ThenByDescending(a => _aislingCheckboxes.FirstOrDefault(x => x.Name == a.Name)?.IsChecked == true ? a.Name : null)
                .ThenBy(a => a.LastSeen.ToString("yyyy-MM-dd HH:mm"))
                .Select(a => a.Name)
                .ToList();
    
            var currentNames = _aislingCheckboxes.Select(x => x.Name).ToHashSet();
    
            foreach (var aisling in _aislings.Values)
            {
                var checkbox = _aislingCheckboxes.FirstOrDefault(x => x.Name == aisling.Name);
                if (checkbox != null)
                {
                    checkbox.IsVisible = aisling.IsVisible; // Ensure IsVisible is updated
                }
            }
            
            // Add new aislings
            foreach (var name in sortedAislings.Except(currentNames))
            {
                _aislingCheckboxes.Add(new AislingCheckboxViewModel { Name = name, IsChecked = false, UpdateAction = UpdateAislingCheckboxes });
            }
    
            // Remove old aislings
            var toRemove = _aislingCheckboxes.Where(x => !sortedAislings.Contains(x.Name)).ToList();
            foreach (var item in toRemove)
            {
                _aislingCheckboxes.Remove(item);
            }
    
            // Reorder the collection
            var reordered = _aislingCheckboxes
                .OrderByDescending(x => sortedAislings.IndexOf(x.Name))
                .ToList();
    
            _aislingCheckboxes.Clear();
            foreach (var item in reordered)
            {
                _aislingCheckboxes.Add(item);
            }
        });
    }
    
    private void RemoveInactiveAislings(object state)
    {
        var cutoffTime = DateTime.UtcNow - TimeSpan.FromMinutes(10);

        // Marshal to UI thread
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            var toRemove = _aislings.Values
                .Where(a => a.LastSeen < cutoffTime && !_aislingCheckboxes.Any(c => c.Name == a.Name && c.IsChecked))
                .Select(a => a.Name.ToLower())
                .ToList();

            foreach (var name in toRemove)
            {
                _aislings.TryRemove(name, out _);
            }

            UpdateAislingCheckboxes();
        });
    }
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