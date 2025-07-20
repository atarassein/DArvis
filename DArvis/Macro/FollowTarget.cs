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
            bool isBreadcrumb = false;
            if (player.IsOnSameMapAs(leader))
            {
                targetNode = NodeBehindPlayer(leader);
            }
            else if (leader.Breadcrumbs.TryGetValue(playerMap, out var breadcrumbNode) && breadcrumbNode != null)
            {
                isBreadcrumb = true;
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

            if (isBreadcrumb && (_currentPath == null || _currentPath.Length == 0))
            {
                // if we're following an invalid breadcrumb then try last position
                if (leader.LastPosition.TryGetValue(playerMap, out var lastPosition) && lastPosition != null)
                {
                    targetNode = lastPosition;
                    _currentPath = PathFinder.FindPath(
                        player.Location.PathNode,
                        targetNode,
                        player.Location.CurrentMap.Terrain);
                }
            }
            
            if (_currentPath == null || _currentPath.Length == 0)
            {
                StopWalking();
                return;
            }

            _currentPathIndex = 0;

            // Execute the path step by step
            while (_currentPathIndex < _currentPath.Length && !cancellationToken.IsCancellationRequested)
            {
                if (!isBreadcrumb && _currentPath.Length - _currentPathIndex < 2)
                {
                    break; // break early
                }
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
    
                // Create a task completion source for this movement
                var moveCompletionSource = new TaskCompletionSource<bool>();
                
                // Subscribe to position changes for this specific move
                void OnMoveCompleted(object sender, System.ComponentModel.PropertyChangedEventArgs e)
                {
                    if (e.PropertyName == nameof(MapLocation.Point))
                    {
                        var currentPos = player.Location.Point;
                        if ((int)currentPos.X == (int)nextNode.Position.X && (int)currentPos.Y == (int)nextNode.Position.Y)
                        {
                            moveCompletionSource.TrySetResult(true);
                        }
                    }
                }
    
                player.Location.PropertyChanged += OnMoveCompleted;
    
                try
                {
                    GameActions.Walk(player, nextNode.Direction);
    
                    // Wait for the move to complete with a timeout
                    var moveTask = moveCompletionSource.Task;
                    var timeoutTask = Task.Delay(500, cancellationToken); // 500ms timeout
    
                    var completedTask = await Task.WhenAny(moveTask, timeoutTask);
    
                    if (completedTask == moveTask && await moveTask)
                    {
                        _currentPathIndex++;
                    }
                    else
                    {
                        Console.WriteLine("Walk timeout - recalculating path");
                        break; // Will trigger path recalculation
                    }
                }
                finally
                {
                    // Always unsubscribe from the event
                    player.Location.PropertyChanged -= OnMoveCompleted;
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

        var player = macro.Client;
        
        // Check if we've deviated from the expected path
        if (_currentPath != null && _currentPathIndex < _currentPath.Length)
        {
            var expectedNode = _currentPath[_currentPathIndex];
            var currentPos = player.Location.Point;

            // If we're not where we expected to be, recalculate the path
            if ((int)currentPos.X != (int)expectedNode.Position.X || (int)currentPos.Y != (int)expectedNode.Position.Y)
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