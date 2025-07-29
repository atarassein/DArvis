using System.Collections.ObjectModel;
using System.IO;
using System.Xml.Serialization;

namespace DArvis.Models;

public class TravelDestinationManager
{
    public Player Player { get; }
    
    public TravelDestination? CurrentDestination { get; set; }
    
    public ObservableCollection<TravelDestination> TravelDestinations { get; }
    
    // Route recording properties
    public bool IsRecording { get; private set; }
    public TravelDestination? RecordingDestination { get; private set; }
    private readonly object _recordingLock = new object();
    
    public TravelDestinationManager(Player player)
    {
        Player = player;
        TravelDestinations = new ObservableCollection<TravelDestination>();
        TravelDestinations.Add(new TravelDestination{ Name = "<None>" });
        
        // Load travel destinations from XML files
        LoadTravelDestinationsFromXml();
    }
    
    private void LoadTravelDestinationsFromXml()
    {
        try
        {
            // Get the executable directory and construct the routes folder path
            var executablePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            var executableDir = Path.GetDirectoryName(executablePath);
            var routesPath = Path.Combine(executableDir, "routes");
            
            // Check if routes folder exists
            if (!Directory.Exists(routesPath))
            {
                return; // No routes folder, nothing to load
            }
            
            // Get all XML files in the routes folder
            var xmlFiles = Directory.GetFiles(routesPath, "*.xml");
            
            var serializer = new XmlSerializer(typeof(TravelDestination));
            
            foreach (var xmlFile in xmlFiles)
            {
                try
                {
                    using var fileStream = new FileStream(xmlFile, FileMode.Open, FileAccess.Read);
                    var destination = (TravelDestination)serializer.Deserialize(fileStream);
                    
                    if (destination != null)
                    {
                        // No need to call OnDeserialized - the property setter handles it automatically
                        TravelDestinations.Add(destination);
                    }
                }
                catch (System.Exception ex)
                {
                    // Log error for individual file, but continue processing other files
                    System.Diagnostics.Debug.WriteLine($"Failed to load travel destination from {xmlFile}: {ex.Message}");
                }
            }
        }
        catch (System.Exception ex)
        {
            // Log general error
            System.Diagnostics.Debug.WriteLine($"Failed to load travel destinations: {ex.Message}");
        }
    }
    
    public void SaveTravelDestinationToXml(TravelDestination destination, string filename)
    {
        try
        {
            var executablePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            var executableDir = Path.GetDirectoryName(executablePath);
            var routesPath = Path.Combine(executableDir, "routes");
            
            // Create routes directory if it doesn't exist
            Directory.CreateDirectory(routesPath);
            
            var filePath = Path.Combine(routesPath, filename);
            
            // No need to call OnSerializing - the property getter handles it automatically
            var serializer = new XmlSerializer(typeof(TravelDestination));
            using var fileStream = new FileStream(filePath, FileMode.Create);
            serializer.Serialize(fileStream, destination);
        }
        catch (System.Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to save travel destination to XML: {ex.Message}");
        }
    }
    
    public void StartRecording(string routeName)
    {
        lock (_recordingLock)
        {
            if (IsRecording) return;
            
            RecordingDestination = new TravelDestination
            {
                Name = routeName,
                MapId = Player.Location.MapNumber
            };
            
            IsRecording = true;
        }
    }
    
    public void StopRecording()
    {
        lock (_recordingLock)
        {
            if (!IsRecording || RecordingDestination == null) return;
            
            IsRecording = false;
            
            // Save the recorded route to XML
            var filename = $"{RecordingDestination.Name}.xml";
            SaveTravelDestinationToXml(RecordingDestination, filename);
            
            // Add to the destinations list
            TravelDestinations.Add(RecordingDestination);
            
            RecordingDestination = null;
        }
    }
    
    public void AddRecordingPoint(int mapId, int x, int y, Direction direction)
    {
        lock (_recordingLock)
        {
            if (!IsRecording || RecordingDestination == null) return;
            
            // Remove existing point for this map if it exists (only one point per map)
            RecordingDestination.Points.RemoveAll(p => p.MapId == mapId);
            
            // Add the new point
            RecordingDestination.Points.Add(new RoutePoint
            {
                MapId = mapId,
                X = x,
                Y = y,
                Direction = direction
            });
        }
    }
}