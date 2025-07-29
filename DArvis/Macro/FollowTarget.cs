using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using DArvis.IO;
using DArvis.IO.Process;
using DArvis.Models;

namespace DArvis.Macro;

public class FollowTarget(PlayerMacroState macro)
{
    private CancellationTokenSource? _walkCancellationSource;
    private PathNode[]? _currentPath;
    private int _currentPathIndex;
    private readonly object _walkLock = new object();
    private TaskCompletionSource<bool>? _moveCompletionSource;

    // Cache the last known position to avoid redundant UI updates
    private Point _lastKnownPosition = new Point(-1, -1);

    private PathNode NodeBehindPlayer(Player target)
    {
        var pathNode = target.Location.PathNode;
        var x = pathNode.Position.X;
        var y = pathNode.Position.Y;
        var direction = target.Location.Direction;
        switch (direction)
        {
            case Direction.North:
                y++;
                break;
            case Direction.South:
                y--;
                break;
            case Direction.East:
                x--;
                break;
            case Direction.West:
                x++;
                break;
        }

        return new PathNode
        {
            Position = new Point(x, y),
            Direction = Direction.North
        };
    }

    public async Task<bool> ShouldWalk()
    {
        var player = macro.Client;
        var leader = player.Leader;
        
        if (macro.IsSpellCasting || player.IsWalking)
        {
            await Task.Delay(100);
            return false;
        }

        // Check if following a leader
        if (leader != null && !player.IsNearby(leader))
        {
            var playerMap = player.Location.MapNumber;
            var leaderMap = leader.Location.MapNumber;

            if (playerMap == leaderMap)
            {
                return true;
            }

            if (leader.Breadcrumbs.TryGetValue(playerMap, out var breadcrumb) && breadcrumb != null)
            {
                return true;
            }

            Console.WriteLine("Player is lost.");
            await Task.Delay(100);
            return false;
        }

        // Check if following a travel route
        var travelDestination = player.TravelDestinationManager.CurrentDestination;
        if (travelDestination != null && travelDestination.Name != "<None>" && travelDestination.Points.Count > 0)
        {
            var playerMap = player.Location.MapNumber;
            
            // Find the current waypoint for this map
            var currentWaypoint = travelDestination.Points.FirstOrDefault(p => p.MapId == playerMap);
            if (currentWaypoint != null)
            {
                // Check if we're already at the waypoint
                if ((int)player.Location.X == currentWaypoint.X && (int)player.Location.Y == currentWaypoint.Y)
                {
                    // At waypoint, check if there's a next waypoint on a different map
                    var currentIndex = travelDestination.Points.IndexOf(currentWaypoint);
                    if (currentIndex >= 0 && currentIndex < travelDestination.Points.Count - 1)
                    {
                        var nextWaypoint = travelDestination.Points[currentIndex + 1];
                        if (nextWaypoint.MapId != playerMap)
                        {
                            // Next waypoint is on different map, wait for transition
                            await Task.Delay(100);
                            return false;
                        }
                    }
                    // Last waypoint reached or next waypoint on same map
                    await Task.Delay(100);
                    return false;
                }
                return true; // Need to walk to waypoint
            }
        }

        await Task.Delay(100);
        return false;
    }

    public async Task Walk()
    {
        var player = macro.Client;
        if (player.IsWalking) return;

        lock (_walkLock)
        {
            _walkCancellationSource?.Cancel();
            _walkCancellationSource = new CancellationTokenSource();

            player.IsWalking = true;

            // Use throttled position monitoring instead of every property change
            Task.Run(() => ExecuteWalk(_walkCancellationSource.Token));
        }
    }

