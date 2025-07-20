using System;
using System.IO;
using System.Text;
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

    private PathNode NodeBehindPlayer(Player target)
    {
        var pathNode = target.Location.PathNode;
        var x = pathNode.Position.X;
        var y = pathNode.Position.Y;
        switch (pathNode.Direction)
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

    public bool ShouldWalk()
    {
        var player = macro.Client;
        if (player.IsWalking) return false;

        var playerMap = player.Location.MapNumber;
        var leader = player.Leader;
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
        return false;
    }

    public void Walk()
    {
        var player = macro.Client;
        if (player.IsWalking) return;

        lock (_walkLock)
        {
            // Cancel any existing walk operation
            _walkCancellationSource?.Cancel();
            _walkCancellationSource = new CancellationTokenSource();

            player.IsWalking = true;

            // Subscribe to position changes
            player.Location.PropertyChanged += OnPlayerPositionChanged;

            // Start walking on a separate thread
            Task.Run(() => ExecuteWalk(_walkCancellationSource.Token));
        }
    }

    private async Task ExecuteWalk(CancellationToken cancellationToken)
    {
        var player = macro.Client;
        var leader = player.Leader;

        if (player.IsDisposing || leader == null || leader.IsDisposing)
        {
            StopWalking();
            return;
        }

        try
        {
            var playerMap = player.Location.MapNumber;
            PathNode? targetNode = null;

            if (player.IsOnSameMapAs(leader))
            {
                targetNode = NodeBehindPlayer(leader);
            }
            else if (leader.Breadcrumbs.TryGetValue(playerMap, out var breadcrumbNode) && breadcrumbNode != null)
            {
                targetNode = breadcrumbNode;
            }

            if (targetNode == null)
            {
                StopWalking();
                return;
            }

            _currentPath = PathFinder.FindPath(
                player.Location.PathNode,
                targetNode,
                player.Location.CurrentMap.Terrain);

            if (_currentPath == null || _currentPath.Length == 0)
            {
                StopWalking();
                return;
            }

            _currentPathIndex = 0;

            // Execute the path step by step
            while (_currentPathIndex < _currentPath.Length && !cancellationToken.IsCancellationRequested)
            {
                var nextNode = _currentPath[_currentPathIndex];
                
                Console.WriteLine($"Walking to step {_currentPathIndex + 1}/{_currentPath.Length}: " +
                                $"({player.Location.PathNode.Position.X}, {player.Location.PathNode.Position.Y}) -> " +
                                $"[{nextNode.Direction}] -> ({nextNode.Position.X}, {nextNode.Position.Y})");

                // Check if we're already at the target position
                if ((int)player.Location.X == (int)nextNode.Position.X && (int)player.Location.Y == (int)nextNode.Position.Y)
                {
                    _currentPathIndex++;
                    continue;
                }

                GameActions.Walk(player, nextNode.Direction);

                // Wait for position update or timeout
                var waitTime = 0;
                const int maxWaitTime = 2000; // 2 seconds timeout
                const int pollInterval = 50;

                while (waitTime < maxWaitTime && !cancellationToken.IsCancellationRequested)
                {
                    if (player.Location.X == nextNode.Position.X && player.Location.Y == nextNode.Position.Y)
                    {
                        _currentPathIndex++;
                        break;
                    }

                    await Task.Delay(pollInterval, cancellationToken);
                    waitTime += pollInterval;
                }

                if (waitTime >= maxWaitTime)
                {
                    Console.WriteLine("Walk timeout - recalculating path");
                    break; // Will trigger path recalculation
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

    private void OnPlayerPositionChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(MapLocation.Point))
            return;

        Console.WriteLine("hooking into position change");
        var player = macro.Client;
        
        // Check if we've deviated from the expected path
        if (_currentPath != null && _currentPathIndex < _currentPath.Length)
        {
            var expectedNode = _currentPath[_currentPathIndex];
            var currentPos = player.Location.Point;

            // If we're not where we expected to be, recalculate the path
            if (currentPos.X != expectedNode.Position.X || currentPos.Y != expectedNode.Position.Y)
            {
                // Check if we've moved to an unexpected position (could be due to lag or other factors)
                var distanceFromExpected = Math.Abs(currentPos.X - expectedNode.Position.X) + 
                                         Math.Abs(currentPos.Y - expectedNode.Position.Y);

                if (distanceFromExpected > 1)
                {
                    Console.WriteLine($"Position deviation detected. Expected: ({expectedNode.Position.X}, {expectedNode.Position.Y}), " +
                                    $"Actual: ({currentPos.X}, {currentPos.Y}). Recalculating path...");
                    
                    // Cancel current walk and restart
                    lock (_walkLock)
                    {
                        _walkCancellationSource?.Cancel();
                    }
                }
            }
        }
    }

    private void StopWalking()
    {
        var player = macro.Client;
        player.IsWalking = false;
        
        // Unsubscribe from position changes
        player.Location.PropertyChanged -= OnPlayerPositionChanged;
        
        _currentPath = null;
        _currentPathIndex = 0;
    }

    public void DoneWalking()
    {
        lock (_walkLock)
        {
            _walkCancellationSource?.Cancel();
        }
    }
}