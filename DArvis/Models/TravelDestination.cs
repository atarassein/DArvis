using System.Collections.Generic;
using System.Xml.Serialization;

namespace DArvis.Models;

[XmlRoot("TravelDestination")]
public class TravelDestination
{
    [XmlElement("Name")]
    public string Name { get; set; } = string.Empty;
    
    [XmlElement("MapId")]
    public int MapId { get; set; }
    
    [XmlArray("Routes")]
    [XmlArrayItem("Route")]
    public List<RoutePoint> Points { get; set; }
    
    public TravelDestination()
    {
        Points = new List<RoutePoint>();
    }
}

[XmlType("Route")]
public class RoutePoint
{
    [XmlAttribute("mapId")]
    public int MapId { get; set; }
    
    [XmlAttribute("x")]
    public int X { get; set; }
    
    [XmlAttribute("y")]
    public int Y { get; set; }
    
    [XmlAttribute("dir")]
    public Direction Direction { get; set; }
}