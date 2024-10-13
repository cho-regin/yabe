/**************************************************************************
*                           MIT License
* 
* Copyright (C) 2014 Morten Kvistgaard <mk@pch-engineering.dk>
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
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;
using System.IO.BACnet;
using System.IO;
using System.IO.BACnet.Storage;
using System.Xml.Serialization;
using System.Threading;
using System.Runtime.Serialization.Formatters.Binary;
using System.Media;
using System.Linq;
using System.Collections;
using System.Reflection;
using ZedGraph;
using WebSocketSharp;
using System.Net;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Utilities;

namespace Yabe
{
    public partial class YabeMainDialog : Form
    {
        private const int MIN_POLL_PERIOD = 100; //ms
        private const int MAX_POLL_PERIOD = 120000; //ms

        private object _selectedNode = null;
        private TreeNode _selectedDevice = null;

        // 0=Offnormal
        // 1=Fault
        // 2=Normal
        private DateTime[] _cachedEventTimeStampsForAcknowledgementButtons = new DateTime[3];

        public int DeviceCount 
        { 
            get {
                int count = 0;

                foreach (var entry in m_devices)
                    count = count + entry.Value.Devices.Count;

                return count; 
            }
        }
        public IEnumerable<TreeNode> DeviceNodes
        {
            get
            {
                // Enumerate each Transport Layer:
                foreach (TreeNode transport in m_DeviceTree.Nodes[0].Nodes)
                {
                    //Enumerate each Parent Device:
                    foreach (TreeNode node in transport.Nodes)
                    {
                        if (node.Tag is BACnetDevice endPoint)
                            yield return (node);
                    }
                }
                yield break;
            }
        }
        public BACnetDevice SelectedDevice => (_selectedDevice?.Tag as BACnetDevice);


        private Dictionary<string, ListViewItem> m_subscription_list = new Dictionary<string, ListViewItem>();
        private Dictionary<string, RollingPointPairList> m_subscription_points = new Dictionary<string, RollingPointPairList>();        
        Color[] GraphColor = {Color.Red, Color.Blue, Color.Green, Color.Violet, Color.Chocolate, Color.Orange};
        GraphPane Pane;
        private ManualResetEvent _plotterPause;
        private bool _plotterRunningFlag = true; // Change this one initial value to make the graphs start paused (false) or in play mode (true).
        private const string PLAY_BUTTON_TEXT_WHEN_RUNNING = "Pause Plotter";
        private const string PLAY_BUTTON_TEXT_WHEN_PAUSED = "Resume Plotter";
        private Random _rand = new Random();

        // Memory of all object names already discovered, first string in the Tuple is the device network address hash
        // The tuple contains two value types, so it's ok for cross session
        public Dictionary<Tuple<String, BacnetObjectId>, String> DevicesObjectsName = new Dictionary<Tuple<String, BacnetObjectId>, String>();

        public bool objectNamesChangedFlag = false;

        public BACnetDevice[] DiscoveredDevices
        {
            get
            {
                lock (m_devices)
                {
                    return (m_devices.Values.SelectMany(line => line.Devices).ToArray());
                }
            }
        }
        private Dictionary<BacnetClient, BACnetDeviceLine> m_devices = new Dictionary<BacnetClient, BACnetDeviceLine>();

        private uint m_next_subscription_id = 0;

        private static DeviceStorage m_storage;
        private List<BacnetObjectDescription> objectsDescriptionExternal, objectsDescriptionDefault;

        YabeMainDialog yabeFrm; // Ref to itself, already affected, usefull for plugin developpmenet inside this code, before exporting it

        private Dictionary<long, string> _proprietaryPropertyMappings = new Dictionary<long, string>();

        private bool LoadVendorPropertyMapping(string path)
        {
            // get all lines from the file
            var lines = File.ReadAllLines(path, Encoding.UTF8);
            bool firstLine = true;

            // helper function to log erros
            void LogError(string message)
            {
                Trace.TraceError($"Invalid line in vendor proprietary BACnet properties file \"{path}\". {message}");
            }

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

            // return an indication that be have handled this file
            return true;
        }


        /// <summary>
        /// Resolves the corresponding <see cref="BACnetDevice"/> from a <paramref name="client"/> and an <paramref name="address"/>.
        /// </summary>
        private BACnetDevice GetDevice(BacnetClient client, BacnetAddress address)
        {
            lock (m_devices)
            {
                return (m_devices[client].Devices.First(dev => dev.Address.Equals(address)));
            }
        }

        public void LoadProprietaryProperties()
        {
            _proprietaryPropertyMappings.Clear();
            if(string.IsNullOrWhiteSpace(Properties.Settings.Default.Proprietary_Properties_Files))
            {
                return;
            }

            string[] filePaths = Properties.Settings.Default.Proprietary_Properties_Files.Split(';',',');

            foreach(string filePath in filePaths)
            {
                if (string.IsNullOrWhiteSpace(filePath))
                {
                    continue;
                }

                try
                {
                    if (!File.Exists(filePath))
                    {
                        Trace.TraceError(String.Format("Failed to open proprietary Bacnet properties file \"{0}\": {1}", filePath, "The file does not exist."));
                        continue;
                    }

                    // attempt to load the mapping in the new format
                    if (LoadVendorPropertyMapping(filePath))
                    {
                        continue;
                    }

                    using (StreamReader reader = new StreamReader(filePath))
                    {
                        bool moreLinesToRead = true;

                        reader.ReadLine(); // Read out the CSV description line

                        while (moreLinesToRead)
                        {
                            string mapping = reader.ReadLine();
                            if(mapping==null)
                            {
                                moreLinesToRead = false;
                                continue;
                            }

                            if(string.IsNullOrWhiteSpace(mapping))
                            {
                                continue;
                            }

                            string[] mappingParts = mapping.Trim().Split(new char[] {';',','}, 2);

                            if(mappingParts.Length<2)
                            {
                                Trace.TraceError(String.Format("Invalid line in proprietary Bacnet properties file \"{0}\" - \"{1}\" is not a valid mapping.", filePath, mapping));
                                continue;
                            }

                            int propIdNumber;
                            try
                            {
                                propIdNumber = Convert.ToInt32(mappingParts[0]);
                            }
                            catch(OverflowException overflowEx)
                            {
                                Trace.TraceError(String.Format("Invalid line in proprietary Bacnet properties file \"{0}\" - \"{1}\" is not a valid property ID number ({2}).", filePath, mappingParts[0], overflowEx.Message));
                                continue;
                            }
                            catch(FormatException formatEx)
                            {
                                Trace.TraceError(String.Format("Invalid line in proprietary Bacnet properties file \"{0}\" - \"{1}\" is not a valid property ID number ({2}).", filePath, mappingParts[0], formatEx.Message));
                                continue;
                            }
                            string propDescription = mappingParts[1].Trim();
                            
                            if(propDescription.StartsWith("\"") && propDescription.EndsWith("\""))
                            {
                                propDescription = propDescription.Substring(1,propDescription.Length-2);
                            }

                            if(_proprietaryPropertyMappings.ContainsKey(propIdNumber))
                            {
                                // If we have the same ID number defined twice, take the very first one and spit out a warning for the rest.
                                Trace.TraceError(String.Format("Warning: duplicate proprietary property definition in \"{0}\" ID number {1} is already defined as \"{2}\" (attemped to redefine as \"{3}\").", filePath, mappingParts[0], _proprietaryPropertyMappings[propIdNumber],propDescription));
                                continue;
                            }
                            else
                            {
                                _proprietaryPropertyMappings.Add(propIdNumber,propDescription);
                            }

                        }
                    }
                }
                catch (Exception ex)
                {
                    Trace.TraceError(String.Format("Failed to open proprietary Bacnet properties file \"{0}\": {1}", filePath, ex.GetType().Name + " - " + ex.Message));
                    continue;
                }
            }
        }
        public bool GetSetting_TimeSynchronize_UTC()
        {
            return Properties.Settings.Default.TimeSynchronize_UTC;
        }

        public string GetProprietaryPropertyName(int id)
        {
            if (_proprietaryPropertyMappings.ContainsKey(id))
            {
                return _proprietaryPropertyMappings[id];
            }
            return null;
        }

        private int AsynchRequestId=0;

        public class BACnetDeviceLine
        {
            public BACnetDeviceLine(BacnetClient client)
            {
                this.Client = client;
            }


            public BacnetClient Client { get; }
            public List<BACnetDevice> Devices { get; } = new List<BACnetDevice>();
            public HashSet<byte> mstp_sources_seen { get; } = new HashSet<byte>();
            public HashSet<byte> mstp_pfm_destinations_seen { get; } = new HashSet<byte>();
        }

        public YabeMainDialog()
        {
            yabeFrm = this;

            try
            {
                if (Properties.Settings.Default.SettingsUpgradeRequired)
                {
                    Properties.Settings.Default.Upgrade();
                    Properties.Settings.Default.SettingsUpgradeRequired = false;
                    Properties.Settings.Default.Save();
                }
            }
            catch   // Corrupted xml file
            { 
                Properties.Settings.Default.Reset();
                Properties.Settings.Default.SettingsUpgradeRequired = false;    // could be previous version
                Properties.Settings.Default.Save();
            }

            InitializeComponent();
            Trace.Listeners.Add(new MyTraceListener(this));

            LoadProprietaryProperties();

            if (_plotterRunningFlag)
            {
                btnPlay.Text = PLAY_BUTTON_TEXT_WHEN_RUNNING;
            }
            else
            {
                btnPlay.Text = PLAY_BUTTON_TEXT_WHEN_PAUSED;
            }

            pollRateSelector.Minimum = MIN_POLL_PERIOD;
            pollRateSelector.Maximum = MAX_POLL_PERIOD;
            pollRateSelector.Value = Math.Max(MIN_POLL_PERIOD, Math.Min(Properties.Settings.Default.Subscriptions_ReplacementPollingPeriod, MAX_POLL_PERIOD));

            pollRateSelector.Enabled = Properties.Settings.Default.UsePollingByDefault;
            CovOpn.Checked = !Properties.Settings.Default.UsePollingByDefault;
            PollOpn.Checked = Properties.Settings.Default.UsePollingByDefault;

            m_DeviceTree.ExpandAll();

            // COV Graph
            Pane = CovGraph.GraphPane;
            Pane.Title.Text = null;
            CovGraph.IsShowPointValues = true;
            // X Axis
            Pane.XAxis.Type = AxisType.Date;
            Pane.XAxis.Title.Text = null;
            Pane.XAxis.MajorGrid.IsVisible = true;
            Pane.XAxis.MajorGrid.Color = Color.Gray;
            // Y Axis
            Pane.YAxis.Title.Text = null;
            Pane.YAxis.MajorGrid.IsVisible = true;
            Pane.YAxis.MajorGrid.Color = Color.Gray;
            CovGraph.AxisChange();
            CovGraph.IsAutoScrollRange = true;

            _plotterPause = new ManualResetEvent(_plotterRunningFlag);
            CovGraph.PointValueEvent += new ZedGraphControl.PointValueHandler(CovGraph_PointValueEvent);

            //load splitter setup & SubsciptionView columns order&size
            try
            {

                if (Properties.Settings.Default.GUI_FormSize != new Size(0, 0))
                    this.Size = Properties.Settings.Default.GUI_FormSize;
                FormWindowState state = (FormWindowState)Enum.Parse(typeof(FormWindowState), Properties.Settings.Default.GUI_FormState);
                if (state != FormWindowState.Minimized)
                    this.WindowState = state;
                if (Properties.Settings.Default.GUI_SplitterButtom != -1)
                    m_SplitContainerButtom.SplitterDistance = Properties.Settings.Default.GUI_SplitterButtom;
                if (Properties.Settings.Default.GUI_SplitterMiddle != -1)
                    m_SplitContainerLeft.SplitterDistance = Properties.Settings.Default.GUI_SplitterMiddle;
                if (Properties.Settings.Default.GUI_SplitterLeft != -1)
                    splitContainer4.SplitterDistance = Properties.Settings.Default.GUI_SplitterLeft;
                if (Properties.Settings.Default.GUI_SplitterRight != -1)
                    m_SplitContainerRight.SplitterDistance = Properties.Settings.Default.GUI_SplitterRight;
                
                if(Properties.Settings.Default.Vertical_Object_Splitter_Orientation)
                {
                    splitContainer4.Orientation = Orientation.Vertical;
                }
                else
                {
                    splitContainer4.Orientation = Orientation.Horizontal;
                }

                // m_SubscriptionView Columns order & size
                if (Properties.Settings.Default.GUI_SubscriptionColumns != null)
                {
                    string[] colprops = Properties.Settings.Default.GUI_SubscriptionColumns.Split(';');

                    if (colprops.Length != m_SubscriptionView.Columns.Count * 2)
                        return;

                    for (int i = 0; i < colprops.Length / 2; i++)
                    {
                        m_SubscriptionView.Columns[i].DisplayIndex = Convert.ToInt32(colprops[i * 2]);
                        m_SubscriptionView.Columns[i].Width = Convert.ToInt32(colprops[i * 2+1]);
                    }

                    m_SubscriptionView.Refresh();
                }

            }
            catch
            {
                //ignore
            }

            int intervalMinutes = Math.Max(Math.Min(Properties.Settings.Default.Auto_Store_Period_Minutes, 480), 1);
            if (intervalMinutes != Properties.Settings.Default.Auto_Store_Period_Minutes)
                Properties.Settings.Default.Auto_Store_Period_Minutes = intervalMinutes;
            SaveObjectNamesTimer.Interval = intervalMinutes * 60000;
            
            SaveObjectNamesTimer.Enabled = true;

            // no longer needed
            // AddDebugMenu();
        }

        string CovGraph_PointValueEvent(ZedGraphControl sender, GraphPane pane, CurveItem curve, int iPt)
        {
            PointPair point= curve[iPt];

            String Name = (String)curve.Tag;
            XDate X = new XDate(point.X);
            string tooltip = Name + Environment.NewLine + X.ToString() + "    " + point.Y.ToString();
            return tooltip;
        }

        private static string ConvertToText(IList<BacnetValue> values)
        {
            if (values == null)
                return "[null]";
            else if (values.Count == 0)
                return "";
            else if (values.Count == 1)
                return values[0].Value.ToString();
            else
            {
                string ret = "{";
                foreach (BacnetValue value in values)
                    ret += value.Value.ToString() + ",";
                ret = ret.Substring(0, ret.Length - 1);
                ret += "}";
                return ret;
            }
        }

        private void ChangeTreeNodePropertyName(TreeNode tn, String Name)
        {
            // Tooltip not set is not null, strange !
            if (tn.ToolTipText == "")
            {
                tn.ToolTipText = tn.Text;
            }
            if (Properties.Settings.Default.DisplayIdWithName)
            {
                string abbreviatedName = ShortenObjectId(tn.ToolTipText);
                if (!abbreviatedName.Equals(tn.ToolTipText))
                {
                    tn.Text = Name + " (" + abbreviatedName + ")";
                }
                else
                {
                    tn.Text = Name + " (" + System.Threading.Thread.CurrentThread.CurrentCulture.TextInfo.ToTitleCase(tn.ToolTipText.ToLower()) + ")";
                }
            }
            else
            {
                tn.Text = Name;
            }

            HighlightTreeNode(tn);
        }

        private void SetSubscriptionStatus(ListViewItem itm, string status)
        {
            if (itm.SubItems[6].Text == status) return;
            itm.SubItems[6].Text = status;
            itm.SubItems[5].Text = DateTime.Now.ToString(Properties.Settings.Default.COVTimeFormater);
        }

        private string EventTypeNiceName(BacnetEventNotificationData.BacnetEventStates state)
        {
            return state.ToString().Substring(12);
        }


        private void OnEventNotify(BacnetClient sender, BacnetAddress adr, byte invoke_id, BacnetEventNotificationData EventData, bool need_confirm)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new BacnetClient.EventNotificationCallbackHandler(OnEventNotify), new object[] { sender, adr, invoke_id, EventData, need_confirm });
                return;
            }

            if (_plotterRunningFlag)
            {
                var dev = GetDevice(sender, adr);

                string sub_key = $"{EventData.initiatingObjectIdentifier.instance}:{EventData.eventObjectIdentifier}";

                ListViewItem itm = null;
                // find the Event in the View
                foreach (ListViewItem l in m_SubscriptionView.Items)
                {
                    if (l.Tag.ToString() == sub_key)
                    {
                        itm = l;
                        break;
                    }
                }

                uint deviceInstance = EventData.initiatingObjectIdentifier.instance;
                BacnetObjectId objectId = EventData.eventObjectIdentifier;

                if (itm == null)
                {
                    itm = m_SubscriptionView.Items.Add("");//device_id.ToString());
                                                           // Always a blank on [0] to allow for the "Show" Column

                    itm.Tag = sub_key;

                    // device id is index [1]
                    itm.SubItems.Add(deviceInstance.ToString()); // device instance
                    itm.SubItems.Add(ShortenObjectId(objectId.ToString())); // object ID [2]

                    string name = objectId.ToString();

                    lock (DevicesObjectsName)
                    {
                        Tuple<String, BacnetObjectId> t = new Tuple<String, BacnetObjectId>(adr.FullHashString(), objectId);
                        if (DevicesObjectsName.ContainsKey(t))
                        {
                            name = DevicesObjectsName[t];
                        }
                        else
                        {
                            name = dev.ReadPropertyAsync<string>(objectId, BacnetPropertyIds.PROP_OBJECT_NAME).Result;
                            if (string.IsNullOrWhiteSpace(name) || name.StartsWith("["))
                            {
                                name = objectId.ToString();
                            }
                        }
                    }

                    itm.SubItems.Add(name);   //name [3]
                    itm.SubItems.Add(EventTypeNiceName(EventData.fromState) + " to " + EventTypeNiceName(EventData.toState)); //value [4]
                    itm.SubItems.Add(EventData.timeStamp.Time.ToString(Properties.Settings.Default.COVTimeFormater));   //time [5]
                    itm.SubItems.Add(EventData.notifyType.ToString());   //status [6]


                    if (Properties.Settings.Default.ShowDescriptionWhenUseful)
                    {
                        itm.SubItems.Add("Yabe received an event notification");   // Description [7]
                    }
                    else
                    {
                        itm.SubItems.Add(""); // Description [7]
                    }
                }
                else
                {
                    string tempName = objectId.ToString();

                    if (itm.SubItems[3].Text.Equals(tempName))
                    {
                        string name = null;

                        lock (DevicesObjectsName)
                        {
                            Tuple<String, BacnetObjectId> t = new Tuple<String, BacnetObjectId>(adr.FullHashString(), objectId);
                            if (DevicesObjectsName.ContainsKey(t))
                            {
                                name = DevicesObjectsName[t];
                            }
                            else
                            {
                                name = dev.ReadPropertyAsync<string>(objectId, BacnetPropertyIds.PROP_OBJECT_NAME).Result;
                                if (string.IsNullOrWhiteSpace(name) || name.StartsWith("["))
                                {
                                    name = objectId.ToString();
                                }
                            }
                        }

                        if (name != null)
                            itm.SubItems[3].Text = name;

                    }

                    itm.SubItems[4].Text = EventTypeNiceName(EventData.fromState) + " to " + EventTypeNiceName(EventData.toState);
                    itm.SubItems[5].Text = EventData.timeStamp.Time.ToString(Properties.Settings.Default.COVTimeFormater);   //time
                    itm.SubItems[6].Text = EventData.notifyType.ToString();   //status
                }

                AddLogAlarmEvent(itm);
            }

            //send ack
            if (need_confirm)
            {
                sender.SimpleAckResponse(adr, BacnetConfirmedServices.SERVICE_CONFIRMED_EVENT_NOTIFICATION, invoke_id);
            }

        }

        private void OnCOVNotification(Subscription subscription, byte invoke_id, uint timeRemaining, bool need_confirm, ICollection<BacnetPropertyValue> values, BacnetMaxSegments max_segments) => OnCOVNotification(subscription.device.Client, subscription.device.Address, invoke_id, subscription.subscribe_id, subscription.device.DeviceId, subscription.object_id, timeRemaining, need_confirm, values, max_segments);
        private void OnCOVNotification(BacnetClient sender, BacnetAddress adr, byte invoke_id, uint subscriberProcessIdentifier, BacnetObjectId initiatingDeviceIdentifier, BacnetObjectId monitoredObjectIdentifier, uint timeRemaining, bool need_confirm, ICollection<BacnetPropertyValue> values, BacnetMaxSegments max_segments)
        {
            var sub_key = adr.ToString() + ":" + initiatingDeviceIdentifier.instance + ":" + subscriberProcessIdentifier;

            // The changing of the bool value _plotterPauseFlag should naturally be thread
            // safe (atomic operation?), so I don't think any locks are needed here. Otherwise,
            // instead we could check the state of the ManualResetEvent that is used in the
            // polling loop if explicit thread safety is desired.
            if (_plotterRunningFlag)
            {
                this.BeginInvoke((MethodInvoker)delegate
                {
                    try
                    {
                        ListViewItem itm;
                        lock (m_subscription_list)
                        {
                            if (m_subscription_list.ContainsKey(sub_key))
                            {
                                itm = m_subscription_list[sub_key];
                            }
                            else
                            {
                                return;
                            }
                        }
                        foreach (BacnetPropertyValue value in values)
                        {

                            switch ((BacnetPropertyIds)value.property.propertyIdentifier)
                            {
                                case BacnetPropertyIds.PROP_PRESENT_VALUE:
                                    itm.SubItems[4].Text = ConvertToText(value.value);
                                    itm.SubItems[5].Text = DateTime.Now.ToString(Properties.Settings.Default.COVTimeFormater);
                                    if (itm.SubItems[6].Text == "Not started") itm.SubItems[6].Text = "OK";
                                    try
                                    {
                                        //  try convert from string
                                        bool Ybool;
                                        bool isBool = bool.TryParse(itm.SubItems[4].Text, out Ybool);
                                        double Y = double.NaN;
                                        if (isBool)
                                        {
                                            Y = Ybool ? 1.0 : 0.0;
                                        }
                                        else
                                        {
                                            Y = Convert.ToDouble(itm.SubItems[4].Text);
                                        }
                                        XDate X = new XDate(DateTime.Now);
                                        //if (!String.IsNullOrWhiteSpace(itm.SubItems[9].Text) && bool.Parse(itm.SubItems[9].Text))
                                        //{
                                        Pane.Title.Text = "";

                                        if ((Properties.Settings.Default.GraphLineStep) && (m_subscription_points[sub_key].Count != 0))
                                        {
                                            PointPair p = m_subscription_points[sub_key].Peek();
                                            m_subscription_points[sub_key].Add(X, p.Y);
                                        }
                                        m_subscription_points[sub_key].Add(X, Y);
                                        CovGraph.AxisChange();
                                        CovGraph.Invalidate();
                                        //}
                                    }
                                    catch { }
                                    break;
                                case BacnetPropertyIds.PROP_STATUS_FLAGS:
                                    if (value.value != null && value.value.Count > 0)
                                    {
                                        BacnetStatusFlags status = (BacnetStatusFlags)((BacnetBitString)value.value[0].Value).ConvertToInt();
                                        string status_text = "";
                                        if ((status & BacnetStatusFlags.STATUS_FLAG_FAULT) == BacnetStatusFlags.STATUS_FLAG_FAULT)
                                            status_text += "FAULT,";
                                        else if ((status & BacnetStatusFlags.STATUS_FLAG_IN_ALARM) == BacnetStatusFlags.STATUS_FLAG_IN_ALARM)
                                            status_text += "ALARM,";
                                        else if ((status & BacnetStatusFlags.STATUS_FLAG_OUT_OF_SERVICE) == BacnetStatusFlags.STATUS_FLAG_OUT_OF_SERVICE)
                                            status_text += "OOS,";
                                        else if ((status & BacnetStatusFlags.STATUS_FLAG_OVERRIDDEN) == BacnetStatusFlags.STATUS_FLAG_OVERRIDDEN)
                                            status_text += "OR,";
                                        if (status_text != "")
                                        {
                                            status_text = status_text.Substring(0, status_text.Length - 1);
                                            itm.SubItems[6].Text = status_text;
                                        }
                                        else
                                            itm.SubItems[6].Text = "OK";
                                    }

                                    break;
                                default:
                                    //got something else? ignore it
                                    break;
                            }
                        }

                        AddLogAlarmEvent(itm);
                    }
                    catch (Exception ex)
                    {
                        Trace.TraceError("Exception in subcribed value: " + ex.Message);
                    }
                });
            }
            //send ack
            if (need_confirm)
            {
                sender.SimpleAckResponse(adr, BacnetConfirmedServices.SERVICE_CONFIRMED_COV_NOTIFICATION, invoke_id);
            }
        }

        #region " Trace Listner "
        private class MyTraceListener : TraceListener
        {
            private YabeMainDialog m_form;

            public MyTraceListener(YabeMainDialog form)
                : base("MyListener")
            {
                m_form = form;
            }

            public override void TraceEvent(TraceEventCache eventCache, string source, TraceEventType eventType, int id, string message)
            {
                if ((this.Filter != null) && !this.Filter.ShouldTrace(eventCache, source, eventType, id, message, null, null, null)) return;

                ConsoleColor color;
                switch (eventType)
                {
                    case TraceEventType.Error:
                        color = ConsoleColor.Red;
                        break;
                    case TraceEventType.Warning:
                        color = ConsoleColor.Yellow;
                        break;
                    case TraceEventType.Information:
                        color = ConsoleColor.DarkGreen;
                        break;
                    default:
                        color = ConsoleColor.Gray;
                        break;
                }

                WriteColor(message + Environment.NewLine, color);
            }

            public override void TraceEvent(TraceEventCache eventCache, string source, TraceEventType eventType, int id, string format, params object[] args)
            {
                if ((this.Filter != null) && !this.Filter.ShouldTrace(eventCache, source, eventType, id, format, args, null, null)) return;

                ConsoleColor color;
                switch (eventType)
                {
                    case TraceEventType.Error:
                        color = ConsoleColor.Red;
                        break;
                    case TraceEventType.Warning:
                        color = ConsoleColor.Yellow;
                        break;
                    case TraceEventType.Information:
                        color = ConsoleColor.DarkGreen;
                        break;
                    default:
                        color = ConsoleColor.Gray;
                        break;
                }

                WriteColor(string.Format(format, args) + Environment.NewLine, color);
            }

            public override void Write(string message)
            {
                WriteColor(message, ConsoleColor.Gray);
            }
            public override void WriteLine(string message)
            {
                WriteColor(message + Environment.NewLine, ConsoleColor.Gray);
            }

            private void WriteColor(string message, ConsoleColor color)
            {
                if (!m_form.IsHandleCreated) return;

                m_form.m_LogText.BeginInvoke((MethodInvoker)delegate { m_form.m_LogText.AppendText(message); });
            }
        }
        #endregion

        private void MainDialog_Load(object sender, EventArgs e)
        {
            //start renew timer at half lifetime
            int lifetime = (int)Properties.Settings.Default.Subscriptions_Lifetime;
            if (lifetime > 0)
            {
                m_subscriptionRenewTimer.Interval = (lifetime / 2) * 1000;
                m_subscriptionRenewTimer.Enabled = true;
            }

            //display nice floats in propertygrid
            Utilities.CustomSingleConverter.DontDisplayExactFloats = true;

            // Plugins
            m_DeviceTree.TreeViewNodeSorter = new NodeSorter();

            string[] listPlugins = Properties.Settings.Default.Plugins.Split(new char[] { ',', ';' });

            if (Environment.OSVersion.Platform.ToString().Contains("Win"))
                foreach (string pluginname in listPlugins)
                {
                    try
                    {
                        // string path = Path.GetDirectoryName(Application.ExecutablePath);
                        string name = pluginname.Replace(" ", String.Empty);
                        // Assembly myDll = Assembly.LoadFrom(path + "\\" + name + ".dll");
                        Assembly myDll = Assembly.LoadFrom(name + ".dll");
                        Trace.WriteLine(String.Format("Loaded plugin \"{0}\".", pluginname));
                        Type[] types = myDll.GetExportedTypes();
                        IYabePlugin plugin = (IYabePlugin)myDll.CreateInstance(name + ".Plugin", true);
                        plugin.Init(this);
                    }
                    catch(Exception ex)
                    {
                        Trace.WriteLine(String.Format("Error loading plugin \"{0}\". {1}",pluginname,ex.Message));
                    }
                }

            if (pluginsToolStripMenuItem.DropDownItems.Count == 0) pluginsToolStripMenuItem.Visible = false;


            // Object Names
            if (Properties.Settings.Default.Auto_Store_Object_Names)
            {
                string fileTotal = Properties.Settings.Default.Auto_Store_Object_Names_File;
                if (!string.IsNullOrWhiteSpace(fileTotal))
                {
                    try
                    {
                        string file = Path.GetFileName(fileTotal);
                        string directory = Path.GetDirectoryName(fileTotal);
                        if (string.IsNullOrWhiteSpace(file))
                        {
                            file = "Auto_Stored_Object_Names.YabeMap";
                            fileTotal = Path.Combine(directory, file);
                            Properties.Settings.Default.Auto_Store_Object_Names_File = fileTotal;
                        }

                        if (File.Exists(fileTotal))
                        {
                            // Try to open the current (if exist) object Id<-> object name mapping file
                            Stream stream = File.Open(fileTotal, FileMode.Open);
                            BinaryFormatter bf = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                            var d = (Dictionary<Tuple<String, BacnetObjectId>, String>)bf.Deserialize(stream);
                            stream.Close();

                            if (d != null)
                            {
                                DevicesObjectsName = d;
                                objectNamesChangedFlag = false;
                                Trace.TraceInformation("Loaded object names from \""+ fileTotal + "\".");
                            }
                        }
                        else
                        {
                            if (!Directory.Exists(directory))
                            {
                                try
                                {
                                    Directory.CreateDirectory(directory);
                                    Trace.TraceInformation("Created directory \"" + directory + "\".");
                                }
                                catch(UnauthorizedAccessException)
                                {
                                    Trace.TraceError("Error trying to setup the auto-save object names function: The directory \"" + directory + "\" does not exist, and Yabe does not have permissions to automatically create this directory. Try changing the Auto_StoreObject_Names_File setting to a different path.");                                    Properties.Settings.Default.Auto_Store_Object_Names = false;
                                }
                            }
                            //Trace.TraceError("Error trying to auto-load object names from file: The file \"" + file + "\" does not exist. Try resetting the Auto_StoreObject_Names_File setting to a valid file path, or disable auto-store.");
                        }

                    }
                    catch (Exception ex)
                    {
                        Trace.TraceError("Exception trying to setup the auto-save object names function: " + ex.Message + ". Try resetting the Auto_StoreObject_Names_File setting to a valid file path.");
                        Properties.Settings.Default.Auto_Store_Object_Names = false;
                    }
                }
                else
                {
                    Properties.Settings.Default.Auto_Store_Object_Names = false;
                }
            }
        }

        private TreeNode FindCommTreeNode(BacnetClient comm)
        {
            foreach (TreeNode node in m_DeviceTree.Nodes[0].Nodes)
            {
                BacnetClient c = node.Tag as BacnetClient;
                if(c != null && c.Equals(comm)) return node;
            }
            return null;
        }

        private TreeNode FindCommTreeNode(IBacnetTransport transport)
        {
            foreach (TreeNode node in m_DeviceTree.Nodes[0].Nodes)
            {
                BacnetClient c = node.Tag as BacnetClient;
                if (c != null && c.Transport.Equals(transport)) return node;
            }
            return null;
        }

        // Only the see Yabe on the net
        void OnWhoIs(BacnetClient sender, BacnetAddress adr, int low_limit, int high_limit)
        {
            uint myId =(uint) Properties.Settings.Default.YabeDeviceId;

            if (low_limit != -1 && myId < low_limit) return;
            else if (high_limit != -1 && myId > high_limit) return;
            sender.Iam(myId, BacnetSegmentations.SEGMENTATION_BOTH, 61440);
        }

        void OnWhoIsIgnore(BacnetClient sender, BacnetAddress adr, int low_limit, int high_limit)
        {
            //ignore whois responses from other devices (or loopbacks)
        }

        private void OnReadPropertyRequest(BacnetClient sender, BacnetAddress adr, byte invoke_id, BacnetObjectId object_id, BacnetPropertyReference property, BacnetMaxSegments max_segments)
        {
            lock (m_storage)
            {
                try
                {
                    IList<BacnetValue> value;
                    DeviceStorage.ErrorCodes code = m_storage.ReadProperty(object_id, (BacnetPropertyIds)property.propertyIdentifier, property.propertyArrayIndex, out value);
                    if (code == DeviceStorage.ErrorCodes.Good)
                        sender.ReadPropertyResponse(adr, invoke_id, sender.GetSegmentBuffer(max_segments), object_id, property, value);
                    else
                        sender.ErrorResponse(adr, BacnetConfirmedServices.SERVICE_CONFIRMED_READ_PROPERTY, invoke_id, BacnetErrorClasses.ERROR_CLASS_DEVICE, BacnetErrorCodes.ERROR_CODE_OTHER);
                }
                catch (Exception)
                {
                    sender.ErrorResponse(adr, BacnetConfirmedServices.SERVICE_CONFIRMED_READ_PROPERTY, invoke_id, BacnetErrorClasses.ERROR_CLASS_DEVICE, BacnetErrorCodes.ERROR_CODE_OTHER);
                }
            }
        }
        private static void OnReadPropertyMultipleRequest(BacnetClient sender, BacnetAddress adr, byte invoke_id, IList<BacnetReadAccessSpecification> properties, BacnetMaxSegments max_segments)
        {
            lock (m_storage)
            {
                try
                {
                    IList<BacnetPropertyValue> value;
                    List<BacnetReadAccessResult> values = new List<BacnetReadAccessResult>();
                    foreach (BacnetReadAccessSpecification p in properties)
                    {
                        if (p.propertyReferences.Count == 1 && p.propertyReferences[0].propertyIdentifier == (uint)BacnetPropertyIds.PROP_ALL)
                        {
                            if (!m_storage.ReadPropertyAll(p.objectIdentifier, out value))
                            {
                                sender.ErrorResponse(adr, BacnetConfirmedServices.SERVICE_CONFIRMED_READ_PROP_MULTIPLE, invoke_id, BacnetErrorClasses.ERROR_CLASS_OBJECT, BacnetErrorCodes.ERROR_CODE_UNKNOWN_OBJECT);
                                return;
                            }
                        }
                        else
                            m_storage.ReadPropertyMultiple(p.objectIdentifier, p.propertyReferences, out value);
                        values.Add(new BacnetReadAccessResult(p.objectIdentifier, value));
                    }

                    sender.ReadPropertyMultipleResponse(adr, invoke_id, sender.GetSegmentBuffer(max_segments), values);

                }
                catch (Exception)
                {
                    sender.ErrorResponse(adr, BacnetConfirmedServices.SERVICE_CONFIRMED_READ_PROP_MULTIPLE, invoke_id, BacnetErrorClasses.ERROR_CLASS_DEVICE, BacnetErrorCodes.ERROR_CODE_OTHER);
                }
            }
        }

        private Dictionary<uint, ushort> deviceVendorMap = new Dictionary<uint, ushort>();

        private ushort? GetCurrentVendorId()
        {
            // Here we try to determine the current vendor ID of the selected device. Note that
            // this could possibly be better integrated into YABE. But for now, this should work.
            if (SelectedDevice != null)
            {
                lock (deviceVendorMap)
                {
                    if (deviceVendorMap.TryGetValue(SelectedDevice.DeviceId.Instance, out var vendorId))
                    {
                        return vendorId;
                    }
                }
            }
            Trace.TraceWarning("Cannot retrieve vendor information of the selected device.");
            return null;
        }

        void OnIam(BacnetClient sender, BacnetAddress adr, uint device_id, uint max_apdu, BacnetSegmentations segmentation, ushort vendor_id)
        {
            DoReceiveIamImplementation(sender, adr, device_id);

            // remember the vendor ID of the device
            lock (deviceVendorMap)
            {
                deviceVendorMap[device_id] = vendor_id;
            }
        }

        private void DoReceiveIamImplementation(BacnetClient sender, BacnetAddress adr, uint device_id)
        {
            var device = new BACnetDevice(sender, adr, device_id);
            lock (m_devices)
            {
                if (!m_devices.ContainsKey(sender)) return;
                if (!m_devices[sender].Devices.Contains(device))
                    m_devices[sender].Devices.Add(device);
                else
                    return;
            }

            //update GUI
            this.BeginInvoke((MethodInvoker)delegate
            {
                TreeNode parent = FindCommTreeNode(sender);
                if (parent == null) return;

                bool Prop_Object_NameOK = false;
                String Identifier = null;

                lock (DevicesObjectsName)
                    Prop_Object_NameOK = DevicesObjectsName.TryGetValue(new Tuple<String, BacnetObjectId>(adr.FullHashString(), new BacnetObjectId(BacnetObjectTypes.OBJECT_DEVICE, device_id)), out Identifier);

                //update existing (this can happen in MSTP)
                foreach (TreeNode s in parent.Nodes)
                {
                    if ((s.Tag is BACnetDevice entry) && (entry.Address.Equals(adr)))
                    {
                        s.Text = device.ToString(s.Parent.Parent != null);
                        s.Tag = device;
                        if (Prop_Object_NameOK)
                        {
                            s.ToolTipText = s.Text;
                            s.Text = Identifier + " [" + device_id.ToString() + "] ";
                        }
                        else
                        {
                            s.ToolTipText = "";
                        }

                        return;
                    }
                }
                // Try to add it under a router if any 
                foreach (TreeNode s in parent.Nodes)
                {
                    if ((s.Tag is BACnetDevice entry) && (entry.Address.IsMyRouter(adr)))
                    {
                        TreeNode node = new TreeNode(device.ToString(true));
                        node.ImageIndex = 2;
                        node.SelectedImageIndex = node.ImageIndex;
                        node.Tag = device;
                        if (Prop_Object_NameOK)
                        {
                            node.ToolTipText = node.Text;
                            node.Text = Identifier + " [" + device_id.ToString() + "] ";
                        }
                        else
                        {
                            node.ToolTipText = "";
                        }
                        s.Nodes.Add(node);

                        m_DeviceTree.ExpandAll();
                        return;
                    }
                }

                //add simply
                TreeNode basicnode = new TreeNode(device.ToString(false));
                basicnode.ImageIndex = 2;
                basicnode.SelectedImageIndex = basicnode.ImageIndex;
                basicnode.Tag = device;
                if (Prop_Object_NameOK)
                {
                    basicnode.ToolTipText = basicnode.Text;
                    basicnode.Text = Identifier + " [" + device_id.ToString() + "] ";
                }
                else
                {
                    basicnode.ToolTipText = "";
                }
                parent.Nodes.Add(basicnode);
                m_DeviceTree.ExpandAll();
            });
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string product;
                
            Assembly currentAssem = this.GetType().Assembly;
            object[] attribs = currentAssem.GetCustomAttributes(typeof(AssemblyProductAttribute), true);
            if (attribs.Length > 0)
            {
                product = ((AssemblyProductAttribute)attribs[0]).Product;
            }
            else
            {
                product = this.GetType().Assembly.GetName().Name;
            }

            MessageBox.Show(this, product + "\nVersion " + this.GetType().Assembly.GetName().Version + "\nBy Morten Kvistgaard - Copyright 2014-2017\nBy Frederic Chaxel - Copyright 2015-2022\n" +
                "\nReferences:"+
                "\nhttp://bacnet.sourceforge.net/" + 
                "\nhttp://www.unified-automation.com/products/development-tools/uaexpert.html" +
                "\nhttp://www.famfamfam.com/"+
                "\nhttp://sourceforge.net/projects/zedgraph/"+
                "\nhttp://www.codeproject.com/Articles/38699/A-Professional-Calendar-Agenda-View-That-You-Will"+
                "\nhttps://github.com/chmorgan/sharppcap"+
                "\nhttps://sourceforge.net/projects/mstreeview" + 
                "\nhttps://github.com/sta/websocket-sharp"
                
                , "About", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void quitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void addDevicesearchToolStripMenuItem_Click(object sender, EventArgs e)
        {
            labelDrop1.Visible = labelDrop2.Visible = false;
            if (TbxHighlightAddress.Text == "HighLight Filter")
                TbxHighlightAddress.Text = TbxHighlightDevice.Text = "";

            SearchDialog dlg = new SearchDialog();
            if (dlg.ShowDialog(this) == System.Windows.Forms.DialogResult.OK)
            {
                BacnetClient comm = dlg.Result;
                try
                {
                    m_devices.Add(comm, new BACnetDeviceLine(comm));
                }
                catch { return ; }

                //add to tree
                TreeNode node = m_DeviceTree.Nodes[0].Nodes.Add(comm.ToString());
                node.Tag = comm;
                switch (comm.Transport.Type)
                {
                    case BacnetAddressTypes.IP:
                        node.ImageIndex = 3;
                        break;
                    case BacnetAddressTypes.MSTP:
                        node.ImageIndex = 1;
                        break;
                    case BacnetAddressTypes.SC:
                        node.ImageIndex = 17;
                        break;
                    default:
                        node.ImageIndex = 8;
                        break;
                }
                node.SelectedImageIndex = node.ImageIndex;
                m_DeviceTree.ExpandAll(); m_DeviceTree.SelectedNode = node;

                try
                {
                    //start BACnet
                    comm.ProposedWindowSize = Properties.Settings.Default.Segments_ProposedWindowSize;
                    comm.Retries = (int)Properties.Settings.Default.DefaultRetries;
                    comm.Timeout = (int)Properties.Settings.Default.DefaultTimeout;
                    comm.MaxSegments = BacnetClient.GetSegmentsCount(Properties.Settings.Default.Segments_Max);
                    if (Properties.Settings.Default.YabeDeviceId >= 0) // If Yabe get a Device id
                    {
                        if (m_storage == null)
                        {
                            // Load descriptor from the embedded xml resource
                            m_storage = m_storage = DeviceStorage.Load("Yabe.YabeDeviceDescriptor.xml", (uint)Properties.Settings.Default.YabeDeviceId);
                            // A fast way to change the PROP_OBJECT_LIST
                            Property Prop = Array.Find<Property>(m_storage.Objects[0].Properties, p => p.Id == BacnetPropertyIds.PROP_OBJECT_LIST);
                            Prop.Value[0] = "OBJECT_DEVICE:" + Properties.Settings.Default.YabeDeviceId.ToString();
                            // change PROP_FIRMWARE_REVISION
                            Prop = Array.Find<Property>(m_storage.Objects[0].Properties, p => p.Id == BacnetPropertyIds.PROP_FIRMWARE_REVISION);
                            Prop.Value[0] = this.GetType().Assembly.GetName().Version.ToString();
                            // change PROP_APPLICATION_SOFTWARE_VERSION
                            Prop = Array.Find<Property>(m_storage.Objects[0].Properties, p => p.Id == BacnetPropertyIds.PROP_APPLICATION_SOFTWARE_VERSION);
                            Prop.Value[0] = this.GetType().Assembly.GetName().Version.ToString();
                        }
                        comm.OnWhoIs += new BacnetClient.WhoIsHandler(OnWhoIs);
                        comm.OnReadPropertyRequest += new BacnetClient.ReadPropertyRequestHandler(OnReadPropertyRequest);
                        comm.OnReadPropertyMultipleRequest += new BacnetClient.ReadPropertyMultipleRequestHandler(OnReadPropertyMultipleRequest);
                    }
                    else
                    {
                        comm.OnWhoIs += new BacnetClient.WhoIsHandler(OnWhoIsIgnore);
                    }
                    comm.OnIam += new BacnetClient.IamHandler(OnIam);
                    comm.OnCOVNotification += new BacnetClient.COVNotificationHandler(OnCOVNotification);
                    comm.OnEventNotify += new BacnetClient.EventNotificationCallbackHandler(OnEventNotify);
                    comm.Start();

                    // WhoIs Min & Max limits
                    int IdMin = -1, IdMax = -1;
                    Int32.TryParse(dlg.WhoLimitLow.Text, out IdMin); Int32.TryParse(dlg.WhoLimitHigh.Text, out IdMax);
                    if (IdMin == 0) IdMin = -1; if (IdMax == 0) IdMax = -1;
                    if ((IdMin!=-1)&&(IdMax==-1)) IdMax=0x3FFFFF;
                    if ((IdMax != -1) && (IdMin == -1)) IdMin = 0;

                    if (comm.Transport.Type == BacnetAddressTypes.SC) comm.Retries = 1; // Not required devices are connected to the Hub

                    //start search
                    if (comm.Transport.Type == BacnetAddressTypes.IP || comm.Transport.Type == BacnetAddressTypes.Ethernet 
                        || comm.Transport.Type == BacnetAddressTypes.IPV6
                        || comm.Transport.Type == BacnetAddressTypes.SC
                        || (comm.Transport is BacnetMstpProtocolTransport && ((BacnetMstpProtocolTransport)comm.Transport).SourceAddress != -1) 
                        || comm.Transport.Type == BacnetAddressTypes.PTP)
                    {
                        System.Threading.ThreadPool.QueueUserWorkItem((o) =>
                        {
                            for (int i = 0; i < comm.Retries; i++)
                            {
                                comm.WhoIs(IdMin,IdMax);
                                System.Threading.Thread.Sleep(comm.Timeout);
                            }
                        }, null);
                    }

                    //special MSTP auto discovery
                    if (comm.Transport is BacnetMstpProtocolTransport)
                    {
                        ((BacnetMstpProtocolTransport)comm.Transport).FrameRecieved += new BacnetMstpProtocolTransport.FrameRecievedHandler(MSTP_FrameRecieved);
                    }
                }
                catch (Exception ex)
                {
                    m_devices.Remove(comm);
                    node.Remove();
                    MessageBox.Show(this, "Couldn't start Bacnet communication: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void MSTP_FrameRecieved(BacnetMstpProtocolTransport sender, BacnetMstpFrameTypes frame_type, byte destination_address, byte source_address, int msg_length)
        {
            try
            {
                if (this.IsDisposed) return;
                BACnetDeviceLine device_line = null;
                foreach (BACnetDeviceLine l in m_devices.Values)
                {
                    if (l.Client.Transport == sender)
                    {
                        device_line = l;
                        break;
                    }
                }
                if (device_line == null) return;
                lock (device_line.mstp_sources_seen)
                {
                    if (!device_line.mstp_sources_seen.Contains(source_address))
                    {
                        device_line.mstp_sources_seen.Add(source_address);

                        var device = new BACnetDevice(device_line.Client, new BacnetAddress(BacnetAddressTypes.MSTP, 0, new byte[] { source_address }));

                        //find parent node
                        TreeNode parent = FindCommTreeNode(sender);

                        //find "free" node. The "free" node might have been added
                        TreeNode free_node = null;
                        foreach (TreeNode n in parent.Nodes)
                        {
                            if (n.Text == "free" + source_address)
                            {
                                free_node = n;
                                break;
                            }
                        }

                        //update gui
                        this.Invoke((MethodInvoker)delegate
                        {
                            TreeNode node = parent.Nodes.Add("device" + source_address);
                            node.ImageIndex = 2;
                            node.SelectedImageIndex = node.ImageIndex;
                            node.Tag = device;
                            if (free_node != null) free_node.Remove();
                            m_DeviceTree.ExpandAll();
                        });

                        //detect collision
                        if (source_address == sender.SourceAddress)
                        {
                            this.Invoke((MethodInvoker)delegate
                            {
                                MessageBox.Show(this, "Selected source address seems to be occupied!", "Collision detected", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                            });
                        }
                    }
                    if (frame_type == BacnetMstpFrameTypes.FRAME_TYPE_POLL_FOR_MASTER && !device_line.mstp_pfm_destinations_seen.Contains(destination_address) && sender.SourceAddress != destination_address)
                    {
                        device_line.mstp_pfm_destinations_seen.Add(destination_address);
                        if (!device_line.mstp_sources_seen.Contains(destination_address) && Properties.Settings.Default.MSTP_DisplayFreeAddresses)
                        {
                            TreeNode parent = FindCommTreeNode(sender);
                            if (this.IsDisposed) return;
                            this.Invoke((MethodInvoker)delegate
                            {
                                TreeNode node = parent.Nodes.Add("free" + destination_address);
                                node.ImageIndex = 9;
                                node.SelectedImageIndex = node.ImageIndex;
                                m_DeviceTree.ExpandAll();
                            });
                        }
                    }
                }
            }
            catch (ObjectDisposedException)
            {
                //we're closing down ... ignore
            }
        }

        private void m_SearchToolButton_Click(object sender, EventArgs e)
        {
            addDevicesearchToolStripMenuItem_Click(this, null);
        }

        private void RemoveSubscriptions(BACnetDevice device) => RemoveSubscriptions(subscr => (subscr.device == device));
        private void RemoveSubscriptions(BacnetClient client) => RemoveSubscriptions(subscr => (subscr.device.Client == client));
        private void RemoveSubscriptions(Predicate<Subscription> subscriptions)
        {
            var keys = new LinkedList<string>();
            foreach (var entry in m_subscription_list)
            {
                var sub = (Subscription)entry.Value.Tag;
                if (subscriptions(sub))
                {
                    m_SubscriptionView.Items.Remove(entry.Value);
                    keys.AddLast(sub.sub_key);
                }
            }
            foreach (string sub_key in keys)
            {
                m_subscription_list.Remove(sub_key);
                try
                {
                    RollingPointPairList points = m_subscription_points[sub_key];
                    foreach (LineItem l in Pane.CurveList)
                        if (l.Points == points)
                        {
                            Pane.CurveList.Remove(l);
                            break;
                        }

                    m_subscription_points.Remove(sub_key);
                }
                catch { }
            }

            CovGraph.AxisChange();
            CovGraph.Invalidate();
        }

        private void removeDeviceToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (m_DeviceTree.SelectedNode == null) return;
            else if (m_DeviceTree.SelectedNode.Tag == null) return;
            BACnetDevice device_entry = m_DeviceTree.SelectedNode.Tag as BACnetDevice;
            BacnetClient comm_entry; 
            if (m_DeviceTree.SelectedNode.Tag is BacnetClient)    
                comm_entry = m_DeviceTree.SelectedNode.Tag as BacnetClient;
            else
                comm_entry = m_DeviceTree.SelectedNode.Parent.Tag as BacnetClient;

            if (device_entry != null)
            {
                if (MessageBox.Show(this, "Delete this device?", "Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == System.Windows.Forms.DialogResult.Yes)
                {
                    BacnetClient comm;
                    if (m_DeviceTree.SelectedNode.Parent.Tag is BacnetClient)
                        comm = m_DeviceTree.SelectedNode.Parent.Tag as BacnetClient;
                    else
                        comm = m_DeviceTree.SelectedNode.Parent.Parent.Tag as BacnetClient; // device under a router

                    m_AddressSpaceTree.Nodes.Clear();   //clear address space
                    AddSpaceLabel.Text = "Address Space";
                    m_DataGrid.SelectedObject = null;   //clear property grid

                    m_devices[comm].Devices.Remove((BACnetDevice)device_entry);

                    m_DeviceTree.Nodes.Remove(m_DeviceTree.SelectedNode);
                    RemoveSubscriptions(device_entry);
                }
            }
            else if (comm_entry != null)
            {
                if (MessageBox.Show(this, "Delete this transport?", "Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == System.Windows.Forms.DialogResult.Yes)
                {
                    if (_selectedDevice ==null || (_selectedDevice.Tag is BACnetDevice currentDelectedDeviceComms && m_devices[comm_entry].Devices.Contains(currentDelectedDeviceComms)))
                    {
                            m_AddressSpaceTree.Nodes.Clear();   //clear address space
                            AddSpaceLabel.Text = "Address Space";
                            m_DataGrid.SelectedObject = null;   //clear property grid
                    }
                    m_devices.Remove(comm_entry);
                    m_DeviceTree.Nodes.Remove(m_DeviceTree.SelectedNode);
                    RemoveSubscriptions(comm_entry);
                    comm_entry.Dispose();
                }
            }
        }

        private void m_RemoveToolButton_Click(object sender, EventArgs e)
        {
            removeDeviceToolStripMenuItem_Click(this, null);
        }

        public static int GetIconNum(BacnetObjectTypes object_type)
        {
            switch (object_type)
            {
                case BacnetObjectTypes.OBJECT_DEVICE:
                    return 2;
                case BacnetObjectTypes.OBJECT_FILE:
                    return 5;
                case BacnetObjectTypes.OBJECT_ANALOG_INPUT:
                case BacnetObjectTypes.OBJECT_ANALOG_OUTPUT:
                case BacnetObjectTypes.OBJECT_ANALOG_VALUE:
                    return 6;
                case BacnetObjectTypes.OBJECT_BINARY_INPUT:
                case BacnetObjectTypes.OBJECT_BINARY_OUTPUT:
                case BacnetObjectTypes.OBJECT_BINARY_VALUE:
                    return 7;
                case BacnetObjectTypes.OBJECT_GROUP:
                    return 10;
                case BacnetObjectTypes.OBJECT_STRUCTURED_VIEW:
                    return 11;
                case BacnetObjectTypes.OBJECT_TRENDLOG:
                    return 12;
                case BacnetObjectTypes.OBJECT_TREND_LOG_MULTIPLE:
                    return 12;
                case BacnetObjectTypes.OBJECT_NOTIFICATION_CLASS:
                    return 13;
                case BacnetObjectTypes.OBJECT_SCHEDULE:
                    return 14;
                case BacnetObjectTypes.OBJECT_CALENDAR:
                    return 15;
                default:
                    return 4;
            }
        }
        private void SetNodeIcon(BacnetObjectTypes object_type, TreeNode node)
        {
            node.ImageIndex = GetIconNum(object_type);            
            node.SelectedImageIndex = node.ImageIndex;
        }

        private async Task AddObjectNodesAsync(BACnetDevice device, bool forceUpdate = false)
        {
            this.Cursor = Cursors.WaitCursor;

            // Select source object list:
            IEnumerable<BACnetObject> objList;
            switch (Properties.Settings.Default.Address_Space_Structured_View)
            {
                case AddressTreeViewType.Structured:
                    objList = await device.GetStructuredObjectListAsync(forceUpdate);
                    break;
                default:
                    var list = await device.GetObjectListAsync(forceUpdate);
                    objList = list.Values
                        .SelectMany(objects => objects)
                        .OrderBy();
                    break;
            }

            // Update GUI:
            AddObjectNodes(objList);

            AddSpaceLabel.Text = $"Address Space : {m_AddressSpaceTree.Nodes.Count} objects";

            this.Cursor = Cursors.Default;
            _selectedNode = null;
            m_DataGrid.SelectedObject = null;
        }
        private void AddObjectNodes(IEnumerable<BACnetObject> objects, TreeNode parent = null)
        {
            var recursive = false;
            switch (Properties.Settings.Default.Address_Space_Structured_View)
            {
                case AddressTreeViewType.Structured:
                case AddressTreeViewType.Both:
                    recursive = true;
                    break;
            }

            var target = (parent?.Nodes ??m_AddressSpaceTree.Nodes);
            foreach (var obj in objects.OrderBy())
            {
                // Add object:
                var objNode = CreateObjectNode(obj);
                target.Add(objNode);

                // Add child nodes:
                if (obj.ObjectId.Type == BacnetObjectTypes.OBJECT_GROUP)
                {
                    var members = (IList<BacnetReadAccessSpecification>)obj[BacnetPropertyIds.PROP_LIST_OF_GROUP_MEMBERS];
                    foreach (var member in members)
                        member.propertyReferences
                            .Select(prop => $"{member.objectIdentifier}:{((BacnetPropertyIds)prop.propertyIdentifier)}")
                            .ForEach(name => objNode.Nodes.Add(CreateObjectNode(obj.Device[member.objectIdentifier], name, objNode)));
                }
                else if ((recursive) && (obj is BACnetView view))
                    AddObjectNodes(view.Children, objNode);
            }
        }
        private TreeNode CreateObjectNode(BACnetObject obj, TreeNode parent = null) => CreateObjectNode(obj, null, parent);
        private TreeNode CreateObjectNode(BACnetObject obj, string name, TreeNode parent = null)
        {
            TreeNode objNode;

            if (string.IsNullOrEmpty(name))
                name = obj.ToString();

            lock (DevicesObjectsName)
            {
                // Get the property name if already known
                if (DevicesObjectsName.TryGetValue(new Tuple<String, BacnetObjectId>(obj.Device.Address.FullHashString(), obj.ObjectId), out string objName) == true)
                {
                    if (Properties.Settings.Default.DisplayIdWithName)
                    {
                        string abbreviatedName = ShortenObjectId(name);
                        if (!abbreviatedName.Equals(name))
                        {
                            objName = objName + " (" + abbreviatedName + ")";
                        }
                        else
                        {
                            string titleCaseName = System.Threading.Thread.CurrentThread.CurrentCulture.TextInfo.ToTitleCase(name.ToLower());
                            objName = objName + " (" + name + ")";
                        }
                    }

                    objNode = new TreeNode(objName);
                    objNode.ToolTipText = name;
                }
                else
                {
                    objNode = new TreeNode(name);
                    objNode.ToolTipText = "";
                }
            }
            objNode.Tag = obj.ObjectId;

            HighlightTreeNode(objNode);

            // icon
            SetNodeIcon(obj.ObjectId.type, objNode);

            return (objNode);
        }

        private async void m_DeviceTree_AfterSelect(object sender, TreeViewEventArgs e)
        {
            AsynchRequestId++; // disabled a possible thread pool work (update) on the AddressSpaceTree
            TreeNode node = e.Node;
            //_selectedDevice = null;
            BACnetDevice device = e.Node.Tag as BACnetDevice;
            if (device != null)
            {
                m_AddressSpaceTree.Nodes.Clear();   //clear
                AddSpaceLabel.Text = "Address Space";

                //unconfigured MSTP?
                if ((device.Client.Transport is BacnetMstpProtocolTransport mstpTransport) && (mstpTransport.SourceAddress == -1))
                {
                    if (MessageBox.Show("The MSTP transport is not yet configured. Would you like to set source_address now?", "Set Source Address", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.No)
                        return;

                    //find suggested address
                    byte address = 0xFF;
                    BACnetDeviceLine line = m_devices[device.Client];
                    lock (line.mstp_sources_seen)
                    {
                        foreach (byte s in line.mstp_pfm_destinations_seen)
                        {
                            if (s < address && !line.mstp_sources_seen.Contains(s))
                                address = s;
                        }
                    }

                    //display choice
                    SourceAddressDialog dlg = new SourceAddressDialog();
                    dlg.SourceAddress = address;
                    if( dlg.ShowDialog(this) == DialogResult.Cancel) return;
                    mstpTransport.SourceAddress = dlg.SourceAddress;
                    Application.DoEvents();     //let the interface relax
                }

                this.Cursor = Cursors.WaitCursor;

                // Update device:
                if (node.ToolTipText == "")
                {
                    try
                    {
                        var devObj = await device.GetDeviceObjectAsync();
                        if (devObj != null)
                        {
                            bool Prop_Object_NameOK = false;
                            String Identifier;

                            lock (DevicesObjectsName)
                                Prop_Object_NameOK = DevicesObjectsName.TryGetValue(new Tuple<String, BacnetObjectId>(device.Address.FullHashString(), devObj.ObjectId), out Identifier);
                            if (Prop_Object_NameOK)
                            {
                                node.ToolTipText = node.Text;
                                node.Text = Identifier + " [" + devObj.ObjectId.Instance.ToString() + "] ";
                            }
                            else
                            {
                                this.Cursor = Cursors.WaitCursor;
                                var devName = await device.GetDeviceNameAsync();

                                node.ToolTipText = node.Text;   // IP or MSTP node id -> in the Tooltip
                                node.Text = devName + " [" + devObj.ObjectId.Instance.ToString() + "] ";  // change @ by the Name    
                                lock (DevicesObjectsName)
                                {
                                    Tuple<String, BacnetObjectId> t = new Tuple<String, BacnetObjectId>(device.Address.FullHashString(), devObj.ObjectId);
                                    if (DevicesObjectsName.ContainsKey(t))
                                    {
                                        if (!DevicesObjectsName[t].Equals(devName))
                                        {
                                            DevicesObjectsName.Remove(t);
                                            DevicesObjectsName.Add(t, devName);
                                            objectNamesChangedFlag = true;
                                        }
                                    }
                                    else
                                    {
                                        DevicesObjectsName.Add(t, devName);
                                        objectNamesChangedFlag = true;
                                    }
                                }
                            }
                        }
                    }
                    catch { }
                }
                _selectedDevice = node;

                // Update address space:
                Application.DoEvents();

                await AddObjectNodesAsync(device);
            }

            // Update controls:
            manual_refresh_objects.Enabled = (device != null);
        }

        private void addDeviceToolStripMenuItem_Click(object sender, EventArgs e)
        {
            addDevicesearchToolStripMenuItem_Click(this, null);
        }

        private void removeDeviceToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            removeDeviceToolStripMenuItem_Click(this, null);
        }

        public string GetNiceName(BacnetPropertyIds property, bool forceShowNumber = false)
        {
            bool prependNumber = forceShowNumber || Properties.Settings.Default.Show_Property_Id_Numbers;
            string name = property.ToString();
            if (name.StartsWith("PROP_"))
            {
                name = name.Substring(5);
                name = name.Replace('_', ' ');
                if(prependNumber)
                {
                    name = String.Format("{0} - {1}", (int)property, System.Threading.Thread.CurrentThread.CurrentCulture.TextInfo.ToTitleCase(name.ToLower()));
                }
                else
                {
                    name = System.Threading.Thread.CurrentThread.CurrentCulture.TextInfo.ToTitleCase(name.ToLower());
                }
            }
            else
            {
                name = GetProprietaryPropertyName((int)property);

                // try to resolve the vendor specific mapping
                var vendorId = GetCurrentVendorId();
                if (vendorId.HasValue)
                {
                    var vendorPropertyNumber = ((long)vendorId << 32) | (uint)property;
                    if (_proprietaryPropertyMappings.TryGetValue(vendorPropertyNumber, out var vendorPropertyName))
                    {
                        name = vendorPropertyName;
                    }
                }

                if(name!=null)
                {
                    if (prependNumber)
                    {
                        name = String.Format("Proprietary {0} - {1}", (int)property, System.Threading.Thread.CurrentThread.CurrentCulture.TextInfo.ToTitleCase(name.ToLower()));
                    }
                    else
                    {
                        name = "Proprietary - " + System.Threading.Thread.CurrentThread.CurrentCulture.TextInfo.ToTitleCase(name.ToLower());
                    }
                }
                else
                {
                    name = String.Format("Proprietary - {0}", (int)property);
                }
            }
            return name;
        }

        private async Task UpdateGrid(Subscription subscription)
        {
            this.Cursor = Cursors.WaitCursor;
            try
            {
                _selectedNode = null;
                m_DataGrid.SelectedObject = null;   //clear

                await UpdateGrid(subscription.device, subscription.object_id);

                _selectedNode = subscription;

            }
            finally
            {
                this.Cursor = Cursors.Default;
            }
        }
        private async Task<string> UpdateGrid(BACnetDevice device, BacnetObjectId object_id)
        {
            string ReturnPROP_OBJECT_NAME = null;
            try
            {
                m_DataGrid.SelectedObject = null;   //clear

                // (BETA) ... TODO UpdateGrid
                // > Resume when created meta information map (Too much missing information to be displayed in 'Utilities.CustomProperty' for now).
                //   e.g. "BacnetPropertyIds -> BacnetApplicationTags = ??"
                // var obj = device[object_id];
                // var properties = await obj.GetPropertiesAsync(true);

                var res = await device.ReadPropertiesAsync(object_id);

                bool[] showAlarmAck = new bool[3] {false, false, false };

                //update grid
                Utilities.DynamicPropertyGridContainer bag = new Utilities.DynamicPropertyGridContainer();
                foreach (BacnetPropertyValue p_value in res[0].values)
                {
                    var propId = (BacnetPropertyIds)p_value.property.propertyIdentifier;
                    var propName = GetNiceName(propId);

                    object value = null;
                    BacnetValue[] b_values = null;
                    if (p_value.value != null)
                    {

                        b_values = new BacnetValue[p_value.value.Count];

                        p_value.value.CopyTo(b_values, 0);
                        if (b_values.Length > 1)
                        {
                            object[] arr = new object[b_values.Length];
                            for (int j = 0; j < arr.Length; j++)
                                arr[j] = b_values[j].Value;
                            value = arr;
                        }
                        else if (b_values.Length == 1)
                            value = b_values[0].Value;
                    }
                    else
                        b_values = new BacnetValue[0];

                    switch (propId)
                    {
                        // PROP_PRESENT_VALUE can be write at null value to clear the prioroityarray if exists
                        case BacnetPropertyIds.PROP_PRESENT_VALUE:
                            // change to the related nullable type
                            Type t = value.GetType();
                            try
                            {
                                if (t != typeof(String)) // a bug on linuxmono where the folling instruction generates a wrong type
                                    t = Type.GetType("System.Nullable`1[" + value.GetType().FullName + "]");
                            }
                            catch { }

                            bag.Add(new Utilities.CustomProperty(propName, value, t != null ? t : typeof(string), false, "", b_values.Length > 0 ? b_values[0].Tag : (BacnetApplicationTags?)null, null, p_value.property));
                            break;
                        case BacnetPropertyIds.PROP_ACKED_TRANSITIONS:
                            if(value is BacnetBitString ackedTrans)
                            {
                                if (!ackedTrans.GetBit(0))
                                {
                                    showAlarmAck[0] = true;
                                }
                                if (!ackedTrans.GetBit(1))
                                {
                                    showAlarmAck[1] = true;
                                }
                                if (!ackedTrans.GetBit(2))
                                {
                                    showAlarmAck[2] = true;
                                }
                            }
                            bag.Add(new Utilities.CustomProperty(GetNiceName((BacnetPropertyIds)p_value.property.propertyIdentifier), value, value != null ? value.GetType() : typeof(string), false, "", b_values.Length > 0 ? b_values[0].Tag : (BacnetApplicationTags?)null, null, p_value.property));
                            break;
                        case BacnetPropertyIds.PROP_EVENT_TIME_STAMPS:
                            for(int i=0;i<b_values.Length;i++)
                            {
                                if (b_values[i].Value is DateTime dt)
                                {
                                    _cachedEventTimeStampsForAcknowledgementButtons[i] = dt;
                                }
                            }
                            bag.Add(new Utilities.CustomProperty(GetNiceName((BacnetPropertyIds)p_value.property.propertyIdentifier), value, value != null ? value.GetType() : typeof(string), false, "", b_values.Length > 0 ? b_values[0].Tag : (BacnetApplicationTags?)null, null, p_value.property));
                            break;

                        default:
                            bag.Add(new Utilities.CustomProperty(propName, value, value != null ? value.GetType() : typeof(string), false, "", b_values.Length > 0 ? b_values[0].Tag : (BacnetApplicationTags?)null, null, p_value.property));
                            break;
                    }

                    // The Prop Name replace the PropId into the Treenode 
                    if (propId == BacnetPropertyIds.PROP_OBJECT_NAME)
                    {
                        ReturnPROP_OBJECT_NAME = value.ToString();
                    }
                }

                m_DataGrid.SelectedObject = bag;

                ack_offnormal.Visible = showAlarmAck[0];
                ack_fault.Visible = showAlarmAck[1];
                ack_normal.Visible = showAlarmAck[2];
            }
            catch
            {
            }

            return ReturnPROP_OBJECT_NAME;
        }
        private async Task UpdateGrid(TreeNode selected_node)
        {
            this.Cursor = Cursors.WaitCursor;
            try
            {
                _selectedNode = null;
                //fetch end point
                if (_selectedDevice == null) return;
                else if (_selectedDevice.Tag == null) return;
                else if (!(_selectedDevice.Tag is BACnetDevice)) return;
                BACnetDevice device = (BACnetDevice)_selectedDevice.Tag;

                if (selected_node.Tag is BacnetObjectId)
                {
                    m_DataGrid.SelectedObject = null;   //clear

                    var object_id = (BacnetObjectId)selected_node.Tag;
                    var NewObjectName = await UpdateGrid(device, object_id);
                    if (NewObjectName != null)
                    {
                        ChangeTreeNodePropertyName(selected_node, NewObjectName);// Update the object name if needed
                        lock (DevicesObjectsName)
                        {
                            Tuple<String, BacnetObjectId> t = new Tuple<String, BacnetObjectId>(device.Address.FullHashString(), object_id);
                            if (DevicesObjectsName.ContainsKey(t))
                            {
                                if (!DevicesObjectsName[t].Equals(NewObjectName))
                                {
                                    DevicesObjectsName.Remove(t);
                                    DevicesObjectsName.Add(t, NewObjectName);
                                    objectNamesChangedFlag = true;
                                }
                            }
                            else
                            {
                                DevicesObjectsName.Add(t, NewObjectName);
                                objectNamesChangedFlag = true;
                            }
                        }
                    }

                    _selectedNode = selected_node;
                }
            }
            finally
            {
                this.Cursor = Cursors.Default;
            }
        }




        // Fixed a small problem when a right click is down in a Treeview
        private void TreeView_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right)
                return;
            // Store the selected node (can deselect a node).
            (sender as TreeView).SelectedNode = (sender as TreeView).GetNodeAt(e.X, e.Y);
        }

        private async void m_DataGrid_PropertyValueChanged(object s, PropertyValueChangedEventArgs e)
        {
            this.Cursor = Cursors.WaitCursor;
            try
            {
                BACnetEndpoint endpoint = null;
                BacnetObjectId object_id;

                if(_selectedNode!=null)
                {
                    if(_selectedNode is Subscription subscr)
                    {
                        //fetch object_id
                        object_id = subscr.object_id;
                        //fetch end point
                        endpoint = subscr.device;
                    }
                    else if(_selectedNode is TreeNode selectedObject)
                    {
                        if(_selectedDevice != null)
                        {
                            //fetch end point
                            if (_selectedDevice == null)
                            {
                                _selectedNode = null;
                                m_DataGrid.SelectedObject = null;
                                return;
                            }
                            else if (_selectedDevice.Tag == null)
                            {
                                _selectedNode = null;
                                m_DataGrid.SelectedObject = null;
                                return;
                            }
                            else if (!(_selectedDevice.Tag is BACnetDevice))
                            {
                                _selectedNode = null;
                                m_DataGrid.SelectedObject = null;
                                return;
                            }

                            endpoint = (BACnetEndpoint)_selectedDevice.Tag;

                            if (selectedObject.Tag == null) return;
                            else if (!(selectedObject.Tag is BacnetObjectId)) return;
                            object_id = (BacnetObjectId)selectedObject.Tag;
                        }
                        else
                        {
                            _selectedNode = null;
                            m_DataGrid.SelectedObject = null;
                            return;
                        }
                    }
                    else
                    {
                        _selectedNode = null;
                        m_DataGrid.SelectedObject = null;
                        return;
                    }
                }
                else
                {
                    _selectedDevice = null;
                    m_DataGrid.SelectedObject = null;
                    return;
                }


                PropertyGrid pg = (s as PropertyGrid);
                Utilities.CustomPropertyDescriptor c=null;
                GridItem gridItem=e.ChangedItem;
                // Go up to the Property (could be a sub-element)

                do
                {
                    if (gridItem.PropertyDescriptor is Utilities.CustomPropertyDescriptor)
                        c = (Utilities.CustomPropertyDescriptor)gridItem.PropertyDescriptor;
                    else
                        gridItem = gridItem.Parent;

                } while ((c == null) && (gridItem != null));

                if (c==null) return; // never occur normaly
 
                //fetch property
                BacnetPropertyReference property = (BacnetPropertyReference)c.CustomProperty.Tag;
                //new value
                object new_value = gridItem.Value;

                //convert to bacnet
                BacnetValue[] b_value = null;
                try
                {
                    if (new_value != null && new_value.GetType().IsArray && new_value.GetType() != typeof(byte[]))
                    {
                        Array arr = (Array)new_value;
                        b_value = new BacnetValue[arr.Length];
                        for (int i = 0; i < arr.Length; i++)
                            b_value[i] = new BacnetValue(arr.GetValue(i));
                    }
                    else
                    {
                        {
                            // Modif FC
                            b_value = new BacnetValue[1];
                            if ((BacnetApplicationTags)c.CustomProperty.bacnetApplicationTags != BacnetApplicationTags.BACNET_APPLICATION_TAG_NULL)
                            {
                                b_value[0] = new BacnetValue((BacnetApplicationTags)c.CustomProperty.bacnetApplicationTags, new_value);
                            }
                            else
                            {
                                object o=null;
                                TypeConverter t = new TypeConverter();
                                // try to convert to the simplest type
                                String[] typelist = { "Boolean", "UInt32", "Int32", "Single", "Double" };

                                foreach (String typename in typelist)
                                {
                                    try
                                    {
                                        o=Convert.ChangeType(new_value, Type.GetType("System."+typename));
                                        break;
                                    }
                                    catch { }
                                }
                                
                                if (o==null)
                                    b_value[0] = new BacnetValue(new_value);
                                else
                                    b_value[0] = new BacnetValue(o);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, "Couldn't convert property: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                //write
                try
                {
                    endpoint.Client.WritePriority = (uint)Properties.Settings.Default.DefaultWritePriority;
                    if (!await endpoint.WritePropertyAsync(object_id, (BacnetPropertyIds)property.propertyIdentifier, b_value))
                    {
                        MessageBox.Show(this, "Couldn't write property", "Communication Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, "Error during write: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }

                //reload
                if (_selectedNode != null)
                {
                    if (_selectedNode is Subscription subscription)
                    {
                        await UpdateGrid(subscription);
                        if(pg!=null)
                        {
                            pg.SelectedGridItem = gridItem;
                        }

                    }
                    else if (_selectedNode is TreeNode selectedObject)
                    {
                        if (_selectedDevice!=null)
                        {
                            await UpdateGrid(selectedObject);
                            if (pg != null)
                            {
                                pg.SelectedGridItem = gridItem;
                            }
                        }
                        else
                        {
                            _selectedNode = null;
                            m_DataGrid.SelectedObject = null;
                            return;
                        }
                    }
                    else
                    {
                        _selectedNode = null;
                        m_DataGrid.SelectedObject = null;
                        return;
                    }
                }
                else
                {
                    _selectedDevice = null;
                    m_DataGrid.SelectedObject = null;
                    return;
                }

            }
            finally
            {
                this.Cursor = Cursors.Default;
            }
        }

        public bool GetObjectLink(out BacnetClient comm, out BacnetAddress adr, out BacnetObjectId object_id, BacnetObjectTypes ExpectedType)
        {

            comm = null;
            adr = new BacnetAddress(BacnetAddressTypes.None, 0, null);
            object_id = new BacnetObjectId();

            try
            {
                if (m_DeviceTree.SelectedNode == null) return false;
                else if (m_DeviceTree.SelectedNode.Tag == null) return false;
                else if (!(m_DeviceTree.SelectedNode.Tag is BACnetDevice)) return false;
                BACnetDevice entry = (BACnetDevice)m_DeviceTree.SelectedNode.Tag;
                adr = entry.Address;
                comm = entry.Client;
            }
            catch
            {
                if (ExpectedType!=BacnetObjectTypes.MAX_BACNET_OBJECT_TYPE)
                    MessageBox.Show(this, "This is not a valid node", "Wrong node", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return false;
            }

            //fetch object_id
            if (
                m_AddressSpaceTree.SelectedNode == null ||
                !(m_AddressSpaceTree.SelectedNode.Tag is BacnetObjectId) ||
                !(((BacnetObjectId)m_AddressSpaceTree.SelectedNode.Tag).type == ExpectedType))
            {
                String S = ExpectedType.ToString().Substring(7).ToLower();
                if (ExpectedType != BacnetObjectTypes.MAX_BACNET_OBJECT_TYPE)
                {
                    MessageBox.Show(this, "The marked object is not a " + S, S, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    return false;
                }
            }

            if (m_AddressSpaceTree.SelectedNode != null)
            {
                if (m_AddressSpaceTree.SelectedNode.Tag == null) return false;
                object_id = (BacnetObjectId)m_AddressSpaceTree.SelectedNode.Tag;
                return true;
            }

            return false;
        }

        private void downloadFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Cursor = Cursors.WaitCursor;
            try
            {
                //fetch end point
                BacnetClient comm = null;
                BacnetAddress adr;             
                BacnetObjectId object_id;
                if (GetObjectLink(out comm, out adr, out object_id, BacnetObjectTypes.OBJECT_FILE) == false) return;

                //where to store file?
                SaveFileDialog dlg = new SaveFileDialog();
                dlg.FileName = Properties.Settings.Default.GUI_LastFilename;
                if (dlg.ShowDialog(this) != System.Windows.Forms.DialogResult.OK) return;
                string filename = dlg.FileName;
                Properties.Settings.Default.GUI_LastFilename = filename;

                //get file size
                int filesize = FileTransfers.ReadFileSize(comm, adr, object_id);
                if (filesize < 0)
                {
                    MessageBox.Show(this, "Couldn't read file size", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                //display progress
                ProgressDialog progress = new ProgressDialog();
                progress.Text = "Downloading file ...";
                progress.Label = "0 of " + (filesize / 1024) + " kb ... (0.0 kb/s)";
                progress.Maximum = filesize;
                progress.Show(this);

                DateTime start = DateTime.Now;
                double kb_per_sec = 0;
                FileTransfers transfer = new FileTransfers();
                EventHandler cancel_handler = (s, a) => { transfer.Cancel = true; };
                progress.Cancel += cancel_handler;
                Action<int> update_progress = (position) =>
                {
                    kb_per_sec = (position / 1024) / (DateTime.Now - start).TotalSeconds;
                    progress.Value = position;
                    progress.Label = string.Format((position / 1024) + " of " + (filesize / 1024) + " kb ... ({0:F1} kb/s)", kb_per_sec);
                };
                Application.DoEvents();
                try
                {
                    if(Properties.Settings.Default.DefaultDownloadSpeed == 2)
                        transfer.DownloadFileBySegmentation(comm, adr, object_id, filename, update_progress);
                    else if(Properties.Settings.Default.DefaultDownloadSpeed == 1)
                        transfer.DownloadFileByAsync(comm, adr, object_id, filename, update_progress);
                    else
                        transfer.DownloadFileByBlocking(comm, adr, object_id, filename, update_progress);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, "Error during download file: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                finally
                {
                    progress.Hide();
                }
            }
            finally
            {
                this.Cursor = Cursors.Default;
            }

            //information
            try
            {
                MessageBox.Show(this, "Done", "Done", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch
            {
            }
        }

        private void uploadFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Cursor = Cursors.WaitCursor;
            try
            {
                //fetch end point
                BacnetClient comm = null;
                BacnetAddress adr;
                BacnetObjectId object_id;
                if (GetObjectLink(out comm, out adr, out object_id, BacnetObjectTypes.OBJECT_FILE) == false) return;

                //which file to upload?
                OpenFileDialog dlg = new OpenFileDialog();
                dlg.FileName = Properties.Settings.Default.GUI_LastFilename;
                if (dlg.ShowDialog(this) != System.Windows.Forms.DialogResult.OK) return;
                string filename = dlg.FileName;
                Properties.Settings.Default.GUI_LastFilename = filename;

                //display progress
                int filesize = (int)(new System.IO.FileInfo(filename)).Length;
                ProgressDialog progress = new ProgressDialog();
                progress.Text = "Uploading file ...";
                progress.Label = "0 of " + (filesize / 1024) + " kb ... (0.0 kb/s)";
                progress.Maximum = filesize;
                progress.Show(this);

                FileTransfers transfer = new FileTransfers();
                DateTime start = DateTime.Now;
                double kb_per_sec = 0;
                EventHandler cancel_handler = (s, a) => { transfer.Cancel = true; };
                progress.Cancel += cancel_handler;
                Action<int> update_progress = (position) =>
                {
                    kb_per_sec = (position / 1024) / (DateTime.Now - start).TotalSeconds;
                    progress.Value = position;
                    progress.Label = string.Format((position / 1024) + " of " + (filesize / 1024) + " kb ... ({0:F1} kb/s)", kb_per_sec);
                };
                try
                {
                    transfer.UploadFileByBlocking(comm, adr, object_id, filename, update_progress);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, "Error during upload file: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                finally
                {
                    progress.Hide();
                }
            }
            finally
            {
                this.Cursor = Cursors.Default;
            }

            //information
            MessageBox.Show(this, "Done", "Done", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        // FC
        private void showTrendLogToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                //fetch end point
                BacnetClient comm;
                BacnetAddress adr;
                BacnetObjectId object_id;

                if (GetObjectLink(out comm, out adr, out object_id, BacnetObjectTypes.OBJECT_TRENDLOG) == false)
                    if (GetObjectLink(out comm, out adr, out object_id, BacnetObjectTypes.OBJECT_TREND_LOG_MULTIPLE) == false) return;             

                new TrendLogDisplay(comm, adr, object_id).ShowDialog();

            }
            catch(Exception ex)
            {
                Trace.TraceError("Error loading TrendLog : " + ex.Message);
            }
        }
        // FC
        private void showScheduleToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                //fetch end point
                BacnetClient comm;
                BacnetAddress adr;
                BacnetObjectId object_id;

                if (GetObjectLink(out comm, out adr, out object_id, BacnetObjectTypes.OBJECT_SCHEDULE) == false) return;

                new ScheduleDisplay(m_AddressSpaceTree.ImageList, comm, adr, object_id).ShowDialog();

            }
            catch(Exception ex) { Trace.TraceError("Error loading Schedule : " + ex.Message); }
        }

        private void deleteObjectToolStripMenuItem_Click(object sender, EventArgs e)
        {

            try
            {
                //fetch end point
                BacnetClient comm;
                BacnetAddress adr;
                BacnetObjectId object_id;

                GetObjectLink(out comm, out adr, out object_id, BacnetObjectTypes.MAX_BACNET_OBJECT_TYPE);

                if (MessageBox.Show("Are you sure you want to delete this object ?", object_id.ToString(), MessageBoxButtons.OKCancel, MessageBoxIcon.Question) == DialogResult.OK)
                {
                    comm.DeleteObjectRequest(adr, object_id);
                    m_DeviceTree_AfterSelect(null, new TreeViewEventArgs(this._selectedDevice));
                }

            }
            catch (Exception ex) 
            {
                Trace.TraceError("Error : " + ex.Message);
                MessageBox.Show("Fail to Delete Object", "DeleteObject", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void showCalendarToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                //fetch end point
                BacnetClient comm;
                BacnetAddress adr;
                BacnetObjectId object_id;

                if (GetObjectLink(out comm, out adr, out object_id, BacnetObjectTypes.OBJECT_CALENDAR) == false) return;

                new CalendarEditor(comm, adr, object_id).ShowDialog();

            }
            catch (Exception ex) { Trace.TraceError("Error loading Calendar : " + ex.Message); }
        }

        //FC
        private void showNotificationToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                //fetch end point
                BacnetClient comm;
                BacnetAddress adr;
                BacnetObjectId object_id;

                if (GetObjectLink(out comm, out adr, out object_id, BacnetObjectTypes.OBJECT_NOTIFICATION_CLASS) == false) return;

                new NotificationEditor(comm, adr, object_id).ShowDialog();

            }
            catch (Exception ex) { Trace.TraceError("Error loading Notification : " + ex.Message); }
        }


        private void m_AddressSpaceTree_ItemDrag(object sender, ItemDragEventArgs e)
        {
            m_AddressSpaceTree.DoDragDrop(e.Item, DragDropEffects.Move);
        }

        private void m_SubscriptionView_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Move;
        }

        private class Subscription
        {
            public BACnetDevice device;
            public BacnetObjectId object_id;
            public string sub_key;
            public uint subscribe_id;
            public bool is_COV_subscription = true; // false if subscription is refused (fallback to polling) or polling is specified explicitly.

            public Subscription(BACnetDevice device, BacnetObjectId object_id, string sub_key, uint subscribe_id)
            {
                this.device = device;
                this.object_id = object_id;
                this.sub_key = sub_key;
                this.subscribe_id = subscribe_id;
            }


            public CancellationToken CommunicationToken => device.CommunicationToken;


            public Task<bool> SubscribeCOVRequestAsync(bool cancel) => SubscribeCOVRequestAsync(cancel, Properties.Settings.Default.Subscriptions_IssueConfirmedNotifies, Properties.Settings.Default.Subscriptions_Lifetime);
            public Task<bool> SubscribeCOVRequestAsync(bool cancel, bool issueConfirmedNotifications, uint lifetime) => device.Client.SubscribeCOVRequestAsync(device.Address, object_id, subscribe_id, cancel, issueConfirmedNotifications, lifetime);

            public bool AlarmAcknowledgement(BacnetEventNotificationData.BacnetEventStates eventState, String AckText, BacnetGenericTime evTimeStamp, BacnetGenericTime ackTimeStamp, byte invoke_id = 0) => device.Client.AlarmAcknowledgement(device.Address, object_id, eventState, AckText, evTimeStamp, ackTimeStamp, invoke_id);
        }

        private string ShortenObjectId(string objectId)
        {
            string result = objectId;

            if(result.StartsWith("OBJECT_"))
            {
                result = result.Substring(7);
            }

            if(result.Contains("ANALOG_INPUT"))
            {
                result = result.Replace("ANALOG_INPUT", "AI");
            }
            if (result.Contains("ANALOG_OUTPUT"))
            {
                result = result.Replace("ANALOG_OUTPUT", "AO");
            }
            if (result.Contains("ANALOG_VALUE"))
            {
                result = result.Replace("ANALOG_VALUE", "AV");
            }
            if (result.Contains("BINARY_INPUT"))
            {
                result = result.Replace("BINARY_INPUT", "BI");
            }
            if (result.Contains("BINARY_OUTPUT"))
            {
                result = result.Replace("BINARY_OUTPUT", "BO");
            }
            if (result.Contains("BINARY_VALUE"))
            {
                result = result.Replace("BINARY_VALUE", "BV");
            }
            if (result.Contains("MULTI_STATE_INPUT"))
            {
                result = result.Replace("MULTI_STATE_INPUT", "MI");
            }
            if (result.Contains("MULTI_STATE_OUTPUT"))
            {
                result = result.Replace("MULTI_STATE_OUTPUT", "MO");
            }
            if (result.Contains("MULTI_STATE_VALUE"))
            {
                result = result.Replace("MULTI_STATE_VALUE", "MV");
            }

            return result;
        }

        private async Task<bool> CreateSubscriptionAsync(BACnetDevice device, BacnetObjectId object_id, bool WithGraph, int pollPeriod = -1)
        {
            this.Cursor = Cursors.WaitCursor;
            try
            {
                String CurveToolTip;
                //fetch device.InstanceId if needed
                await device.UpdateDeviceIdAsync();

                m_next_subscription_id++;
                string sub_key = $"{device.Address}:{device.InstanceId}:{m_next_subscription_id}";
                Subscription sub = new Subscription(device, object_id, sub_key, m_next_subscription_id);

                string obj_id = object_id.ToString().Substring(7);
                obj_id = ShortenObjectId(obj_id);

                CurveToolTip = await device.ReadPropertyAsync<string>(object_id, BacnetPropertyIds.PROP_OBJECT_NAME);

                bool useCov;
                
                // If pollPeriod is <0, it means we are deciding NOW whether we want polling or using COV, depending
                // on which of the option buttons is checked.
                //
                // If pollPeriod is >=0, it means the subscription came from a file that had previously been exported
                // from the COV graph, where either the polling period or COV selection (i.e., polling period == 0) is
                // recorded. So we override the setting of the option buttons in this case.
                //
                if (pollPeriod<0)
                {
                    useCov = CovOpn.Checked;
                }
                else
                {
                    useCov = (pollPeriod == 0);
                }


                //add to list
                ListViewItem itm = m_SubscriptionView.Items.Add("");
                // Always a blank on [0] to allow for the "Show" Column


                // device id is index [1]
                itm.SubItems.Add(device.InstanceId.ToString()); 
                itm.SubItems.Add(obj_id); // object id [2]
                itm.SubItems.Add(CurveToolTip);   //name [3]
                itm.SubItems.Add("");   //value [4]
                itm.SubItems.Add("");   //time [5]
                itm.SubItems.Add("Not started");   //status [6]
                if (Properties.Settings.Default.ShowDescriptionWhenUseful)
                {
                    var descr = await device.ReadPropertyAsync<string>(object_id, BacnetPropertyIds.PROP_DESCRIPTION);
                    if (descr != default)
                    {
                        itm.SubItems.Add(descr);   // Description [7]
                        CurveToolTip = CurveToolTip + Environment.NewLine + descr;
                    }
                }
                else
                    itm.SubItems.Add(""); // Description [7]

                itm.SubItems.Add("");   // Graph Line Color [8]
                itm.SubItems.Add(WithGraph.ToString());   // With Graph? [9]
                itm.SubItems.Add("-1");   // COV or Polled with Period [10]
                itm.Tag = sub;
                lock (m_subscription_list)
                {
                    m_subscription_list.Add(sub_key, itm);
                    if (WithGraph)
                    {
                        itm.Checked = true;
                    }
                    RollingPointPairList points = new RollingPointPairList(10000);
                    m_subscription_points.Add(sub_key, points);
                    Color color= GraphColor[Pane.CurveList.Count%GraphColor.Length];
                    LineItem l = Pane.AddCurve("", points, color, Properties.Settings.Default.GraphDotStyle);
                    l.IsVisible = itm.Checked;
                    l.Tag = CurveToolTip; // store the Name to display it in the Tooltip
                    itm.SubItems[8].BackColor = color;
                    itm.UseItemStyleForSubItems = false;
                    CovGraph.Invalidate();
                }

                // Now either subscribe to the data point, or set up a polling thread.
                bool SubscribeOK = false;

                if (useCov)
                {
                    try
                    {
                        SubscribeOK = await device.SubscribeCOVRequestAsync(object_id, m_next_subscription_id, false, Properties.Settings.Default.Subscriptions_IssueConfirmedNotifies, Properties.Settings.Default.Subscriptions_Lifetime);
                    }
                    catch(Exception ex) { Trace.TraceWarning("The COV subscription request generated an error: {0} - {1}", ex.GetType().Name, ex.Message); }

                    if(!SubscribeOK)
                    {
                        string prompt = String.Format("Failed to subscribe to COV for {0}. Point will be polled instead.", CurveToolTip);
                        Trace.TraceWarning(prompt);
                    }
                }

                if (!SubscribeOK) // echec : launch period acquisiton in the ThreadPool
                {
                    sub.is_COV_subscription = false;

                    int period = -1;
                    if (pollPeriod>0)
                    {
                        period = pollPeriod;
                    }
                    else
                    {
                        period = (int)pollRateSelector.Value;
                    }

                    // Polling - set the period data field to the period we are using.
                    // Note that this field can be displayed as a column on the subscription
                    // table if needed in future.
                    lock (m_subscription_list)
                    {
                        itm.SubItems[10].Text = period.ToString();
                    }

                    
                    _ = Task.Run(() => ReadPropertySubscriptionPollingInsteadOfCOV(sub, period), sub.CommunicationToken);
                }
                else
                {
                    // COV - set period indicator to 0.
                    // Note that this field can be displayed as a column on the subscription
                    // table if needed in future.
                    lock (m_subscription_list)
                    {
                        itm.SubItems[10].Text = "0";
                    }
                }
            }
            catch
            {
                return false;
            }
            finally
            {
                this.Cursor = Cursors.Default;                
            }

            return true;
        }

        // COV echec, PROP_PRESENT_VALUE polling method (alternative to COV).
        // The polling period is in milliseconds.
        private async Task ReadPropertySubscriptionPollingInsteadOfCOV(Subscription sub, int period)
        {
            bool wasPaused = !_plotterRunningFlag;
            bool firstIteration = true;

            // Save this for later so maybe we can notify the user when polling has crashed/stopped
            ListViewItem.ListViewSubItem statusItemFromListBox = null;
            lock (m_subscription_list)
            {
                if (m_subscription_list.ContainsKey(sub.sub_key))
                    statusItemFromListBox = m_subscription_list[sub.sub_key].SubItems[6];
            }

            while (true)
            {
                // Delay:
                if (!firstIteration)
                {
                    await Task.Delay(Math.Max(Math.Min(MAX_POLL_PERIOD, period), MIN_POLL_PERIOD));

                    if (!_plotterPause.WaitOne(0))
                    {
                        wasPaused = true;
                        _plotterPause.WaitOne();
                    }
                    if (wasPaused)
                        await Task.Delay(Math.Max(Math.Min(MAX_POLL_PERIOD, _rand.Next(0, 250)), MIN_POLL_PERIOD));
                }
                else
                    firstIteration = false;

                // Validate subscription:
                if (sub == null)
                    break;
                if (sub.device == null)
                    break;
                lock (m_subscription_list)
                {
                    if (!m_subscription_list.ContainsKey(sub.sub_key))
                        break;
                }

                // Read subscribed properties:
                try
                {
                    // We have no real way of checking wheter sub.comm has been disposed other than catching the exception?
                    // I suppose hopefully sub.is_active_subscription will be false by the time that happens...
                    var res = await sub.device.ReadPropertiesAsync(sub.object_id, BacnetPropertyIds.PROP_PRESENT_VALUE, BacnetPropertyIds.PROP_STATUS_FLAGS);

                    lock (m_subscription_list)
                    {
                        if (m_subscription_list.ContainsKey(sub.sub_key))
                            OnCOVNotification(sub, 0, 0, false, res.Single().values, BacnetMaxSegments.MAX_SEG0);
                        else
                            break;
                    }
                }
                catch (NullReferenceException)
                {
                    break;
                }
                catch (Exception)
                {
                    Trace.TraceError(String.Format("Failed reading subscribed properties while polling device {0}, object {1}.", sub.device.InstanceId.ToString(), sub.object_id.ToString()));
                }
            }

            if (statusItemFromListBox!=null && statusItemFromListBox.Text!=null)
            {
                this.BeginInvoke((MethodInvoker)delegate
                {
                    statusItemFromListBox.Text = "Polling stopped";
                });
            }
        }

        private void TogglePlotter()
        {

            if (_plotterRunningFlag)
            {
                _plotterRunningFlag = false;
                btnPlay.Text = PLAY_BUTTON_TEXT_WHEN_PAUSED;
                _plotterPause.Reset();
            }
            else
            {
                _plotterRunningFlag = true;
                btnPlay.Text = PLAY_BUTTON_TEXT_WHEN_RUNNING;
                _plotterPause.Set();
            }
        }

        private void ExportCovGraph()
        {
            StringBuilder sb = new StringBuilder();
            int count=0;
            foreach (KeyValuePair<string,ListViewItem> subscription in m_subscription_list)
            {
                // sub_key = adr.ToString() + ":" + device_id + ":" + m_next_subscription_id;
                bool hasGraph = false;
                if(!string.IsNullOrWhiteSpace(subscription.Value.SubItems[9].Text))
                {
                    bool graphBoolParsed;
                    if(bool.TryParse(subscription.Value.SubItems[9].Text, out graphBoolParsed))
                    {
                        hasGraph = graphBoolParsed;
                    }
                }

                sb.Append(hasGraph ? "P" : "T");
                sb.Append(';');

                string key = subscription.Key;
                string[] keyComponents = key.Split(':');
                if(keyComponents.Length!=4)
                {
                    continue;
                }
                sb.Append(keyComponents[2]);
                sb.Append(';');
                string value = string.Empty;
                try
                {
                    value = ((Subscription)subscription.Value.Tag).object_id.ToString();
                    //value = subscription.Value.SubItems[2].Text;
                }
                catch { continue; }
                if(value.Length==0 || !value.Contains(':'))
                {
                    continue;
                }
                sb.Append(value);
                sb.Append(';');
                sb.AppendLine(subscription.Value.SubItems[10].Text);
                count++;
            }
            if (count==0)
            {
                MessageBox.Show("No valid setup on COV graph to write to file.", "Write to file fail", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }
            string path = string.Empty;
            string fullPath = string.Empty;
            if (!String.IsNullOrWhiteSpace(Properties.Settings.Default.COV_Export_Path) && Properties.Settings.Default.COV_Export_Path.Length>0)
            {
                path = Path.GetDirectoryName(Properties.Settings.Default.COV_Export_Path);
                while(path.StartsWith("\\"))
                {
                    path = path.Substring(1);
                }
                if(!String.IsNullOrWhiteSpace(path) && !Directory.Exists(path))
                {
                    // Attempt to create
                    try
                    {
                        Directory.CreateDirectory(path);
                    }
                    catch (Exception e)
                    {
                        MessageBox.Show(String.Format("Failed to create directory \"{0}\". {1} - {2}", path, e.GetType().ToString(), e.Message), "Write to file error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                }
            }
            DateTime now = DateTime.Now;
            string fileName = String.Format("COV_Graph_Setup_Export_{0:0000}-{1:00}-{2:00}_{3:00}.{4:00}.{5:00}.txt",
                    now.Year,             /* Year in which the file was created */
                    now.Month,            /* Month in which the file was created */
                    now.Day,              /* Day in which the file was created */
                    now.Hour,             /* Hour in which the file was created */
                    now.Minute,           /* Minute in which the file was created */
                    now.Second);          /* Second in which the file was created */

            if(string.IsNullOrWhiteSpace(path))
            {
                fullPath = fileName;
            }
            else
            {
                fullPath = Path.Combine(path, fileName);
            }

            try
            {
                File.WriteAllText(fullPath, sb.ToString());
            }
            catch(Exception e)
            {
                MessageBox.Show(String.Format("Failed to write COV graph setup data to file \"{0}\". {1} - {2}", fullPath, e.GetType().ToString(), e.Message), "Write to file error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            MessageBox.Show(String.Format("Wrote COV graph setup data to file \"{0}\".", fullPath), "Write to file success", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private async void m_SubscriptionView_DragDrop(object sender, DragEventArgs e)
        {
            // Drop from the adress space
            if (e.Data.GetDataPresent("CodersLab.Windows.Controls.NodesCollection", false))
            {
                //fetch end point
                if (_selectedDevice == null) return;
                else if (_selectedDevice.Tag == null) return;
                else if (!(_selectedDevice.Tag is BACnetDevice)) return;
                BACnetDevice device = (BACnetDevice)_selectedDevice.Tag;

                //fetch object_id
                var nodes = (CodersLab.Windows.Controls.NodesCollection)e.Data.GetData("CodersLab.Windows.Controls.NodesCollection");
                //node[0]

                // Nodes are in a non controlable order, so puts the objectIds in order
                List<BacnetObjectId> Bobjs = new List<BacnetObjectId>();
                for (int i = 0; i < nodes.Count; i++)
                {
                    if ((nodes[i].Tag != null) && (nodes[i].Tag is BacnetObjectId))
                        Bobjs.Add((BacnetObjectId)nodes[i].Tag);
                }

                Bobjs.Sort();

                for (int i = 0; i < Bobjs.Count; i++)
                {
                    if (await CreateSubscriptionAsync(device, Bobjs[i], sender==CovGraph) == false)
                        break;
                }
            }

            // Drop a file deviceId;object:Id
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files.Length != 1) return;
                try
                {
                    StreamReader sr = new StreamReader(files[0]);
                    while (!sr.EndOfStream)
                    {
                        string line=sr.ReadLine();
                        if ((line.Length > 0) && (line[0] != '#'))
                        {
                            string[] description = line.Split(';');

                            // Create subscription:
                            int pollPeriod = -1;
                            switch (description.Length)
                            {
                                case 3: break;
                                case 4: pollPeriod = Int32.Parse(description[3]); break;

                                default: continue;
                            }

                            try
                            {
                                var withGraph = description[0].Equals("P", StringComparison.OrdinalIgnoreCase);
                                var deviceId = Convert.ToUInt32(description[1]);
                                var objectIdString = description[2];
                                if (!objectIdString.StartsWith("OBJECT_"))
                                {
                                    objectIdString = "OBJECT_" + objectIdString;
                                }
                                var objectId = BacnetObjectId.Parse(objectIdString);
                                var device = m_devices.Values
                                    .SelectMany(devLine => devLine.Devices)
                                    .FirstOrDefault(dev => (dev.InstanceId == deviceId));
                                if (device != null)
                                    await CreateSubscriptionAsync(device, objectId, withGraph, pollPeriod);
                            }
                            catch
                            {
                            }
                        }
                    }
                    sr.Close();
                }
                catch
                {
                }
            }
        }

        private void MainDialog_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                //commit setup
                Properties.Settings.Default.GUI_SplitterButtom = m_SplitContainerButtom.SplitterDistance;
                Properties.Settings.Default.GUI_SplitterMiddle = m_SplitContainerLeft.SplitterDistance;
                Properties.Settings.Default.GUI_SplitterRight = m_SplitContainerRight.SplitterDistance;
                Properties.Settings.Default.GUI_SplitterLeft = splitContainer4.SplitterDistance;
                Properties.Settings.Default.GUI_FormSize = this.Size;
                Properties.Settings.Default.GUI_FormState = this.WindowState.ToString();

                StringBuilder s=new StringBuilder();
                for (int i = 0; i < m_SubscriptionView.Columns.Count;i++)
                    s.Append(m_SubscriptionView.Columns[i].DisplayIndex.ToString() + ";" + m_SubscriptionView.Columns[i].Width.ToString() + ";");
                s.Remove(s.Length-1,1);

                Properties.Settings.Default.GUI_SubscriptionColumns=s.ToString();

                //save
                Properties.Settings.Default.Save();

                // save object name<->id file
                DoSaveObjectNamesIfNecessary();

            }
            catch
            {
                //ignore
            }
        }

        private async void m_SubscriptionView_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode != Keys.Delete) return;

            if (m_SubscriptionView.SelectedItems.Count >= 1)
            {
                foreach (ListViewItem itm in m_SubscriptionView.SelectedItems)
                {
                    //ListViewItem itm = m_SubscriptionView.SelectedItems[0];
                    if (itm.Tag is Subscription)    // It's a subscription or not (Event/Alarm)
                    {
                        Subscription sub = (Subscription)itm.Tag;
                        if (m_subscription_list.ContainsKey(sub.sub_key))
                        {
                            //remove from device
                            try
                            {
                                if (sub.is_COV_subscription)
                                    if (!await sub.SubscribeCOVRequestAsync(true, false, 0))
                                    {
                                        MessageBox.Show(this, "Couldn't unsubscribe", "Communication Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                        return;
                                    }
                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show(this, "Couldn't delete subscription: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                return;
                            }
                        }
                        //remove from interface
                        m_SubscriptionView.Items.Remove(itm);
                        lock (m_subscription_list)
                        {
                            m_subscription_list.Remove(sub.sub_key);
                            try
                            {
                                RollingPointPairList points = m_subscription_points[sub.sub_key];
                                foreach (LineItem l in Pane.CurveList)
                                    if (l.Points == points)
                                    {
                                        Pane.CurveList.Remove(l);
                                        break;
                                    }

                                m_subscription_points.Remove(sub.sub_key);
                            }
                            catch { }
                        }

                        CovGraph.AxisChange();
                        CovGraph.Invalidate();
                        //m_SubscriptionView.Items.Remove(itm);
                    }
                    else
                    {
                        m_SubscriptionView.Items.Remove(itm);
                    }
                }
            }
        }

        private void sendWhoIsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                BacnetClient comm = (BacnetClient)m_DeviceTree.SelectedNode.Tag;
                comm.WhoIs();
            }
            catch
            {
                MessageBox.Show(this, "Please select a \"transport\" node first", "Wrong node", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void AddRemoteIpToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //fetch end point
            BacnetClient comm = null;
            try
            {
                if (m_DeviceTree.SelectedNode == null) return;
                else if (m_DeviceTree.SelectedNode.Tag == null) return;
                else if (!(m_DeviceTree.SelectedNode.Tag is BacnetClient)) return;
                comm = (BacnetClient)m_DeviceTree.SelectedNode.Tag;

                if (comm.Transport is BacnetIpUdpProtocolTransport) // only IPv4 today, v6 maybe a day
                {

                    var Input =
                        new GenericInputBox<TextBox>("Ipv4/Udp Bacnet Node", "DeviceId - xx.xx.xx.xx:47808",
                          (o) =>
                          {
                              // adjustment to the generic control
                          }, 1, true, "Unknown device Id can be replaced by 4194303 or ?");
                    DialogResult res = Input.ShowDialog();

                    if (res == DialogResult.OK)
                    {
                        string[] entry=Input.genericInput.Text.Split('-');
                        if (entry[0][0] == '?') entry[0] = "4194303";
                        OnIam(comm, new BacnetAddress(BacnetAddressTypes.IP, entry[1].Trim()), Convert.ToUInt32(entry[0]), 0, BacnetSegmentations.SEGMENTATION_NONE, 0);
                    }
                }
                else
                {
                    MessageBox.Show(this, "Please select an \"IPv4 transport\" node first", "Wrong node", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch 
            {
                MessageBox.Show(this, "Invalid parameter", "Wrong node or IP @", MessageBoxButtons.OK, MessageBoxIcon.Information);          
            }
        }

        private void AddRemoteIpListToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //fetch end point
            BacnetClient comm = null;

            // Make sure we are connected
            if (m_DeviceTree.SelectedNode == null) return;
            else if (m_DeviceTree.SelectedNode.Tag == null) return;

            if ((m_DeviceTree.SelectedNode.Tag is BacnetClient))
            {
                comm = (BacnetClient)m_DeviceTree.SelectedNode.Tag;
            }
            else if (!(m_DeviceTree.SelectedNode.Parent.Tag is BacnetClient))
            {
                comm = (BacnetClient)m_DeviceTree.SelectedNode.Parent.Tag;
            }
            else
            {
                return;
            }

            comm = (BacnetClient)m_DeviceTree.SelectedNode.Tag;

            if (comm.Transport is BacnetIpUdpProtocolTransport) // only IPv4 today, v6 maybe a day
            {
                //select file to store
                OpenFileDialog dlg = new OpenFileDialog();
                if (dlg.ShowDialog(this) != System.Windows.Forms.DialogResult.OK) return;
                Application.DoEvents();
                string fileName = dlg.FileName;

                try
                {
                    this.Cursor = Cursors.WaitCursor;

                    string[] lines = File.ReadAllLines(fileName);
                    foreach (string line in lines)
                    {
                        if (!line.StartsWith("#"))  // Comment
                        { 
                            Application.DoEvents();
                            string[] entry = line.Split('-');
                            if (entry.Length != 2)
                            {
                                Trace.TraceWarning(String.Format("Failed to add a remote IPv4 node: \"{0}\" is not in the correct format (DeviceId - IP1.IP2.IP3.IP4:Port).", line.Trim()));
                                continue;
                            }
                            if (!uint.TryParse(entry[0].Trim(), out uint deviceIdIn))
                            {
                                Trace.TraceWarning(String.Format("Failed to add a remote IPv4 node: \"{0}\" is not in the correct format (DeviceId - IP1.IP2.IP3.IP4:Port).", line.Trim()));
                                continue;
                            }
                            if (!TryParseIPEndPoint(entry[1].Trim(), out IPEndPoint ipIn))
                            {
                                Trace.TraceWarning(String.Format("Failed to add a remote IPv4 node: \"{0}\" is not in the correct format (DeviceId - IP1.IP2.IP3.IP4:Port).", line.Trim()));
                                continue;
                            }
                            if (entry[0][0] == '?') entry[0] = "4194303";
                            try
                            {
                                OnIam(comm, new BacnetAddress(BacnetAddressTypes.IP, entry[1].Trim()), Convert.ToUInt32(entry[0]), 0, BacnetSegmentations.SEGMENTATION_NONE, 0);
                            }
                            catch (Exception ex)
                            {
                                Trace.TraceWarning(String.Format("Failed to add a remote IPv4 node: {0} - {1}", ex.GetType().Name, ex.Message));
                                continue;

                            }
                            Trace.TraceInformation(String.Format("Added remote IPv4 node: {0} - {1}", deviceIdIn.ToString(), ipIn.ToString()));
                        }
                    }
                }
                finally
                {
                    this.Cursor = Cursors.Default;
                }
            }
            else
            {
                MessageBox.Show(this, "Please select an \"IPv4 transport\" node first", "Wrong node", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            
        }

        private bool TryParseIPEndPoint(string inputString, out IPEndPoint endPoint)
        {
            endPoint = null;
            string[] ep = inputString.Split(':');
            if (ep.Length != 2) return false;
            IPAddress ip;
            if (!IPAddress.TryParse(ep[0], out ip))
            {
                return false;
            }
            int port;
            if (!int.TryParse(ep[1], NumberStyles.None, NumberFormatInfo.CurrentInfo, out port))
            {
                return false;
            }
            endPoint =  new IPEndPoint(ip, port);
            return true;
        }

        private void helpToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            string readme_path = Path.GetDirectoryName(Application.ExecutablePath)+"/README.txt";
            System.Diagnostics.Process.Start(readme_path);
        }

        private void settingsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            bool prevVertOrientation = Properties.Settings.Default.Vertical_Object_Splitter_Orientation;

            new SettingsDialog(Properties.Settings.Default).ShowDialog(this);

            bool changedOrientation = prevVertOrientation ^ Properties.Settings.Default.Vertical_Object_Splitter_Orientation;

            if(changedOrientation)
            {
                if (Properties.Settings.Default.Vertical_Object_Splitter_Orientation)
                {
                    splitContainer4.Orientation = Orientation.Vertical;
                    Properties.Settings.Default.GUI_SplitterLeft = (int)(m_SplitContainerLeft.SplitterDistance * 0.45f);
                }
                else
                {
                    splitContainer4.Orientation = Orientation.Horizontal;
                    Properties.Settings.Default.GUI_SplitterLeft = m_SplitContainerButtom.SplitterDistance / 2;
                }
                splitContainer4.SplitterDistance = Properties.Settings.Default.GUI_SplitterLeft;

            }

        }

        /// <summary>
        /// This will download all values from a given device and store it in a xml format, fit for the DemoServer
        /// This can be a good way to test serializing
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void exportDeviceDBToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //fetch end point
            BacnetClient comm = null;
            BacnetAddress adr;
            uint device_id;

            FetchEndPoint(out comm, out adr, out device_id);

            if (comm == null)
            {
                MessageBox.Show(this, "Please select a device node", "Wrong node", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
  

            //select file to store
            SaveFileDialog dlg = new SaveFileDialog();
            dlg.Filter = "xml|*.xml";
            if (dlg.ShowDialog(this) != System.Windows.Forms.DialogResult.OK) return;

            this.Cursor = Cursors.WaitCursor;
            Application.DoEvents();

            bool removeObject = false;

            try
            {
                //get all objects
                System.IO.BACnet.Storage.DeviceStorage storage = new System.IO.BACnet.Storage.DeviceStorage();
                IList<BacnetValue> value_list;
                comm.ReadPropertyRequest(adr, new BacnetObjectId(BacnetObjectTypes.OBJECT_DEVICE, device_id), BacnetPropertyIds.PROP_OBJECT_LIST, out value_list);
                LinkedList<BacnetObjectId> object_list = new LinkedList<BacnetObjectId>();
                foreach (BacnetValue value in value_list)
                {
                    if (Enum.IsDefined(typeof(BacnetObjectTypes), ((BacnetObjectId)value.Value).Type))
                        object_list.AddLast((BacnetObjectId)value.Value);
                    else
                        removeObject = true;
                }

                foreach (BacnetObjectId object_id in object_list)
                {
                    //read all properties
                    IList<BacnetReadAccessResult> multi_value_list;
                    BacnetPropertyReference[] properties = new BacnetPropertyReference[] { new BacnetPropertyReference((uint)BacnetPropertyIds.PROP_ALL, System.IO.BACnet.Serialize.ASN1.BACNET_ARRAY_ALL) };
                    comm.ReadPropertyMultipleRequest(adr, object_id, properties, out multi_value_list);

                    //store
                    foreach (BacnetPropertyValue value in multi_value_list[0].values)
                    {
                        try
                        {
                            storage.WriteProperty(object_id, (BacnetPropertyIds)value.property.propertyIdentifier, value.property.propertyArrayIndex, value.value, true);
                        }
                        catch { }
                    }
                }

                //save to disk
                storage.Save(dlg.FileName);

                //display
                MessageBox.Show(this, "Done", "Export done", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Error during export: " + ex.Message, "Export Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                this.Cursor = Cursors.Default;
                if (removeObject == true)
                    Trace.TraceWarning("All proprietary Objects removed from export");
            }
        }

        private async void subscribeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //fetch end point
            if (m_DeviceTree.SelectedNode == null) return;
            else if (m_DeviceTree.SelectedNode.Tag == null) return;
            else if (!(m_DeviceTree.SelectedNode.Tag is BACnetDevice)) return;
            BACnetDevice device = (BACnetDevice)m_DeviceTree.SelectedNode.Tag;

            //test object_id with the last selected node
            if (
                m_AddressSpaceTree.SelectedNode == null ||
                !(m_AddressSpaceTree.SelectedNode.Tag is BacnetObjectId))
            {
                MessageBox.Show(this, "The marked object is not an object", "Not an object", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }
            // advise all selected nodes, stop at the first COV reject (even if a period polling is done)
            foreach (TreeNode t in m_AddressSpaceTree.SelectedNodes)
            {
                BacnetObjectId object_id = (BacnetObjectId)t.Tag;
                if (!await CreateSubscriptionAsync(device, object_id, false))
                    return;
            }
        }

        private async void m_subscriptionRenewTimer_Tick(object sender, EventArgs e)
        {
            // don't want to lock the list for a while
            // so get element one by one using the indexer            
            int ItmCount;
            lock (m_subscription_list)
                ItmCount = m_subscription_list.Count;

            for (int i = 0; i < ItmCount; i++)
            {
                ListViewItem itm = null;

                // lock another time the list to get the item by indexer
                try
                {
                    lock (m_subscription_list)
                        itm = m_subscription_list.Values.ElementAt(i);
                }
                catch { }

                if (itm != null)
                {
                    try
                    {
                        Subscription sub = (Subscription)itm.Tag;

                        if (sub.is_COV_subscription == false) // not needs to renew, periodic pooling in operation (or nothing) due to COV subscription refused by the remote device, or "polling" selected in the UI.
                            return;

                        if (!await sub.SubscribeCOVRequestAsync(false))
                        {
                            SetSubscriptionStatus(itm, "Offline");
                            Trace.TraceWarning("Couldn't renew subscription " + sub.subscribe_id);
                        }
                    }
                    catch (Exception ex)
                    {
                        Trace.TraceError("Exception during renew subscription: " + ex.Message);
                    }
                }
            }
        }

        private void sendWhoIsToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            sendWhoIsToolStripMenuItem_Click(this, null);
        }

        private void exportDeviceDBToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            exportDeviceDBToolStripMenuItem_Click(this, null);
        }

        private void downloadFileToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            downloadFileToolStripMenuItem_Click(this, null);
        }

        private void showTrendLogToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            showTrendLogToolStripMenuItem_Click(null, null);
        }

        private void showScheduleToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            showScheduleToolStripMenuItem_Click(null, null);
        }

        private void showCalendarToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            showCalendarToolStripMenuItem_Click(null, null);
        }

        private void showNotificationToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            showNotificationToolStripMenuItem_Click(null, null);
        }
        private void uploadFileToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            uploadFileToolStripMenuItem_Click(this, null);
        }

        private void subscribeToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            subscribeToolStripMenuItem_Click(this, null);
        }

        private void timeSynchronizeToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            timeSynchronizeToolStripMenuItem_Click(this, null);
        }

        /// <summary>
        /// Retreive the BacnetClient, BacnetAddress, device id of the selected node,
        /// </summary>
        private bool FetchEndPoint(out BacnetClient comm, out BacnetAddress adr, out uint device_id) => FetchEndPoint(m_DeviceTree.SelectedNode, out comm, out adr, out device_id);
        /// <summary>
        /// Retreive the BacnetClient, BacnetAddress, device id of <paramref name="node"/>.
        /// </summary>
        private bool FetchEndPoint(TreeNode node, out BacnetClient comm, out BacnetAddress adr, out uint device_id)
        {
            comm = null; adr = null; device_id = 0;
            try
            {
                if (node == null) return false;
                else if (node.Tag == null) return false;
                else if (!(node.Tag is BACnetDevice)) return false;
                BACnetDevice entry = (BACnetDevice)node.Tag;
                adr = entry.Address;
                device_id = entry.InstanceId;
                if (node.Parent.Tag is BacnetClient)
                    comm = (BacnetClient)node.Parent.Tag;
                else
                    comm = (BacnetClient)node.Parent.Parent.Tag; // When device is under a Router
                return true;
            }
            catch
            {
                return false;
            }
        }

        private void FetchEndPoint(out List<(BacnetClient Client, BacnetAddress Address, uint DeviceId)> endPoints)
        {
            endPoints = new List<(BacnetClient Client, BacnetAddress Address, uint DeviceId)>();
            if (FetchEndPoint(out BacnetClient comm, out BacnetAddress adr, out uint device_id))
                endPoints.Add((comm, adr, device_id));
        }
        private void FetchEndPoints(out List<(BacnetClient Client, BacnetAddress Address, uint DeviceId)> endPoints)
        {
            endPoints = new List<(BacnetClient, BacnetAddress, uint)>();
            foreach (var node in DeviceNodes)
            {
                FetchEndPoint(node, out var comm, out var adr, out var deviceId);
                if (comm != null)
                    endPoints.Add((comm, adr, deviceId));
            }
        }

        private void timeSynchronizeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //fetch end point
            BacnetClient comm = null;
            BacnetAddress adr;
            uint device_id;

            FetchEndPoint(out comm, out adr, out device_id);

            if (comm == null)
            {
                MessageBox.Show(this, "Please select a device node", "Wrong node", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            //send
            if(Properties.Settings.Default.TimeSynchronize_UTC)
                comm.SynchronizeTime(adr, DateTime.Now.ToUniversalTime(), true);
            else
                comm.SynchronizeTime(adr, DateTime.Now, false);

            //done
            MessageBox.Show(this, "OK", "Time Synchronize", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void communicationControlToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //fetch end point
            BacnetClient comm = null;
            BacnetAddress adr;
            uint device_id;

            FetchEndPoint(out comm, out adr, out device_id);

            if (comm == null)
            {
                MessageBox.Show(this, "Please select a device node", "Wrong node", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            //Options
            DeviceCommunicationControlDialog dlg = new DeviceCommunicationControlDialog();
            if (dlg.ShowDialog(this) != System.Windows.Forms.DialogResult.OK) return;

            if (dlg.IsReinitialize)
            {
                //Reinitialize Device
                if (!comm.ReinitializeRequest(adr, dlg.ReinitializeState, dlg.Password))
                    MessageBox.Show(this, "Couldn't perform device communication control", "Device Communication Control", MessageBoxButtons.OK, MessageBoxIcon.Error);
                else
                    MessageBox.Show(this, "OK", "Device Communication Control", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                //Device Communication Control
                if (!comm.DeviceCommunicationControlRequest(adr, dlg.Duration, dlg.DisableCommunication ? (uint)1 : (uint)0, dlg.Password))
                    MessageBox.Show(this, "Couldn't perform device communication control", "Device Communication Control", MessageBoxButtons.OK, MessageBoxIcon.Error);
                else
                    MessageBox.Show(this, "OK", "Device Communication Control", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void communicationControlToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            communicationControlToolStripMenuItem_Click(this, null);
        }

        private async void exportEDEFilesSelDeviceToolStripMenuItem_Click(object sender, EventArgs e) => await exportDeviceEDEFile(true);
        private async void exportEDEFilesAllDevicesToolStripMenuItem_Click(object sender, EventArgs e) => await exportDeviceEDEFile(false);
        private const string EDE_EXPORT_TITLE = "EDE file export";
        private const string EDE_EXPORT_EXT = "csv";
        private async Task exportDeviceEDEFile(bool selDeviceOnly)
        {
            // Fetch devices:
            BACnetDevice[] devices;
            if (selDeviceOnly)
                devices = new BACnetDevice[] { this.SelectedDevice };
            else
                devices = DiscoveredDevices;
            if (devices.IsEmpty())
                return;

            string file;
            var singleFile = ((devices.Length == 1) || (Properties.Settings.Default.EDE_SingleFile));
            if (singleFile)
            {
                // Export device(s) in into single file:
                var dev = devices.First();
                if (devices.Length == 1)
                    file = $"Device{dev.InstanceId}.{EDE_EXPORT_EXT}";
                else
                    file = $"Devices.{EDE_EXPORT_EXT}";
                var dlg = new SaveFileDialog() {
                    Filter = "csv|*.csv",
                    FileName = file
                };
                if (dlg.ShowDialog(this) != DialogResult.OK)
                    return;

                file = dlg.FileName.Remove(dlg.FileName.Length - 4, 4);
                await exportDeviceEDEFile(devices, file);
            }
            else
            {
                // Export devices in into separate files:
                var dlg = new FolderBrowserDialog() {
                    Description = $"Select output folder to export {devices.Length} EDE files to."
                };
                if (dlg.ShowDialog(this) != DialogResult.OK)
                    return;

                foreach (var dev in devices)
                {
                    file = Path.Combine(dlg.SelectedPath, $"Device{dev.InstanceId}.{EDE_EXPORT_EXT}");
                    await exportDeviceEDEFile(dev, file);
                }
            }

            if (_selectedDevice != null)
                // Now re-display the tree:
                m_DeviceTree_AfterSelect(null, new TreeViewEventArgs(_selectedDevice));

            MessageBox.Show(this, $"Exported {devices.Length} device(s).", EDE_EXPORT_TITLE, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        /// <summary>
        /// Exports a single device to EDE file.
        /// </summary>
        private Task exportDeviceEDEFile(BACnetDevice device, String fileName) => exportDeviceEDEFile(new BACnetDevice[] { device }, fileName);
        /// <summary>
        /// Exports one or more devices to EDE file.
        /// </summary>
        /// <remarks>
        /// This will download all values from a given device and store it in 2 csv files: EDE and StateText (for Binary and Multistate objects).
        /// Ede files for Units and ObjectTypes are common when all values are coming from the standard
        /// </remarks>
        /// <example>
        /// Base on https://www.big-eu.org/s/big_ede_2_3.zip
        /// </example>
        private async Task exportDeviceEDEFile(BACnetDevice[] devices, string fileName)
        {
            var stateTextReferences = new List<string>();

            using (var edeWriter = new StreamWriter($"{fileName}_EDE.csv"))
            {
                edeWriter.WriteLine("#Engineering-Data-Exchange - B.I.G.-EU");
                edeWriter.WriteLine("PROJECT_NAME");
                edeWriter.WriteLine("VERSION_OF_REFERENCEFILE");
                edeWriter.WriteLine("TIMESTAMP_OF_LAST_CHANGE;" + DateTime.Now.ToShortDateString());
                edeWriter.WriteLine("AUTHOR_OF_LAST_CHANGE;YABE Yet Another Bacnet Explorer");
                edeWriter.WriteLine("VERSION_OF_LAYOUT;2.3");
                edeWriter.WriteLine("#mandatory;mandator;mandatory;mandatory;mandatory;optional;optional;optional;optional;optional;optional;optional;optional;optional;optional;optional");
                edeWriter.WriteLine("# keyname;device obj.-instance;object-name;object-type;object-instance;description;present-value-default;min-present-value;max-present-value;settable;supports COV;hi-limit;low-limit;state-text-reference;unit-code;vendor-specific-addres");

                foreach (var dev in devices)
                    await exportDeviceEDEFile(dev, edeWriter, stateTextReferences);
            }
            using (var stateTextWriter = new StreamWriter($"{fileName}_StateTexts.csv"))
            {
                stateTextWriter.WriteLine("#State Text Reference");
                if (stateTextReferences.Count > 0)
                {
                    var maxStates = stateTextReferences
                        .Select(stateRef => stateRef.Count(c => c.Equals(';')) + 1)
                        .Max();
                    var columns = Enumerable
                        .Range(0, maxStates + 1)
                        .Select(col => $"Text {col}")
                        .ToArray();
                    if (maxStates >= 0) columns[0] = "Reference Number";
                    if (maxStates >= 1) columns[1] += " or Inactive-Text";
                    if (maxStates >= 2) columns[2] += " or Active-Text";
                    stateTextWriter.WriteLine("#" + string.Join(";", columns));

                    int i = 0;
                    foreach (var stateRef in stateTextReferences)
                    {
                        stateTextWriter.Write($"{i++};");
                        stateTextWriter.WriteLine(stateRef);
                    }
                }
            }

            if (Properties.Settings.Default.EDE_CommonFiles)
            {
                using (var objTypesWriter = new StreamWriter($"{fileName}_ObjTypes.csv"))
                {
                    objTypesWriter.WriteLine("#Encoding of BACnet Object Types;");
                    objTypesWriter.WriteLine("#Code;Object Type;");
                    foreach (var objType in Common.ObjectType_EdeTexts)
                        objTypesWriter.WriteLine($"{(int)objType.Key};{objType.Value};");
                }
                using (var unitsWriter = new StreamWriter($"{fileName}_Units.csv"))
                {
                    unitsWriter.WriteLine("#Encoding of BACnet Engineering Units;");
                    unitsWriter.WriteLine("#Code;Unit Text;");
                    foreach (var unit in Common.Unit_EdeTexts)
                        unitsWriter.WriteLine($"{(int)unit.Key};{unit.Value};");
                }
            }
        }
        /// <summary>
        /// Gathers a devices EDE data and writes it to a file stream.
        /// </summary>
        private async Task exportDeviceEDEFile(BACnetDevice device, StreamWriter edeWriter, List<string> stateTextReferences)
        {
            // If the device doesn't support Read Prop. Multiple, or if the object list exceeds the max. APDU (common
            // for MSTP devices), we will end up in the AddObjectListOneByOneAsync method. But this method would otherwise
            // continue and try and export the EDE without any objects in the list... so we must wait for the One by One
            // to complete. This is achieved by checking the object sender parameter in m)DeviceTree_Afterselect - if it is
            // edeWriter, then we will NOT use AddObjectListOneByOneAsync, but we use AddObjectListOneByOne instead.
            // This is kind of a dirty hack, however moth other easy ways to achieve this will be dirty as well, and introduce
            // a lot of code to handle the aggregation of data between the UI thread and ThreadPool threads.

            // update devices
            var objList = await device.GetObjectListAsync();
            var objects = objList.Values.SelectMany(item => item).ToArray();

            this.Cursor = Cursors.WaitCursor;
            Application.DoEvents();

            try
            {
                // Read 6 (textual) properties even if not existing in the given object
                var readProp = new BacnetPropertyIds[] {
                    BacnetPropertyIds.PROP_OBJECT_NAME,
                    BacnetPropertyIds.PROP_DESCRIPTION,
                    BacnetPropertyIds.PROP_UNITS,
                    BacnetPropertyIds.PROP_STATE_TEXT,
                    BacnetPropertyIds.PROP_INACTIVE_TEXT,
                    BacnetPropertyIds.PROP_ACTIVE_TEXT
                };

                // Object list is already in the AddressSpaceTree, so no need to query it again
                foreach (var obj in objects)
                {
                    // Read properties:
                    var properties = await obj.GetPropertiesAsync(false, readProp);

                    properties.TryGetValue(BacnetPropertyIds.PROP_OBJECT_NAME, out string idfr);
                    properties.TryGetValue(BacnetPropertyIds.PROP_DESCRIPTION, out string descr);
                    properties.TryGetValue(BacnetPropertyIds.PROP_UNITS, out string unitCode);
                    properties.TryGetValue(BacnetPropertyIds.PROP_STATE_TEXT, out IList<string> stateText);
                    properties.TryGetValue(BacnetPropertyIds.PROP_INACTIVE_TEXT, out string inactiveText);
                    properties.TryGetValue(BacnetPropertyIds.PROP_ACTIVE_TEXT, out string activeText);

                    // Write state texts:
                    int? stateTextIdx = null;
                    IEnumerable<string> stateTexts = null;
                    if (stateText != null)
                        stateTexts = stateText;
                    else if ((!string.IsNullOrEmpty(inactiveText)) && (!string.IsNullOrEmpty(activeText)))
                        stateTexts = new string[] { inactiveText, activeText };

                    if (stateTexts != null)
                    {
                        var line = string.Join(";", stateTexts);
                        stateTextIdx = stateTextReferences.IndexOf(line);
                        if (stateTextIdx == -1)
                        {
                            stateTextIdx = stateTextReferences.Count;
                            stateTextReferences.Add(line);
                        }
                        else
                            ; // Use previously created reference.
                    }
                    edeWriter.WriteLine(string.Format("{0};{1};{2};{3};{4};{5};;;;;;;;{6};{7}",
                        obj.ObjectId,
                        device.InstanceId,
                        idfr,
                        (int)obj.ObjectId.type,
                        obj.ObjectId.instance,
                        descr,
                        stateTextIdx,
                        unitCode
                    ));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Export failed:\r\n{ex.Message}", EDE_EXPORT_TITLE, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                this.Cursor = Cursors.Default;
            }
        }

        private void foreignDeviceRegistrationToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //fetch end point
            BacnetClient comm = null;
            try
            {
                if (m_DeviceTree.SelectedNode == null) return;
                else if (m_DeviceTree.SelectedNode.Tag == null) return;
                else if (!(m_DeviceTree.SelectedNode.Tag is BacnetClient)) return;
                comm = (BacnetClient)m_DeviceTree.SelectedNode.Tag;
            }
            finally
            {

                if (comm == null) MessageBox.Show(this, "Please select an \"IP transport\" node first", "Wrong node", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }

            Form F = new ForeignRegistry(comm);
            F.ShowDialog();
        }

        private void alarmSummaryToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            alarmSummaryToolStripMenuItem_Click(sender, e);
        }

        private void alarmSummaryToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //fetch end point
            BacnetClient comm = null;
            BacnetAddress adr;
            uint device_id;

            FetchEndPoint(out comm, out adr, out device_id);

            if (comm == null)
            {
                MessageBox.Show(this, "Please select a device node", "Wrong node", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            new AlarmSummary(m_AddressSpaceTree.ImageList, comm, adr, device_id, DevicesObjectsName).ShowDialog();
        }

        // Read the Adress Space, and change all object Id by name
        // Popup ToolTipText Get Properties Name
        private void readPropertiesNameToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //fetch end point
            BacnetClient comm = null;
            BacnetAddress adr;
            uint device_id;

            FetchEndPoint(out comm, out adr, out device_id);

            if (comm == null)
            {
                MessageBox.Show(this, "Please select a device node", "Wrong node", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
           
            // Go
            ChangeObjectIdByName(m_AddressSpaceTree.Nodes, comm, adr);

        }

        // In the Objects TreeNode, get all elements without the Bacnet PROP_OBJECT_NAME not Read out
        private void GetRequiredObjectName(TreeNodeCollection tnc, List<BacnetReadAccessSpecification> bras)
        {
            foreach (TreeNode tn in tnc)
            {
                if ((tn.ToolTipText == "")&&(tn.Tag!=null))
                {
                    if (!bras.Exists(o => o.objectIdentifier.Equals((BacnetObjectId)tn.Tag)))
                        bras.Add(new BacnetReadAccessSpecification((BacnetObjectId)tn.Tag, new BacnetPropertyReference[] { new BacnetPropertyReference((uint)BacnetPropertyIds.PROP_OBJECT_NAME, System.IO.BACnet.Serialize.ASN1.BACNET_ARRAY_ALL) }));
                }
                if (tn.Nodes != null)
                    GetRequiredObjectName(tn.Nodes, bras);
            }
        }
        // In the Objects TreeNode, set all elements with the ReadPropertyMultiple response
        private void SetObjectName(TreeNodeCollection tnc, IList<BacnetReadAccessResult> result, BacnetAddress adr)
        {
            foreach (TreeNode tn in tnc)
            {
                BacnetObjectId b=(BacnetObjectId)tn.Tag;

                try
                {
                    if (tn.ToolTipText == "")
                    {
                        BacnetReadAccessResult r = result.Single(o => o.objectIdentifier.Equals(b));
                        // ChangeTreeNodePropertyName(tn, r.values[0].value[0].ToString());
                        lock (DevicesObjectsName)
                        {
                            var t = new Tuple<String, BacnetObjectId>(adr.FullHashString(), (BacnetObjectId)tn.Tag);
                            DevicesObjectsName.Remove(t); // sometimes the same object appears at several place (in Groups for instance).
                            DevicesObjectsName.Add(t, r.values[0].value[0].ToString());
                            objectNamesChangedFlag = true;
                        }
                    }
                }
                catch { }

                if (tn.Nodes != null)
                    SetObjectName(tn.Nodes, result, adr);
            }

        }
        // Try a ReadPropertyMultiple for all PROP_OBJECT_NAME not already known
        private void ChangeObjectIdByName(TreeNodeCollection tnc, BacnetClient comm, BacnetAddress adr)
        {
            int _retries = comm.Retries;
            comm.Retries = 1;
            bool IsOK = false;

            List<BacnetReadAccessSpecification> bras = new List<BacnetReadAccessSpecification>();
            GetRequiredObjectName(tnc, bras);

            if (bras.Count==0)
                IsOK = true;
            else
            {
                this.Cursor = Cursors.WaitCursor;
                try
                {
                    IList<BacnetReadAccessResult> result = null;
                    if (comm.ReadPropertyMultipleRequest(adr, bras, out result) == true)
                    {
                        SetObjectName(tnc, result, adr);
                        IsOK = true;
                    }
                }
                catch{}
            }
            
            if (IsOK)
            {
                // We did not update the tree as we went (for speed), so do it all at once now
                m_DeviceTree_AfterSelect(null, new TreeViewEventArgs(this._selectedDevice));
                this.Cursor = Cursors.Default;
            }
            else
            {
                // Fail, so go One by One, in a background thread
                System.Threading.ThreadPool.QueueUserWorkItem((o) =>
                {
                    ChangeObjectIdByNameOneByOne(m_AddressSpaceTree.Nodes, comm, adr, AsynchRequestId);
                });
                this.Cursor = Cursors.Default;
            }
        }

        private void ChangeObjectIdByNameOneByOne(TreeNodeCollection tnc, BacnetClient comm, BacnetAddress adr, int AsynchRequestId)
        {
            int _retries = comm.Retries;
            comm.Retries = 1;

            foreach (TreeNode tn in tnc)
            {
                if ((tn.ToolTipText == "") && (tn.Tag != null))
                {
                    IList<BacnetValue> name;
                    try
                    {
                        if (comm.ReadPropertyRequest(adr, (BacnetObjectId)tn.Tag, BacnetPropertyIds.PROP_OBJECT_NAME, out name) == true)
                        {
                            if (AsynchRequestId != this.AsynchRequestId) // Selected device is no more the good one
                            {
                                comm.Retries = _retries;
                                return;
                            }

                            this.Invoke((MethodInvoker)delegate
                            {
                                if (AsynchRequestId != this.AsynchRequestId) return; // another test in the GUI thread

                                // We are already going on-by-one (SLOW), in a different thread, so just update
                                // as we go. Don't bother optimising (Tested and only ~15% faster in this case)
                                ChangeTreeNodePropertyName(tn, name[0].Value.ToString());

                                lock (DevicesObjectsName)
                                {
                                    var t = new Tuple<String, BacnetObjectId>(adr.FullHashString(), (BacnetObjectId)tn.Tag);
                                    DevicesObjectsName.Remove(t); // sometimes the same object appears at several place (in Groups for instance).
                                    DevicesObjectsName.Add(t, name[0].Value.ToString());
                                    objectNamesChangedFlag = true;
                                }
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        Trace.TraceWarning("Failed to obtain object name for object " + tn.Tag + ": " + ex);
                    }
                }

                if (tn.Nodes != null)
                    ChangeObjectIdByNameOneByOne(tn.Nodes, comm, adr, AsynchRequestId);

                comm.Retries = _retries;
            }
        }

        // Open a serialized Dictionnay of object id <-> object name file
        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //which file to upload?
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.InitialDirectory = Path.GetDirectoryName(Properties.Settings.Default.Auto_Store_Object_Names_File);
            dlg.DefaultExt = "YabeMap";
            dlg.Filter = "Yabe Map files (*.YabeMap)|*.YabeMap|All files (*.*)|*.*";
            if (dlg.ShowDialog(this) != System.Windows.Forms.DialogResult.OK) return;
            string filename = dlg.FileName;
            

            try
            {
                Stream stream = File.Open(filename, FileMode.Open);
                BinaryFormatter bf = new BinaryFormatter();
                var d = (Dictionary<Tuple<String, BacnetObjectId>, String>)bf.Deserialize(stream);
                stream.Close();

                if (d != null)
                {
                    DevicesObjectsName = d;
                    objectNamesChangedFlag = true;
                    Trace.TraceInformation("Loaded object names from \"" + filename + "\".");
                }

                Properties.Settings.Default.Auto_Store_Object_Names_File = filename;
                Properties.Settings.Default.Auto_Store_Object_Names = true;
                Properties.Settings.Default.Save();
            }
            catch
            {
                MessageBox.Show(this, "File error", "Wrong file", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        // save a serialized Dictionnay of object id <-> object name file
        private void saveAsToolStripMenuItem_Click(object sender, EventArgs e)
        {

            SaveFileDialog dlg = new SaveFileDialog();
            dlg.InitialDirectory = Path.GetDirectoryName(Properties.Settings.Default.Auto_Store_Object_Names_File);
            dlg.DefaultExt = "YabeMap";
            dlg.Filter = "Yabe Map files (*.YabeMap)|*.YabeMap|All files (*.*)|*.*";
            if (dlg.ShowDialog(this) != System.Windows.Forms.DialogResult.OK) return;
            string filename = dlg.FileName;
            try
            {
                Stream stream = File.Open(filename, FileMode.Create);
                BinaryFormatter bf = new BinaryFormatter();
                bf.Serialize(stream, DevicesObjectsName);
                stream.Close();
                Trace.TraceInformation("Saved object names to \"" + filename + "\".");

                Properties.Settings.Default.Auto_Store_Object_Names_File = filename;
                Properties.Settings.Default.Auto_Store_Object_Names = true;
                Properties.Settings.Default.Save();
            }
            catch
            {
                MessageBox.Show(this, "File error", "Wrong file", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }

        }

        private void createObjectToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            createObjectToolStripMenuItem_Click(sender, e);
        }
        private void createObjectToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //fetch end point
            BacnetClient comm = null;
            BacnetAddress adr;
            uint device_id;

            FetchEndPoint(out comm, out adr, out device_id);

            if (comm == null)
                return;

            CreateObject F = new CreateObject();
            if (F.ShowDialog() == DialogResult.OK)
            {

                try
                {
                    BacnetPropertyValue[] initialvalues = null;

                    if (F.ObjectName.Text != null) // Add the initial propery name
                    {
                        initialvalues = new BacnetPropertyValue[1];
                        initialvalues[0] = new BacnetPropertyValue();
                        initialvalues[0].property.propertyIdentifier = (uint)BacnetPropertyIds.PROP_OBJECT_NAME;
                        initialvalues[0].property.propertyArrayIndex = System.IO.BACnet.Serialize.ASN1.BACNET_ARRAY_ALL;
                        initialvalues[0].value = new BacnetValue[1];
                        initialvalues[0].value[0] = new BacnetValue(F.ObjectName.Text);
                    }
                    comm.CreateObjectRequest(adr, new BacnetObjectId((BacnetObjectTypes)F.ObjectType.SelectedIndex, (uint)F.ObjectId.Value), initialvalues);

                    m_DeviceTree_AfterSelect(null, new TreeViewEventArgs(this._selectedDevice));
                }
                catch (Exception ex)
                {
                    Trace.TraceError("Error : " + ex.Message);
                    MessageBox.Show("Fail to Create Object","CreateObject", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }

        }

        private void editBBMDTablesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            BacnetClient comm = null;
            BacnetAddress adr;
            uint device_id;

            FetchEndPoint(out comm, out adr, out device_id);

            if ((comm != null) && (comm.Transport is BacnetIpUdpProtocolTransport) && (adr != null) && (adr.RoutedSource == null))
                new BBMDEditor(comm, adr).ShowDialog();
            else
                MessageBox.Show("An IPv4 device is required", "Wrong device", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        private void cleanToolStripMenuItem_Click(object sender, EventArgs e)
        {             
            DialogResult res = MessageBox.Show(this, "Clean all "+DevicesObjectsName.Count.ToString()+" entries from \""+Properties.Settings.Default.Auto_Store_Object_Names_File+"\", really?", "Name database suppression", MessageBoxButtons.OKCancel, MessageBoxIcon.Information);
            if (res == DialogResult.OK)
            {
                DevicesObjectsName = new Dictionary<Tuple<String, BacnetObjectId>, String>();
                Trace.TraceInformation("Created new object names dictionary.");
                objectNamesChangedFlag = true;
                DoSaveObjectNames();
                // Enumerate each Transport Layer:
                foreach (TreeNode transport in m_DeviceTree.Nodes[0].Nodes)
                {
                    //Enumerate each Parent Device:
                    foreach (TreeNode node in transport.Nodes)
                    {
                        try
                        {
                            if (node.Tag is BACnetDevice entry)
                            {
                                node.Text = entry.ToString(false);
                                node.ToolTipText = "";
                            }

                        }
                        catch(Exception)
                        {
                        }

                        //Enumerate routed nodes
                        foreach (TreeNode subNode in node.Nodes)
                        {
                            try
                            {
                                if (subNode.Tag is BACnetDevice entry)
                                {
                                    subNode.Text = entry.ToString(true);
                                    subNode.ToolTipText = "";
                                }
                                
                            }
                            catch(Exception)
                            {
                            }
                        }
                    }
                }

                m_DeviceTree.SelectedNode = null;
                m_AddressSpaceTree.SelectedNode = null;
                m_AddressSpaceTree.Nodes.Clear();
                m_DataGrid.SelectedObject = null;
                _selectedDevice = null;
                _selectedNode = null;
            }
        }

        // Change the WritePriority Value
        private void MainDialog_KeyDown(object sender, KeyEventArgs e)
        {
            if ((e.Modifiers == (Keys.Control | Keys.Alt)))
            {

                if ((e.KeyCode >= Keys.D0 && e.KeyCode <= Keys.D9) || (e.KeyCode >= Keys.NumPad0 && e.KeyCode <= Keys.NumPad9))
                {
                    string s = e.KeyCode.ToString();
                    int i = Convert.ToInt32(s[s.Length-1]) - 48;

                    Properties.Settings.Default.DefaultWritePriority = (BacnetWritePriority)i;
                    SystemSounds.Beep.Play();
                    Trace.WriteLine("WritePriority change to level " + i.ToString() + " : " + ((BacnetWritePriority)i).ToString());
                }
            }
        }

        #region "Alarm Logger"

        StreamWriter AlarmFileWritter;
        object lockAlarmFileWritter = new object();

        private void EventAlarmLogToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (AlarmFileWritter != null)
            {
                lock (lockAlarmFileWritter)
                {
                    AlarmFileWritter.Close();
                    EventAlarmLogToolStripMenuItem.Text = "Start saving Cov/Event/Alarm Log";
                    AlarmFileWritter = null;
                }
                return;

            }

            //which file to use ?
            SaveFileDialog dlg = new SaveFileDialog();
            dlg.DefaultExt = ".csv";
            dlg.Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*";
            if (dlg.ShowDialog(this) != System.Windows.Forms.DialogResult.OK) return;
            string filename = dlg.FileName;

            try
            {
                AlarmFileWritter = new StreamWriter(filename);
                AlarmFileWritter.WriteLine("Device;ObjectId;Name;Value;Time;Status;Description");
                EventAlarmLogToolStripMenuItem.Text="Stop saving Cov/Event/Alarm Log";                
            }
            catch
            {
                MessageBox.Show(this, "File error", "Unable to open the file", MessageBoxButtons.OK, MessageBoxIcon.Error);
                AlarmFileWritter = null;
            }
        }
        // Event/Alarm logging
        private void AddLogAlarmEvent(ListViewItem itm)
        {
            lock (lockAlarmFileWritter)
            {
                if (AlarmFileWritter != null)
                {
                    for (int i = 1; i < itm.SubItems.Count-2; i++)
                    {
                        AlarmFileWritter.Write(((i != 1) ? ";" : "") + itm.SubItems[i].Text);
                    }
                    AlarmFileWritter.WriteLine();
                    AlarmFileWritter.Flush();
                }
            }
        }

        #endregion

        private void btnExport_Click(object sender, EventArgs e)
        {
            ExportCovGraph();
        }

        private async void m_AddressSpaceTree_AfterSelect(object sender, TreeViewEventArgs e)
        {
            this.m_SubscriptionView.SelectedItems.Clear();
            await UpdateGrid(e.Node);
            BacnetClient cl; BacnetAddress ba; BacnetObjectId objId;

            // Hide all elements in the toolstip menu
            foreach (object its in m_AddressSpaceMenuStrip.Items)
                (its as ToolStripMenuItem).Visible = false;
            // Set Subscribe always visible
            m_AddressSpaceMenuStrip.Items[0].Visible = true;
            // Set Search always visible
            m_AddressSpaceMenuStrip.Items[8].Visible = true;

            // Get the node type
            GetObjectLink(out cl, out ba, out objId, BacnetObjectTypes.MAX_BACNET_OBJECT_TYPE);
            // Set visible some elements depending of the object type
            switch (objId.type)
            {
                case BacnetObjectTypes.OBJECT_FILE:
                    m_AddressSpaceMenuStrip.Items[1].Visible = true;
                    m_AddressSpaceMenuStrip.Items[2].Visible = true;
                    break;

                case BacnetObjectTypes.OBJECT_TRENDLOG:
                case BacnetObjectTypes.OBJECT_TREND_LOG_MULTIPLE:
                    m_AddressSpaceMenuStrip.Items[3].Visible = true;
                    break;

                case BacnetObjectTypes.OBJECT_SCHEDULE:
                    m_AddressSpaceMenuStrip.Items[4].Visible = true;
                    break;

                case BacnetObjectTypes.OBJECT_NOTIFICATION_CLASS:
                    m_AddressSpaceMenuStrip.Items[5].Visible = true;
                    break;

                case BacnetObjectTypes.OBJECT_CALENDAR:
                    m_AddressSpaceMenuStrip.Items[6].Visible = true;
                    break;
            }

            // Allows delete menu 
            if (objId.type != BacnetObjectTypes.OBJECT_DEVICE)
                m_AddressSpaceMenuStrip.Items[7].Visible = true;

        }

        private async void m_SubscriptionView_SelectedIndexChanged(object sender, EventArgs e)
        {
            ListView.SelectedListViewItemCollection selectedSubscriptions = this.m_SubscriptionView.SelectedItems;
            if(selectedSubscriptions==null || selectedSubscriptions.Count==0)
            {
                return;
            }

            this.m_AddressSpaceTree.SelectedNode = null;
            this.m_AddressSpaceTree.SelectedNodes.Clear();

            ListViewItem itm = selectedSubscriptions[0];

            if(itm.Tag==null)
            {
                return;
            }

            if(itm.Tag is Subscription subscription)
            {
                await UpdateGrid(subscription);
            }
        }

        private void btnPlay_Click(object sender, EventArgs e)
        {
            TogglePlotter();
        }

        private void m_SubscriptionView_ItemChecked(object sender, ItemCheckedEventArgs e)
        {
            if(e.Item.Tag is Subscription sub)
            {
                lock (m_subscription_list)
                {
                    try
                    {
                        RollingPointPairList points = m_subscription_points[sub.sub_key];
                        foreach (LineItem li in Pane.CurveList)
                            if (li.Points == points)
                            {
                                li.IsVisible = e.Item.Checked;
                                e.Item.SubItems[9].Text = e.Item.Checked.ToString();
                                CovGraph.AxisChange();
                                CovGraph.Invalidate();
                                break;
                            }
                    }
                    catch { }
                }
            }
            else
            {
                e.Item.Checked = false;
            }
        }

        private void ClearPlotterButton_Click(object sender, EventArgs e)
        {
            lock (m_subscription_list)
            {
                foreach (RollingPointPairList p in m_subscription_points.Values)
                {
                    try
                    {
                        p.Clear();
                    }
                    catch { }
                }
                CovGraph.AxisChange();
                CovGraph.Invalidate();
            }
        }

        private void pollRateSelector_ValueChanged(object sender, EventArgs e)
        {
            uint period = Math.Max(Math.Min((uint)((NumericUpDown)sender).Value, MAX_POLL_PERIOD), MIN_POLL_PERIOD);
            Properties.Settings.Default.Subscriptions_ReplacementPollingPeriod = period;
        }

        private void PollOpn_CheckedChanged(object sender, EventArgs e)
        {
            if(PollOpn.Checked)
            {
                pollRateSelector.Enabled = true;
                Properties.Settings.Default.UsePollingByDefault = true;
            }
            else
            {
                pollRateSelector.Enabled = false;
                Properties.Settings.Default.UsePollingByDefault = false;
            }
        }

        private int _saveFaultCount = 0;
        private void SaveObjectNamesTimer_Tick(object sender, EventArgs e)
        {
            int intervalMinutes = Math.Max(Math.Min(Properties.Settings.Default.Auto_Store_Period_Minutes, 480), 1);
            if (intervalMinutes != Properties.Settings.Default.Auto_Store_Period_Minutes)
                Properties.Settings.Default.Auto_Store_Period_Minutes = intervalMinutes;
            SaveObjectNamesTimer.Interval = intervalMinutes * 60000;
            
            DoSaveObjectNamesIfNecessary();
        }

        private void DoSaveObjectNamesIfNecessary(string path = null)
        {
            if (Properties.Settings.Default.Auto_Store_Object_Names)
            {
                if (objectNamesChangedFlag)
                {
                    DoSaveObjectNames();
                }
            }
        }

        private void DoSaveObjectNames(string path = null)
        {
            string fileTotal;
            if(string.IsNullOrWhiteSpace(path))
            {
                fileTotal = Properties.Settings.Default.Auto_Store_Object_Names_File;
            }
            else
            {
                fileTotal = path;
            }

            if (!string.IsNullOrWhiteSpace(fileTotal))
            {
                try
                {
                    string file = Path.GetFileName(fileTotal);
                    string directory = Path.GetDirectoryName(fileTotal);
                    if (string.IsNullOrWhiteSpace(file))
                    {
                        if (path == null)
                        {
                            file = "Auto_Stored_Object_Names.YabeMap";
                        }
                        else
                        {
                            DateTime d = DateTime.Now;
                            file = "New_Object_Names_File_" + d.Year.ToString() + "-" + d.Month.ToString() + "-" + d.Day.ToString() + "_" + d.Hour.ToString() + "_" + d.Minute.ToString() + ".YabeMap";
                        }
                        fileTotal = Path.Combine(directory, file);
                        Properties.Settings.Default.Auto_Store_Object_Names_File = fileTotal;
                    }

                    if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
                    {
                        try
                        {
                            Directory.CreateDirectory(directory);
                        }
                        catch (UnauthorizedAccessException)
                        {
                            Trace.TraceError("Error trying to auto-save object names to file: The directory \"" + directory + "\" does not exist, and Yabe does not have permissions to automatically create this directory. Try changing the Auto_StoreObject_Names_File setting to a different path.");
                            Properties.Settings.Default.Auto_Store_Object_Names = false;
                            return;
                        }
                    }

                }
                catch (Exception ex)
                {
                    Trace.TraceError("Exception trying to auto-save object names to file: " + ex.Message + ". Try resetting the Auto_StoreObject_Names_File setting to a valid file path.");
                    Properties.Settings.Default.Auto_Store_Object_Names = false;
                    return;
                }

                try
                {
                    Stream stream = File.Open(fileTotal, FileMode.Create);
                    BinaryFormatter bf = new BinaryFormatter();
                    bf.Serialize(stream, DevicesObjectsName);
                    stream.Close();
                    objectNamesChangedFlag = false;
                    _saveFaultCount = 0;
                    Trace.TraceInformation("Saved object names to \"" + fileTotal + "\".");
                }
                catch (Exception ex)
                {
                    _saveFaultCount++;
                    int maxFault = 3;
                    if (_saveFaultCount >= maxFault)
                    {
                        Trace.TraceError(String.Format("Exception trying to auto-save object names to file: " + ex.Message + ". We will retry {0} more time(s).", maxFault - _saveFaultCount));
                    }
                    else
                    {
                        Trace.TraceError(String.Format("Exception trying to auto-save object names to file: " + ex.Message + ". This error happened {0} times, so auto-save is being disabled. Try resetting the Auto_StoreObject_Names_File setting to a valid file path.", _saveFaultCount));
                        Properties.Settings.Default.Auto_Store_Object_Names = false;
                        return;
                    }
                }
            }
            else
            {
                Trace.TraceError("Error trying to auto-save object names to file: There is no file specified. Try resetting the Auto_StoreObject_Names_File setting to a valid file path.");
                Properties.Settings.Default.Auto_Store_Object_Names = false;
                return;
            }
        }

        private void HighlightTreeNodes(TreeNodeCollection nodes, String text, Color color)
        {
            foreach (TreeNode node in nodes)
            {
                HighlightTreeNode(node, text, color);
                HighlightTreeNodes(node.Nodes, text, color);
            }
        }
        private void HighlightTreeNode(TreeNode node) => HighlightTreeNode(node, TbxHighlightAddress.Text, Color.Red);
        private void HighlightTreeNode(TreeNode node, String text, Color color)
        {
            if ((!string.IsNullOrEmpty(text)) && (node.Text.ToLower().Contains(text.ToLower())))
                node.ForeColor = color;
            else
                node.ForeColor = Color.Black;
        }

        private void TbxHighlightTreeView_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode != Keys.Return) return;

            TbxHighlightTreeView_Update(sender, null);
        }

        private void TbxHighlightTreeView_Update(object sender, EventArgs e)
        {
            var color = Color.Red;
            var tbx = (TextBox)sender;
            if ((tbx.Text == null) || (tbx.Text.Length == 0))
                // Clear:
                color = Color.Black;

            TreeView control;
            if (tbx == TbxHighlightAddress)
                control = m_AddressSpaceTree;
            else
                control = m_DeviceTree;
            HighlightTreeNodes(control.Nodes, tbx.Text, color);
        }

        private async void manual_refresh_properties_Click(object sender, EventArgs e)
        {
            // perform manual update
            if (_selectedNode != null)
            {
                if (_selectedNode is Subscription sub)
                {
                    await UpdateGrid(sub);
                }
                else if (_selectedNode is TreeNode node)
                {
                    await UpdateGrid(node);
                }
                else
                {
                    _selectedNode = null;
                    m_DataGrid.SelectedObject = null;
                    return;
                }
            }
            else
            {
                m_DataGrid.SelectedObject = null;
                return;
            }
        }
        private async void manual_refresh_objects_Click(object sender, EventArgs e)
        {
            if (SelectedDevice != null)
            {
                await SelectedDevice.GetObjectListAsync(true);

                m_DeviceTree_AfterSelect(null, new TreeViewEventArgs(this._selectedDevice));
            }
        }

        private void menuStrip1_MenuActivate(object sender, EventArgs e)
        {
            // Updates controls' enabled state:
            FetchEndPoints(out var endPoints);
            exportEDEFilesSelDeviceToolStripMenuItem.Enabled = FetchEndPoint(out _, out _, out _);
            exportEDEFilesAllDevicesToolStripMenuItem.Enabled = (endPoints.Count >= 1);
        }

        private async Task DoAckAsync(BacnetEventNotificationData.BacnetEventStates eventState)
        {
            this.Cursor = Cursors.WaitCursor;
            try
            {
                KeyValuePair<BacnetAddress, uint> entry;
                BacnetAddress adr;
                BacnetClient comm;

                if (_selectedNode == null) return;

                if (_selectedNode is TreeNode treeNode)
                {
                    //fetch end point
                    if (_selectedDevice == null) return;
                    else if (_selectedDevice.Tag == null) return;
                    else if (!(_selectedDevice.Tag is KeyValuePair<BacnetAddress, uint>)) return;
                    entry = (KeyValuePair<BacnetAddress, uint>)_selectedDevice.Tag;
                    adr = entry.Key;
                    if (_selectedDevice.Parent.Tag is BacnetClient)
                        comm = (BacnetClient)_selectedDevice.Parent.Tag;
                    else
                        comm = (BacnetClient)_selectedDevice.Parent.Parent.Tag;  // routed node

                    if (treeNode.Tag is BacnetObjectId object_id)
                    {
                        BacnetGenericTime ackT = new BacnetGenericTime();
                        BacnetGenericTime evtT = new BacnetGenericTime();
                        evtT.Tag = BacnetTimestampTags.TIME_STAMP_DATETIME;
                        switch(eventState)
                        {
                            case BacnetEventNotificationData.BacnetEventStates.EVENT_STATE_NORMAL:
                                evtT.Time = _cachedEventTimeStampsForAcknowledgementButtons[2];
                                break;
                            case BacnetEventNotificationData.BacnetEventStates.EVENT_STATE_FAULT:
                                evtT.Time = _cachedEventTimeStampsForAcknowledgementButtons[1];
                                break;
                            case BacnetEventNotificationData.BacnetEventStates.EVENT_STATE_OFFNORMAL:
                                evtT.Time = _cachedEventTimeStampsForAcknowledgementButtons[0];
                                break;
                        }

                        ackT.Tag = BacnetTimestampTags.TIME_STAMP_DATETIME;
                        ackT.Time = DateTime.Now;
                        if (comm.AlarmAcknowledgement(adr, object_id, eventState, eventState.ToString() + " acked manually in Yabe", evtT, ackT))
                        {
                            await UpdateGrid(treeNode).ConfigureAwait(false);
                        }
                        else
                        {
                            AckFail();
                            return;
                        }
                    }
                    else
                    {
                        return;
                    }
                }
                else if (_selectedNode is Subscription subscription)
                {
                    adr = subscription.device.Address;
                    comm = subscription.device.Client;

                    BacnetObjectId object_id = subscription.object_id;

                    BacnetGenericTime ackT = new BacnetGenericTime();
                    BacnetGenericTime evtT = new BacnetGenericTime();
                    evtT.Tag = BacnetTimestampTags.TIME_STAMP_DATETIME;
                    switch (eventState)
                    {
                        case BacnetEventNotificationData.BacnetEventStates.EVENT_STATE_NORMAL:
                            evtT.Time = _cachedEventTimeStampsForAcknowledgementButtons[2];
                            break;
                        case BacnetEventNotificationData.BacnetEventStates.EVENT_STATE_FAULT:
                            evtT.Time = _cachedEventTimeStampsForAcknowledgementButtons[1];
                            break;
                        case BacnetEventNotificationData.BacnetEventStates.EVENT_STATE_OFFNORMAL:
                            evtT.Time = _cachedEventTimeStampsForAcknowledgementButtons[0];
                            break;
                    }

                    ackT.Tag = BacnetTimestampTags.TIME_STAMP_DATETIME;
                    ackT.Time = DateTime.Now;

                    if (comm.AlarmAcknowledgement(adr, object_id, eventState, eventState.ToString() + " acked manually in Yabe", evtT, ackT))
                    {
                        await UpdateGrid(subscription).ConfigureAwait(false);
                    }
                    else
                    {
                        AckFail();
                        return;
                    }
                }
                else
                {
                    return;
                }
            }
            finally
            {
                this.Cursor = Cursors.Default;
            }
        }

        private void AckFail()
        {
            MessageBox.Show("Alarm acknowledge failed!","Error",MessageBoxButtons.OK,MessageBoxIcon.Warning);
        }

        private async void ack_offnormal_Click(object sender, EventArgs e) => await DoAckAsync(BacnetEventNotificationData.BacnetEventStates.EVENT_STATE_OFFNORMAL);
        private async void ack_fault_Click(object sender, EventArgs e) => await DoAckAsync(BacnetEventNotificationData.BacnetEventStates.EVENT_STATE_FAULT);
        private async void ack_normal_Click(object sender, EventArgs e) => await DoAckAsync(BacnetEventNotificationData.BacnetEventStates.EVENT_STATE_NORMAL);

        private void searchToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            GenericInputBox<TextBox> search = new GenericInputBox<TextBox>("Search object", "Name",     (o) =>
            {
                o.Text = m_AddressSpaceTree.SelectedNode.Text;
            });

            if (search.ShowDialog() == DialogResult.OK)
            {
                string find = search.genericInput.Text.ToLower();
                foreach (TreeNode tn in m_AddressSpaceTree.Nodes)
                {
                    if (tn.Text.ToLower().Contains(find))
                    {
                        tn.EnsureVisible();
                        m_AddressSpaceTree.SelectedNode = tn;                       
                        break;
                    }
                }
            }
        }

        /********************************
         * was used for testing the async interface, no longer needed 
                /// <summary>
                /// Adds debug menu for test and developing purposes to the main menu.
                /// </summary>
                [Conditional("DEBUG")]
                public void AddDebugMenu()
                {
                    var debugMenu = new ToolStripMenuItem("Debug");
                    debugMenu.DropDownItems.AddRange(new ToolStripItem[] {
                        new ToolStripMenuItem("Cancel communication", null, (sender, args) => SelectedDevice?.Client.CommunicationToken.Cancel()),
                        new ToolStripSeparator(),
                        new ToolStripMenuItem("Test", null, (sender, args) => {
                            var device = SelectedDevice;

                            var objList = device.GetObjectListAsync().Result;
                            //var obj = objList.Values.First().First();
                            var obj = objList.Values.ElementAt(13).First();
                            var props = obj.GetPropertiesAsync().Result;

                        })
                    }); ;
                    menuStrip1.Items.Add(debugMenu);
                }
        **************************************************/
    }
    // Used to sort the devices Tree by device_id
    public class NodeSorter : IComparer
    {
        public int Compare(object x, object y)
        {
            // Two device, compare the device_id
            var tx = (TreeNode)x;
            var ty = (TreeNode)y;
            if ((tx?.Tag is BACnetDevice entryx) && (ty?.Tag is BACnetDevice entryy))
                return entryx.InstanceId.CompareTo(entryy.InstanceId);
            else // something must be provide
                return tx.Text.CompareTo(ty.Text);
        }
    }

    public enum AddressTreeViewType
    {
        List,
        Structured,
        Both
    }
}
