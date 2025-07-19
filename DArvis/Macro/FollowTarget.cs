using System;

namespace DArvis.Macro;

public class FollowTarget
{
   private PlayerMacroState _macro;

   public FollowTarget(PlayerMacroState macro)
   {
      _macro = macro;
   }
   public bool ShouldWalk()
   {
      return true;
   }
   
   public void Walk()
   {
      var player = _macro.Client;
      var target = _macro.Client.Leader;

      if (player.IsOnSameMapAs(target))
      {
         Console.WriteLine("Walking to " + _macro.Client.Leader.Location.X + " " + _macro.Client.Leader.Location.Y);
      } else if (_macro.Client.Leader.Breadcrumb != null)
      {
         Console.WriteLine("Walking to breadcrumb " + _macro.Client.Leader.Breadcrumb.Value);
      }
   }

   public void DoneWalking()
   {
      
   }
}