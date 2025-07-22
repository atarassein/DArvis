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
    }
    
    // use this class to track aisling entities seen, whether they are logged in or not, visible on the map or not, etc
    private readonly ConcurrentDictionary<string, Aisling> _aislings = new();
    public void AddAisling(MapEntity? aislingEntity)
    {
        if (aislingEntity == null || aislingEntity.Serial == 0)
            return; // not an aisling

        var aisling = new Aisling
        {
            Name = aislingEntity.Name,
            Serial = aislingEntity.Serial,
            X = aislingEntity.X,
            Y = aislingEntity.Y,
            Direction = aislingEntity.Direction,
            IsVisible = true,
            IsHidden = false // TODO
        };
        
        _aislings.AddOrUpdate(aisling.Name.ToLower(), aisling, (k, v) => aisling);
    }
    
    public void UpdateAisling(MapEntity aislingEntity)
    {
        if (_aislings.TryGetValue(aislingEntity.Name.ToLower(), out var aisling))
        {
            aisling.X = aislingEntity.X;
            aisling.Y = aislingEntity.Y;
            aisling.Direction = aislingEntity.Direction;
            aisling.IsVisible = true;
            aisling.IsHidden = false; // TODO: determine hidden state
        }
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
            aisling.IsHidden = false; // TODO: determine hidden state
            return;
        }
    }
    
}