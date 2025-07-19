using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace DArvis.Models;

public class PathFinder
{
    public static PathNode[] FindPath(PathNode start, PathNode end, int[,] terrain)
    {
        if (start?.Position == null || end?.Position == null || terrain == null)
            return Array.Empty<PathNode>();

        var mapWidth = terrain.GetLength(0);
        var mapHeight = terrain.GetLength(1);

        // Validate bounds
        if (!IsValidPosition(start.Position, mapWidth, mapHeight) ||
            !IsValidPosition(end.Position, mapWidth, mapHeight))
            return Array.Empty<PathNode>();

        // Already at destination
        if (start.Position.Equals(end.Position))
            return Array.Empty<PathNode>();

        var openSet = new SortedDictionary<float, List<PathNode>>();
        var allNodes = new Dictionary<Point, PathNode>();
        var closedSet = new HashSet<Point>();

        // Initialize start node
        var startNode = new PathNode
        {
            Position = start.Position,
            Direction = start.Direction,
            HScore = CalculateHeuristic(start.Position, end.Position),
            GScore = 0,
            Previous = null
        };

        allNodes[start.Position] = startNode;
        AddToOpenSet(openSet, startNode);

        while (openSet.Count > 0)
        {
            var current = GetNextNode(openSet);

            if (current.Position.Equals(end.Position))
            {
                return ReconstructPath(current);
            }

            closedSet.Add(current.Position);

            // Explore all four directions
            foreach (var direction in GetDirections())
            {
                var neighborPos = GetNeighborPosition(current.Position, direction);

                if (closedSet.Contains(neighborPos) ||
                    !IsValidPosition(neighborPos, mapWidth, mapHeight) ||
                    !IsPassable(neighborPos, terrain))
                    continue;

                var movementCost = CalculateMovementCost(current.Direction, direction);
                var directionChangeCost = CalculateDirectionChangeCost(current.Direction, direction, end.Direction);
                var gScore = current.GScore + movementCost + directionChangeCost;
                var hScore = CalculateHeuristic(neighborPos, end.Position);

                if (allNodes.TryGetValue(neighborPos, out var existingNode))
                {
                    if (gScore < existingNode.GScore)
                    {
                        RemoveFromOpenSet(openSet, existingNode);
                        existingNode.GScore = gScore;
                        existingNode.Direction = direction;
                        existingNode.Previous = current;
                        AddToOpenSet(openSet, existingNode);
                    }
                }
                else
                {
                    var newNode = new PathNode
                    {
                        Position = neighborPos,
                        Direction = direction,
                        HScore = hScore,
                        GScore = gScore,
                        Previous = current
                    };
                    
                    allNodes[neighborPos] = newNode;
                    AddToOpenSet(openSet, newNode);
                }
            }
        }

        return Array.Empty<PathNode>(); // No path found
    }

    private static float CalculateMovementCost(Direction currentDirection, Direction newDirection)
    {
        return 1.0f; // Base movement cost
    }

    private static float CalculateDirectionChangeCost(Direction currentDirection, Direction newDirection, Direction endDirection)
    {
        const float directionChangePenalty = 0.3f;
        const float endDirectionBonus = 0.1f;

        float cost = 0;

        // Add penalty for changing direction
        if (currentDirection != Direction.None && currentDirection != newDirection)
        {
            cost += directionChangePenalty;
        }

        // Small bonus for moving toward the end direction (helps with straight lines)
        if (newDirection == endDirection)
        {
            cost -= endDirectionBonus;
        }

        return cost;
    }

    private static float CalculateHeuristic(Point current, Point goal)
    {
        // Manhattan distance
        return Math.Abs((int)current.X - (int)goal.X) + Math.Abs((int)current.Y - (int)goal.Y);
    }

    private static Direction[] GetDirections()
    {
        return new[] { Direction.North, Direction.South, Direction.East, Direction.West };
    }

    private static Point GetNeighborPosition(Point position, Direction direction)
    {
        return direction switch
        {
            Direction.North => new Point(position.X, position.Y - 1),
            Direction.South => new Point(position.X, position.Y + 1),
            Direction.East => new Point(position.X + 1, position.Y),
            Direction.West => new Point(position.X - 1, position.Y),
            _ => position
        };
    }

    private static bool IsValidPosition(Point position, int width, int height)
    {
        return position.X >= 0 && position.X < width &&
               position.Y >= 0 && position.Y < height;
    }

    private static bool IsPassable(Point position, int[,] terrain)
    {
        var tileValue = terrain[(int)position.X, (int)position.Y];
        return (tileValue & ((int)TileFlags.Wall | (int)TileFlags.BlockingEntity)) == 0;
    }

    private static void AddToOpenSet(SortedDictionary<float, List<PathNode>> openSet, PathNode node)
    {
        var fScore = node.FScore;
        if (!openSet.TryGetValue(fScore, out var nodes))
        {
            nodes = new List<PathNode>();
            openSet[fScore] = nodes;
        }
        nodes.Add(node);
    }

    private static void RemoveFromOpenSet(SortedDictionary<float, List<PathNode>> openSet, PathNode node)
    {
        var fScore = node.FScore;
        if (openSet.TryGetValue(fScore, out var nodes))
        {
            nodes.Remove(node);
            if (nodes.Count == 0)
                openSet.Remove(fScore);
        }
    }

    private static PathNode GetNextNode(SortedDictionary<float, List<PathNode>> openSet)
    {
        var firstEntry = openSet.First();
        var node = firstEntry.Value[0];

        firstEntry.Value.RemoveAt(0);
        if (firstEntry.Value.Count == 0)
            openSet.Remove(firstEntry.Key);

        return node;
    }

    private static PathNode[] ReconstructPath(PathNode endNode)
    {
        var pathNodes = new List<PathNode>();
        var current = endNode;

        while (current != null)
        {
            pathNodes.Add(current);
            current = current.Previous;
        }

        pathNodes.Reverse();

        // Link the path nodes together
        for (int i = 0; i < pathNodes.Count; i++)
        {
            if (i > 0)
                pathNodes[i].Previous = pathNodes[i - 1];
            if (i < pathNodes.Count - 1)
                pathNodes[i].Next = pathNodes[i + 1];
        }

        // Remove the starting position since we're already there
        if (pathNodes.Count > 0)
            pathNodes.RemoveAt(0);

        return pathNodes.ToArray();
    }
}

public class PathNode
{
    public Point Position { get; set; }
    public Direction Direction { get; set; } = Direction.None;
    public float GScore { get; set; }
    public float HScore { get; set;  } 
    public float FScore => GScore + HScore;
    
    public PathNode? Previous { get; set; }
    
    public PathNode? Next { get; set; }
}

