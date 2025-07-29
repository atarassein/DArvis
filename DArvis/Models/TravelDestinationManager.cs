using System.Collections.Generic;

namespace DArvis.Models;

public class TravelDestinationManager
{
    public Player Player { get; }
    
    public TravelDestination? CurrentDestination { get; set; }
    
    public List<TravelDestination> TravelDestinations { get; }
    
    public TravelDestinationManager(Player player)
    {
        Player = player;
        TravelDestinations = new List<TravelDestination>();
        TravelDestinations.Add(new TravelDestination{ Name = "<None>" });
        // TODO: serialize and deserialize the travel destinations from xml files
        
    }
}