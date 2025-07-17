using System.Collections.ObjectModel;
using System.Linq;

namespace DArvis.Models;

public sealed class LeaderSelectionManager
{
    private static readonly LeaderSelectionManager instance = new();

    public static LeaderSelectionManager Instance => instance;

    private LeaderSelectionManager()
    {
        Leaders.Add(new LeaderSelectionItem());
    }

    public ObservableCollection<LeaderSelectionItem> Leaders
    {
        get
        {
            var sortedClients = from p in PlayerManager.Instance.LoggedInPlayers orderby p.Name where p.IsLoggedIn select p;
            var visiblePlayers = sortedClients.ToList();

            var leaders = new ObservableCollection<LeaderSelectionItem>();
            leaders.Add(new LeaderSelectionItem());

            foreach (var p in visiblePlayers)
            {
                var item = new LeaderSelectionItem(p);
                leaders.Add(item);
            }
            
            return leaders;
        }
    }
}

public class LeaderSelectionItem
{
    public Player Player { get; set; }
    public string DisplayName { get; set; }
    
    public string ProcessIdLabel { get; set; }
    
    public bool IsNone { get; set; }
    
    public LeaderSelectionItem()
    {
        IsNone = true;
        DisplayName = "<None>";
    }
    
    public LeaderSelectionItem(Player player)
    {
        Player = player;
        DisplayName = player.Name;
        ProcessIdLabel = player.Process.ProcessId > 0 ? $"{(player.Process.ProcessId)}" : "";
        IsNone = false;
    }
}