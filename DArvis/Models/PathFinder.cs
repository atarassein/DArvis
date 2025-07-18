using System;
using System.Collections.Generic;
using System.Windows;

namespace DArvis.Models;

public class PathFinder
    {
        private class PathNode
        {
            public int X { get; set; }
            public int Y { get; set; }
            public int GCost { get; set; } // Distance from start
            public int HCost { get; set; } // Heuristic distance to target
            public int FCost => GCost + HCost; // Total cost
            public PathNode Parent { get; set; }

            public PathNode(int x, int y)
            {
                X = x;
                Y = y;
            }
        }

        private class PathNodeComparer : IComparer<PathNode>
        {
            public int Compare(PathNode x, PathNode y)
            {
                int compare = x.FCost.CompareTo(y.FCost);
                return compare == 0 ? x.HCost.CompareTo(y.HCost) : compare;
            }
        }

        private readonly Map _map;

        public PathFinder(Map map)
        {
            _map = map ?? throw new ArgumentNullException(nameof(map));
        }

        public Point[] FindPathToLeader()
        {
            if (_map.Owner?.Leader == null || !_map.Owner.IsOnSameMapAs(_map.Owner.Leader))
                return new Point[0];

            return FindPath(_map.Owner.Location.X, _map.Owner.Location.Y,
                           _map.Owner.Leader.Location.X, _map.Owner.Leader.Location.Y);
        }

        public Point[] FindPath(int startX, int startY, int targetX, int targetY)
        {
            if (!IsValidCoordinate(startX, startY) || !IsValidCoordinate(targetX, targetY))
                return new Point[0];

            if (startX == targetX && startY == targetY)
                return new Point[0];

            var openSet = new SortedSet<PathNode>(new PathNodeComparer());
            var closedSet = new HashSet<string>();
            var allNodes = new Dictionary<string, PathNode>();

            var startNode = new PathNode(startX, startY);
            startNode.GCost = 0;
            startNode.HCost = CalculateHeuristic(startX, startY, targetX, targetY);

            openSet.Add(startNode);
            allNodes[GetNodeKey(startX, startY)] = startNode;

            // 4-directional movement: Up, Right, Down, Left
            int[] dx = { 0, 1, 0, -1 };
            int[] dy = { -1, 0, 1, 0 };

            while (openSet.Count > 0)
            {
                var currentNode = openSet.Min;
                openSet.Remove(currentNode);

                string currentKey = GetNodeKey(currentNode.X, currentNode.Y);
                closedSet.Add(currentKey);

                // Found target
                if (currentNode.X == targetX && currentNode.Y == targetY)
                {
                    return ReconstructPath(currentNode);
                }

                // Check all 4 directions
                for (int i = 0; i < 4; i++)
                {
                    int newX = currentNode.X + dx[i];
                    int newY = currentNode.Y + dy[i];
                    string neighborKey = GetNodeKey(newX, newY);

                    if (!IsValidCoordinate(newX, newY) ||
                        closedSet.Contains(neighborKey) ||
                        !_map.IsPassableForPathfinding(newX, newY))
                        continue;

                    int tentativeGCost = currentNode.GCost + 10; // Each step costs 10

                    if (!allNodes.TryGetValue(neighborKey, out PathNode neighborNode))
                    {
                        neighborNode = new PathNode(newX, newY);
                        neighborNode.HCost = CalculateHeuristic(newX, newY, targetX, targetY);
                        allNodes[neighborKey] = neighborNode;
                    }

                    if (!openSet.Contains(neighborNode))
                    {
                        neighborNode.GCost = tentativeGCost;
                        neighborNode.Parent = currentNode;
                        openSet.Add(neighborNode);
                    }
                    else if (tentativeGCost < neighborNode.GCost)
                    {
                        openSet.Remove(neighborNode);
                        neighborNode.GCost = tentativeGCost;
                        neighborNode.Parent = currentNode;
                        openSet.Add(neighborNode);
                    }
                }
            }

            return new Point[0]; // No path found
        }

        private bool IsValidCoordinate(int x, int y)
        {
            return x >= 0 && x < _map.Attributes.Width && y >= 0 && y < _map.Attributes.Height;
        }

        private int CalculateHeuristic(int x1, int y1, int x2, int y2)
        {
            // Manhattan distance for 4-directional movement
            return 10 * (Math.Abs(x2 - x1) + Math.Abs(y2 - y1));
        }

        private string GetNodeKey(int x, int y)
        {
            return $"{x},{y}";
        }

        private Point[] ReconstructPath(PathNode targetNode)
        {
            var path = new List<Point>();
            var current = targetNode;

            while (current.Parent != null)
            {
                path.Add(new Point(current.X, current.Y));
                current = current.Parent;
            }

            path.Reverse();
            return path.ToArray();
        }
    }