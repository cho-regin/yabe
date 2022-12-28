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

            // bscHub scHub=new bscHub ("wss://127.0.0.1:47808", @"..\..\PKI", null, 50000);
            // ... add a ws server channel in loopback mode on port 50000 (with no activity).
            // It's used to re-send each receive frame in a uncyphered channel for debug purpose (usefull only 
            // when a device don't allows ws communication) : Wireshark can get all messages received by the HUB.
            // The HUB offers a decryption service in this case.
            // Wireshark must be started and listen on this port 50000 (Loopback interface) before lauching 
            // the application, because it's the way done by it to detect hub.bsc.bacnet.org subprotocol.
            // Of course the sender and receiver IP is always 127.0.0.1. You should look deeper the VMAC.
            // With a very light code modification it can be used to redirect all the trafic on another PC !

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
