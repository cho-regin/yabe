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
using SharpPcap;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.BACnet;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Serialization;

namespace Yabe
{
    // One object per discovered device on the network.
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
        private static List<BacnetObjectDescription> objectsDescriptionExternal, objectsDescriptionDefault;
        // Memory of all object names already discovered, first string in the Tuple is the device network address hash
        // The tuple contains two value types, so it's ok for cross session
        public static Dictionary<Tuple<String, BacnetObjectId>, String> DevicesObjectsName = new Dictionary<Tuple<String, BacnetObjectId>, String>();
        public static bool objectNamesChangedFlag = false;
        public enum ReadPopertyMultipleStatus { Unknow, Accepted, NotSupported };

        public ReadPopertyMultipleStatus ReadMultiple = ReadPopertyMultipleStatus.Unknow;

        public BacnetClient channel;
        public BacnetAddress BacAdr=new BacnetAddress(BacnetAddressTypes.None, 0, null);  
        public uint deviceId;
        public uint vendor_Id;

        // Updated during the dialog
        public int MaxAPDULenght = -1; 
        public BacnetSegmentations Segmentation = BacnetSegmentations.SEGMENTATION_UNKNOW;

        bool ReadListOneShort = true;

        // Don't sort it if ReadListOneShort or it will be displayed on two different ways when getting back the cache
        public bool SortableDictionnary { get { return ReadListOneShort; } }    

