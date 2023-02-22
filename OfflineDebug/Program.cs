﻿/**************************************************************************
*                           MIT License
* 
* Copyright (C) 2015 Frederic Chaxel <fchaxel@free.fr>
*
* Permission is hereby granted, free of charge, to any person obtaining
* a copy of this software and associated documentation files (the
* "Software"), to deal in the Software without restriction, including
* without limitation the rights to use, copy, modify, merge, publish,
* distribute, sublicense, and/or sell copies of the Software, and to
* permit persons to whom the Software is furnished to do so, subject to
* the following conditions:
*
* The above copyright notice and this permission notice shall be included
* in all copies or substantial portions of the Software.
*
* THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
* EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
* MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
* IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY
* CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
* TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
* SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*
*********************************************************************/
using System;
using System.Collections.Generic;
using System.IO.BACnet;
using System.IO.BACnet.Serialize;
using SharpPcap;
using SharpPcap.LibPcap;
using System.Net.Sockets;
using System.Net;

namespace OfflineStackDebug
{
    class Program
    {
        static BacnetDebugTransport debug_transport = new BacnetDebugTransport();
        static BacnetClient bacnet_client = new BacnetClient(debug_transport);
        static BacnetAddress nowhere = new BacnetAddress(BacnetAddressTypes.None, "");

        static void Main(string[] args)
        {
            bacnet_client.Start();
            ServiceToBeTested();
        }
       
        // To send raw udp activity
        static void RawUdpTest()
        {
            PcapDevice pcap = new CaptureFileReaderDevice(@"..\..\EventNotification3.pcap");
            pcap.Open();
            PacketCapture raw;

            pcap.GetNextPacket(out raw);

            byte[] b = new byte[raw.Data.Length - 0x2a];

            int UdpSize = (raw.Data[0x26] << 8) + raw.Data[0x27]; // to remove Ethernet padding if present
            Array.Copy(raw.Data.ToArray(), 0x2a, b, 0, UdpSize - 8);

            UdpClient u = new UdpClient();

            u.Send(b, b.Length, "127.0.0.1", 47808);
            Console.WriteLine("Event Sent");

            IPEndPoint ep = null;
            u.Receive(ref ep);
            Console.WriteLine("receive");
            pcap.GetNextPacket(out raw);

            b = new byte[raw.Data.Length - 0x2a];

            UdpSize = (raw.Data[0x26] << 8) + raw.Data[0x27];
            Array.Copy(raw.Data.ToArray(), 0x2a, b, 0, UdpSize - 8);

            u.Send(b, b.Length, "127.0.0.1", 47808);
            Console.WriteLine("Ack Sent");

            for (; ; )
            {
                u.Receive(ref ep);
                Console.WriteLine("receive");
            }

        }

        
       static void ServiceToBeTested()
       {

           // Try to debug the reception of the first frame from the .pcap file
           // 2nd param is the number of frames (more than 1 in case of segmentation) 
           byte invokeId = SetReplyPackets(@"..\..\ReadPropMultiple.pcap", 1, 0x2e);

           BacnetPropertyReference[] properties = new BacnetPropertyReference[] { new BacnetPropertyReference((uint)BacnetPropertyIds.PROP_ALL, System.IO.BACnet.Serialize.ASN1.BACNET_ARRAY_ALL) };
           IList<BacnetReadAccessResult> multi_value_list;

           BacnetObjectId BOid = new BacnetObjectId(BacnetObjectTypes.OBJECT_DEVICE, 16);

           System.Diagnostics.Debugger.Break();
           bacnet_client.ReadPropertyMultipleRequest(nowhere, BOid, properties, out multi_value_list, invokeId);

           // to debug server handle methods, just send any kind of unconfirmed message such as whois
       }

