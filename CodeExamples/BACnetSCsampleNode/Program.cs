/**************************************************************************
*                           MIT License
*
* Copyright (C) 2022 Frederic Chaxel <fchaxel@free.fr
* Yabe SourceForge Explorer and Full Open source Stack
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
using System.IO;
using System.Text;

namespace BACnetSCsampleNode
{
    //
    // A very simple read/write client code based on Yabe code
    //
    class Program
    {
        static BacnetClient bacnet_client;

        static uint DeviceId = 54000;

        // All the present Bacnet Device List
        static List<BacNode> DevicesList = new List<BacNode>();

        /*****************************************************************************************************/
        static void Main(string[] args)
        {
            Trace.Listeners.Add(new ConsoleTraceListener());

            try
            {
                StartActivity();
                Console.WriteLine("Started");

                Thread.Sleep(2000); // Wait a fiew time for WhoIs responses (managed in handler_OnIam)

                ReadExample();
            }
            catch { }

            Console.ReadKey();            
        }
        /*****************************************************************************************************/
        static void StartActivity()
        {
            // Bacnet SC unSecure Channel with the Hub ws://127.0.0.1:4443
            // work well with Testhub from https://sourceforge.net/projects/bacnet-sc-reference-stack/
           
            BACnetSCConfigChannel config = new BACnetSCConfigChannel()
            {
                primaryHubURI = "ws://127.0.0.1:4443",
                UUID = "{92fb9be8-bac0-0000-0cab-171d5ec08e6c}",
                // For certificates Files reference or X509 (X509Certificate2 objects) can be set here
            };

            bacnet_client = new BacnetClient(new BACnetTransportSecureConnect(config));

            // Configuration could also be done with a File side the .exe, see BACnetSCConfig.config in Yabe directory
            // StreamReader sr = new StreamReader("BACnetSCConfig.config");
            // bacnet_client = new BacnetClient(new BACnetTransportSecureConnect(sr.BaseStream));

            // Send WhoIs in order to get back all the Iam responses :  
            bacnet_client.OnIam += new BacnetClient.IamHandler(handler_OnIam);
            bacnet_client.OnWhoIs += new BacnetClient.WhoIsHandler(handler_OnWhoIs);

            bacnet_client.Start();    // go

            bacnet_client.WhoIs();
        }
        /*****************************************************************************************************/
        static void ReadExample()
        {

            BacnetValue Value;
            bool ret;
            // Read Description property on the object DEVICE provided by the device 666001
            // Scalar value only
            ret = ReadScalarValue(666001, new BacnetObjectId(BacnetObjectTypes.OBJECT_DEVICE, 666001), BacnetPropertyIds.PROP_DESCRIPTION, out Value);

            if (ret == true)
                Console.WriteLine("Read value : " + Value.Value.ToString());
            else
                Console.WriteLine("Error somewhere !");
        }

        /*****************************************************************************************************/
        static void handler_OnIam(BacnetClient sender, BacnetAddress adr, uint device_id, uint max_apdu, BacnetSegmentations segmentation, ushort vendor_id)
        {
            lock (DevicesList)
            {
                // Device already registred ?
                foreach (BacNode bn in DevicesList)
                    if (bn.getAdd(device_id) != null) return;   // Yes

                // Not already in the list
                DevicesList.Add(new BacNode(adr, device_id));   // add it
            }
        }
        /*****************************************************************************************************/
        static void handler_OnWhoIs(BacnetClient sender, BacnetAddress adr, int low_limit, int high_limit)
        {
            if (low_limit != -1 && DeviceId < low_limit) return;
            else if (high_limit != -1 && DeviceId > high_limit) return;
            sender.Iam(DeviceId, BacnetSegmentations.SEGMENTATION_BOTH, 61440);
        }

        /*****************************************************************************************************/
        static bool ReadScalarValue(int device_id, BacnetObjectId BacnetObjet, BacnetPropertyIds Propriete, out BacnetValue Value)
        {
            BacnetAddress adr;
            IList<BacnetValue> NoScalarValue;

            Value = new BacnetValue(null);

            // Looking for the device
            adr=DeviceAddr((uint)device_id);
            if (adr == null) return false;  // not found

            // Property Read
            if (bacnet_client.ReadPropertyRequest(adr, BacnetObjet, Propriete, out NoScalarValue)==false)
                return false;

            Value = NoScalarValue[0];
            return true;
        }

        /*****************************************************************************************************/
        static bool WriteScalarValue(int device_id, BacnetObjectId BacnetObjet, BacnetPropertyIds Propriete, BacnetValue Value)
        {
            BacnetAddress adr;

            // Looking for the device
            adr = DeviceAddr((uint)device_id);
            if (adr == null) return false;  // not found

            // Property Write
            BacnetValue[] NoScalarValue = { Value };
            if (bacnet_client.WritePropertyRequest(adr, BacnetObjet, Propriete, NoScalarValue) == false)
                return false;

            return true;
        }

        /*****************************************************************************************************/
        static BacnetAddress DeviceAddr(uint device_id)
        {
            BacnetAddress ret;

            lock (DevicesList)
            {
                foreach (BacNode bn in DevicesList)
                {
                    ret = bn.getAdd(device_id);
                    if (ret != null) return ret;
                }
                // not in the list
                return null;
            }
        }
    }

    class BacNode
    {
        BacnetAddress adr;
        uint device_id;

        public BacNode(BacnetAddress adr, uint device_id)
        {
            this.adr = adr;
            this.device_id = device_id;
        }

        public BacnetAddress getAdd(uint device_id)
        {
            if (this.device_id == device_id)
                return adr;
            else
                return null;
        }
    }
}