        // PROP_OBJECT_LIST  cache
        uint ListCountExpected;
        List<BacnetObjectId> Prop_ObjectList; 

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
                return true;
            }
            return false;
        }
        public override int GetHashCode() { return (int)deviceId; } // deviceId should be unique in the network 

        private string FullHashString() // A kind of unique Id. Normaly deviceId is enough on a correct network
        {

            BacnetAddress Addr;

            if (BacAdr.RoutedSource != null)
                Addr = BacAdr.RoutedSource;
            else
                Addr = BacAdr;

            StringBuilder s = new StringBuilder(deviceId.ToString() + "_" + Addr.type.ToString() + Addr.net.ToString() + "_");

            int Adrsize;
            switch (Addr.type)
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
                    Adrsize = 0;
                    break;
                default:
                    Adrsize = Addr.adr.Length;
                    break;
            }

            if (Addr.adr != null) // Normaly never null
                for (int i = 0; i < Adrsize; i++)
                    s.Append(Addr.adr[i].ToString("X2"));

            return s.ToString();

        }

        public BacnetAddressTypes GetNetworkType()  // Can be used to adjust packets size
        {
            if (BacAdr.RoutedSource != null)
               return BacAdr.RoutedSource.type;
            else
                return BacAdr.type;
        }

        public void ClearCache()
        {
            Prop_Cached.Clear();
            Prop_ObjectList = null;
            ListCountExpected = 0;
        }
        public bool ReadObjectList(out List<BacnetObjectId> ObjectList, out uint Count, bool ForceRead = false)
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
                    IList<BacnetValue> value_list;
                    if (channel.ReadPropertyRequest(BacAdr, new BacnetObjectId(BacnetObjectTypes.OBJECT_DEVICE, deviceId), BacnetPropertyIds.PROP_OBJECT_LIST, out value_list))
                    {
                        ListCountExpected = (uint)value_list.Count;
                        Prop_ObjectList = new List<BacnetObjectId>();
                        foreach (var val in value_list)
                            Prop_ObjectList.Add((BacnetObjectId)val.Value);

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
        
        public bool ReadObjectListItem(out BacnetObjectId ObjId, uint Count, bool ForceRead = false)
        {
            ObjId=new BacnetObjectId();

            // Already in the cache ?
            if (ForceRead == false)
            {
                if ((Prop_ObjectList != null) && (Prop_ObjectList.Count >= Count))
                {
                    ObjId = (BacnetObjectId)Prop_ObjectList[(int)(Count - 1)];
                    return true;
                }
            }

            if (Prop_ObjectList == null) Prop_ObjectList = new List<BacnetObjectId>();

            if (Prop_ObjectList.Count != Count - 1)
                return false;   // Wrong sequence, should be 1..n in order, today not required / not accepted

            try
            {
                IList<BacnetValue> value_list;
                if (!channel.ReadPropertyRequest(BacAdr, new BacnetObjectId(BacnetObjectTypes.OBJECT_DEVICE, deviceId), BacnetPropertyIds.PROP_OBJECT_LIST, out value_list, 0, Count))
                    return false;
                else
                {
                    Prop_ObjectList.Add((BacnetObjectId)value_list[0].Value);
                    ObjId = (BacnetObjectId)value_list[0].Value;
                    return true;
                }
            }
            catch { }

            return false;
        }
        
        // Here it is expected that the given properties (array) can be read in one shot. Nothing done if it's not possible
        // But group and view lists are "normaly" small to fit even in a mstp frame 
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
        public bool ReadProperty(BacnetObjectId object_id, BacnetPropertyIds property_id, ref IList<BacnetPropertyValue> values, uint array_index = System.IO.BACnet.Serialize.ASN1.BACNET_ARRAY_ALL)
        {
            BacnetPropertyValue new_entry = new BacnetPropertyValue();
            new_entry.property = new BacnetPropertyReference((uint)property_id, array_index);
            IList<BacnetValue> value;
            try
            {
                if (!channel.ReadPropertyRequest(BacAdr, object_id, property_id, out value, 0, array_index))
                    return false;     //ignore
            }
            catch
            {
                return false;         //ignore
            }
            new_entry.value = value;

            values.Add(new_entry);
            return true;
        }

        public bool ReadAllPropertiesBySingle(BacnetObjectId object_id, out IList<BacnetReadAccessResult> value_list)
        {

            if (objectsDescriptionDefault == null)  // first call, Read Objects description from internal & optional external xml file
                LoadObjectsDescription();

            value_list = null;

            IList<BacnetPropertyValue> values = new List<BacnetPropertyValue>();

            int old_retries = channel.Retries;
            channel.Retries = 1;       //we don't want to spend too much time on non existing properties
            try
            {
                // PROP_LIST was added as an addendum to 135-2010
                // Test to see if it is supported, otherwise fall back to the the predefined delault property list.
                bool objectDidSupplyPropertyList = ReadProperty(object_id, BacnetPropertyIds.PROP_PROPERTY_LIST, ref values);

                //Used the supplied list of supported Properties, otherwise fall back to using the list of default properties.
                if (objectDidSupplyPropertyList)
                {
                    var proplist = values.Last();

                    foreach (var enumeratedValue in proplist.value)
                    {
                        BacnetPropertyIds bpi = (BacnetPropertyIds)Convert.ToInt32(enumeratedValue.Value);
                        // read all specified properties given by the PROP_PROPERTY_LIST, except the 3 previous one
                        ReadProperty(object_id, bpi, ref values);
                    }

                    // 3 required properties not in the list

                    // ReadProperty(comm, adr, object_id, BacnetPropertyIds.PROP_OBJECT_IDENTIFIER, ref values)
                    // No need to query it, known value
                    BacnetPropertyValue new_entry = new BacnetPropertyValue();
                    new_entry.property = new BacnetPropertyReference((uint)BacnetPropertyIds.PROP_OBJECT_IDENTIFIER, System.IO.BACnet.Serialize.ASN1.BACNET_ARRAY_ALL);
                    new_entry.value = new BacnetValue[] { new BacnetValue(object_id) };
                    values.Add(new_entry);

                    // ReadProperty(comm, adr, object_id, BacnetPropertyIds.PROP_OBJECT_TYPE, ref values);
                    // No need to query it, known value
                    new_entry = new BacnetPropertyValue();
                    new_entry.property = new BacnetPropertyReference((uint)BacnetPropertyIds.PROP_OBJECT_TYPE, System.IO.BACnet.Serialize.ASN1.BACNET_ARRAY_ALL);
                    new_entry.value = new BacnetValue[] { new BacnetValue(BacnetApplicationTags.BACNET_APPLICATION_TAG_ENUMERATED, (uint)object_id.type) };
                    values.Add(new_entry);
                    // We do not know the value here
                    ReadProperty(object_id, BacnetPropertyIds.PROP_OBJECT_NAME, ref values);
                }
                else
                {
                    // Three mandatory common properties to all objects : PROP_OBJECT_IDENTIFIER,PROP_OBJECT_TYPE, PROP_OBJECT_NAME

                    // ReadProperty(comm, adr, object_id, BacnetPropertyIds.PROP_OBJECT_IDENTIFIER, ref values)
                    // No need to query it, known value
                    BacnetPropertyValue new_entry = new BacnetPropertyValue();
                    new_entry.property = new BacnetPropertyReference((uint)BacnetPropertyIds.PROP_OBJECT_IDENTIFIER, System.IO.BACnet.Serialize.ASN1.BACNET_ARRAY_ALL);
                    new_entry.value = new BacnetValue[] { new BacnetValue(object_id) };
                    values.Add(new_entry);

                    // ReadProperty(comm, adr, object_id, BacnetPropertyIds.PROP_OBJECT_TYPE, ref values);
                    // No need to query it, known value
                    new_entry = new BacnetPropertyValue();
                    new_entry.property = new BacnetPropertyReference((uint)BacnetPropertyIds.PROP_OBJECT_TYPE, System.IO.BACnet.Serialize.ASN1.BACNET_ARRAY_ALL);
                    new_entry.value = new BacnetValue[] { new BacnetValue(BacnetApplicationTags.BACNET_APPLICATION_TAG_ENUMERATED, (uint)object_id.type) };
                    values.Add(new_entry);

                    // We do not know the value here
                    ReadProperty(object_id, BacnetPropertyIds.PROP_OBJECT_NAME, ref values);

                    // for all other properties, the list is comming from the internal or external XML file

                    BacnetObjectDescription objDescr = new BacnetObjectDescription(); ;

                    int Idx = -1;
                    // try to find the Object description from the optional external xml file
                    if (objectsDescriptionExternal != null)
                        Idx = objectsDescriptionExternal.FindIndex(o => o.typeId == object_id.type);

                    if (Idx != -1)
                        objDescr = objectsDescriptionExternal[Idx];
                    else
                    {
                        // try to find from the embedded resoruce
                        Idx = objectsDescriptionDefault.FindIndex(o => o.typeId == object_id.type);
                        if (Idx != -1)
                            objDescr = objectsDescriptionDefault[Idx];
                    }

                    if (Idx != -1)
                        foreach (BacnetPropertyIds bpi in objDescr.propsId)
                            // read all specified properties given by the xml file
                            ReadProperty(object_id, bpi, ref values);
                }
            }
            catch { }

            channel.Retries = old_retries;
            value_list = new BacnetReadAccessResult[] { new BacnetReadAccessResult(object_id, values) };
            return true;
        }

        // Multiple properties on a single object
        public bool ReadPropertyMultipleRequest(BacnetObjectId object_id, IList<BacnetPropertyReference> properties, out IList<BacnetReadAccessResult> multi_value_list) => channel.ReadPropertyMultipleRequest(BacAdr, object_id, properties, out multi_value_list);
        // Multiple properties on multiple objects
        public bool ReadPropertyMultipleRequest(IList<BacnetReadAccessSpecification> properties, out IList<BacnetReadAccessResult> values) => channel.ReadPropertyMultipleRequest(BacAdr, properties, out values);
        public bool WritePropertyRequest(BacnetObjectId object_id, BacnetPropertyIds property_id, IEnumerable<BacnetValue> value_list) => channel.WritePropertyRequest(BacAdr, object_id, property_id, value_list);
        public bool ReadPropertyRequest(BacnetObjectId object_id, BacnetPropertyIds property_id, out IList<BacnetValue> value_list) => channel.ReadPropertyRequest(BacAdr, object_id, property_id, out value_list);
        public void SimpleAckResponse(BacnetConfirmedServices service, byte invoke_id) => channel.SimpleAckResponse(BacAdr, service, invoke_id);
        public bool SubscribeCOVRequest(BacnetObjectId object_id, uint subscribe_id, bool cancel, bool issue_confirmed_notifications, uint lifetime)=>channel.SubscribeCOVRequest(BacAdr, object_id, subscribe_id, cancel, issue_confirmed_notifications, lifetime);
        public static void LoadObjectsDescription()
        {
            // Use to read object properties when ReadMultiple is not accepted (very simple devices on MSTP without segmentation)
            StreamReader sr;
            XmlSerializer xs = new XmlSerializer(typeof(List<BacnetObjectDescription>));

            // embedded resource
            System.Reflection.Assembly _assembly;
            _assembly = System.Reflection.Assembly.GetExecutingAssembly();
            sr = new StreamReader(_assembly.GetManifestResourceStream("Yabe.ReadSinglePropDescrDefault.xml"));
            objectsDescriptionDefault = (List<BacnetObjectDescription>)xs.Deserialize(sr);

            try  // External optional file
            {
                sr = new StreamReader("ReadSinglePropDescr.xml");
                objectsDescriptionExternal = (List<BacnetObjectDescription>)xs.Deserialize(sr);
            }
            catch { }

        }
        public static bool LoadVendorPropertyMapping() // Daniel Evers, BOSCH
        {
            _proprietaryPropertyMappings = new Dictionary<long, string>();

            string path = "VendorPropertyMapping.csv";

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
                // silently
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

                if (_proprietaryPropertyMappings == null)   // First call
                    LoadVendorPropertyMapping();

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

        #region "Cross session Object Name dictionnary"
        public void UpdateObjectNameMapping(BacnetObjectId Bacobj, string Name)
        {
            if ((Name == null) || (Name == "")) return;
            
            lock (DevicesObjectsName)
            {
                Tuple<String, BacnetObjectId> t = new Tuple<String, BacnetObjectId>(FullHashString(), Bacobj);
                if (DevicesObjectsName.ContainsKey(t))
                {
                    if (!DevicesObjectsName[t].Equals(Name))
                    {
                        DevicesObjectsName.Remove(t);
                        DevicesObjectsName.Add(t, Name);
                        objectNamesChangedFlag = true;
                    }
                }
                else
                {
                    DevicesObjectsName.Add(t, Name);
                    objectNamesChangedFlag = true;
                }
            }
        }

        /// <summary>
        /// Provides the name if it is in the cache, uses ReadObjectName if value is mandatory
        /// </summary>
        public String GetObjectName(BacnetObjectId object_id)
        {
            String Name = null;
            lock (DevicesObjectsName)
                DevicesObjectsName.TryGetValue(new Tuple<String, BacnetObjectId>(FullHashString(), object_id), out Name);
            return Name;
        }
        /// <summary>
        /// Uses GetObjectName with network access if required 
        /// </summary>
        public string ReadObjectName(BacnetObjectId object_id, bool ForceRead = false)
        {
            if (ForceRead == false)
            {
                String Name;
                lock (DevicesObjectsName)
                    if (DevicesObjectsName.TryGetValue(new Tuple<String, BacnetObjectId>(FullHashString(), object_id), out Name))
                        return Name;
            }
            try
            {
                IList<BacnetValue> value;
                if (!ReadPropertyRequest(object_id, BacnetPropertyIds.PROP_OBJECT_NAME, out value))
                    return "";
                if (value == null || value.Count == 0)
                    return "";
                else
                {
                    UpdateObjectNameMapping(object_id, value[0].Value.ToString());
                    return value[0].Value.ToString();
                }
            }
            catch
            {
                return "";
            }
        }
        #endregion
    }
}
