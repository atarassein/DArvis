using System;
using System.Threading.Tasks;

namespace DArvis.Macro;

public class BuffTargets(PlayerMacroState macro)
{
    public async Task<bool> ShouldBuff()
    {
        foreach (var spell in macro.Client.BuffManager.ActiveSpells)
        {
            if (spell.IsOnCooldown) continue;
        }

        // Console.WriteLine("should buff");
        return true;
    }

    public async Task Buff()
    {
        
    }
}