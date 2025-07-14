using System;
using DArvis.Types;

namespace DArvis.DataHandlers;

public static class Incoming
    {
        
        public static void Stub(object sender, Packet packet)
        {
            // This is a stub for incoming packets that are not handled.
            // You can log or handle these packets as needed.
            Console.WriteLine($"Unhandled packet received: {packet}");
        }

    }