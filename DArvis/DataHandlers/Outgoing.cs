using System;
using DArvis.Types;

namespace DArvis.DataHandlers;

public static class Outgoing
{

    //this must be a background capture.
    //because we will wait for user to login.
    internal static void Stub(object sender, Packet packet)
    {
        Console.WriteLine($"Unhandled packet sentn: {packet}");
    }

}