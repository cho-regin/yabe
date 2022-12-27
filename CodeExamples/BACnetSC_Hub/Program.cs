using System;
using System.IO.BACnet;
using System.Diagnostics;

namespace HubApp
{
    // For Windows 10 see trunk\Docs\ActivateTLS1.3 On Win10.reg
    internal class Program
    {
        // A questionable (partially respecting the protocol) but working implementation of HUB service
        // more to test certificates than BACnet/SC
        // run also without security using ws://
        static void Main()
        {
            Trace.Listeners.Add(new ConsoleTraceListener());

            // Hub
            bscHub scHub=new bscHub ("wss://127.0.0.1:47808", @"..\..\PKI");

            for (; ;)
            {
                Console.WriteLine("\r\nEsc to exit, other key to refresh the certificates lists\r\n");

                if (Console.ReadKey(true).Key == ConsoleKey.Escape)
                    return;

                scHub.RefreshRejectedAndTrustedCertificatesLists();
            }
        }
    }
}
