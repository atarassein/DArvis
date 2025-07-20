using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows;
using DArvis.IO;
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
      }
      
      return new PathNode {
         Position = new Point(x, y),
         Direction = Direction.North
      };
   }
   public bool ShouldWalk()
   {
      var player = macro.Client;
      if (player.IsWalking) return false;
      
      var playerMap = player.Location.MapNumber;
      var leader = macro.Client.Leader;
      var leaderMap = leader.Location.MapNumber;
      
      if (playerMap == leaderMap)
      {
         return true;
      }
      
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
      if (player.IsWalking) return;
      player.IsWalking = true;
      
      var leader = player.Leader;
      if (player == null || leader == null)
      {
         player.IsWalking = false;
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
         if (path == null || path.Length == 0)
         {
            player.IsWalking = false;
            return;
         }
         
         var node = path[0];
         Console.WriteLine($"({player.Location.PathNode.Position.X}, {player.Location.PathNode.Position.Y}) -> [{node.Direction}] -> ({node.Position.X}, {node.Position.Y})");
         if (player.Location.X == node.Position.X && player.Location.Y == node.Position.Y)
         {
            player.IsWalking = false;
            return;
         }
         GameActions.Walk(player, node.Direction);
         Thread.Sleep(50);
      } else if (leader.Breadcrumbs.TryGetValue(playerMap, out var breadcrumbNode))
      {
         var path = PathFinder.FindPath(
            player.Location.PathNode,
            breadcrumbNode,
            player.Location.CurrentMap.Terrain);
         
         while (player.Location.CurrentMap.IsValidNodePath(path, 0))
         {
            GameActions.Walk(player, path[0].Direction);
            path = PathFinder.FindPath(
               player.Location.PathNode,
               breadcrumbNode,
               player.Location.CurrentMap.Terrain);
         }
      }
      
      player.IsWalking = false;
   }

   public void DoneWalking()
   {
      
      //stream.Read()
      // x in memory 882E68 + 23C
      // y in memory 882E6C + 238
   }
}