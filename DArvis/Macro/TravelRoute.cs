using System;
using System.Threading.Tasks;

namespace DArvis.Macro;

public class TravelRoute(PlayerMacroState macro)
{

    public async Task<bool> ShouldTravel()
    {
        if (macro.IsSpellCasting || macro.Client.IsWalking)
        {
            await Task.Delay(100);
            return false;
        }

        var travelDestination = macro.Client.TravelDestinationManager.CurrentDestination;

        if (travelDestination == null)
        {
            await Task.Delay(100);
            return false;
        }
        
        Console.WriteLine(travelDestination.Name);
        await Task.Delay(100);
        return false;
    }

    public async Task Travel()
    {
    }
}