    private async Task ExecuteWalk(CancellationToken cancellationToken)
    {
        var player = macro.Client;
        var leader = player.Leader;

        if (player.IsDisposing)
        {
            StopWalking();
            return;
        }

        try
        {
            var playerMap = player.Location.MapNumber;
            PathNode? targetNode = null;
            bool isBreadcrumb = false;
            
            // Check if following a leader
            if (leader != null && !leader.IsDisposing)
            {
                if (player.IsOnSameMapAs(leader))
                {
                    targetNode = NodeBehindPlayer(leader);
                }
                else if (leader.Breadcrumbs.TryGetValue(playerMap, out var breadcrumbNode) && breadcrumbNode != null)
                {
                    isBreadcrumb = true;
                    targetNode = breadcrumbNode;
                }

                if (isBreadcrumb && targetNode != null)
                {
                    _currentPath = PathFinder.FindPath(
                        player.Location.PathNode,
                        targetNode,
                        player.Location.CurrentMap.Terrain,
                        isBreadcrumb);

                    if (_currentPath == null || _currentPath.Length == 0)
                    {
                        if (leader.LastPosition.TryGetValue(playerMap, out var lastPosition) && lastPosition != null)
                        {
                            targetNode = lastPosition;
                            _currentPath = PathFinder.FindPath(
                                player.Location.PathNode,
                                targetNode,
                                player.Location.CurrentMap.Terrain, 
                                isBreadcrumb);
                        }
                    }
                }
            }
            // Check if following a travel route
            else
            {
                var travelDestination = player.TravelDestinationManager.CurrentDestination;
                if (travelDestination != null && travelDestination.Points.Count > 0)
                {
                    var currentWaypoint = travelDestination.Points.FirstOrDefault(p => p.MapId == playerMap);
                    if (currentWaypoint != null)
                    {
                        isBreadcrumb = true; // Treat route waypoints like breadcrumbs
                        targetNode = new PathNode
                        {
                            Position = new Point(currentWaypoint.X, currentWaypoint.Y),
                            Direction = currentWaypoint.Direction
                        };
                    }
                }
            }

            if (targetNode == null)
            {
                StopWalking();
                return;
            }

            // Find path if not already calculated for leader breadcrumb
            if (_currentPath == null)
            {
                _currentPath = PathFinder.FindPath(
                    player.Location.PathNode,
                    targetNode,
                    player.Location.CurrentMap.Terrain,
                    isBreadcrumb);
            }
            
            if (_currentPath == null || _currentPath.Length == 0)
            {
                StopWalking();
                return;
            }

            _currentPathIndex = 0;
            _lastKnownPosition = player.Location.Point;

            // Execute the path step by step
            while (_currentPathIndex < _currentPath.Length && !cancellationToken.IsCancellationRequested)
            {
                if (!isBreadcrumb && _currentPath.Length - _currentPathIndex < 2)
                {
                    break; // break early
                }
                
                var nextNode = _currentPath[_currentPathIndex];

                // Check if we're already at the target position
                if ((int)player.Location.X == (int)nextNode.Position.X && (int)player.Location.Y == (int)nextNode.Position.Y)
                {
                    _currentPathIndex++;
                    _lastKnownPosition = nextNode.Position;
                    continue;
                }

                _moveCompletionSource = new TaskCompletionSource<bool>();

                // Start position monitoring task
                var monitoringTask = Task.Run(() => MonitorPositionChange(nextNode.Position, cancellationToken), cancellationToken);

                try
                {
                    // if player is casting a spell then stop walking until they're done
                    if (macro.IsSpellCasting)
                    {
                        break;
                    }
                    await GameActions.WalkAsync(player, nextNode.Direction);

                    // Wait for the move to complete with a timeout
                    var timeoutTask = Task.Delay(500, cancellationToken);
                    var completedTask = await Task.WhenAny(_moveCompletionSource.Task, timeoutTask);

                    if (completedTask == _moveCompletionSource.Task && await _moveCompletionSource.Task)
                    {
                        _currentPathIndex++;
                        _lastKnownPosition = nextNode.Position;
                    }
                    else
                    {
                        // Console.WriteLine("Walk timeout - recalculating path");
                        break;
                    }
                }
                finally
                {
                    _moveCompletionSource = null;
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when cancellation is requested
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Walk error: {ex.Message}");
        }
        finally
        {
            StopWalking();
        }
    }

    private async Task MonitorPositionChange(Point expectedPosition, CancellationToken cancellationToken)
    {
        var player = macro.Client;
        
        try
        {
            while (!cancellationToken.IsCancellationRequested && _moveCompletionSource != null)
            {
                // Poll position directly instead of relying on property change events
                var currentPos = player.Location.Point;
                
                // Only check if position actually changed to reduce overhead
                if (currentPos != _lastKnownPosition)
                {
                    if ((int)currentPos.X == (int)expectedPosition.X && (int)currentPos.Y == (int)expectedPosition.Y)
                    {
                        _moveCompletionSource?.TrySetResult(true);
                        return;
                    }
                    
                    // Check for unexpected deviation
                    var distanceFromExpected = Math.Abs(currentPos.X - expectedPosition.X) + 
                                             Math.Abs(currentPos.Y - expectedPosition.Y);
                    
                    if (distanceFromExpected > 1.5) // Allow slight tolerance for floating point
                    {
                        Console.WriteLine($"Position deviation detected. Expected: ({expectedPosition.X}, {expectedPosition.Y}), " +
                                        $"Actual: ({currentPos.X}, {currentPos.Y}). Recalculating path...");
                        
                        // Cancel current walk operation to trigger recalculation
                        lock (_walkLock)
                        {
                            _walkCancellationSource?.Cancel();
                        }
                        return;
                    }
                    
                    _lastKnownPosition = currentPos;
                }

                // Use a longer polling interval to reduce CPU usage
                await Task.Delay(5, cancellationToken); // 5ms polling instead of property change events
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when cancellation is requested
        }
    }

    private void StopWalking()
    {
        var player = macro.Client;
        player.IsWalking = false;
        
        _currentPath = null;
        _currentPathIndex = 0;
        _moveCompletionSource?.TrySetResult(false);
        _moveCompletionSource = null;
    }

    public void DoneWalking()
    {
        lock (_walkLock)
        {
            _walkCancellationSource?.Cancel();
        }
    }
}