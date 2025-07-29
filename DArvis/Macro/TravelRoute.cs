using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using DArvis.IO;
using DArvis.Models;

namespace DArvis.Macro;

public class TravelRoute(PlayerMacroState macro)
{

    private bool _isTraveling = false;

    public async Task<bool> ShouldTravel()
    {
        if (_isTraveling || macro.IsSpellCasting || macro.Client.IsWalking)
        {
            await Task.Delay(100);
            return false;
        }

        var travelDestination = macro.Client.TravelDestinationManager.CurrentDestination;

        if (travelDestination == null || travelDestination.Points.Count < 1 || travelDestination.Points[0].MapId != macro.Client.Location.MapNumber)
        {
            await Task.Delay(100);
            return false;
        }
        
        return true;
    }

    public async Task Travel()
    {
        _isTraveling = true;
        var destination = macro.Client.TravelDestinationManager.CurrentDestination;
        if (destination == null || destination.Points.Count < 1)
        {
            _isTraveling = false;
            return;
        }

        var points = destination.Points.ToArray();

        for (var i = 0; i < points.Length; i++)
        {
            if (points[i].MapId == macro.Client.Location.CurrentMap.Attributes.MapNumber)
            {
                // We are on the correct map, we need to travel to the specified point
                // We can walk directly to the next point
                var targetX = points[i].X;
                var targetY = points[i].Y;

                if (i + 1 < points.Length)
                {
                    var nextMap = points[i + 1].MapId;
                    // travel to targetX and targetY then wait for current map attributes mapNumber to equal nextMap
                    // we can "continue" when macro.Client.Location.CurrentMap.Attributes.MapNumber == nextMap
                    var target = new PathNode
                    {
                        Position = new Point(targetX, targetY),
                        Direction = points[i].Direction
                    };
                    var terrain = macro.Client.Location.CurrentMap.Terrain;
                    var path = PathFinder.FindPath(macro.Client.Location.PathNode, target, terrain, true);
                    // TODO: walk the path nodes
                }
                else
                {
                    // there is no next point, we can just walk to the targetX and targetY
                }
            }
            else
            {
                // We are lost, console log and abandon traveling
            }
        }

        _isTraveling = false;
    }
}