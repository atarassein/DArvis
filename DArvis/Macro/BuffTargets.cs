using System;
using System.Threading.Tasks;

namespace DArvis.Macro;

public class BuffTargets(PlayerMacroState macro)
{
    public async Task<bool> ShouldBuff()
    {
        // Console.WriteLine("should buff");
        return true;
    }

    public async Task Buff()
    {
        
    }
}