using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text;
using System.Windows;
using DArvis.Common;
using DArvis.IO.Process;

namespace DArvis.Models
{
    public class MapLocationAttributes
    {
        public string MapName { get; set; } = "Unknown";
        public int MapNumber { get; set; } = -1;
        public int Width { get; set; } = -1;
        public int Height { get; set; } = -1;
    }
    
    public sealed class MapLocation : UpdatableObject
    {
        private const string MapNumberKey = @"MapNumber";
        private const string MapNameKey = @"MapName";
        private const string MapXKey = @"MapX";
        private const string MapYKey = @"MapY";

        private readonly Stream stream;
        private readonly BinaryReader reader;

        private MapLocationAttributes attributes;
        private int x;
        private int y;
        private int mapNumber;
        private string mapName;
        private Direction direction;
        private string mapHash;
        private Map? _currentMap;

        public Map? CurrentMap
        {
            get => _currentMap;
            private set => SetProperty(ref _currentMap, value);
        }
        
        private Point _point;
        public Point Point
        {
            get => _point;
            set => SetProperty(ref _point, value, onChanged: OnPlayerPointChanged);
        }
        
        public void OnPlayerPointChanged(Point point)
        {
            if (CurrentMap == null)
                return;
            
            // update our followers map when we move
            if (Owner.Follower != null && Owner.IsOnSameMapAs(Owner.Follower) && Owner.Follower.Location.CurrentMap != null)
            {
                Owner.Follower.Location.CurrentMap.Update();
            }
            
            // update our map whenever we move if we're following a leader
            if (Owner.Leader != null)
            {
                CurrentMap.Update();
            }
        }

        public PathNode PathNode => new() { Position = Point, Direction = Direction };

        public Player Owner { get; init; }

        public MapLocationAttributes Attributes
        {
            get => attributes;
            set => SetProperty(ref attributes, value, onChanged: OnMapAttributesChanged);
        }
        
        public int X
        {
            get => x;
            set => SetProperty(ref x, value);
        }

        public int Y
        {
            get => y;
            set => SetProperty(ref y, value);
        }

        public int MapNumber
        {
            get => mapNumber;
            set => SetProperty(ref mapNumber, value, onChanging: OnMapNumberChanging);
        }
        
        public string MapName
        {
            get => mapName;
            set => SetProperty(ref mapName, value);
        }
        
        public void OnMapNumberChanging(int mapNumber)
        {
            // When the current map changes the Player object location coordinates are still at their
            // previous values. We can take advantage of the delay between the map change and the 
            // Player position update to drop a breadcrumb at the previous map so followers know where to go.

            var isRecordingRoute = Owner.TravelDestinationManager.IsRecording;
            var isLeadingFollower = Owner.Follower != null && Owner.Follower?.Location?.MapNumber != null
                                                           && mapNumber != Owner.Follower.Location.MapNumber; 
            if (isRecordingRoute || isLeadingFollower)
            {
                var oldX = x;
                var oldY = y;
                var oldDir = direction;

                Owner.LastPosition[MapNumber] = new PathNode
                {
                    Position = new Point(oldX, oldY),
                    Direction = oldDir
                };
                
                switch (oldDir)
                {
                    case Direction.North:
                    {
                        oldY -= 1;
                        break;
                    }
                    case Direction.East:
                    {
                        oldX += 1;
                        break;
                    }
                    case Direction.South:
                    {
                        oldY += 1;
                        break;
                    }
                    case Direction.West:
                    {
                        oldX -= 1;
                        break;
                    }
                }
                
                // Drop a breadcrumb at the previous map location
                Owner.Breadcrumbs[MapNumber] = new PathNode
                {
                    Position = new Point(oldX, oldY),
                    Direction = oldDir,
                };
                
                // If recording a route, add this point to the recording
                if (Owner.TravelDestinationManager.IsRecording)
                {
                    Owner.TravelDestinationManager.AddRecordingPoint(MapNumber, oldX, oldY, oldDir);
                }
                
                // Console.WriteLine($"{Owner.Name} dropped a breadcrumb at ({oldX}, {oldY}) on map {MapName} ({MapNumber})");
                // Update the follower's map
                if (isLeadingFollower)
                    Owner.Follower.Location.CurrentMap.Update();
            }
        }

        
        public Direction Direction
        {
            get => direction;
            set => SetProperty(ref direction, value);
        }
        