       // Old technic
       static byte SetReplyPackets()
       {
           // NPDU & APDU : from wireshark, export byte 'Offset Hex'
           // replace the 7 first chars by a space
           // remplace space by ",0x" 
           // get the InvokeId for usage for the service call
           debug_transport.MsgToSendBack = new byte[1][]
           {new byte[]{
               // NPDU
               0x01,0x00
               // APDU
               ,0x30,0x03,0x1d,0x0e,0x0c,0x00,0x00,0x00,0x01,0x19,0x04,0x2a,0x05,0xe0,0x3e,0x2e
               ,0xa4,0x73,0x0a,0x16,0x04,0xb4,0x06,0x26,0x1a,0x00,0x2f,0x2e,0xa4,0xff,0xff,0xff
               ,0xff,0xb4,0xff,0xff,0xff,0xff,0x2f,0x2e,0xa4,0x73,0x0a,0x16,0x04,0xb4,0x06,0x26
               ,0x1a,0x00,0x2f,0x3f,0x49,0x00,0x5a,0x05,0xe0,0x6e,0x21,0x7f,0x21,0x7f,0x21,0x7f
               ,0x6f,0x0c,0x00,0x40,0x00,0x00,0x19,0x04,0x2a,0x05,0x60,0x3e,0x2e,0xa4,0x73,0x0a
               ,0x16,0x04,0xb4,0x08,0x2f,0x13,0x00,0x2f,0x0c,0x00,0x00,0x00,0x00,0x2e,0xa4,0x73
               ,0x0a,0x16,0x04,0xb4,0x06,0x26,0x1a,0x00,0x2f,0x3f,0x49,0x00,0x5a,0x05,0xe0,0x6e
               ,0x21,0x7f,0x21,0x7f,0x21,0x7f,0x6f,0x0f,0x19,0x00
            }};

           return 3; // invokeId
       }

       // Simplest technic
       static byte SetReplyPackets(string PcapFile, int NumberofFrames, int NPDU_offset)
       {

           // Read into the Wireshark or tcpdump export file
           // retrieve the service call InvokeId if any
           // assume NPDU_offset is the same in all the consecutive response frames, normaly it does

           PcapDevice pcap = new CaptureFileReaderDevice(PcapFile);
           pcap.Open();
           PacketCapture raw;
           byte InvokeId = 0;

           debug_transport.MsgToSendBack = new byte[NumberofFrames][];

           for (int i = 0; i < NumberofFrames; i++)
           {
               pcap.GetNextPacket(out raw);
               debug_transport.MsgToSendBack[i] = new byte[raw.Data.Length - NPDU_offset];
               Array.Copy(raw.Data.ToArray(), NPDU_offset, debug_transport.MsgToSendBack[i], 0, raw.Data.Length - NPDU_offset);
               if (i == 0)
               {
                   BacnetNpduControls npdu_function;
                   BacnetAddress destination, source;
                   byte hop_count;
                   BacnetNetworkMessageTypes nmt;
                   ushort vendor_id;

                   // Find the InvokeId in the first frame
                   // skip the NPDU
                   int offset=NPDU.Decode(debug_transport.MsgToSendBack[0],0,out npdu_function,out destination, out source,out hop_count, out nmt,out vendor_id);
                   // if -1 we don't care
                   InvokeId = (byte)APDU.GetDecodedInvokeId(debug_transport.MsgToSendBack[0], offset);
               }
           }

           return InvokeId; // invokeId
       }
   }

   /////////////////////////////////////////////////////////////////////////////////////////////////
   /////////////////////////////////////////////////////////////////////////////////////////////////
   /////////////////////////////////////////////////////////////////////////////////////////////////
   // Used to debug received packets, captured with wireshark for instance
   class BacnetDebugTransport : IBacnetTransport
   {
       public byte[][] MsgToSendBack;
       public event MessageRecievedHandler MessageRecieved;

       public BacnetDebugTransport() {}     
       public BacnetAddressTypes Type { get { return BacnetAddressTypes.None; } }
       public BacnetAddress GetBroadcastAddress() { return new BacnetAddress(BacnetAddressTypes.None, ""); }
       public int HeaderLength { get { return 0; } }
       public BacnetMaxAdpu MaxAdpuLength { get { return BacnetMaxAdpu.MAX_APDU1476; } }
       public int MaxBufferLength { get { return 1500; } }
       public byte MaxInfoFrames { get { return 0xff; } set { } }   
       public bool WaitForAllTransmits(int timeout) { return true; } // not used 

       public void Start() {}
       public void Dispose() {}

       public int Send(byte[] buffer, int offset, int data_length, BacnetAddress address, bool wait_for_transmission, int timeout)
       {        
           BacnetAddress ba=new BacnetAddress(BacnetAddressTypes.None,"");

           if (MessageRecieved != null)
           {
               for (int i=0;i<MsgToSendBack.Length;i++)
                   MessageRecieved(this, MsgToSendBack[i], 0, MsgToSendBack[i].Length, ba);
           }                        
           return data_length;
       }
    }
}
