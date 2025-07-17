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

    public LeaderSelectionItem getBlankLeader()
    {
        foreach (var item in Leaders)
        {
            if (item.IsNone)
            {
                return item;
            }
        }

        return null;
    }
    
    public LeaderSelectionItem getLeaderSelectionItem(Player player)
    {
        foreach (var item in Leaders)
        {
            if (item.Player != null && item.Player == player)
            {
                return item;
            }
        }

        return null;
    }
    
    private ObservableCollection<LeaderSelectionItem> _leaders = new();
    public ObservableCollection<LeaderSelectionItem> Leaders
    {
        get
        {
            var sortedClients = from p in PlayerManager.Instance.LoggedInPlayers orderby p.Name where p.IsLoggedIn select p;
            var visiblePlayers = sortedClients.ToList();

            foreach (var player in visiblePlayers)
            {
                if (!_leaders.Any(item => item.Player == player))
                {
                    _leaders.Add(new LeaderSelectionItem(player));
                }
            }
            
            return _leaders;
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