        public string MapHash
        {
            get => mapHash;
            set => SetProperty(ref mapHash, value);
        }

        public MapLocation(Player owner)
        {
            Owner = owner ?? throw new ArgumentNullException(nameof(owner));

            stream = owner.Accessor.GetStream();
            reader = new BinaryReader(stream, Encoding.ASCII);
        }

        public bool FindPathTo(MapLocation target, out string path)
        {
            CheckIfDisposed();
            
            path = null;

            if (target == null || !IsSameMap(target))
                return false;
            

            // Implement pathfinding logic here.
            // For now, we will just return a dummy path.
            path = $"Path from ({X}, {Y}) to ({target.X}, {target.Y}) on map {Attributes.MapName}";
            return true;
        }
        
        public bool IsSameMap(MapLocation other)
        {
            CheckIfDisposed();
            return Attributes.MapNumber == other.Attributes.MapNumber && string.Equals(Attributes.MapName, other.Attributes.MapName, StringComparison.Ordinal);
        }

        public bool IsNearby(MapLocation other, int maxDistance = 1)
        {
            CheckIfDisposed();

            if (!IsSameMap(other))
                return false;

            var deltaX = Math.Abs(X - other.X);
            var deltaY = Math.Abs(Y - other.Y);

            return deltaX + deltaY <= maxDistance;
        }
        
        public bool IsWithinRange(int x, int y, int maxX = 10, int maxY = 10)
        {
            CheckIfDisposed();

            var deltaX = Math.Abs(X - x);
            var deltaY = Math.Abs(Y - y);

            return deltaX <= maxX && deltaY <= maxY;
        }
        
        public bool IsWithinRange(MapLocation other, int maxX = 10, int maxY = 10)
        {
            CheckIfDisposed();

            if (!IsSameMap(other))
                return false;

            var deltaX = Math.Abs(X - other.X);
            var deltaY = Math.Abs(Y - other.Y);

            return deltaX <= maxX && deltaY <= maxY;
        }

        private void OnMapAttributesChanged(MapLocationAttributes attributes)
        {
            CurrentMap = Map.loadFromAttributes(Owner, attributes);
        }
        
        protected override void OnUpdate()
        {
            return;
            var version = Owner.Version;

            if (version == null)
            {
                ResetDefaults();
                return;
            }

            var mapNumberVariable = version.GetVariable(MapNumberKey);
            var mapXVariable = version.GetVariable(MapXKey);
            var mapYVariable = version.GetVariable(MapYKey);
            var mapNameVariable = version.GetVariable(MapNameKey);

            if (mapNumberVariable != null && mapNumberVariable.TryReadInt32(reader, out var mapNumber))
                Attributes.MapNumber = mapNumber;
            else
                Attributes.MapNumber = 0;

            if (mapXVariable != null && mapXVariable.TryReadInt32(reader, out var mapX))
                X = mapX;
            else
                X = 0;

            if (mapYVariable != null && mapYVariable.TryReadInt32(reader, out var mapY))
                Y = mapY;
            else
                Y = 0;

            if (Owner.Name == "AmorFati")
                Console.WriteLine(Owner.Name + " is at map " + Attributes.MapNumber + " @ (" + X + "," + Y + ")");
            if (mapNameVariable != null && mapNameVariable.TryReadString(reader, out var mapName))
                Attributes.MapName = mapName;
            else
                Attributes.MapName = null;
        }

        protected override void Dispose(bool isDisposing)
        {
            if (isDisposed)
                return;

            if (isDisposing)
            {
                reader?.Dispose();
                stream?.Dispose();
            }

            base.Dispose(isDisposing);
        }

        private void ResetDefaults()
        {
            Attributes = new MapLocationAttributes();
            X = 0;
            Y = 0;
            MapHash = null;
        }

        public override string ToString()
        {
            return string.Format("{0} [{1}] @ {2}, {3}", Attributes.MapName ?? "Unknown Map",
               Attributes.MapNumber.ToString(),
               X.ToString(),
               Y.ToString());
        }
    }
}
