/**************************************************************************
*                           MIT License
* 
* Copyright (C) 2024 Frederic Chaxel <fchaxel@free.fr>
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
using System.Diagnostics;
using System.IO;
using System.IO.BACnet;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Yabe
{
    // One object per discovered device on the network. Referenced at least by the TreeNode.Tag in the DeviceTreeview and m_devices Dictionary
    // Used to cache some properties values & the object dictionary (PROP_OBJECT_LIST content in DEVICE object)
    public class BACnetDevice : IComparable<BACnetDevice>
    {
        class BACObjectPropertyValue
        {
            public BacnetObjectId objid;
            public BacnetPropertyIds PropertyIds;
            public IList<BacnetValue> Value_Cache;

            public BACObjectPropertyValue(BacnetObjectId objid, BacnetPropertyIds PropertyIds, IList<BacnetValue> Value_Cache)
            {
                this.objid = objid;
                this.PropertyIds = PropertyIds;
                this.Value_Cache = Value_Cache;
            }
        }

        private static Dictionary<long, string> _proprietaryPropertyMappings = null; 

        public enum ReadPopertyMultipleStatus { Unknow, Accepted, NotSupported };

        public ReadPopertyMultipleStatus ReadMultiple = ReadPopertyMultipleStatus.Unknow;

        public BacnetClient channel;
        public BacnetAddress BacAdr=new BacnetAddress(BacnetAddressTypes.None, 0, null);  
        public uint deviceId;
        public uint vendor_Id;

        bool ReadListOneShort = true;

        // Don't sort it if ReadListOneShort or it will be displayed on two different ways when getting back the cache
        public bool SortableDictionnary { get { return ReadListOneShort; } }    

        // PROP_OBJECT_LIST  cache
        uint ListCountExpected;
        IList<BacnetValue> Prop_ObjectList; 

        // Several Properties Caches (View List, Group List, ...), only needed to displays the Dictionnary, not all properties values
        List<BACObjectPropertyValue> Prop_Cached=new List<BACObjectPropertyValue>();    

        public BACnetDevice(BacnetClient sender, BacnetAddress addr, uint deviceId, uint vendor_id = System.IO.BACnet.Serialize.ASN1.BACNET_MAX_INSTANCE)
        {
            channel = sender;
            BacAdr = addr;
            this.deviceId = deviceId;
            vendor_Id = vendor_id;
        }

        public int CompareTo(BACnetDevice other)
        {
            return deviceId.CompareTo(other.deviceId); 
        }

        public override bool Equals(object obj)
        {
            if (obj is BACnetDevice other)
            {
                if (!BacAdr.Equals(other.BacAdr)) return false;
                if (this.deviceId != other.deviceId) return false;
                if (vendor_Id != System.IO.BACnet.Serialize.ASN1.BACNET_MAX_INSTANCE) 
                    if (this.vendor_Id != other.vendor_Id)  return false;
                return true;
            }
            return false;
        }
        public override int GetHashCode() { return (int)deviceId; } // deviceId should be unique in the network 

        public string FullHashString() // A kind of unique Id. Normaly deviceId is enough on a correct network
        {
            StringBuilder s = new StringBuilder(deviceId.ToString() + "_" + BacAdr.type.ToString() + BacAdr.net.ToString() + "_");

            /*
            if (BacAdr.RoutedSource != null)
                s.Append("R"); // Just add an indication
            */
            int Adrsize;
            switch (BacAdr.type)
            {
                case BacnetAddressTypes.IP:
                    Adrsize = 4;    // without Port it can be change (with this Stack)
                    break;
                case BacnetAddressTypes.Ethernet:
                    Adrsize = 6;
                    break;
                case BacnetAddressTypes.IPV6:
                    Adrsize = 16; // without Port it can be change (with this Stack)
                    break;
                case BacnetAddressTypes.MSTP:
                    Adrsize = 1;
                    break;
                case BacnetAddressTypes.SC: // SC (RandomVMAC no sens, values never the same)
                default:
                    Adrsize = 0;
                    break;
            }

            if (BacAdr.adr != null) // Normaly never null
                for (int i = 0; i < Adrsize; i++)
                    s.Append(BacAdr.adr[i].ToString("X2"));

            return s.ToString();

        }
        public void ClearCache()
        {
            Prop_Cached.Clear();
            Prop_ObjectList = null;
            ListCountExpected = 0;
        }
        public bool ReadObjectList(out IList<BacnetValue> ObjectList, out uint Count, bool ForceRead = false)
        {
            ObjectList = null;
            Count = 0;

            // Already in the cache ?
            if (ForceRead == false)
            {
                if ((Prop_ObjectList != null) && (Prop_ObjectList.Count == ListCountExpected)) // Already in the cache ?
                {
                    ObjectList = Prop_ObjectList;
                    Count = ListCountExpected;
                    return true;
                }

                if (ListCountExpected != 0)  // Quantity in the PROP_LIST is already known, don't read it again
                {
                    Count = ListCountExpected;
                    return true;
                }
            }

            if (ReadListOneShort == true) // If a previous test without success was done, no way to try it this way
            {
                try
                {
                    if (channel.ReadPropertyRequest(BacAdr, new BacnetObjectId(BacnetObjectTypes.OBJECT_DEVICE, deviceId), BacnetPropertyIds.PROP_OBJECT_LIST, out Prop_ObjectList))
                    {
                        ListCountExpected = (uint)Prop_ObjectList.Count;
                        ObjectList = Prop_ObjectList;
                        Count = ListCountExpected;
                        return true;
                    }
                    else
                        ReadListOneShort = false; // assume it's done by this

                }
                catch { ReadListOneShort = false; } // assume it's done by this 
            }

            try // Unfortunatly get List count for a One by One operation after
            {
                Prop_ObjectList = null;

                IList<BacnetValue> value_list = null;
                if (channel.ReadPropertyRequest(BacAdr, new BacnetObjectId(BacnetObjectTypes.OBJECT_DEVICE, deviceId), BacnetPropertyIds.PROP_OBJECT_LIST, out value_list, 0, 0))
                {
                    ListCountExpected = (uint)(ulong)value_list[0].Value;
                    Count = ListCountExpected;
                    return true;
                }
            }
            catch { }

            return false;
        }
        public bool ReadObjectList(out List<BacnetObjectId> ObjectList, out uint Count, bool ForceRead = false)
        {
            ObjectList = null;

            IList<BacnetValue> L;
            bool ret = ReadObjectList(out L, out Count, ForceRead);
            if (L!=null)
            {
                ObjectList=new List<BacnetObjectId>();
                foreach (var val in L)
                    ObjectList.Add((BacnetObjectId)val.Value);
            }

            return ret;
        }
        public bool ReadObjectListItem(out BacnetObjectId ObjId, uint Count, bool ForceRead = false)
        {
            ObjId=new BacnetObjectId();

            // Already in the cache ?
            if (ForceRead == false)
            {
                if ((Prop_ObjectList != null) && (Prop_ObjectList.Count >= Count))
                {
                    ObjId = (BacnetObjectId)Prop_ObjectList[(int)(Count - 1)].Value;
                    return true;
                }
            }

            if (Prop_ObjectList == null) Prop_ObjectList = new List<BacnetValue>();

            if (Prop_ObjectList.Count != Count - 1)
                return false;   // Wrong sequence, should be 1..n in order, today not required / not accepted

            IList<BacnetValue> value_list;
            try
            {
                if (!channel.ReadPropertyRequest(BacAdr, new BacnetObjectId(BacnetObjectTypes.OBJECT_DEVICE, deviceId), BacnetPropertyIds.PROP_OBJECT_LIST, out value_list, 0, Count))
                    return false;
                else
                {
                    Prop_ObjectList.Add(value_list[0]);
                    ObjId = (BacnetObjectId)value_list[0].Value;
                    return true;
                }
            }
            catch { }

            return false;
        }
        
        public bool ReadCachablePropertyRequest(out IList<BacnetValue> value_list, BacnetObjectId object_id, BacnetPropertyIds PropertyId, bool ForceRead = false)
        {
            value_list = null;
            BACObjectPropertyValue Property = null;

            try {Property = Prop_Cached.First(o => (o.PropertyIds == PropertyId)&&(o.objid.Equals(object_id))); } catch { }

            // Already in the cache ?
            if ((ForceRead == false)&&(Property != null))
            {
                value_list = Property.Value_Cache;// also null
                return true;
            }

            bool ret=false;
            try
            {
                ret = channel.ReadPropertyRequest(BacAdr, object_id, PropertyId, out value_list);
            }
            catch { }

            // Change the value or Push it in the cache
            if (Property != null)
                Property.Value_Cache = value_list;   // change the value
            else
                Prop_Cached.Add(new BACObjectPropertyValue(object_id, PropertyId, value_list));

            return ret;
        }

        public static bool LoadVendorPropertyMapping()
        {
            _proprietaryPropertyMappings = new Dictionary<long, string>();

            string path = Properties.Settings.Default.Proprietary_Properties_Files;

            // helper function to log erros
            void LogError(string message)
            {
                Trace.TraceError($"Invalid line in vendor proprietary BACnet properties file \"{path}\". {message}");
            }

            string[] lines;
            try
            {
                // get all lines from the file
                lines = File.ReadAllLines(path, Encoding.UTF8);
            }
            catch
            {
                Trace.TraceError("Cannot read Vendor proprietary BACnet file");
                return false;
            }

            bool firstLine = true;

            // parse each line
            foreach (var line in lines)
            {
                // use the first line to detect the format
                if (firstLine)
                {
                    // check the first line strictly so that we can verify that it is the new format
                    if (!Regex.Match(line, @"^Vendor ID[,;]Property ID[,;]Property Name$").Success)
                    {
                        // return an indication that we do not have handled this file
                        return false;
                    }
                    firstLine = false;
                    continue;
                }

                // parse a line with a vendor property mapping
                var match = Regex.Match(line, @"^(\d+)[,;](\d+)[,;]([^,;]+)"); // Allow here more than 3 columns, e.g. for an editorial comment in the file
                if (!match.Success)
                {
                    LogError($"The row \"'{line}\" is not a valid mapping.");
                    continue;
                }

                // parse the vendor ID
                if (!ushort.TryParse(match.Groups[1].Value, out var vendorId))
                {
                    LogError($"The value {match.Groups[1].Value} is not a valid vendor ID number.");
                    continue;
                }

                // parse the property ID
                if (!uint.TryParse(match.Groups[2].Value, out var propertyId))
                {
                    LogError($"The value {match.Groups[2].Value} is not a valid property ID number.");
                    continue;
                }

                // combine the vendor ID and the property ID so that be can store it in our central mapping list
                var vendorPropertyNumber = ((long)vendorId << 32) | propertyId;
                if (_proprietaryPropertyMappings.ContainsKey(vendorPropertyNumber))
                {
                    LogError($"The property ID {propertyId} of vendor ID {vendorId} is already defined as \"{_proprietaryPropertyMappings[vendorPropertyNumber]}\".");
                    continue;
                }
                _proprietaryPropertyMappings.Add(vendorPropertyNumber, match.Groups[3].Value);
            }

            // return an indication that we have handled this file
            return true;
        }

        public string GetNiceName(BacnetPropertyIds property, bool forceShowNumber = false)
        {

            if (_proprietaryPropertyMappings == null)   // First call
                LoadVendorPropertyMapping();

            bool prependNumber = forceShowNumber || Properties.Settings.Default.Show_Property_Id_Numbers;
            string name = property.ToString();
            if (name.StartsWith("PROP_"))
            {
                name = name.Substring(5);
                name = name.Replace('_', ' ');
                if (prependNumber)
                {
                    name = String.Format("{0}: {1}", (int)property, System.Threading.Thread.CurrentThread.CurrentCulture.TextInfo.ToTitleCase(name.ToLower()));
                }
                else
                {
                    name = System.Threading.Thread.CurrentThread.CurrentCulture.TextInfo.ToTitleCase(name.ToLower());
                }
            }
            else
            {
                var vendorPropertyNumber = ((long)vendor_Id << 32) | (uint)property;
                if (_proprietaryPropertyMappings.TryGetValue(vendorPropertyNumber, out var vendorPropertyName))
                {
                    name = vendorPropertyName;
                }

                if (name != null)
                {
                    if (prependNumber)
                    {
                        name = String.Format("Proprietary {0}: {1}", (int)property, System.Threading.Thread.CurrentThread.CurrentCulture.TextInfo.ToTitleCase(name.ToLower()));
                    }
                    else
                    {
                        name = "Proprietary: " + System.Threading.Thread.CurrentThread.CurrentCulture.TextInfo.ToTitleCase(name.ToLower());
                    }
                }
                else
                {
                    name = String.Format("Proprietary: {0}", (int)property);
                }
            }
            return name;
        }
    }
}
