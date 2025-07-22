using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace DArvis.Models;

public class AislingManager(Player Owner)
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
                if (aisling.Serial != newAisling.Serial)
                    continue;
                
                if (aisling.LastSeen < DateTime.UtcNow - TimeSpan.FromHours(4))
                    continue; // TODO: configure memory time for aislings
                
                // we found a hidden aisling
                aisling.X = newAisling.X;
                aisling.Y = newAisling.Y;
                aisling.Direction = newAisling.Direction;
                aisling.IsHidden = true;
                // Console.WriteLine($"~{newAisling.Name} @ ({newAisling.X},{newAisling.Y})");
                return;
            }
        }
        
        // Console.WriteLine($" {newAisling.Name} @ {newAisling.Direction.ToString()[0]}({newAisling.X},{newAisling.Y})");
        _aislings.AddOrUpdate(newAisling.Name.ToLower(), newAisling, (k, v) => newAisling);
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
            aisling.IsVisible = false;
            aisling.LastSeen = DateTime.UtcNow;
            // Console.WriteLine($" {aisling.Name} -> {aisling.Direction.ToString()[0]}({aisling.X},{aisling.Y})");
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
            return;
        }
    }
    
}