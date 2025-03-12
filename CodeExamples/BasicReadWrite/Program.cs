/**************************************************************************
*                           MIT License
* 
* Copyright (C) 2015 Morten Kvistgaard <mk@pch-engineering.dk>
*                    Frederic Chaxel <fchaxel@free.fr 
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
using System.Threading;
using System.Diagnostics;
using Yabe;

namespace BasicReadWrite
{
    //
    // A very simple read/write client code based on Yabe code
    //
    class Program
    {
        static BacnetClient bacnet_client;

        // All the present Bacnet Device List
        static List<BACnetDevice> DevicesList = new List<BACnetDevice>();

        /*****************************************************************************************************/
        static void Main(string[] args)
        {
            Trace.Listeners.Add(new ConsoleTraceListener());

            try
            {
                StartActivity();
                Console.WriteLine("Started");
                ReadWriteExample();
            }
            catch { }

            Console.ReadKey();            
        }
        /*****************************************************************************************************/
        static void StartActivity()
        {
            // Bacnet on UDP/IP/Ethernet
            // to bind to a specific network interface such as 100.75.35.6 
            // bacnet_client = new BacnetClient(new BacnetIpUdpProtocolTransport(0xBAC0, false, false, 1472, "100.75.35.6"));
            // otherwise the default interface is open ... sometimes OK, sometimes not !
            bacnet_client = new BacnetClient(new BacnetIpUdpProtocolTransport(0xBAC0, false));

            // Or Bacnet Ethernet
            // bacnet_client = new BacnetClient(new BacnetEthernetProtocolTransport("Connexion au réseau local"));          
            // Or Bacnet on IPV6, default interface
            // bacnet_client = new BacnetClient(new BacnetIpV6UdpProtocolTransport(0xBAC0));

            // If BacnetTransportSerial.cs is added to this project one can use for instance :
            // Bacnet Mstp on COM4 à 38400 bps, own master id 8
            // m_bacnet_client = new BacnetClient(new BacnetMstpProtocolTransport("COM4", 38400, 8);

            bacnet_client.Start();    // go

            // Send WhoIs in order to get back all the Iam responses :  
            bacnet_client.OnIam += new BacnetClient.IamHandler(handler_OnIam);

            bacnet_client.OnWhoIs += (_,__,___,____)=> { };

            /* Optional Remote Registration as A Foreign Device on a BBMD at @192.168.1.1 on the default 0xBAC0 port              
            bacnet_client.RegisterAsForeignDevice("192.168.1.1", 60);
            Thread.Sleep(20);
            bacnet_client.RemoteWhoIs("192.168.1.1");
            */
        }

        /*****************************************************************************************************/
        static void ReadWriteExample()
        {

            // Read Present_Value property on the object ANALOG_INPUT:0 provided by the device 12345
            // Write Present_Value property on the object ANALOG_OUTPUT:3 provided by the device 400001

            bacnet_client.WhoIs(12345);
            bacnet_client.WhoIs(400001);
            // bacnet_client.WhoIs(); // can be done with more network pollution

            Thread.Sleep(1000); // Wait a few time for WhoIs responses (managed in handler_OnIam)

            BACnetDevice device1234 = GetBACnetDeviceFromId(12345);
            BACnetDevice device400001 = GetBACnetDeviceFromId(400001);

            if ((device1234==null)||(device400001==null)) // No Iam reception from one both
            {
                Console.WriteLine("device 12345 or device 400001 not present");
                return;
            }

            IList<BacnetValue> Value;

            bool ret = device1234.ReadPropertyRequest(new BacnetObjectId(BacnetObjectTypes.OBJECT_ANALOG_INPUT, 0), BacnetPropertyIds.PROP_PRESENT_VALUE, out Value);

            if (ret == true)
            {
                // Value is an array because properties could be arrays, but here only the [0] is for us
                Console.WriteLine("Read value : " + Value[0].Value.ToString());

                // BACNET_APPLICATION_TAG_xxx can be identified with Yabe : displayed in the property grid
                BacnetValue newValue = new BacnetValue(BacnetApplicationTags.BACNET_APPLICATION_TAG_REAL, Convert.ToSingle(Value[0].Value));   // expect it's a float
                BacnetValue[] NoScalarValue = { newValue };

                ret = device400001.WritePropertyRequest(new BacnetObjectId(BacnetObjectTypes.OBJECT_ANALOG_OUTPUT, 3), BacnetPropertyIds.PROP_PRESENT_VALUE, NoScalarValue);

                Console.WriteLine("Write feedback : " + ret.ToString());
            }
            else
                Console.WriteLine("Error somewhere !");
        }

        /*****************************************************************************************************/
        static void handler_OnIam(BacnetClient sender, BacnetAddress adr, uint device_id, uint max_apdu, BacnetSegmentations segmentation, ushort vendor_id)
        {
            lock (DevicesList)
            {
                // Device already registred ?
                foreach (BACnetDevice bn in DevicesList)
                    if (bn.deviceId==device_id)  return;   // Yes

                // Not already in the list
                DevicesList.Add(new BACnetDevice(sender,adr, device_id));   // add it
            }
        }
        /*****************************************************************************************************/
        static BACnetDevice GetBACnetDeviceFromId(uint device_id) 
        {
            lock (DevicesList)
            {
                foreach (BACnetDevice bn in DevicesList)
                    if (bn.deviceId == device_id) return bn; ;   // Yes  
            }
            return null;
        }
    }
}
