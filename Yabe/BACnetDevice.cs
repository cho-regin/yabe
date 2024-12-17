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
using System.IO.BACnet;

namespace Yabe
{
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

        public BacnetClient channel;
        public BacnetAddress BacAdr;  
        public uint deviceId;
        public uint vendor_Id;

        bool ReadListOneShort = true;

        // Don't sort it or it will be displayed two different ways
        public bool SortableDictionnary { get { return ReadListOneShort; } }    

        // PROP_OBJECT_LIST  cache
        uint ListCountExpected;
        IList<BacnetValue> Prop_ObjectList; 

        // Several Properties Caches (Device Name, View List, Group List, ...), only needed to displays the Dictionnary
        List<BACObjectPropertyValue> Prop_Cached=new List<BACObjectPropertyValue>();    

        public BACnetDevice(BacnetClient sender, BacnetAddress addr, uint Id, uint vendor_id=uint.MaxValue)
        {
            channel = sender;
            BacAdr = addr;
            deviceId = Id;
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
                if (vendor_Id!=uint.MaxValue) 
                    if (this.vendor_Id != other.vendor_Id)  return false;
                return true;
            }
            return false;
        }
        public override int GetHashCode() { return (int)deviceId; } // deviceId should be unique in the network 

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
            }

            if ((ListCountExpected != 0) && (ForceRead == false)) // already done
            {
                Count = ListCountExpected;
                return true;
            }

            if (ReadListOneShort==true) // If a previous test without success was done, no way to try it this way
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
                        ReadListOneShort = false;

                }
                catch { ReadListOneShort = false; }

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

        public bool ReadObjectListOneByOne(out IList<BacnetValue> value_list, uint Count, bool ForceRead=false)
        {
            value_list = null;

            // Already in the cache ?
            if (ForceRead==false)
            {
                if ((Prop_ObjectList != null) && (Prop_ObjectList.Count >= Count))
                {
                    value_list = new List<BacnetValue>();
                    value_list.Add(Prop_ObjectList[(int)(Count - 1)]);
                    return true;
                }
            }
            
            if (Prop_ObjectList==null) Prop_ObjectList=new List<BacnetValue>();

            if (Prop_ObjectList.Count != Count - 1)
                return false;   // Wrong sequence, today not required, not accepted

            value_list = null;

            try
            {
                if (!channel.ReadPropertyRequest(BacAdr, new BacnetObjectId(BacnetObjectTypes.OBJECT_DEVICE, deviceId), BacnetPropertyIds.PROP_OBJECT_LIST, out value_list, 0, Count))
                    return false;
                else
                {
                    Prop_ObjectList.Add(value_list[0]);
                    return true;
                }
            }
            catch { }

            return false;
        }

        public bool ReadPropertyRequest(out IList<BacnetValue> value_list, BacnetObjectId object_id, BacnetPropertyIds PropertyId, bool ForceRead = false)
        {
            value_list = null;

            // Already in the cache ?
            if (ForceRead == false)
            {
                foreach (BACObjectPropertyValue v in Prop_Cached)
                {
                    if ((v.objid.Equals(object_id)) && (v.PropertyIds == PropertyId))
                    {
                        value_list = v.Value_Cache;// also null
                        return true;
                    }
                }
            }

            bool ret=false;
            try
            {
                ret = channel.ReadPropertyRequest(BacAdr, object_id, PropertyId, out value_list);
            }
            catch { }

            // Push it in the cache
            foreach (BACObjectPropertyValue v in Prop_Cached)
            {
                if ((v.objid.Equals(object_id)) && (v.PropertyIds == PropertyId))
                {
                    v.Value_Cache = value_list;   // change the value
                    return ret;
                }
            }
            Prop_Cached.Add(new BACObjectPropertyValue(object_id, PropertyId, value_list));
            return ret;

        }
    }
}
