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

using Mstp.BacnetCapture;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.IO.BACnet;
using System.Linq;
using System.Media;
using System.Net;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Xml.Serialization;
using WebSocketSharp;
using ZedGraph;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.ProgressBar;

namespace Yabe
{
    public partial class YabeMainDialog : Form
    {
        private const int MIN_POLL_PERIOD = 100; //ms
        private const int MAX_POLL_PERIOD = 120000; //ms

        public class BacnetDeviceLine
        {
            public List<BACnetDevice> Devices = new List<BACnetDevice>();
            public HashSet<byte> mstp_sources_seen = new HashSet<byte>();
            public HashSet<byte> mstp_pfm_destinations_seen = new HashSet<byte>();

        }
        private Dictionary<BacnetClient, BacnetDeviceLine> m_devices = new Dictionary<BacnetClient, BacnetDeviceLine>();
        public Dictionary<BacnetClient, BacnetDeviceLine> DiscoveredDevices { get { return m_devices; } }
        public int DeviceCount
        {
            get {
                int count = 0;
                lock (m_devices)
                    foreach (var entry in m_devices)
                        count += entry.Value.Devices.Count;

                return count;
            }
        }
        public BACnetDevice[] YabeDiscoveredDevices
        {
            get
            {
                lock (m_devices)
                {
                    return (m_devices.Values.SelectMany(line => line.Devices).ToArray());
                }
            }
        }
        // Selection can be done in the address space or in the event/sub grip, could be a TreeNode or a Subscription
        private object _selectedNode = null;
        // Only In the Device TreeView
        private TreeNode _selectedDevice = null;

        List<BacnetObjectId> _structuredViewParents = null;

        // 0=Offnormal  1=Fault   2=Normal
        private DateTime[] _cachedEventTimeStampsForAcknowledgementButtons = new DateTime[3];

        private Dictionary<string, ListViewItem> m_subscription_list = new Dictionary<string, ListViewItem>();
        private Dictionary<string, RollingPointPairList> m_subscription_points = new Dictionary<string, RollingPointPairList>();        
        Color[] GraphColor = {Color.Red, Color.Blue, Color.Green, Color.Violet, Color.Chocolate, Color.Orange};
        GraphPane Pane;
        private ManualResetEvent _plotterPause;
        private bool _plotterRunningFlag = true; // Change this one initial value to make the graphs start paused (false) or in play mode (true).
        private const string PLAY_BUTTON_TEXT_WHEN_RUNNING = "Pause Plotter";
        private const string PLAY_BUTTON_TEXT_WHEN_PAUSED = "Resume Plotter";
        private Random _rand = new Random();
        private uint m_next_subscription_id = 0;

        YabeDevice m_Server;

        YabeMainDialog yabeFrm; // Ref to itself, already affected, usefull for plugin developpmenet inside this code, before exporting it

        string[] ExpandedProperties; // List of properties always expanded in the properties grid, from a Setting property
        List<BacnetObjectDescription> SimplifiedViewFilter;
        bool ToogleViewSimplified = false; // Used to change the view mode independently of the chosen setting mode (short cut : ctrl alt S)
        string AddrSpaceTxt = "Objects";

        public TreeNode NetworkViewTreeNode, DeviceClassViewTreeNode;

        public bool GetSetting_TimeSynchronize_UTC() // GlobalCommander is using it
        {
            return Properties.Settings.Default.TimeSynchronize_UTC;
        }

        private int AsynchRequestId=0;

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
                Properties.Settings.Default.SettingsUpgradeRequired = false;    // the corrupted file could be previous version one
                Properties.Settings.Default.Save();
            }

            InitializeComponent();
            Trace.Listeners.Add(new MyTraceListener(m_LogText));

            // During "long" call of ReadPropertiesMultiple cutted into several ReadProperty. Could also displays some information.
            // Not very usefull
            BACnetDevice.DoEvents += (_,__)=> Application.DoEvents();

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

