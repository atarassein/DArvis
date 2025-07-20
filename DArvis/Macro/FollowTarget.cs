using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows;
using DArvis.IO.Process;
using DArvis.Models;

namespace DArvis.Macro;

public class FollowTarget(PlayerMacroState macro)
{
   private PathNode NodeBehindPlayer(Player target)
   {
      var pathNode = target.Location.PathNode;
      var x = pathNode.Position.X;
      var y = pathNode.Position.Y;
      switch (pathNode.Direction)
      {
         case Direction.North:
            y++;
            break;
         case Direction.South:
            y--;
            break;
         case Direction.East:
            x--;
            break;
         case Direction.West:
            x++;
            break;
         default:
            throw new ArgumentOutOfRangeException();
      }
      
      return new PathNode {
         Position = new Point(x, y),
         Direction = Direction.North
      };
   }
   public bool ShouldWalk()
   {
      var player = macro.Client;
      var playerMap = player.Location.MapNumber;
      var leader = macro.Client.Leader;
      var leaderMap = leader.Location.MapNumber;
      
      if (playerMap == leaderMap)
      {
         return true;
      }
      
      Thread.Sleep(500);
      if (leader.Breadcrumbs.TryGetValue(playerMap, out var breadcrumb))
      {
         if (playerMap == leaderMap || leader.Breadcrumbs[playerMap] != null)
         {
            return true;
         }
      }
      
      Console.WriteLine("Player is lost.");
      return false;
   }
   
   public void Walk()
   {
      var player = macro.Client;
      player.IsWalking = true;
      var leader = player.Leader;
      if (player == null || leader == null)
      {
         return;
      }
      
      var playerMap = player.Location.MapNumber;
      if (player.IsOnSameMapAs(leader))
      {
         var targetNode = NodeBehindPlayer(leader);
         var path = PathFinder.FindPath(
            player.Location.PathNode,
            targetNode,
            player.Location.CurrentMap.Terrain);
         Console.WriteLine("Walking to " + leader.Location.X + " " + leader.Location.Y);
      } else if (leader.Breadcrumbs.TryGetValue(playerMap, out var breadcrumb))
      {
         var breadcrumbPoint = new PathNode
         {
            Position = new Point(breadcrumb.Value.X, breadcrumb.Value.Y),
            Direction = Direction.North // Assuming North for simplicity, adjust as needed
         };
         
         Console.WriteLine("Walking to breadcrumb " + breadcrumb.Value);
      }
   }

   public void DoneWalking()
   {
      
      //stream.Read()
      // x in memory 882E68 + 23C
      // y in memory 882E6C + 238
   }
}