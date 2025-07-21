using System.Threading.Tasks;

namespace DArvis.Macro;

public class BuffTargets(PlayerMacroState macro)
{
    public async Task<bool> ShouldBuff()
    {
        return true;
    }

    public async Task Buff()
    {
        
    }
}