            SetSimplifiedLabels();
        }
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

            // List of properties always expanded in the properties grid such as Priority Array, ...
            if (Properties.Settings.Default.GridAlwaysExpandProperties != "")
                ExpandedProperties = Properties.Settings.Default.GridAlwaysExpandProperties.Split(new char[] { ',', ';' });

            // User Menu with key shortcut
            InitUserCmd();

            // Plugins, Vendor Properties, Name mapping file loaded within the ThreadPool

            ThreadPool.QueueUserWorkItem(o=> // speed up start, no need immediatly, Trace listen thread safe.
            {
                if (Environment.OSVersion.Platform.ToString().Contains("Win"))
                {
                    string[] listPlugins = Properties.Settings.Default.Plugins.Split(new char[] { ',', ';' });
                    foreach (string pluginname in listPlugins)
                    {
                        try
                        {
                            // string path = Path.GetDirectoryName(Application.ExecutablePath);
                            string name = pluginname.Replace(" ", String.Empty);
                            // Assembly myDll = Assembly.LoadFrom(path + "\\" + name + ".dll");
                            Assembly myDll = Assembly.LoadFrom("Plugins\\" + name + ".dll");
                            Trace.WriteLine(String.Format("Loaded plugin \"{0}\".", pluginname));
                            Type[] types = myDll.GetExportedTypes();
                            IYabePlugin plugin = (IYabePlugin)myDll.CreateInstance(name + ".Plugin", true);
                            Invoke(new Action(() => { plugin.Init(this); }));
                        }
                        catch (Exception ex)
                        {
                            if (Debugger.IsAttached) // Not loaded plugins can be detected without this message
                                Trace.WriteLine(String.Format("Error loading plugin \"{0}\". {1}", pluginname, ex.Message));
                        }
                    }
                }

                if (File.Exists("SimplifiedViewFilter.xml"))
                {
                    try
                    {
                        StreamReader sr;
                        XmlSerializer xs = new XmlSerializer(typeof(List<BacnetObjectDescription>));
                        sr = new StreamReader("SimplifiedViewFilter.xml");
                        SimplifiedViewFilter = (List<BacnetObjectDescription>)xs.Deserialize(sr);
                    }
                    catch (Exception ex)
                    {
                        Trace.WriteLine("Error with SimplifiedViewFilter.xml :" + ex.Message);
                    }
                }

                BACnetDevice.LoadVendorPropertyMapping();

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
                                    BACnetDevice.DevicesObjectsName = d;
                                    Trace.TraceInformation("Loaded object names from \"" + fileTotal + "\".");
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
                                    catch
                                    {
                                        Trace.TraceError("Error trying to setup the auto-save object names function: The directory \"" + directory + "\" does not exist, and Yabe cannot create this directory. Try changing the Auto_StoreObject_Names_File setting to a different path."); Properties.Settings.Default.Auto_Store_Object_Names = false;
                                    }
                                }
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
            });

            NetworkViewTreeNode = new TreeNode("Network View");
            NetworkViewTreeNode.Tag = -1; // For the Node sorter

            if (Properties.Settings.Default.DeviceViewMode != DeviceTreeViewType.Network)
                DeviceClassViewTreeNode = CreateDeviceClassView();

            if ((!(Properties.Settings.Default.DeviceViewMode==DeviceTreeViewType.DeviceClass)) || (DeviceClassViewTreeNode==null))
                m_DeviceTree.Nodes.Add(NetworkViewTreeNode);

            m_DeviceTree.ExpandAll();

            m_DeviceTree.TreeViewNodeSorter = new NodeSorter(Properties.Settings.Default.DeviceViewMode == DeviceTreeViewType.NetworkThenDeviceClass);

            ThreadPool.QueueUserWorkItem( _ => 
            {
                Thread.Sleep(100);
                Invoke(new Action( () => { addDevicesearchToolStripMenuItem_Click(this, null); }));
            });
            
        }
        TreeNode CreateDeviceClassView()
        {
            String Descr = Properties.Settings.Default.DeviceClassStructure;
            if (string.IsNullOrEmpty(Descr))
                return null;

            String[] Groups = Descr.Split(';');
            TreeNode tnDeviceClass = new TreeNode("Device Class View");
            tnDeviceClass.Tag = -2;    // For the Node sorter

            List<TreeNode> AddedTn = new List<TreeNode>();

            int Count = -1;
            foreach (string Group in Groups)
            {
                string[] elements = Group.Split(new char[] { '(', ')', ',' });
                TreeNode tnGroup= new TreeNode(elements[0].Trim(), 11,11); // with a folder icon
                List<int> DeviceIds = new List<int>();
                DeviceIds.Add(Count--); // For the Node sorter, and cannot be a DeviceId
                for (int i=1;i<elements.Length;i++)
                {
                    if (!string.IsNullOrWhiteSpace(elements[i]))
                        if (Int32.TryParse(elements[i], out int id)==true)  // It's a DeviceId
                            DeviceIds.Add(id);
                        else // It's a reference to a previous group
                        {
                            int idx=AddedTn.FindIndex(o => o.Text.ToLower() == elements[i].Trim().ToLower());
                            if (idx!=-1) // maybe an error
                            {
                                tnGroup.Nodes.Add(AddedTn[idx]);
                                AddedTn.Remove(AddedTn[idx]);
                            }
                        }
                }
                tnGroup.Tag = DeviceIds;
                AddedTn.Add(tnGroup);
            }

            foreach (TreeNode tn in AddedTn)
                tnDeviceClass.Nodes.Add(tn);

            if (tnDeviceClass.Nodes.Count > 1) // No view if to small
            {
                m_DeviceTree.Nodes.Add(tnDeviceClass);
                return tnDeviceClass;
            }
            else
                return null;
        }
        // By default device are added first in the Network view, then cloned to be add in this view
        bool AddToDeviceClassView(TreeNode deviceNode, TreeNode CurrentNode=null, bool IsAffected=false) 
        {
            if ((DeviceClassViewTreeNode == null)||(deviceNode==null)) return false;

            if (CurrentNode == null) CurrentNode = DeviceClassViewTreeNode;

            // Find and add at all right places
            foreach (TreeNode tn in CurrentNode.Nodes)
                if (!(tn.Tag is BACnetDevice)) // The Node is a folder or not
                    if (AddToDeviceClassView(deviceNode, tn, IsAffected)==true) // It's a folder recursive call
                        IsAffected = true;

            if (CurrentNode.Tag is List<int> DeviceIds) 
            {
                if (DeviceIds.Contains((int)(deviceNode.Tag as BACnetDevice).deviceId))
                {
                    CurrentNode.Nodes.Add((TreeNode)deviceNode.Clone());
                    IsAffected = true;
                }
                // Not added, put it in the folder for all not affected devices
                if ((DeviceIds.Count == 1) && (IsAffected == false))
                    CurrentNode.Nodes.Add((TreeNode)deviceNode.Clone());
            }

            return IsAffected;
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

                StringBuilder s = new StringBuilder();
                for (int i = 0; i < m_SubscriptionView.Columns.Count; i++)
                    s.Append(m_SubscriptionView.Columns[i].DisplayIndex.ToString() + ";" + m_SubscriptionView.Columns[i].Width.ToString() + ";");
                s.Remove(s.Length - 1, 1);

                Properties.Settings.Default.GUI_SubscriptionColumns = s.ToString();

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
        // Devices can be at several places, so a modification of the Tree node at one place must be propagated to each clone
        public void UpdateTreeNodeDeviceName(BACnetDevice device, TreeNode original, TreeNodeCollection Root = null)
        {
            if (Root == null)
                Root = m_DeviceTree.Nodes;

            foreach (TreeNode t in Root)
            {
                if (t == original)
                    continue;

                if ((t.Tag is BACnetDevice dev) && (dev == device))
                {
                    t.ToolTipText = original.ToolTipText;
                    t.Text = original.Text;
                }
                if (t.Nodes.Count != 0)
                    UpdateTreeNodeDeviceName(device, original, t.Nodes);
            }
        }

        public void DeleteTreeNodeDevice(BACnetDevice device, TreeNodeCollection Root = null)
        {
            if (Root == null)
                Root = m_DeviceTree.Nodes;

            // never delete inside the iterator and never in the forward direction
            int RootCount=Root.Count;
            for (int i=0;i< RootCount; i++)
            {
                if ((Root[RootCount-i-1].Nodes.Count != 0))
                    DeleteTreeNodeDevice(device, Root[RootCount - i - 1].Nodes);
                if ((Root[RootCount - i - 1].Tag is BACnetDevice dev) && (dev == device))
                    Root.Remove(Root[RootCount - i - 1]);
            }

        }
        string CovGraph_PointValueEvent(ZedGraphControl sender, GraphPane pane, CurveItem curve, int iPt)
        {
            PointPair point= curve[iPt];

            String Name = (String)curve.Tag;
            XDate X = new XDate(point.X);
            string tooltip = Name + Environment.NewLine + X.ToString() + "    " + point.Y.ToString();
            return tooltip;
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

            if ((!TbxHighlightAddress.Text.IsNullOrEmpty()) && (tn.Text.ToLower().Contains(TbxHighlightAddress.Text.ToLower())))
                tn.ForeColor = Color.Red;
            else
                tn.ForeColor = Color.Black;
        }

        private void OnEventNotify(BacnetClient sender, BacnetAddress adr, byte invoke_id, BacnetEventNotificationData EventData, bool need_confirm)
        {
            string EventTypeNiceName(BacnetEventNotificationData.BacnetEventStates state) => state.ToString().Substring(12);

            if (InvokeRequired)
            {
                BeginInvoke(new BacnetClient.EventNotificationCallbackHandler(OnEventNotify), new object[] { sender, adr, invoke_id, EventData, need_confirm });
                return;
            }

            uint deviceInstance = EventData.initiatingObjectIdentifier.instance;
            BACnetDevice device = new BACnetDevice(sender, adr, deviceInstance); // could not be in the m_devices dictionary (broadcast events for instance)              

            if (_plotterRunningFlag) {
                string sub_key = EventData.initiatingObjectIdentifier.instance + ":" + EventData.eventObjectIdentifier.type + ":" + EventData.eventObjectIdentifier.instance;

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

                BacnetObjectId objectId = EventData.eventObjectIdentifier;

                if (itm == null)
                {
                    itm = m_SubscriptionView.Items.Add("");
                    // Always a blank on [0] to allow for the "Show" Column

                    itm.Tag = sub_key;

                    // device id is index [1]
                    itm.SubItems.Add(deviceInstance.ToString()); // device instance
                    itm.SubItems.Add(ShortenObjectId(objectId.ToString())); // object ID [2]

                    string name = device.ReadObjectName(objectId);

                    if (string.IsNullOrWhiteSpace(name) || name.StartsWith("["))
                        name = objectId.ToString();

                    itm.SubItems.Add(name);   //name [3]
                    itm.SubItems.Add(EventTypeNiceName(EventData.fromState) + " to " + EventTypeNiceName(EventData.toState)); //value [4]
                    itm.SubItems.Add(EventData.timeStamp.Time.ToString(Properties.Settings.Default.COVTimeFormater));   //time [5]
                    itm.SubItems.Add(EventData.notifyType.ToString());   //status [6]

                    if (Properties.Settings.Default.ShowDescriptionWhenUseful)
                    {
                        string Descr = "";
                        try
                        {
                            // Get the Description, network activity, time consuming
                            IList<BacnetValue> descr;
                            bool retcode = device.ReadPropertyRequest(objectId, BacnetPropertyIds.PROP_DESCRIPTION, out descr);

                            if (retcode)
                                Descr = descr[0].Value.ToString();
                        }
                        catch { }
                        itm.SubItems.Add(Descr);   // Description [7]
                    }
                    else
                    {
                        itm.SubItems.Add(""); // Description [7]
                    }
                }
                else
                {
                    string tempName = objectId.ToString();

                    string name;
                    if (itm.SubItems[3].Text.Equals(tempName))
                    {
                        name = device.ReadObjectName(objectId);

                        if (string.IsNullOrWhiteSpace(name) || name.StartsWith("["))
                            name = objectId.ToString();
                        
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
                device.SimpleAckResponse(BacnetConfirmedServices.SERVICE_CONFIRMED_EVENT_NOTIFICATION, invoke_id);
            }

        }

        private void OnCOVNotification(BacnetClient sender, BacnetAddress adr, byte invoke_id, uint subscriberProcessIdentifier, BacnetObjectId initiatingDeviceIdentifier, BacnetObjectId monitoredObjectIdentifier, uint timeRemaining, bool need_confirm, ICollection<BacnetPropertyValue> values, BacnetMaxSegments max_segments)
        {
            string ConvertToText(IList<BacnetValue> val)
            {
                if (val == null)
                    return "[null]";
                else if (val.Count == 0)
                    return "";
                else if (val.Count == 1)
                    return val[0].Value.ToString();
                else
                {
                    string ret = "{";
                    foreach (BacnetValue value in val)
                        ret += value.Value.ToString() + ",";
                    ret = ret.Substring(0, ret.Length - 1);
                    ret += "}";
                    return ret;
                }
            }
            
            string sub_key = adr.ToString() + ":" + initiatingDeviceIdentifier.instance + ":" + subscriberProcessIdentifier;

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
        
        void OnIam(BacnetClient sender, BacnetAddress adr, uint device_id, uint max_apdu, BacnetSegmentations segmentation, ushort vendor_id)
        {
            BACnetDevice new_device = new BACnetDevice(sender, adr, device_id, max_apdu, segmentation, vendor_id);
            lock (m_devices)
            {
                int idx = m_devices[sender].Devices.IndexOf(new_device);
                if (idx == -1)
                    m_devices[sender].Devices.Add(new_device);
                else
                {
                    m_devices[sender].Devices[idx].vendor_Id = vendor_id; // Update vendor id (mstp case)
                    m_devices[sender].Devices[idx].MaxAPDULenght= max_apdu;
                    m_devices[sender].Devices[idx].Segmentation= segmentation;
                }
            }

            //update GUI
            this.Invoke((MethodInvoker)delegate
            {

                TreeNode parent = FindCommTreeNode(sender);
                if (parent == null) return; // should never occur

                String Identifier = new_device.deviceName;

                foreach (TreeNode s in parent.Nodes)
                {
                       
                    BACnetDevice entry = s.Tag as BACnetDevice;

                    // update existing (this can happen in MSTP)
                    if ((entry!=null)&&(entry.Equals(new_device)))
                    {
                        s.Text = "Device " + new_device.deviceId + " - " + new_device.BacAdr.ToString(s.Parent.Parent != null);
                        if (Identifier != null)
                        {
                            s.ToolTipText = s.Text;
                            s.Text = Identifier + " [" + device_id.ToString() + "] ";
                        }
                        else
                        {
                            s.ToolTipText = "";
                        }

                        UpdateTreeNodeDeviceName(entry, s);

                        return;
                    }
                }
  
                TreeNode newNode = new TreeNode("Device " + new_device.deviceId + " - " + new_device.BacAdr.ToString(true));
                newNode.ImageIndex = 2;
                newNode.SelectedImageIndex = newNode.ImageIndex;
                newNode.Tag = new_device;
                if (Identifier != null)
                {
                    newNode.ToolTipText = newNode.Text;
                    newNode.Text = Identifier + " [" + device_id.ToString() + "] ";
                }
                else
                {
                    newNode.ToolTipText = "";
                }

                // Try to add it under a router if any 
                if ((new_device.BacAdr.RoutedSource != null))
                    foreach (TreeNode s in parent.Nodes)
                    {
                        BACnetDevice router = s.Tag as BACnetDevice;
                        if (router != null && router.BacAdr.IsMyRouter(adr))
                        {
                            s.Nodes.Add(newNode);
                            m_DeviceTree.ExpandAll();
                            return;
                        }
                    }

                //add simply
                parent.Nodes.Add(newNode);
                AddToDeviceClassView(newNode);
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

            MessageBox.Show(this, product + "\nVersion " + this.GetType().Assembly.GetName().Version + "\nBy Morten Kvistgaard - Copyright 2014-2017\nBy Frederic Chaxel - Copyright 2015-2025\n" +
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

            SearchDialog dlg = new SearchDialog();
            if (dlg.ShowDialog(this) == System.Windows.Forms.DialogResult.OK)
            {
                BacnetClient comm = dlg.Result;
                try
                {
                    m_devices.Add(comm, new BacnetDeviceLine());
                }
                catch { return ; }

                //add to tree
                TreeNode node = NetworkViewTreeNode.Nodes.Add(comm.ToString());
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
                        if (m_Server == null)
                            m_Server = new YabeDevice((uint)Properties.Settings.Default.YabeDeviceId);
                        m_Server.AddCom(comm);
                    }
                    else
                        comm.OnWhoIs += (_,__,___,____)=>{ };    // Ignore OnWhois

                    comm.OnIam += OnIam;
                    comm.OnCOVNotification += OnCOVNotification;
                    comm.OnEventNotify += OnEventNotify;
                    comm.Start();

                    m_Server?.Iam(comm); // will not be done if not appropriated

                    if (dlg.chkSendWhois.Checked == true)
                    {
                        // WhoIs Min & Max limits
                        int IdMin = -1, IdMax = -1;
                        Int32.TryParse(dlg.WhoLimitLow.Text, out IdMin); Int32.TryParse(dlg.WhoLimitHigh.Text, out IdMax);
                        if (IdMin == 0) IdMin = -1; if (IdMax == 0) IdMax = -1;
                        if ((IdMin != -1) && (IdMax == -1)) IdMax = 0x3FFFFF;
                        if ((IdMax != -1) && (IdMin == -1)) IdMin = 0;

                        //start search
                        if (comm.Transport.Type == BacnetAddressTypes.IP
                            || comm.Transport.Type == BacnetAddressTypes.Ethernet
                            || comm.Transport.Type == BacnetAddressTypes.IPV6
                            || comm.Transport.Type == BacnetAddressTypes.SC
                            || (comm.Transport is BacnetMstpProtocolTransport && ((BacnetMstpProtocolTransport)comm.Transport).SourceAddress != -1)
                            || comm.Transport.Type == BacnetAddressTypes.PTP)
                        {
                            System.Threading.ThreadPool.QueueUserWorkItem((o) =>
                            {
                                for (int i = 0; i < comm.Retries; i++)
                                {
                                    comm.WhoIs(IdMin, IdMax);
                                    System.Threading.Thread.Sleep(comm.Timeout);
                                }

                            }, null);
                        }
                    }

                    //special MSTP auto discovery
                    if (comm.Transport is BacnetMstpProtocolTransport)
                    {
                        ((BacnetMstpProtocolTransport)comm.Transport).FrameRecieved += new BacnetMstpProtocolTransport.FrameRecievedHandler(MSTP_FrameRecieved);
                    }

                    labelDrop1.Visible = labelDrop2.Visible = false;
                    if (TbxHighlightAddress.Text == "HighLight Filter")
                        TbxHighlightAddress.Text = TbxHighlightDevice.Text = "";

                }
                catch (Exception ex)
                {
                    m_devices.Remove(comm);
                    node.Remove();
                    MessageBox.Show(this, "Couldn't start BACnet communication: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }

            }
        }

        private void MSTP_FrameRecieved(BacnetMstpProtocolTransport sender, BacnetMstpFrameTypes frame_type, byte destination_address, byte source_address, int msg_length)
        {
            try
            {
                if (this.IsDisposed) return;

                BacnetDeviceLine device_line = m_devices.First(o => o.Key.Transport == sender).Value;
                BacnetClient client= m_devices.First(o => o.Key.Transport == sender).Key;

                lock (device_line.mstp_sources_seen)
                {
                    if (!device_line.mstp_sources_seen.Contains(source_address))
                    {
                        device_line.mstp_sources_seen.Add(source_address);

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
                            node.Tag = new BACnetDevice(client, new BacnetAddress(BacnetAddressTypes.MSTP, 0, new byte[] { source_address }), 0xFFFFFFFF);
                            if (free_node != null) free_node.Remove();
                            m_DeviceTree.ExpandAll();
                        });

                        //detect collision
                        if (source_address == sender.SourceAddress)
                        {
                            this.Invoke((MethodInvoker)delegate
                            {
                                Trace.WriteLine("Mstp Collision detected");
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
                                node.Tag = new object();    // Just to have one like the others
                                m_DeviceTree.ExpandAll();
                            });
                        }
                    }
                }
            }
            catch
            {
                //we're closing down ... ignore
            }
        }

        private void RemoveSubscriptions(BACnetDevice device, BacnetClient comm)
        {
            lock (m_subscription_list)
            {
                LinkedList<string> deletes = new LinkedList<string>();
                foreach (KeyValuePair<string, ListViewItem> entry in m_subscription_list)
                {
                    Subscription sub = (Subscription)entry.Value.Tag;
                    if ((sub.device == device) || (sub.device.channel == comm))
                    {
                        m_SubscriptionView.Items.Remove(entry.Value);
                        deletes.AddLast(sub.sub_key);
                    }
                }
                foreach (string sub_key in deletes)
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
                if (MessageBox.Show(m_DeviceTree.SelectedNode.Text+"\r\nDelete this device?", "Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == System.Windows.Forms.DialogResult.Yes)
                {
                    m_AddressSpaceTree.Nodes.Clear();   //clear address space
                    AddSpaceLabel.Text = AddrSpaceTxt;
                    m_DataGrid.SelectedObject = null;   //clear property grid

                    DeleteTreeNodeDevice(device_entry);
                    _selectedDevice = null;
                    RemoveSubscriptions(device_entry, null);

                    lock (m_devices)
                        m_devices[device_entry.channel].Devices.Remove(device_entry);

                }
            }
            else if (comm_entry != null)
            {
                if (MessageBox.Show(m_DeviceTree.SelectedNode.Text + "\r\nDelete this transport?", "Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == System.Windows.Forms.DialogResult.Yes)
                {
                    if (_selectedDevice == null || (_selectedDevice.Tag is BACnetDevice currentDelectedDeviceComms && m_devices[comm_entry].Devices.Contains(currentDelectedDeviceComms)))
                    {
                            m_AddressSpaceTree.Nodes.Clear();   //clear address space
                            AddSpaceLabel.Text = AddrSpaceTxt;
                            m_DataGrid.SelectedObject = null;   //clear property grid
                    }

                    if (m_Server!=null)
                        m_Server.RemoveCom(comm_entry);

                    lock (m_devices)
                    {
                        foreach (BACnetDevice dev in m_devices[comm_entry].Devices)
                            DeleteTreeNodeDevice(dev);
                        m_devices.Remove(comm_entry);
                    }
                    m_DeviceTree.Nodes.Remove(m_DeviceTree.SelectedNode);
                    RemoveSubscriptions(null, comm_entry);
                    comm_entry.Dispose();
                }
            }
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
                case BacnetObjectTypes.OBJECT_NETWORK_PORT:
                    return 8;
                case BacnetObjectTypes.OBJECT_GROUP:
                    return 10;
                case BacnetObjectTypes.OBJECT_STRUCTURED_VIEW:
                    return 11;
                case BacnetObjectTypes.OBJECT_TRENDLOG:
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

        private void AddObjectEntry(BACnetDevice device, string name, BacnetObjectId object_id, TreeNodeCollection nodes, bool ForceRead=false)
        {
            bool iAmTheCreator = false;
            bool recursionDetected = false;

            if (((Properties.Settings.Default.Address_Space_Structured_View == AddressTreeViewType.FieldTechnician) ^ ToogleViewSimplified==true) && (SimplifiedViewFilter != null))
                if (!SimplifiedViewFilter.Exists(o => o.typeId == object_id.Type))
                    return;

            if (object_id.type==BacnetObjectTypes.OBJECT_STRUCTURED_VIEW)
            {
                if(_structuredViewParents==null)
                {
                    _structuredViewParents = new List<BacnetObjectId>();
                    iAmTheCreator = true;
                }

                if(_structuredViewParents.Contains(object_id))
                {
                    recursionDetected = true;
                }
                else
                {
                    _structuredViewParents.Add(object_id);
                }
            }

            if (string.IsNullOrEmpty(name)) name = object_id.ToString();

            TreeNode node;

            string objName = device.GetObjectName(object_id);
            // Get the property name if already known
            if (objName != null)
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

                node = new TreeNode(objName);
                node.ToolTipText = name;
            }
            else
            {
                node = new TreeNode(name);
                node.ToolTipText = "";
            }
            

            // Add to tree
            node.Tag = object_id;
            nodes.Add(node);
            
            if ((!TbxHighlightAddress.Text.IsNullOrEmpty())&&(node.Text.ToLower().Contains(TbxHighlightAddress.Text.ToLower())))
                node.ForeColor = Color.Red;

            // Set icon
            node.ImageIndex = GetIconNum(object_id.type);
            node.SelectedImageIndex = node.ImageIndex;

            //fetch sub properties
            if (object_id.type == BacnetObjectTypes.OBJECT_GROUP)
            {
                FetchGroupProperties(device, object_id, node.Nodes, ForceRead);
            }
            else if ((object_id.type == BacnetObjectTypes.OBJECT_STRUCTURED_VIEW)) // && (Properties.Settings.Default.Address_Space_Structured_View == AddressTreeViewType.Structured || Properties.Settings.Default.Address_Space_Structured_View == AddressTreeViewType.Both))
            {
                if (recursionDetected)
                {
                        TreeNode recursiveNode = node.Nodes.Add("WARNING: RECURSIVE NODE DETECTED");
                        recursiveNode.ImageIndex = 16;
                        recursiveNode.SelectedImageIndex = 16;
                }
                else
                {
                    FetchViewObjects(device, object_id, node.Nodes, ForceRead);
                }
            }
            else if ((object_id.type == BacnetObjectTypes.OBJECT_DEVICE) && (node.Parent == null) && (Properties.Settings.Default.Address_Space_Structured_View == AddressTreeViewType.Structured || Properties.Settings.Default.Address_Space_Structured_View == AddressTreeViewType.Both))
            {
                FetchStructuredObjects(device, object_id.Instance, node.Nodes, ForceRead);
            }

            if ((device.DeviceIdUnconfigured) && (object_id.type == BacnetObjectTypes.OBJECT_DEVICE)) // Mstp & Remote Device with 0x3FFFFF
            {
                device.deviceId = object_id.Instance;

                AddToDeviceClassView(_selectedDevice);
                m_DeviceTree.ExpandAll();
            }

            if (object_id.type == BacnetObjectTypes.OBJECT_STRUCTURED_VIEW)
            {
                if (_structuredViewParents != null)
                {
                    if(_structuredViewParents.Contains(object_id))
                    {
                        _structuredViewParents.Remove(object_id);
                    }
                    if (iAmTheCreator)
                    {
                        _structuredViewParents = null;
                    }
                }
                
            }
        }

        private int FetchStructuredObjects(BACnetDevice device, uint device_id, TreeNodeCollection nodes, bool ForceRead=false)
        {
            IList<BacnetValue> ret;
            int old_reties = device.channel.Retries;
            try
            {
                device.channel.Retries = 1;       //only do 1 retry
                if (!device.ReadPropertyRequest(new BacnetObjectId(BacnetObjectTypes.OBJECT_DEVICE, device_id),BacnetPropertyIds.PROP_STRUCTURED_OBJECT_LIST, out ret, ForceRead))
                {
                    Trace.TraceInformation("Didn't get response from 'Structured Object List'");
                    return 0;
                }
                else
                {
                    List<BacnetObjectId> objectList = SortBacnetObjects(ret);
                    foreach (BacnetObjectId objid in objectList)
                        AddObjectEntry(device, null, objid, nodes, ForceRead);
                }
            }
            catch (Exception)
            {
                return 0;
            }
            finally
            {
                device.channel.Retries = old_reties;
            }
            return ret.Count;
        }

        private void AddObjectListOneByOneAsync(BACnetDevice device, uint count, int AsynchRequestId, bool ForceRead=false)
        {
            System.Threading.ThreadPool.QueueUserWorkItem((o) =>
            {
                AddObjectListOneByOne(device, count, AsynchRequestId, ForceRead);
            });
        }
        private void AddObjectListOneByOne(BACnetDevice device, uint count, int AsynchRequestId, bool ForceRead = false)
        {
            try
            {
                for (uint i = 1; i <= count; i++)
                {
                    BacnetObjectId objId;

                    if (!device.ReadObjectListItem(out objId, i, ForceRead))
                    {
                        Trace.WriteLine("Couldn't fetch object list index");
                        return;
                    }

                    if (AsynchRequestId != this.AsynchRequestId) return; // Selected device is no more the good one

                    //add to tree
                    this.Invoke(new Action( () =>
                    {
                        if (AsynchRequestId != this.AsynchRequestId) return;  // another test in the GUI thread
                        AddObjectEntry(device, null, objId, m_AddressSpaceTree.Nodes, ForceRead);
                        if (i != count)
                            AddSpaceLabel.Text = AddrSpaceTxt+" : " + i + " objects / " + count + " expected";
                        else
                            AddSpaceLabel.Text = AddrSpaceTxt+" : " + i + " objects";
                    }));
                    
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine("Error during read: " + ex.Message);
                return;
            }

            return;
        }

        public List<BacnetObjectId> SortBacnetObjects(IList<BacnetValue> RawList, bool Sort=true)
        {

            List<BacnetObjectId> SortedList = new List<BacnetObjectId>();
            foreach (BacnetValue value in RawList)
                if (value.Value is BacnetObjectId) // with BacnetObjectId
                    SortedList.Add((BacnetObjectId)value.Value);
                else // with Subordinate_List for StructuredView
                {
                    BacnetDeviceObjectReference v = (BacnetDeviceObjectReference)value.Value;
                    SortedList.Add(v.objectIdentifier); // ignore deviceIdentifier
                }

            if (Sort)
                SortedList.Sort();

            return SortedList;
        }

        void SetSimplifiedLabels()
        {
            if (((Properties.Settings.Default.Address_Space_Structured_View == AddressTreeViewType.FieldTechnician) ^ ToogleViewSimplified == true) && (SimplifiedViewFilter != null))
            {
                LblProperties.Text = "Properties (Simplified)";
                AddSpaceLabel.Text = AddrSpaceTxt = "Objects (Simplified)";
            }
            else
            {
                LblProperties.Text = "Properties";
                AddSpaceLabel.Text = AddrSpaceTxt = "Objects";
            }
        }
        private void m_DeviceTree_AfterSelect(object sender, TreeViewEventArgs e)
        {
            SetSimplifiedLabels();

            TreeNode node = e.Node;
            if (node == null) return; // Toogle Simplified View without selected node

            BACnetDevice device = node.Tag as BACnetDevice;
            if (device == null) return;  // Not a TreeNode linked to a device

            if ((sender as String == "ObjRename") || (sender == manual_refresh_objects)|| (sender as String == "ObjNewDelete"))
                _selectedDevice = null; // invalidate the current selection to force a renew

            if (_selectedDevice != null)
                if ((_selectedDevice.Tag == device)) return;  // Same device, on a clone TreeNode

            _selectedDevice = node;

            AsynchRequestId++; // disabled a possible thread pool work (update) on the AddressSpaceTree

            if ((sender == manual_refresh_objects) || (sender as String == "ObjNewDelete" ) || (!Properties.Settings.Default.UseObjectsCache))
                device.ClearCache();

            m_AddressSpaceTree.Nodes.Clear();   // clear Objects dictionnary, the device is changed
            AddSpaceLabel.Text = AddrSpaceTxt;

            BacnetClient comm = device.channel;
            BacnetAddress adr = device.BacAdr;
            uint device_id = device.deviceId;


            //unconfigured MSTP?
            if (comm.Transport is BacnetMstpProtocolTransport && ((BacnetMstpProtocolTransport)comm.Transport).SourceAddress == -1)
            {
                if (MessageBox.Show("The MSTP transport is not yet configured. Would you like to set source_address now?", "Set Source Address", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == System.Windows.Forms.DialogResult.No) return;

                //find suggested address
                byte address = 0xFF;
                BacnetDeviceLine line = m_devices[comm];
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
                if( dlg.ShowDialog(this) == System.Windows.Forms.DialogResult.Cancel) return;
                ((BacnetMstpProtocolTransport)comm.Transport).SourceAddress = dlg.SourceAddress;
                Application.DoEvents();     //let the interface relax
            }

            //update "address space"?
            this.Cursor = Cursors.WaitCursor;
            Application.DoEvents();
            int old_timeout = comm.Timeout;

            try
            {
                int CountStructuredObjects = 0;
                if (Properties.Settings.Default.Address_Space_Structured_View == AddressTreeViewType.Structured)
                {
                    CountStructuredObjects = FetchStructuredObjects(device, device_id, m_AddressSpaceTree.Nodes);

                    // If the Device name not set, try to update it
                    if (node.ToolTipText == "")   // already update with the device name
                    {
                        BacnetObjectId bobj_id = new BacnetObjectId(BacnetObjectTypes.OBJECT_DEVICE, device_id);
                        String Identifier = device.ReadObjectName(bobj_id);
                        if (!string.IsNullOrWhiteSpace(Identifier))
                        {
                            node.ToolTipText = node.Text;
                            node.Text = Identifier + " [" + bobj_id.Instance.ToString() + "] ";
                            UpdateTreeNodeDeviceName(device, node);
                        }
                    }

                }

                if (CountStructuredObjects != 0)
                {
                    AddSpaceLabel.Text = AddrSpaceTxt + " : " + CountStructuredObjects.ToString() + " objects";
                }
                else // Without PROP_STRUCTURED_OBJECT_LIST we fall back to a flat view of the dictionnary
                {
                    //fetch normal list
                    uint list_count;
                    List<BacnetObjectId> objectList;

                    if (!device.ReadObjectList(out objectList, out list_count))
                    {
                        Trace.TraceWarning("Didn't get response from 'Object List'");
                        return;
                    }

                    //fetch list one-by-one
                    if ((objectList == null) && (list_count != 0))
                    {
                        AddSpaceLabel.Text = AddrSpaceTxt + " : 0 objects / " + list_count.ToString() + " expected";
                        AddObjectListOneByOneAsync(device, list_count, AsynchRequestId);
                        _selectedDevice = node;
                        return;
                    }

                    if (device.SortableDictionnary)
                        objectList.Sort();

                    AddSpaceLabel.Text = AddrSpaceTxt + " : " + objectList.Count.ToString() + " objects";
                    //add to tree
                    foreach (BacnetObjectId bobj_id in objectList)
                    {
                        // If the Device name not set, try to update it
                        if (bobj_id.type == BacnetObjectTypes.OBJECT_DEVICE)
                        {
                            // If the Device name not set, try to update it
                            if (node.ToolTipText == "")   // already update with the device name
                            {
                                String Identifier = device.ReadObjectName(bobj_id);

                                if (!string.IsNullOrWhiteSpace(Identifier))
                                {
                                    node.ToolTipText = node.Text;
                                    node.Text = Identifier + " [" + bobj_id.Instance.ToString() + "] ";
                                    UpdateTreeNodeDeviceName(device, node);
                                }
                            }
                        }
                        AddObjectEntry(device, null, bobj_id, m_AddressSpaceTree.Nodes);
                    }
                }
            }
            catch { }
            finally
            {
                this.Cursor = Cursors.Default;
                _selectedNode = null;
                m_DataGrid.SelectedObject = null;
            }

            if ((!TbxHighlightDevice.Text.IsNullOrEmpty()) && (node.Text.ToLower().Contains(TbxHighlightDevice.Text.ToLower())))
                node.ForeColor = Color.Red;
            else
                node.ForeColor = Color.Black;
        }

        private void FetchViewObjects(BACnetDevice device, BacnetObjectId object_id, TreeNodeCollection nodes, bool ForceRead=false)
        {
            try
            {
                IList<BacnetValue> values;
                if (device.ReadPropertyRequest(object_id, BacnetPropertyIds.PROP_SUBORDINATE_LIST, out values, ForceRead))
                {
                    List<BacnetObjectId> objectList = SortBacnetObjects(values);
                    foreach (BacnetObjectId objid in objectList)
                        AddObjectEntry(device, null, objid, nodes, ForceRead);
                }
                else
                {
                    Trace.TraceWarning("Couldn't fetch view members");
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError("Couldn't fetch view members: " + ex.Message);
            }
        }
        
        private void FetchGroupProperties(BACnetDevice device, BacnetObjectId object_id, TreeNodeCollection nodes, bool ForceRead=false)
        {
            try
            {
                IList<BacnetValue> values;
                if (device.ReadPropertyRequest(object_id, BacnetPropertyIds.PROP_LIST_OF_GROUP_MEMBERS, out values, ForceRead))
                {
                    foreach (BacnetValue value in values)
                    {
                        if (value.Value is BacnetReadAccessSpecification)
                        {
                            BacnetReadAccessSpecification spec = (BacnetReadAccessSpecification)value.Value;
                            foreach (BacnetPropertyReference p in spec.propertyReferences)
                            {
                                AddObjectEntry(device, spec.objectIdentifier.ToString() + ":" + ((BacnetPropertyIds)p.propertyIdentifier).ToString(), spec.objectIdentifier, nodes, ForceRead);
                            }
                        }
                    }
                }
                else
                {
                    Trace.TraceWarning("Couldn't fetch group members");
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError("Couldn't fetch group members: " + ex.Message);
            }
        }
        private String UpdateGrid(BACnetDevice device, BacnetObjectId object_id)
        {
            BacnetClient comm = device.channel;
            BacnetAddress adr = device.BacAdr;
            
            string ReturnPROP_OBJECT_NAME = null;
            try
            {
                m_DataGrid.SelectedObject = null;   //clear

                BacnetPropertyReference[] properties = new BacnetPropertyReference[] { new BacnetPropertyReference((uint)BacnetPropertyIds.PROP_ALL, System.IO.BACnet.Serialize.ASN1.BACNET_ARRAY_ALL) };
                IList<BacnetReadAccessResult> multi_value_list=null;

                //fetch properties
                device.ReadPropertyMultipleRequest(object_id, properties, out multi_value_list);

                if (multi_value_list==null) // not accepted
                {
                    Trace.TraceWarning("Couldn't perform ReadProperties");
                    return null;
                }

                bool[] showAlarmAck = new bool[3] {false, false, false };

                List<BacnetPropertyIds> PropertyFilter =null;
                if (((Properties.Settings.Default.Address_Space_Structured_View == AddressTreeViewType.FieldTechnician) ^ ToogleViewSimplified == true) && (SimplifiedViewFilter != null))
                    PropertyFilter = SimplifiedViewFilter.Find(o => o.typeId == object_id.Type).propsId;

                //update grid
                Utilities.DynamicPropertyGridContainer bag = new Utilities.DynamicPropertyGridContainer();

                foreach (BacnetPropertyValue p_value in multi_value_list[0].values)
                {

                    // Do not displays property if requested
                    if ((PropertyFilter != null) && (!PropertyFilter.Exists(o => o == (BacnetPropertyIds)p_value.property.propertyIdentifier) && (!PropertyFilter.Exists(o=>o == BacnetPropertyIds.MAX_BACNET_PROPERTY_ID))))
                        continue;

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

                    switch ((BacnetPropertyIds)p_value.property.propertyIdentifier)
                    {
                        // Got two communication parameters about the device, could be used later.
                        case BacnetPropertyIds.PROP_SEGMENTATION_SUPPORTED:
                            device.Segmentation = (BacnetSegmentations)(Convert.ToUInt32(value));
                            bag.Add(new Utilities.CustomProperty(device.GetNiceName((BacnetPropertyIds)p_value.property.propertyIdentifier), value, value != null ? value.GetType() : typeof(string), false, "", b_values.Length > 0 ? b_values[0].Tag : (BacnetApplicationTags?)null, null, p_value.property));
                            break;
                        case BacnetPropertyIds.PROP_MAX_APDU_LENGTH_ACCEPTED:
                            device.MaxAPDULenght = Convert.ToUInt32(value);
                            bag.Add(new Utilities.CustomProperty(device.GetNiceName((BacnetPropertyIds)p_value.property.propertyIdentifier), value, value != null ? value.GetType() : typeof(string), false, "", b_values.Length > 0 ? b_values[0].Tag : (BacnetApplicationTags?)null, null, p_value.property));
                            break;
                        // PROP_PRESENT_VALUE can be write at null value to clear the prioroityarray if exists
                        case BacnetPropertyIds.PROP_PRESENT_VALUE:
                            // change to the related nullable type
                            Type t = null; 
                            try
                            {
                                t= value.GetType();
                                if (t != typeof(String)) // a bug on linuxmono where the folling instruction generates a wrong type
                                    t = Type.GetType("System.Nullable`1[" + value.GetType().FullName + "]");
                            }
                            catch { }
                            bag.Add(new Utilities.CustomProperty(device.GetNiceName((BacnetPropertyIds)p_value.property.propertyIdentifier), value, t != null ? t : typeof(string), false, "", b_values.Length > 0 ? b_values[0].Tag : (BacnetApplicationTags?)null, null, p_value.property));
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
                            bag.Add(new Utilities.CustomProperty(device.GetNiceName((BacnetPropertyIds)p_value.property.propertyIdentifier), value, value != null ? value.GetType() : typeof(string), false, "", b_values.Length > 0 ? b_values[0].Tag : (BacnetApplicationTags?)null, null, p_value.property));
                            break;
                        case BacnetPropertyIds.PROP_EVENT_TIME_STAMPS:
                            for(int i=0;i<b_values.Length;i++)
                            {
                                if (b_values[i].Value is DateTime dt)
                                {
                                    _cachedEventTimeStampsForAcknowledgementButtons[i] = dt;
                                }
                            }
                            bag.Add(new Utilities.CustomProperty(device.GetNiceName((BacnetPropertyIds)p_value.property.propertyIdentifier), value, value != null ? value.GetType() : typeof(string), false, "", b_values.Length > 0 ? b_values[0].Tag : (BacnetApplicationTags?)null, null, p_value.property));
                            break;

                        default:
                            bag.Add(new Utilities.CustomProperty(device.GetNiceName((BacnetPropertyIds)p_value.property.propertyIdentifier), value, value != null ? value.GetType() : typeof(string), false, "", b_values.Length > 0 ? b_values[0].Tag : (BacnetApplicationTags?)null, null, p_value.property));
                            break;
                    }

                    // The Prop Name replace the PropId into the Treenode 
                    if (p_value.property.propertyIdentifier == (byte)BacnetPropertyIds.PROP_OBJECT_NAME)
                    {
                        ReturnPROP_OBJECT_NAME = value.ToString();
                    }
                }

                m_DataGrid.SelectedObject = bag;

                // Expand some Arrays (or expandable properties) in the Grid
                if ((Properties.Settings.Default.GridArrayExpandMaxSize > 1) || (ExpandedProperties!=null)) // Arrays with 1 element are not expandable
                {
                    // Get the top level item, no clear other way than this one to take the root. Normally one and only one loop done here.
                    GridItem root = m_DataGrid.SelectedGridItem;
                    while (root.Parent != null) root = root.Parent;

                    foreach (GridItem g in root.GridItems)
                    {
                        if (g.Expandable)
                        {
                            if ((g.Value is Array ar) && (ar.Length <= Properties.Settings.Default.GridArrayExpandMaxSize)) // small Array
                                g.Expanded = true;
                            else
                                if (((ExpandedProperties != null)) && (ExpandedProperties.Contains(g.Label))) // chosen Properties
                                    g.Expanded = true;
                        }
                    }
                }

                ack_offnormal.Visible = showAlarmAck[0];
                ack_fault.Visible = showAlarmAck[1];
                ack_normal.Visible = showAlarmAck[2];
            }
            catch { }

            return ReturnPROP_OBJECT_NAME;
        }


        private void UpdateGrid(TreeNode selected_node)
        {
            this.Cursor = Cursors.WaitCursor;
            try
            {
                _selectedNode = null;
                //fetch end point
                if (_selectedDevice == null) return;
                else if (_selectedDevice.Tag == null) return;
                else if (!(_selectedDevice.Tag is BACnetDevice)) return;

                BACnetDevice entry = (BACnetDevice)_selectedDevice.Tag;
                BacnetAddress adr = entry.BacAdr;
                BacnetClient comm = entry.channel;
                uint device_id = entry.deviceId;

                if (selected_node.Tag is BacnetObjectId)
                {
                    m_DataGrid.SelectedObject = null;   //clear

                    BacnetObjectId object_id = (BacnetObjectId)selected_node.Tag;

                    String NewObjectName = UpdateGrid(entry, object_id);

                    if (NewObjectName != null)
                    {
                        ChangeTreeNodePropertyName(selected_node, NewObjectName);// Update the object name if needed
                        entry.UpdateObjectNameMapping(object_id, NewObjectName);
                    }

                    _selectedNode = selected_node;
                }
            }
            catch { }
            finally
            {
                this.Cursor = Cursors.Default;
            }
        }

        private void UpdateGrid(Subscription subscription)
        {
            this.Cursor = Cursors.WaitCursor;
            try
            {
                _selectedNode = null;
                BACnetDevice device = subscription.device;

                m_DataGrid.SelectedObject = null;   //clear

                BacnetObjectId object_id = subscription.object_id;

                UpdateGrid(device, object_id);

                _selectedNode = subscription;

            }
            catch { }
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

        private void m_DataGrid_PropertyValueChanged(object s, PropertyValueChangedEventArgs e)
        {
            this.Cursor = Cursors.WaitCursor;
            try
            {
                BACnetDevice device;
                BacnetObjectId object_id;

                if (_selectedNode == null)
                {
                    _selectedNode = null;
                    m_DataGrid.SelectedObject = null;
                    return;
                }

                if (_selectedNode is Subscription)
                {
                    Subscription subscription = _selectedNode as Subscription;
                    //fetch object_id
                    object_id = subscription.object_id;
                    //fetch end point
                    device = subscription.device;
                }
                else if (_selectedNode is TreeNode)
                {
                    TreeNode selectedObject = _selectedNode as TreeNode;
                    if (_selectedDevice != null)
                    {
                        //fetch end point
                        if ((_selectedDevice == null) || (_selectedDevice.Tag == null) || (!(_selectedDevice.Tag is BACnetDevice)))
                        {
                            _selectedNode = null;
                            m_DataGrid.SelectedObject = null;
                            return;
                        }

                        device = (BACnetDevice)_selectedDevice.Tag;
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

                Utilities.CustomPropertyDescriptor c = null;
                GridItem gridItem = e.ChangedItem;
                // Go up to the Property (could be a sub-element)
                do
                {
                    if (gridItem.PropertyDescriptor is Utilities.CustomPropertyDescriptor)
                        c = (Utilities.CustomPropertyDescriptor)gridItem.PropertyDescriptor;
                    else
                        gridItem = gridItem.Parent;

                } while ((c == null) && (gridItem != null));

                if (c == null) return; // never occur normaly

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
                            b_value = new BacnetValue[1];
                            if ((BacnetApplicationTags)c.CustomProperty.bacnetApplicationTags != BacnetApplicationTags.BACNET_APPLICATION_TAG_NULL)
                            {
                                b_value[0] = new BacnetValue((BacnetApplicationTags)c.CustomProperty.bacnetApplicationTags, new_value);
                            }
                            else
                            {
                                object o = null;
                                TypeConverter t = new TypeConverter();
                                // try to convert to the simplest type
                                String[] typelist = { "Boolean", "UInt32", "Int32", "Single", "Double" };

                                foreach (String typename in typelist)
                                {
                                    try
                                    {
                                        o = Convert.ChangeType(new_value, Type.GetType("System." + typename));
                                        break;
                                    }
                                    catch { }
                                }

                                if (o == null)
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
                    device.channel.WritePriority = (uint)Properties.Settings.Default.DefaultWritePriority;
                    if (!device.WritePropertyRequest(object_id, (BacnetPropertyIds)property.propertyIdentifier, b_value))
                    {
                        MessageBox.Show(this, "Couldn't write property", "Communication Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, "Error during write: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }

                //reload
                if (_selectedNode is Subscription)
                {
                    Subscription subscription = _selectedNode as Subscription;
                    UpdateGrid(subscription);
                    m_DataGrid.SelectedGridItem = gridItem;


                }
                else if (_selectedNode is TreeNode)
                {
                    TreeNode selectedObject = _selectedNode as TreeNode;
                    UpdateGrid(selectedObject);
                    m_DataGrid.SelectedGridItem = gridItem;
                }

            }
            catch { }
            finally
            {
                this.Cursor = Cursors.Default;
            }
        }

        public bool GetObjectLink(out BACnetDevice device, out BacnetObjectId object_id, BacnetObjectTypes ExpectedType=BacnetObjectTypes.MAX_BACNET_OBJECT_TYPE)
        {
            object_id = new BacnetObjectId();

            device = FetchEndPoint();
            if (device == null) return false;

            //fetch object_id
            if ((m_AddressSpaceTree.SelectedNode != null) && (m_AddressSpaceTree.SelectedNode.Tag is BacnetObjectId objid))
            {
                object_id = objid;
                if (objid.type == ExpectedType)
                    return true;
                else
                    return false;
            }

            return false;
        }

        private void downloadFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Cursor = Cursors.WaitCursor;
            try
            {
                //fetch end point

                BacnetObjectId object_id;
                BACnetDevice device;

                if (GetObjectLink(out device, out object_id, BacnetObjectTypes.OBJECT_FILE) == false) return;

                //where to store file?
                SaveFileDialog dlg = new SaveFileDialog();
                dlg.FileName = Properties.Settings.Default.GUI_LastFilename;
                if (dlg.ShowDialog(this) != System.Windows.Forms.DialogResult.OK) return;
                string filename = dlg.FileName;
                Properties.Settings.Default.GUI_LastFilename = filename;

                //get file size
                int filesize = FileTransfers.ReadFileSize(device, object_id);
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
                    if (Properties.Settings.Default.DefaultDownloadSpeed == 2)
                        transfer.DownloadFileBySegmentation(device, object_id, filename, update_progress);
                    else if (Properties.Settings.Default.DefaultDownloadSpeed == 1)
                        transfer.DownloadFileByAsync(device, object_id, filename, update_progress);
                    else
                        transfer.DownloadFileByBlocking(device, object_id, filename, update_progress);
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
            catch { }
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

                BacnetObjectId object_id;
                BACnetDevice device;
                if (GetObjectLink(out device, out object_id, BacnetObjectTypes.OBJECT_FILE) == false) return;

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
                    transfer.UploadFileByBlocking(device, object_id, filename, update_progress);
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
            catch { }
            finally
            {
                this.Cursor = Cursors.Default;
            }

            //information
            MessageBox.Show(this, "Done", "Done", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void showTrendLogToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                //fetch end point
                BACnetDevice device;
                BacnetObjectId object_id;


                if (GetObjectLink(out device, out object_id, BacnetObjectTypes.OBJECT_TRENDLOG) == false)
                    if (GetObjectLink(out device, out object_id, BacnetObjectTypes.OBJECT_TREND_LOG_MULTIPLE) == false) return;

                BacnetClient comm = device.channel;
                BacnetAddress adr = device.BacAdr;

                new TrendLogDisplay(comm, adr, object_id).ShowDialog();

            }
            catch(Exception ex)
            {
                Trace.TraceError("Error loading TrendLog : " + ex.Message);
            }
        }
        private void showScheduleToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                //fetch end point
                BACnetDevice device;
                BacnetObjectId object_id;

                if (GetObjectLink(out device, out object_id, BacnetObjectTypes.OBJECT_SCHEDULE) == false) return;

                BacnetClient comm = device.channel;
                BacnetAddress adr = device.BacAdr;

                new ScheduleDisplay(m_AddressSpaceTree.ImageList, comm, adr, object_id).ShowDialog();

            }
            catch(Exception ex) { Trace.TraceError("Error loading Schedule : " + ex.Message); }
        }

        private void deleteObjectToolStripMenuItem_Click(object sender, EventArgs e)
        {

            try
            {
                //fetch end point
                BACnetDevice device;
                BacnetObjectId object_id;

                GetObjectLink(out device,out object_id);

                BacnetClient comm = device.channel;
                BacnetAddress adr = device.BacAdr;

                if (MessageBox.Show("Are you sure you want to delete this object ?", object_id.ToString(), MessageBoxButtons.OKCancel, MessageBoxIcon.Question) == DialogResult.OK)
                {
                    comm.DeleteObjectRequest(adr, object_id);
                    m_DeviceTree_AfterSelect("ObjNewDelete", new TreeViewEventArgs(this._selectedDevice));
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
                BACnetDevice device;
                BacnetObjectId object_id;

                if (GetObjectLink(out device, out object_id, BacnetObjectTypes.OBJECT_CALENDAR) == false) return;

                BacnetClient comm = device.channel;
                BacnetAddress adr = device.BacAdr;

                new CalendarEditor(comm, adr, object_id).ShowDialog();

            }
            catch (Exception ex) { Trace.TraceError("Error loading Calendar : " + ex.Message); }
        }

        private void showNotificationToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                //fetch end point
                BACnetDevice device;
                BacnetObjectId object_id;

                if (GetObjectLink(out device, out object_id, BacnetObjectTypes.OBJECT_NOTIFICATION_CLASS) == false) return;

                BacnetClient comm = device.channel;
                BacnetAddress adr = device.BacAdr;

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

            public readonly BACnetDevice device;

            public readonly BacnetObjectId object_id;
            public readonly string sub_key;
            public readonly uint subscribe_id;
            public bool is_COV_subscription = true; // false if subscription is refused (fallback to polling) or polling is specified explicitly.

            public Subscription(BACnetDevice device, BacnetObjectId object_id, string sub_key, uint subscribe_id)
            {
                this.device = device;
                this.object_id = object_id;
                this.sub_key = sub_key;
                this.subscribe_id = subscribe_id;
            }
        }

        private string ShortenObjectId(string objectId)
        {
            String[] Replacement={ "ANALOG_INPUT", "AI", "ANALOG_OUTPUT", "AO", "ANALOG_VALUE", "AV","BINARY_INPUT", "BI",
                 "BINARY_OUTPUT", "BO","BINARY_VALUE", "BV","MULTI_STATE_INPUT", "MI","MULTI_STATE_OUTPUT", "MO","MULTI_STATE_VALUE", "MV" };
            
            string result = objectId;

            if(result.StartsWith("OBJECT_"))
            {
                result = result.Substring(7);
            }

            for (int i = 0; i < Replacement.Length; i += 2)
                result = result.Replace(Replacement[i], Replacement[i + 1]);

            return result;
        }

        private bool CreateSubscription(BACnetDevice device, BacnetObjectId object_id, bool WithGraph, int pollPeriod = -1)
        {
            uint FetchDeviceId(BacnetClient _comm, BacnetAddress _adr)
            {
                IList<BacnetValue> value;
                if (_comm.ReadPropertyRequest(_adr, new BacnetObjectId(BacnetObjectTypes.OBJECT_DEVICE, System.IO.BACnet.Serialize.ASN1.BACNET_MAX_INSTANCE), BacnetPropertyIds.PROP_OBJECT_IDENTIFIER, out value))
                {
                    if (value != null && value.Count > 0 && value[0].Value is BacnetObjectId)
                    {
                        BacnetObjectId obj_id = (BacnetObjectId)value[0].Value;
                        return obj_id.instance;
                    }
                    else
                        return 0xFFFFFFFF;
                }
                else
                    return 0xFFFFFFFF;
            }

            BacnetClient comm = device.channel;
            BacnetAddress adr = device.BacAdr;
            uint device_id=device.deviceId;

            this.Cursor = Cursors.WaitCursor;
            try
            {
                String CurveToolTip;
                //fetch device_id if needed
                if (device_id >= System.IO.BACnet.Serialize.ASN1.BACNET_MAX_INSTANCE)
                {
                    device_id = FetchDeviceId(comm, adr);
                }

                m_next_subscription_id++;
                string sub_key = adr.ToString() + ":" + device_id + ":" + m_next_subscription_id;
                Subscription sub = new Subscription(device, object_id, sub_key, m_next_subscription_id);

                string obj_id = object_id.ToString().Substring(7);
                obj_id = ShortenObjectId(obj_id);

                CurveToolTip = device.ReadObjectName(object_id);

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
                itm.SubItems.Add(device_id.ToString()); 
                itm.SubItems.Add(obj_id); // object id [2]
                itm.SubItems.Add(CurveToolTip);   //name [3]
                itm.SubItems.Add("");   //value [4]
                itm.SubItems.Add("");   //time [5]
                itm.SubItems.Add("Not started");   //status [6]
                if (Properties.Settings.Default.ShowDescriptionWhenUseful)
                {
                    IList<BacnetValue> values;
                    if (device.ReadPropertyRequest(object_id, BacnetPropertyIds.PROP_DESCRIPTION, out values))
                    {
                        itm.SubItems.Add(values[0].Value.ToString());   // Description [7]
                        CurveToolTip = CurveToolTip + Environment.NewLine + values[0].Value.ToString();
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
                        SubscribeOK = device.SubscribeCOVRequest(object_id, m_next_subscription_id, false, Properties.Settings.Default.Subscriptions_IssueConfirmedNotifies, Properties.Settings.Default.Subscriptions_Lifetime);
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

                    ThreadPool.QueueUserWorkItem(_ => ReadPropertySubscriptionPollingInsteadOfCOV(sub, period));
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
        private void ReadPropertySubscriptionPollingInsteadOfCOV(Subscription sub, int period)
        {
            int errorCount = 0;
            bool wasPaused = !_plotterRunningFlag;
            bool firstIteration = true;
            
            // Save this for later so maybe we can notify the user when polling has crashed/stopped
            ListViewItem.ListViewSubItem statusItemFromListBox = null;
            lock (m_subscription_list)
            {
                if(m_subscription_list.ContainsKey(sub.sub_key))
                {
                    statusItemFromListBox = m_subscription_list[sub.sub_key].SubItems[6];
                }
            }
            
            BacnetPropertyReference[] propertiesToPoll = new BacnetPropertyReference[] { new BacnetPropertyReference((uint)BacnetPropertyIds.PROP_PRESENT_VALUE, System.IO.BACnet.Serialize.ASN1.BACNET_ARRAY_ALL), new BacnetPropertyReference((uint)BacnetPropertyIds.PROP_STATUS_FLAGS, System.IO.BACnet.Serialize.ASN1.BACNET_ARRAY_ALL) };
            IList<BacnetReadAccessResult> multi_value_list = null;

            for (; ; )
            {
                if (!firstIteration)
                {
                    Thread.Sleep(Math.Max(Math.Min(MAX_POLL_PERIOD, period), MIN_POLL_PERIOD));

                    if (!_plotterPause.WaitOne(0))
                    {
                        wasPaused = true;
                        _plotterPause.WaitOne();
                    }

                    if (wasPaused)
                    {
                        Thread.Sleep(Math.Max(Math.Min(MAX_POLL_PERIOD, _rand.Next(0, 250)), MIN_POLL_PERIOD));
                    }
                }
                else
                    firstIteration = false;
                
                if ((sub == null)|| (sub.device == null))
                    break;

                lock (m_subscription_list)
                {
                    if (!m_subscription_list.ContainsKey(sub.sub_key))
                    {
                        break;
                    }
                }
                
                try
                {
                    // We have no real way of checking wheter sub.comm has ben disposed other than catching the exception?
                    // I suppose hopefully sub.is_active_subscription will be false by the time that happens...

                    bool readValuesSuccessfully = sub.device.ReadPropertyMultipleRequest(sub.object_id, propertiesToPoll, out multi_value_list);

                    if (readValuesSuccessfully)
                    {
                        lock (m_subscription_list)
                        {
                            if (m_subscription_list.ContainsKey(sub.sub_key))
                            {
                                OnCOVNotification(sub.device.channel, sub.device.BacAdr, 0, sub.subscribe_id, new BacnetObjectId(BacnetObjectTypes.OBJECT_DEVICE, sub.device.deviceId), sub.object_id, 0, false, multi_value_list[0].values, BacnetMaxSegments.MAX_SEG0);
                                errorCount = 0;
                            }
                            else
                            {
                                statusItemFromListBox=null;
                                break;
                            }
                        }
                    }
                    else
                        errorCount++;
                }
                catch
                { 
                    errorCount++;
                }
               
                if (errorCount >= 4)
                {
                    Trace.TraceError(String.Format("The Notify function (while polling device {0}, object {1}", sub.device.deviceId.ToString(), sub.object_id.ToString()));
                    break;
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
                bool hasGraph = false;
                bool.TryParse(subscription.Value.SubItems[9].Text, out hasGraph);
                sb.Append(hasGraph ? "P;" : "D;");
                
                // Device Id
                sb.Append(((Subscription)subscription.Value.Tag).device.deviceId+";");
                // Object Id
                sb.Append(((Subscription)subscription.Value.Tag).object_id.ToString() + ";");
                // Period or 0 if COV
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

        private void m_SubscriptionView_DragDrop(object sender, DragEventArgs e)
        {
            // Drop from the adress space
            if (e.Data.GetDataPresent("CodersLab.Windows.Controls.NodesCollection", false))
            {
                //fetch end point
                if (_selectedDevice == null) return;
                else if (_selectedDevice.Tag == null) return;
                else if (!(_selectedDevice.Tag is BACnetDevice)) return;
                BACnetDevice entry = (BACnetDevice)_selectedDevice.Tag;
                BacnetAddress adr = entry.BacAdr;
                BacnetClient comm = entry.channel;

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
                    CreateSubscription(entry, Bobjs[i], sender == CovGraph);

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
                            if ((description.Length == 3) || (description.Length ==4))
                            {
                                try
                                {
                                    uint deviceId;
                                    deviceId = Convert.ToUInt32(description[1]);
                                    string objectIdString = description[2];
                                    if(!objectIdString.StartsWith("OBJECT_"))
                                    {
                                        objectIdString = "OBJECT_" + objectIdString;
                                    }
                                    BacnetObjectId objectId = BacnetObjectId.Parse(objectIdString);

                                    lock (m_devices)
                                    foreach (var E in m_devices)
                                        { 
                                            var devices = E.Value.Devices;
                                            foreach (var deviceEntry in devices)
                                            {
                                                if (deviceEntry.deviceId == deviceId)
                                                {
                                                    if (description.Length==4)
                                                        CreateSubscription(deviceEntry, objectId, description[2]=="P", Int32.Parse(description[3]));
                                                    else
                                                        CreateSubscription(deviceEntry, objectId, description[2] == "P");

                                                    break;
                                                }
                                            }

                                        }
                                }
                                catch { }
                            }

                        }
                    }
                    sr.Close();

                }
                catch { }

            }

        }

        private void m_SubscriptionView_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode != Keys.Delete) return;

            if (m_SubscriptionView.SelectedItems.Count >= 1)
            {
                foreach (ListViewItem itm in m_SubscriptionView.SelectedItems)
                {
                    if (itm.Tag is Subscription)    // It's a subscription or not (Event/Alarm)
                    {
                        Subscription sub = (Subscription)itm.Tag;

                        if (m_subscription_list.ContainsKey(sub.sub_key))
                        {
                            //remove from device
                            try
                            {
                                if (sub.is_COV_subscription)
                                    if (!sub.device.channel.SubscribeCOVRequest(sub.device.BacAdr, sub.object_id, sub.subscribe_id, true, false, 0))
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

                        lock (m_subscription_list)
                        {
                            m_subscription_list.Remove(sub.sub_key);
                            //remove from interface
                            m_SubscriptionView.Items.Remove(itm);
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
            BacnetClient comm = null;

            try
            {
                if (m_DeviceTree.SelectedNode.Tag is BacnetClient)
                {
                    comm = m_DeviceTree.SelectedNode.Tag as BacnetClient;
                }
                if (m_DeviceTree.SelectedNode.Tag is BACnetDevice)
                {
                    comm = (m_DeviceTree.SelectedNode.Tag as BACnetDevice).channel;
                }

                comm.WhoIs();
            }
            catch
            {
                foreach (TreeNode tn in NetworkViewTreeNode.Nodes)    // Nothing is selected, send on every available channel
                {
                    comm = (BacnetClient)tn.Tag;
                    comm?.WhoIs();
                }
            }
        }

        private void AddRemoteIpToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //fetch end point
            BacnetClient comm = FetchTransportClientEndPoint(typeof(BacnetIpUdpProtocolTransport));
            if (comm == null) return;

            try
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
            catch 
            {
                MessageBox.Show(this, "Invalid parameter", "Wrong node or IP @", MessageBoxButtons.OK, MessageBoxIcon.Information);          
            }
        }
        
        private void AddRemoteIpListToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //fetch end point
            BacnetClient comm = FetchTransportClientEndPoint(typeof(BacnetIpUdpProtocolTransport));
            if (comm == null) return;

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
            catch { }
            finally
            {
                this.Cursor = Cursors.Default;
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
        private void subscribeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //fetch end point
            if (m_DeviceTree.SelectedNode == null) return;
            else if (m_DeviceTree.SelectedNode.Tag == null) return;
            else if (!(m_DeviceTree.SelectedNode.Tag is BACnetDevice)) return;
            BACnetDevice entry = (BACnetDevice)m_DeviceTree.SelectedNode.Tag;
            BacnetAddress adr = entry.BacAdr;
            BacnetClient comm;
            if (m_DeviceTree.SelectedNode.Parent.Tag is BacnetClient)
                comm = (BacnetClient)m_DeviceTree.SelectedNode.Parent.Tag;
            else
                comm = (BacnetClient)m_DeviceTree.SelectedNode.Parent.Parent.Tag; // When device is under a Router
            uint device_id = entry.deviceId;

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
                //create 
                if (CreateSubscription(entry, object_id, false) == false)
                    return;
            }
        }
        private void m_subscriptionRenewTimer_Tick(object sender, EventArgs e)
        {

            void SetSubscriptionStatus(ListViewItem itm, string status)
            {
                if (itm.SubItems[6].Text == status) return;
                itm.SubItems[6].Text = status;
                itm.SubItems[5].Text = DateTime.Now.ToString(Properties.Settings.Default.COVTimeFormater);
            }
            
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

                        if (!sub.device.channel.SubscribeCOVRequest(sub.device.BacAdr, sub.object_id, sub.subscribe_id, false, Properties.Settings.Default.Subscriptions_IssueConfirmedNotifies, Properties.Settings.Default.Subscriptions_Lifetime))
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
        BacnetClient FetchTransportClientEndPoint(Type transportType, bool WithErrorMessage=true)
        {
            BacnetClient comm = null;
            // Make sure we are connected
            if (m_DeviceTree.SelectedNode == null) return null;
            else if (m_DeviceTree.SelectedNode.Tag == null) return null;

            if (m_DeviceTree.SelectedNode.Tag is BacnetClient)
            {
                comm = m_DeviceTree.SelectedNode.Tag as BacnetClient;
            }
            if (m_DeviceTree.SelectedNode.Tag is BACnetDevice)
            {
                comm = (m_DeviceTree.SelectedNode.Tag as BACnetDevice).channel;
            }
            // BacnetIpUdpProtocolTransport  transportType.IsInstanceOfType
            //if ((comm == null) || !(comm.Transport is transportType.))
            if ((comm == null) || (!transportType.IsInstanceOfType(comm.Transport)))
            {
                if ((WithErrorMessage==true)&&(transportType == typeof(BacnetIpUdpProtocolTransport)))
                    MessageBox.Show(this, "Please select an \"IP V4 transport\" node first", "Wrong node", MessageBoxButtons.OK, MessageBoxIcon.Information);
                else if ((WithErrorMessage == true) && (transportType == typeof(BacnetMstpProtocolTransport)))
                    MessageBox.Show(this, "Please select an \"Mstp transport\" node first", "Wrong node", MessageBoxButtons.OK, MessageBoxIcon.Information);
                else
                    MessageBox.Show(this, "Please select a \"transport "+ transportType.Name+ "\" node first", "Wrong node", MessageBoxButtons.OK, MessageBoxIcon.Information);

                return null;
            }

            return comm;
        }
        // Get the current device associated to the current node, or a node
        private BACnetDevice FetchEndPoint() => FetchEndPoint(m_DeviceTree.SelectedNode);
        private BACnetDevice FetchEndPoint(TreeNode node)
        {
            if (node != null)
                return (node.Tag as BACnetDevice);
            else
                return null;
        }
        private void FetchEndPoint(out List<BACnetDevice> endPoints)
        {
            endPoints = new List<BACnetDevice>();
            var device = FetchEndPoint();
            if (device != null)
                endPoints.Add(device);
        }
        // Get all devices
        private void FetchEndPoints(out List<BACnetDevice> endPoints)
        {
            lock (m_devices)
            {
                endPoints=m_devices.Values.SelectMany(line => line.Devices).ToList();
            }
        }
        private BACnetDevice FetchDeviceFromDeviceId(uint deviceId)
        {
            lock (m_devices)
            {
                foreach (var entry in m_devices)
                {
                    int idx = entry.Value.Devices.FindIndex(o=>o.deviceId==deviceId);
                    if (idx != -1)
                        return entry.Value.Devices[idx];
                }
                return null;
            }
        }
        TreeNode FindCommTreeNode(BacnetClient comm)
        {
            foreach (TreeNode node in NetworkViewTreeNode.Nodes)
            {
                BacnetClient c = node.Tag as BacnetClient;
                if (c != null && c.Equals(comm)) return node;
            }
            return null;
        }
        TreeNode FindCommTreeNode(IBacnetTransport transport)
        {
            foreach (TreeNode node in NetworkViewTreeNode.Nodes)
            {
                BacnetClient c = node.Tag as BacnetClient;
                if (c != null && c.Transport.Equals(transport)) return node;
            }
            return null;
        }
        private void timeSynchronizeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //fetch end point
            BACnetDevice device = FetchEndPoint();

            if (device == null)
            {
                MessageBox.Show(this, "Please select a device node", "Wrong node", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            //send
            if(Properties.Settings.Default.TimeSynchronize_UTC)
                device.channel.SynchronizeTime(device.BacAdr, DateTime.Now.ToUniversalTime(), true);
            else
                device.channel.SynchronizeTime(device.BacAdr, DateTime.Now, false);

            //done
            MessageBox.Show(this, "OK", "Time Synchronize", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        private void communicationControlToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //fetch end point
            BACnetDevice device = FetchEndPoint();

            if (device == null)
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
                if (!device.channel.ReinitializeRequest(device.BacAdr, dlg.ReinitializeState, dlg.Password))
                    MessageBox.Show(this, "Couldn't perform device communication control", "Device Communication Control", MessageBoxButtons.OK, MessageBoxIcon.Error);
                else
                    MessageBox.Show(this, "OK", "Device Communication Control", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                //Device Communication Control
                if (!device.channel.DeviceCommunicationControlRequest(device.BacAdr, dlg.Duration, dlg.DisableCommunication ? (uint)1 : (uint)0, dlg.Password))
                    MessageBox.Show(this, "Couldn't perform device communication control", "Device Communication Control", MessageBoxButtons.OK, MessageBoxIcon.Error);
                else
                    MessageBox.Show(this, "OK", "Device Communication Control", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void foreignDeviceRegistrationToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //fetch end point
            BacnetClient comm = FetchTransportClientEndPoint(typeof(BacnetIpUdpProtocolTransport));
            if (comm == null) return;

            Form F = new ForeignRegistry(comm);
            F.ShowDialog();

        }
        private void alarmSummaryToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //fetch end point
            BACnetDevice device = FetchEndPoint();

            if (device == null)
            {
                MessageBox.Show(this, "Please select a device node", "Wrong node", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            new AlarmSummary(m_AddressSpaceTree.ImageList, device).ShowDialog();
        }

        // Read the Adress Space, and change all object Id by name
        // Popup ToolTipText Get Properties Name
        private void readPropertiesNameToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //fetch end point
            BACnetDevice device = FetchEndPoint();

            if (device == null)
            {
                MessageBox.Show(this, "Please select a device node", "Wrong node", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
           
            // Go
            ChangeObjectIdByName(m_AddressSpaceTree.Nodes, device);

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
        private void SetObjectName(TreeNodeCollection tnc, IList<BacnetReadAccessResult> result, BACnetDevice device)
        {
            foreach (TreeNode tn in tnc)
            {
                BacnetObjectId b=(BacnetObjectId)tn.Tag;

                try
                {
                    if (tn.ToolTipText == "")
                    {
                        BacnetReadAccessResult r = result.Single(o => o.objectIdentifier.Equals(b));
                        device.UpdateObjectNameMapping((BacnetObjectId)tn.Tag, r.values[0].value[0].ToString());
                    }
                }
                catch { }

                if (tn.Nodes != null)
                    SetObjectName(tn.Nodes, result, device);
            }

        }
        // Try a ReadPropertyMultiple for all PROP_OBJECT_NAME not already known
        private void ChangeObjectIdByName(TreeNodeCollection tnc, BACnetDevice device)
        {
            int _retries = device.channel.Retries;
            device.channel.Retries = 1;
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
                    if (device.ReadPropertyMultipleRequest(bras, out result) == true)
                    {
                        SetObjectName(tnc, result, device);
                        IsOK = true;
                    }
                }
                catch{}
            }

            device.channel.Retries = _retries ;

            if (IsOK)
            {
                // We did not update the tree as we went (for speed), so do it all at once now
                m_DeviceTree_AfterSelect("ObjRename", new TreeViewEventArgs(this._selectedDevice));
                this.Cursor = Cursors.Default;
            }
            else
            {
                // Fail, so go One by One, in a background thread
                System.Threading.ThreadPool.QueueUserWorkItem((o) =>
                {
                    ChangeObjectIdByNameOneByOne(m_AddressSpaceTree.Nodes, device, AsynchRequestId);
                });
                this.Cursor = Cursors.Default;
            }
        }

        private void ChangeObjectIdByNameOneByOne(TreeNodeCollection tnc, BACnetDevice device, int AsynchRequestId)
        {
            int _retries = device.channel.Retries;
            device.channel.Retries = 1;

            foreach (TreeNode tn in tnc)
            {
                if ((tn.ToolTipText == "") && (tn.Tag != null))
                {
                    try
                    {
                        String Name=device.ReadObjectName((BacnetObjectId)tn.Tag);
                        if (!string.IsNullOrWhiteSpace(Name))
                        {
                            if (AsynchRequestId != this.AsynchRequestId) // Selected device is no more the good one
                            {
                                device.channel.Retries = _retries;
                                return;
                            }

                            this.Invoke((MethodInvoker)delegate
                            {
                                if (AsynchRequestId != this.AsynchRequestId) return; // another test in the GUI thread

                                // We are already going on-by-one (SLOW), in a different thread, so just update
                                // as we go. Don't bother optimising (Tested and only ~15% faster in this case)
                                ChangeTreeNodePropertyName(tn, Name);

                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        Trace.TraceWarning("Failed to obtain object name for object " + tn.Tag + ": " + ex);
                    }
                }

                if (tn.Nodes != null)
                    ChangeObjectIdByNameOneByOne(tn.Nodes, device, AsynchRequestId);

                device.channel.Retries = _retries;
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
                    BACnetDevice.DevicesObjectsName = d;
                    BACnetDevice.objectNamesChangedFlag = true;
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
                bf.Serialize(stream, BACnetDevice.DevicesObjectsName);
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

        private void createObjectToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //fetch end point
            BACnetDevice device=FetchEndPoint();

            if (device == null)
            {
                MessageBox.Show(this, "Please select a device node", "Wrong node", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

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
                    device.channel.CreateObjectRequest(device.BacAdr, new BacnetObjectId((BacnetObjectTypes)F.ObjectType.SelectedIndex, (uint)F.ObjectId.Value), initialvalues);

                    m_DeviceTree_AfterSelect("ObjNewDelete", new TreeViewEventArgs(this._selectedDevice));
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

            BACnetDevice device=FetchEndPoint();

            if ((device != null) && (device.channel.Transport is BacnetIpUdpProtocolTransport) && (device.BacAdr != null) && (device.BacAdr.RoutedSource == null))
                new BBMDEditor(device.channel, device.BacAdr).ShowDialog();
            else
                MessageBox.Show("An IPv4 device is required", "Wrong device", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        private void cleanToolStripMenuItem_Click(object sender, EventArgs e)
        {             
            DialogResult res = MessageBox.Show(this, "Clean all "+ BACnetDevice.DevicesObjectsName.Count.ToString()+" entries from \""+Properties.Settings.Default.Auto_Store_Object_Names_File+"\", really?", "Name database suppression", MessageBoxButtons.OKCancel, MessageBoxIcon.Information);
            if (res == DialogResult.OK)
            {
                BACnetDevice.DevicesObjectsName = new Dictionary<Tuple<String, BacnetObjectId>, String>();
                Trace.TraceInformation("Created new object names dictionary.");
                BACnetDevice.objectNamesChangedFlag = true;
                DoSaveObjectNames();
                // Enumerate each Transport Layer:
                foreach (TreeNode transport in NetworkViewTreeNode.Nodes)
                {   
                    //Enumerate each Parent Device:
                    foreach (TreeNode node in transport.Nodes)
                    {
                        try
                        {
                            BACnetDevice entryNullable = node.Tag as BACnetDevice;
                            if(entryNullable!=null)
                            {
                                BACnetDevice entry = entryNullable;

                                node.Text = "Device " + entry.deviceId + " - " + entry.BacAdr.ToString(false);
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
                                BACnetDevice entryNullable2 = subNode.Tag as BACnetDevice;
                                if(entryNullable2!=null)
                                {
                                    BACnetDevice entry2 = entryNullable2;
                                    subNode.Text = "Device " + entry2.deviceId + " - " + entry2.BacAdr.ToString(true);
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

        // Change the WritePriority Value & Simplified view
        private void MainDialog_KeyDown(object sender, KeyEventArgs e)
        {
            if ((e.Modifiers == (Keys.Control | Keys.Alt)))
            {
                if (e.KeyValue == (int)Keys.S)
                    ToggleViewMenuItem_Click(null, null);
                    
                if (e.KeyValue == (int)Keys.N)
                    readPropertiesNameToolStripMenuItem_Click(null,null);

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

        private void m_AddressSpaceTree_AfterSelect(object sender, TreeViewEventArgs e)
        {
            this.m_SubscriptionView.SelectedItems.Clear();
            UpdateGrid(e.Node);

            // Hide all elements in the toolstip menu
            foreach (object its in m_AddressSpaceMenuStrip.Items)
                (its as ToolStripMenuItem).Visible = false;
            // Set Subscribe always visible
            m_AddressSpaceMenuStrip.Items[0].Visible = true;
            // Set Search always visible
            m_AddressSpaceMenuStrip.Items[8].Visible = true;
            // Toggle view always visible
            m_AddressSpaceMenuStrip.Items[9].Visible = true;

            // Get the node type
            BACnetDevice device;
            BacnetObjectId objId;
            GetObjectLink(out device, out objId); // objId cannot be null it's a value type
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

        private void m_SubscriptionView_SelectedIndexChanged(object sender, EventArgs e)
        {
            
            try
            {
                ListView.SelectedListViewItemCollection selectedSubscriptions = this.m_SubscriptionView.SelectedItems;

                if ((selectedSubscriptions==null) || (selectedSubscriptions.Count==0))
                    return;
                
                this.m_AddressSpaceTree.SelectedNode = null;
                this.m_AddressSpaceTree.SelectedNodes.Clear();
                
                ListViewItem itm=selectedSubscriptions[0] ;

                if (!(itm.Tag is Subscription subscription))
                    return;
                else
                    UpdateGrid(subscription);

            }
            catch { }

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
                if (BACnetDevice.objectNamesChangedFlag)
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
                    bf.Serialize(stream, BACnetDevice.DevicesObjectsName);
                    stream.Close();
                    BACnetDevice.objectNamesChangedFlag = false;
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

        private void HighlightTreeNodes(TreeNodeCollection tnc, String text, Color color)
        {
            foreach (TreeNode tn in tnc)
            {
                if (tn.Text.ToLower().Contains(text.ToLower()))
                {
                    tn.ForeColor = color;
                }
                else
                    tn.ForeColor = Color.Black;

                if (tn.Nodes.Count != 0)
                    HighlightTreeNodes(tn.Nodes, text, color);

            }
        }
        private void TbxHighlightTreeView_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode != Keys.Return) return;

            TbxHighlightTreeView_Update(sender, null);
        }

        private void TbxHighlightTreeView_Update(object sender, EventArgs e)
        {
            TextBox tbx = (TextBox)sender;

            Color color = Color.Red;
            if ((tbx.Text == null) || (tbx.Text.Length == 0))
                color = Color.Black;

            if (tbx == TbxHighlightAddress)
                HighlightTreeNodes(m_AddressSpaceTree.Nodes, tbx.Text, color);
            else
                HighlightTreeNodes(m_DeviceTree.Nodes, tbx.Text, color);
        }

        private void manual_refresh_properties_Click(object sender, EventArgs e)
        {
            // perform manual update
            if (_selectedNode != null)
            {
                if (_selectedNode is Subscription sub)
                {
                    UpdateGrid(sub);
                }
                else if (_selectedNode is TreeNode node)
                {
                    UpdateGrid(node);
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

        private void menuStrip1_MenuActivate(object sender, EventArgs e)
        {
            // Updates controls enabled state.
            FetchEndPoints(out var endPoints);
            exportEDEFilesSelDeviceToolStripMenuItem.Enabled = (FetchEndPoint() != null);
            exportEDEFilesAllDevicesToolStripMenuItem.Enabled = (endPoints.Count >= 1);
        }

        private void DoAck(BacnetEventNotificationData.BacnetEventStates eventState)
        {
            void AckFail()
            {
                MessageBox.Show("Alarm acknowledge failed!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

            this.Cursor = Cursors.WaitCursor;
            try
            {
                BACnetDevice device;
                BacnetAddress adr;
                BacnetClient comm;

                if (_selectedNode == null) return;

                if (_selectedNode is TreeNode treeNode)
                {
                    //fetch end point
                    if (_selectedDevice == null) return;
                    else if (_selectedDevice.Tag == null) return;
                    else if (!(_selectedDevice.Tag is BACnetDevice)) return;
                    device = (BACnetDevice)_selectedDevice.Tag;
                    adr = device.BacAdr;
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
                            UpdateGrid(treeNode);
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
                    adr = subscription.device.BacAdr;
                    comm = subscription.device.channel;

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
                        UpdateGrid(subscription);
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

        private void ack_icon_Click(object sender, EventArgs e)
        {
            if (sender==ack_offnormal)
                DoAck(BacnetEventNotificationData.BacnetEventStates.EVENT_STATE_OFFNORMAL);
            if (sender == ack_fault)
                DoAck(BacnetEventNotificationData.BacnetEventStates.EVENT_STATE_FAULT);
            if (sender == ack_normal)
                DoAck(BacnetEventNotificationData.BacnetEventStates.EVENT_STATE_NORMAL);
        }

        private void manual_refresh_objects_Click(object sender, EventArgs e)
        {
            if (_selectedDevice == null) return;
                m_DeviceTree_AfterSelect(sender, new TreeViewEventArgs(_selectedDevice));
        }

        private void searchToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            GenericInputBox<TextBox> search = new GenericInputBox<TextBox>("Search object", "Name",     (o) =>
            {
                    o.Text = m_AddressSpaceTree.SelectedNode?.Text;
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

        private void LblLog_DoubleClick(object sender, EventArgs e)
        {
            m_LogText.Text = "";    // Clear the Log
        }

        private void restartToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Restart();
        }

        Mstp.BacnetCapture.BacnetCapture CaptureForm;
        private void mstpCaptureToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!BacnetCapture.SingletonIsRunning)
            {
                BacnetClient comm = FetchTransportClientEndPoint(typeof(BacnetMstpProtocolTransport));
                if (comm != null)
                {
                    CaptureForm = new Mstp.BacnetCapture.BacnetCapture((BacnetMstpProtocolTransport)comm.Transport);
                    CaptureForm.Show();
                }
            }
            else
                CaptureForm.BringToFront();
        }
        private void ToggleViewMenuItem_Click(object sender, EventArgs e)
        {

            TreeNode stn = _selectedNode as TreeNode;
            Subscription sub = _selectedNode as Subscription;

            ToogleViewSimplified = !ToogleViewSimplified;

            if (ToogleViewSimplified)
                toolStripMenuView2.Text = toolStripMenuView1.Text = "Full Normal View";
            else
                toolStripMenuView2.Text = toolStripMenuView1.Text = "Simplified View";

            m_DeviceTree_AfterSelect("ObjRename", new TreeViewEventArgs(this._selectedDevice));
            if (stn != null)
            {
                UpdateGrid(stn); // maybe the object is no more displayed, it's not a problem
                foreach (TreeNode tn in m_AddressSpaceTree.Nodes) // try to select the same
                {
                    if (tn.Tag.ToString() == stn.Tag.ToString())
                    {
                        m_AddressSpaceTree.SelectedNode = tn;
                        _selectedNode = tn;
                        return;
                    }
                    foreach (TreeNode tn2 in tn.Nodes)
                    {
                        if (tn2.Tag.ToString() == stn.Tag.ToString())
                        {
                            m_AddressSpaceTree.SelectedNode = tn2;
                            _selectedNode = tn2;
                            return;
                        }
                    }
                }
            }
            if (sub != null)
                UpdateGrid(sub); // maybe the object is no more displayed, it's not a problem               
            
        }
    }

    #region " Trace Listner "
    class MyTraceListener : TraceListener
    {
        private TextBox m_LogText;
        public MyTraceListener(TextBox txtLog)
            : base("MyListener")
        {
            m_LogText = txtLog;
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
            if (!m_LogText.IsHandleCreated)
                return;

            m_LogText.BeginInvoke((MethodInvoker)delegate { m_LogText.AppendText(message); });
        }
    }
    #endregion
    // Used to sort the devices Tree
    public class NodeSorter : IComparer
    {
        bool NetworkBeforeClass;
        public NodeSorter(bool NetworkBeforeClass)
        {
            this.NetworkBeforeClass = NetworkBeforeClass;
        }
        public int Compare(object x, object y)
        {
            TreeNode tx = x as TreeNode;
            TreeNode ty = y as TreeNode;

            // Two Devices node, orderd by deviceId
            if ((tx.Tag is BACnetDevice txdev) && (ty.Tag is BACnetDevice tydev))
                return txdev.deviceId.CompareTo(tydev.deviceId);

            // Two Group Nodes
            if ((tx.Tag is List<int> txList) && (ty.Tag is List<int> tyList))
                return tyList[0].CompareTo(txList[0]);

            // A Group and a device
            if ((tx.Tag is BACnetDevice) && (ty.Tag is List<int>))
                return 1;
            if ((tx.Tag is List<int>) && (ty.Tag is BACnetDevice))
                return -1;

            // The Two Root nodes ordered with the user choice
            if ((tx.Tag is int txidev) && (ty.Tag is int tyidev))
            if (NetworkBeforeClass)
                return tyidev.CompareTo(txidev);
            else
                return txidev.CompareTo(tyidev);


            return 0;   // Don't care
        }
    }

    public enum AddressTreeViewType
    {
        List,
        Structured,
        Both,
        FieldTechnician
    }

    public enum DeviceTreeViewType
    {
        Network,
        DeviceClass,
        NetworkThenDeviceClass,
        DeviceClassThenNetwork
    }
}
