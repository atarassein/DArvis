using System;
using DArvis.Models;

namespace DArvis.Macro;

public class FollowTarget(PlayerMacroState macro)
{
   private Player? Player => PlayerManager.Instance.GetPlayer(macro.Client.Process.ProcessId);

   private Player? Leader => PlayerManager.Instance.GetPlayer(macro.Client.Leader.Process.ProcessId);

   public bool ShouldWalk()
   {
      if (Player?.Location.CurrentMap == null
          || Leader?.Location.CurrentMap == null)
      {
         return false;
      }
      
      if (Player.IsOnSameMapAs(Leader) 
          || Leader.Breadcrumb != null)
      {
         return true;
      }
      
      Console.WriteLine("Player is lost.");
      return false;
   }
   
   public void Walk()
   {
      var player = Player;
      var target = Leader;
      if (player == null || target == null)
      {
         return;
      }
      
      if (player.IsOnSameMapAs(target))
      {
         Console.WriteLine("Walking to " + macro.Client.Leader.Location.X + " " + macro.Client.Leader.Location.Y);
      } else if (macro.Client.Leader.Breadcrumb != null)
      {
         Console.WriteLine("Walking to breadcrumb " + macro.Client.Leader.Breadcrumb.Value);
      }
   }

   public void DoneWalking()
   {
      
   }
}