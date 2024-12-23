using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;
using Yabe;
using System.IO.BACnet;
using System.IO;

namespace FindPrioritiesGlobal
{
    public partial class FindPrioritiesGlobal : Form
    {
        private const int PATIENCE_INTERVAL = 7500;

        private YabeMainDialog _yabeFrm;

        private List<BacnetDeviceExport> _commonDevices;
        private List<BacnetDeviceExport> _selectedDevices;
        private List<BacnetPointExport> _pointListForDisplay;
        public bool[] aPrioFilter = new bool[16];

        public string[] sPrioNames = {  "Manual-Life Safety",
                                        "Automatic-Life Safety",
                                        "Available",
                                        "Available",
                                        "Critical Equipment Control",
                                        "Minimum On/Off",
                                        "Available",
                                        "Manual Operator",
                                        "Available",
                                        "Available",
                                        "Available",
                                        "Available",
                                        "Available",
                                        "Available",
                                        "Available",
                                        "Available"
                                    };
        public string GUI_LastFilename = "";
        private Dictionary<Tuple<String, BacnetObjectId>, String> DevicesObjectsName { get { return _yabeFrm.DevicesObjectsName; } }
        private bool ObjectNamesChangedFlag { get { return _yabeFrm.objectNamesChangedFlag; } set { _yabeFrm.objectNamesChangedFlag = value; } }

        public FindPrioritiesGlobal(YabeMainDialog yabeFrm)
        {
            Cursor.Current = Cursors.WaitCursor;
            this._yabeFrm = yabeFrm;

            Icon = yabeFrm.Icon; // gets Yabe Icon
            InitializeComponent();
        }

        private void FindPrioritiesGlobal_Load(object sender, EventArgs e)
        {
            Cursor.Current = Cursors.WaitCursor;
            UpdatePrioFilter(sender, e);
        }
        public List<BacnetDeviceExport> PopulateDevicesWithNames(bool commandProgBar = false)
        {
            int progTotal = _yabeFrm.YabeDiscoveredDevices.Count() + 1;
            int prog = 0;
            List<BacnetDeviceExport> deviceList = new List<BacnetDeviceExport>();
            foreach (BACnetDevice device in _yabeFrm.YabeDiscoveredDevices)
            {
                BacnetDeviceExport devExport = new BacnetDeviceExport(device, this);
                string identifier = null;

                identifier=_yabeFrm.ReadObjectName(device, new BacnetObjectId(BacnetObjectTypes.OBJECT_DEVICE, device.deviceId));

                identifier = identifier + " [" + device.deviceId.ToString() + "] ";
               
                if (identifier != null)
                {
                    devExport.Name = identifier;
                }
                if (deviceList.Find(item => item.Device == device) == null)
                {
                    deviceList.Add(devExport);
                }

                if (commandProgBar)
                {
                    prog++;
                    progBar.Value = (int)(100 * prog / progTotal);
                    Application.DoEvents();
                }
            }
            return deviceList;
        }

        public bool PopulatePointsForDevices(List<BacnetDeviceExport> devices, bool commandProgBar = false)
        {
            int progTotal = devices.Count + 2;
            int prog = 0;
            bool result = true;
            for (int i = 0; i < devices.Count; i++)
            {
                bool particularResult = PopulatePointsForDevice(devices[i]);
                if (!particularResult)
                {
                    result = false;
                    return result;
                }
                if (commandProgBar)
                {
                    prog++;
                    progBar.Value = (int)(100 * prog / progTotal);
                    Application.DoEvents();
                }
            }
            return result;
        }

        public bool PopulatePointsForDevice(BacnetDeviceExport devExport)
        {
            var device = devExport.Device;

            device.ReadObjectList(out _, out uint Count);

            devExport.Points.Clear();
            for (uint j=1;j<= Count;j++)
            {
                BacnetObjectId bobj_id;
                device.ReadObjectListOneByOne(out bobj_id, j);

                // Only the following objects contain or may contain a PriorityArray Property (135-2020)
                //
                // Access Door
                // Analog Output
                // Analog Value
                // Binary Lighting Output
                // Binary Output
                // Binary Value
                // Bitstring Value
                // Channel
                // Character String Value
                // Date Value
                // Date Pattern Value
                // Date Time Value
                // Date Time Pattern Value
                // Integer Value
                // Large Analog Value
                // Lighting Output
                // Multistate Output
                // Multistate Value
                // Octet String Value
                // Positive Integer Value
                // Time Value
                // Time Pattern Value

                if (bobj_id.type != BacnetObjectTypes.OBJECT_ACCESS_DOOR &&
                    bobj_id.type != BacnetObjectTypes.OBJECT_ANALOG_OUTPUT &&
                    bobj_id.type != BacnetObjectTypes.OBJECT_ANALOG_VALUE &&
                    bobj_id.type != BacnetObjectTypes.OBJECT_BINARY_LIGHTING_OUTPUT &&
                    bobj_id.type != BacnetObjectTypes.OBJECT_BINARY_OUTPUT &&
                    bobj_id.type != BacnetObjectTypes.OBJECT_BINARY_VALUE &&
                    bobj_id.type != BacnetObjectTypes.OBJECT_BITSTRING_VALUE &&
                    bobj_id.type != BacnetObjectTypes.OBJECT_CHANNEL &&
                    bobj_id.type != BacnetObjectTypes.OBJECT_CHARACTERSTRING_VALUE &&
                    bobj_id.type != BacnetObjectTypes.OBJECT_DATE_VALUE &&
                    bobj_id.type != BacnetObjectTypes.OBJECT_DATE_PATTERN_VALUE &&
                    bobj_id.type != BacnetObjectTypes.OBJECT_DATETIME_VALUE &&
                    bobj_id.type != BacnetObjectTypes.OBJECT_DATETIME_PATTERN_VALUE &&
                    bobj_id.type != BacnetObjectTypes.OBJECT_INTEGER_VALUE &&
                    bobj_id.type != BacnetObjectTypes.OBJECT_LARGE_ANALOG_VALUE &&
                    bobj_id.type != BacnetObjectTypes.OBJECT_LIGHTING_OUTPUT &&
                    bobj_id.type != BacnetObjectTypes.OBJECT_MULTI_STATE_OUTPUT &&
                    bobj_id.type != BacnetObjectTypes.OBJECT_MULTI_STATE_VALUE &&
                    bobj_id.type != BacnetObjectTypes.OBJECT_OCTETSTRING_VALUE &&
                    bobj_id.type != BacnetObjectTypes.OBJECT_POSITIVE_INTEGER_VALUE &&
                    bobj_id.type != BacnetObjectTypes.OBJECT_TIME_VALUE &&
                    bobj_id.type != BacnetObjectTypes.OBJECT_TIME_PATTERN_VALUE)
                {
                    // Trace.WriteLine("Object does not support Priority Array property");
                    continue;
                }

                if (true) // if (bobj_id.type != BacnetObjectTypes.OBJECT_DEVICE || bobj_id.instance != device.DeviceID)
                {
                    BacnetPointExport point = new BacnetPointExport(devExport, bobj_id);

                    // If the Device name not set, try to update it
                    string identifier = null;
                    string objectName = null;

                    objectName = _yabeFrm.ReadObjectName(device, bobj_id);
                    identifier = objectName + " [" + bobj_id.ToString() + "] ";

                    // check priorities
                    bool bFound = false;
                    try
                    {
                        IList<BacnetValue> values;
                        device.channel.ReadPropertyRequest(device.BacAdr, bobj_id, BacnetPropertyIds.PROP_PRIORITY_ARRAY, out values);
                        if (values.Count == 16)
                        {
                            int i;
                            for (i = 0; i < 16; i++)
                            {
                                point.aPriosSet[i] = false; // initialize with false as default, no priority set at this level
                                    if (null != values[i].Value && aPrioFilter[i])
                                    {
                                        bFound = true; // if any prio is set and not filtered, item will be displayed in the list
                                        point.aPriosSet[i] = true; // if there is a value remember this slot
                                point.aValues[i] = values[i].Value.ToString();
                                }
                            }
                        }
                    }
                    catch
                    {
                    }
                    if (identifier != null)
                    {
                        point.Name = identifier;
                        point.ObjectName = objectName;
                    }

                    if (devExport.Points.Find(item => item.ObjectID.Equals(bobj_id)) == null)
                    {
                        if (bFound)
                            devExport.Points.Add(point);
                    }
                }
            }
            return true;
        }

        public class BacnetDeviceExport : IEquatable<BacnetDeviceExport>, IComparable<BacnetDeviceExport>
        {
            private string _name;
            public string Name { get { return _name; } set { _nameIsSet = true; _name = value; } }
            private bool _nameIsSet;
            public bool NameIsSet { get { return _nameIsSet; } }
            public BACnetDevice Device { get; }
            public FindPrioritiesGlobal ParentWindow { get; }
            public List<BacnetPointExport> Points { get; }

            public override string ToString()
            {
                return Name;
            }

            public bool Equals(BacnetDeviceExport other)
            {
                return Name.Equals(other.Name, StringComparison.OrdinalIgnoreCase);
            }

            public int CompareTo(BacnetDeviceExport other)
            {
                return Name.CompareTo(other.Name);
            }

            public BacnetDeviceExport(BACnetDevice device, FindPrioritiesGlobal parentWindow)
            {
                Device = device;
                ParentWindow = parentWindow;
                _name = Device.deviceId.ToString();
                _nameIsSet = false;
                Points = new List<BacnetPointExport>();
            }
        }

        public class BacnetPointExport : IEquatable<BacnetPointExport>, IComparable<BacnetPointExport>
        {
            public bool[] aPriosSet = new bool[16];
            public string[] aValues = new string[16];

            public BacnetDeviceExport ParentDevice { get; }
            public BacnetObjectId ObjectID { get; }
            private string _name;
            public string Name { get { return _name; } set { _nameIsSet = true; _name = value; } }
            private bool _nameIsSet;
            public bool NameIsSet { get { return _nameIsSet; } }
            private string _objectName;
            public string ObjectName { get { return _objectName; } set { _objectNameIsSet = true; _objectName = value; } }
            private bool _objectNameIsSet;
            public bool ObjectNameIsSet { get { return _objectNameIsSet; } }
            public List<BacnetPropertyExport> Properties { get; }

            public override string ToString()
            {
                return Name;
            }

            public bool Equals(BacnetPointExport other)
            {
                return Name.Equals(other.Name, StringComparison.OrdinalIgnoreCase);
            }

            public int CompareTo(BacnetPointExport other)
            {
                return Name.CompareTo(other.Name);
            }
            public BacnetPointExport(BacnetDeviceExport parentDevice, BacnetObjectId objectID)
            {
                ObjectID = objectID;
                ParentDevice = parentDevice;
                _name = objectID.ToString();
                _nameIsSet = false;
                Properties = new List<BacnetPropertyExport>();

            }
        }

        public class BacnetPropertyExport
        {
            private string ArrayToString(object[] arrayObj)
            {
                StringBuilder sb = new StringBuilder();
                sb.Append("{");
                for (int i = 0; i < arrayObj.Length; i++)
                {
                    if (arrayObj[i] != null)
                    {
                        sb.Append(arrayObj[i].ToString());
                    }
                    else
                    {
                        sb.Append("null");
                    }
                    if (i < (arrayObj.Length - 1))
                    {
                        sb.Append(",");
                    }
                }
                sb.Append("}");
                return sb.ToString();
            }
        }

        public class ListViewItemBetterString : ListViewItem
        {
            public override string ToString()
            {
                if (!string.IsNullOrEmpty(Name))
                {
                    return Name;
                }
                else if (!string.IsNullOrEmpty(Text))
                {
                    return Text;
                }
                else if (Tag != null)
                {
                    return Tag.ToString();
                }
                else
                {
                    return base.ToString();
                }
            }
        }

        private void StartPatienceTimer()
        {
            PatienceTimer.Interval = PATIENCE_INTERVAL;
            PatienceTimer.Enabled = true;
        }

        private void RequestPatience()
        {
            PatienceTimer.Enabled = false;
            PatienceLabel.Visible = true;
        }

        private void ResetPatience()
        {
            PatienceTimer.Enabled = false;
            PatienceLabel.Visible = false;
        }

        private void PatienceTimer_Tick(object sender, EventArgs e)
        {
            RequestPatience();
        }
        private void ResetFromObjectsList()
        {
            treeView1.SelectedNode = null;
            treeView1.Nodes.Clear();
        }

        private void cmdSearchDevices_Click(object sender, EventArgs e)
        {

            Cursor.Current = Cursors.WaitCursor;
            StartPatienceTimer();
            progBar.Value = 0;
            Application.DoEvents();
            treeView1.SelectedNode = null;
            treeView1.Nodes.Clear();

            _commonDevices = PopulateDevicesWithNames(true);
            _commonDevices.Sort();

            DeviceList.Items.Clear();

            foreach (BacnetDeviceExport device in _commonDevices)
            {
                ListViewItemBetterString item = new ListViewItemBetterString();
                item.Text = device.Name;
                item.Name = item.Text;
                item.Tag = device;
                DeviceList.Items.Add(item);
            }
            progBar.Value = 100;
            ResetPatience();
            Application.DoEvents();
            Cursor.Current = Cursors.Default;

            // Select all items in the ListBox
            DeviceList.BeginUpdate();

            for (int i = 0; i < DeviceList.Items.Count; i++)
                DeviceList.SetSelected(i, true);

            DeviceList.EndUpdate();
        }
        private void cmdSearchPriorities_Click(object sender, EventArgs e)
        {
            ResetFromObjectsList();

            _selectedDevices = new List<BacnetDeviceExport>();

            if (DeviceList.SelectedItem != null)
            {
                foreach (ListViewItemBetterString item in DeviceList.SelectedItems)
                {
                    _selectedDevices.Add((BacnetDeviceExport)item.Tag);
                }
            }
            else
            {
                MessageBox.Show("No device(s) selected.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            Cursor.Current = Cursors.WaitCursor;
            StartPatienceTimer();
            progBar.Value = 0;
            Application.DoEvents();

            _pointListForDisplay = new List<BacnetPointExport>();


            if (_selectedDevices != null && _selectedDevices.Count > 0)
            {
                bool result = PopulatePointsForDevices(_selectedDevices, true);
                if (result)
                {
                    _pointListForDisplay = new List<BacnetPointExport>();

                    {
                        // Display a big exhaustive list of every object from every selected controller.

                        foreach (BacnetDeviceExport device in _selectedDevices)
                        {
                            foreach (BacnetPointExport point in device.Points)
                            {
                                _pointListForDisplay.Add(point);
                            }
                        }
                    }
                }
                else
                {
                    return;
                }
            }

            int nNodeNumber = -1;
            string lastParentName = "";
            treeView1.Nodes.Clear();
            foreach (BacnetPointExport point in _pointListForDisplay)
            {
                string item;
                item = point.Name;

                // Add Device
                if( lastParentName != point.ParentDevice.Name )
                {
                    lastParentName = point.ParentDevice.Name;
                    treeView1.Nodes.Add(point.ParentDevice.Name);
                    nNodeNumber++;
                }
                
                // Add object which contains priorities
                treeView1.Nodes[nNodeNumber].Nodes.Add(item);

                // Add priorities which contain a value != NULL
                for (int i = 0; i < 16; i++)
                    if (point.aPriosSet[i])
                    {
                        int n = i + 1;
                        string sItem;
                        sItem = "Prio: " + n.ToString();
                        if (IncludePriorityLevelNames.Checked)
                            sItem = sItem + " " + sPrioNames[i];
                        if (PrintValues.Checked)
                            sItem = sItem + " Value: " + point.aValues[i];
                        treeView1.Nodes[nNodeNumber].Nodes.Add(sItem);
                    }
            }

            progBar.Value = 100;
            ResetPatience();

            Application.DoEvents();
            Cursor.Current = Cursors.Default;
        }

        private void UpdatePrioFilter(object sender, EventArgs e)
        {
            int i;

            for (i = 0; i < 16; i++)
                aPrioFilter[i] = false;

            if (Prio1.Checked) aPrioFilter[0] = true;
            if (Prio2.Checked) aPrioFilter[1] = true;
            if (Prio3.Checked) aPrioFilter[2] = true;
            if (Prio4.Checked) aPrioFilter[3] = true;
            if (Prio5.Checked) aPrioFilter[4] = true;
            if (Prio6.Checked) aPrioFilter[5] = true;
            if (Prio7.Checked) aPrioFilter[6] = true;
            if (Prio8.Checked) aPrioFilter[7] = true;
            if (Prio9.Checked) aPrioFilter[8] = true;
            if (Prio10.Checked) aPrioFilter[9] = true;
            if (Prio11.Checked) aPrioFilter[10] = true;
            if (Prio12.Checked) aPrioFilter[11] = true;
            if (Prio13.Checked) aPrioFilter[12] = true;
            if (Prio14.Checked) aPrioFilter[13] = true;
            if (Prio15.Checked) aPrioFilter[14] = true;
            if (Prio16.Checked) aPrioFilter[15] = true;
        }

        private void Prio1_CheckedChanged(object sender, EventArgs e)
        {
            UpdatePrioFilter(sender, e);
        }

        private void Prio2_CheckedChanged(object sender, EventArgs e)
        {
            UpdatePrioFilter(sender, e);
        }

        private void Prio3_CheckedChanged(object sender, EventArgs e)
        {
            UpdatePrioFilter(sender, e);
        }

        private void Prio4_CheckedChanged(object sender, EventArgs e)
        {
            UpdatePrioFilter(sender, e);
        }

        private void Prio5_CheckedChanged(object sender, EventArgs e)
        {
            UpdatePrioFilter(sender, e);
        }

        private void Prio6_CheckedChanged(object sender, EventArgs e)
        {
            UpdatePrioFilter(sender, e);
        }

        private void Prio7_CheckedChanged(object sender, EventArgs e)
        {
            UpdatePrioFilter(sender, e);
        }

        private void Prio8_CheckedChanged(object sender, EventArgs e)
        {
            UpdatePrioFilter(sender, e);
        }

        private void Prio9_CheckedChanged(object sender, EventArgs e)
        {
            UpdatePrioFilter(sender, e);
        }

        private void Prio10_CheckedChanged(object sender, EventArgs e)
        {
            UpdatePrioFilter(sender, e);
        }

        private void Prio11_CheckedChanged(object sender, EventArgs e)
        {
            UpdatePrioFilter(sender, e);
        }

        private void Prio12_CheckedChanged(object sender, EventArgs e)
        {
            UpdatePrioFilter(sender, e);
        }

        private void Prio13_CheckedChanged(object sender, EventArgs e)
        {
            UpdatePrioFilter(sender, e);
        }

        private void Prio14_CheckedChanged(object sender, EventArgs e)
        {
            UpdatePrioFilter(sender, e);
        }

        private void Prio15_CheckedChanged(object sender, EventArgs e)
        {
            UpdatePrioFilter(sender, e);
        }

        private void Prio16_CheckedChanged(object sender, EventArgs e)
        {
            UpdatePrioFilter(sender, e);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            treeView1.CollapseAll();
        }

        private void button_expand_Click(object sender, EventArgs e)
        {
            treeView1.ExpandAll();
        }

        private void buttonExport_Click(object sender, EventArgs e)
        {

            if (treeView1.GetNodeCount(false) == 0)
            {
                MessageBox.Show(" List is empty, nothing to export.");
                return;
            }

            SaveFileDialog dlg = new SaveFileDialog();
            dlg.FileName = GUI_LastFilename;
            dlg.Filter = "Yabe Export files (*.csv)|*.csv|All files (*.*)|*.*";
            if (dlg.ShowDialog(this) != System.Windows.Forms.DialogResult.OK) 
                return;
            string filename = dlg.FileName;
            GUI_LastFilename = filename;    // remember last filename

            try
            {
                var csv = new StringBuilder();
                string sSeparator = ";";
                int i;

                // Create headline for csv-file
                var sHeadLine = string.Format(
                    "Device" +
                    sSeparator +
                    "Object");
                for( i = 0; i < 16; i++ )
                {
                    int nPrio;
                    sHeadLine += sSeparator;
                    nPrio = i + 1;
                    sHeadLine += nPrio.ToString();
                    if (IncludePriorityLevelNames.Checked)
                    {
                        sHeadLine += " - ";
                        sHeadLine += sPrioNames[i];
                    }
                }
                csv.AppendLine(sHeadLine);

                foreach (BacnetPointExport point in _pointListForDisplay)
                {
                    string sItem;

                    sItem = point.ParentDevice.Name;
                    sItem += sSeparator;
                    sItem += point.Name;
                    for (i = 0; i < 16; i++)
                    {
                        sItem += sSeparator;
                        if (point.aPriosSet[i])
                        {
                            if(PrintValues.Checked)
                                sItem += point.aValues[i];
                            else
                                sItem += "x";
                        }
                        else
                            sItem += "";
                    }
                    csv.AppendLine( sItem );
                }

                File.WriteAllText(filename, csv.ToString());
            }
            catch
            {
                MessageBox.Show(this, "File error", "Error saving file, sorry!", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            MessageBox.Show(this, "File saved", "File successfully exported.", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void Button_SelectNone_Click(object sender, EventArgs e)
        {
            Prio1.Checked = false;
            Prio2.Checked = false;
            Prio3.Checked = false;
            Prio4.Checked = false;
            Prio5.Checked = false;
            Prio6.Checked = false;
            Prio7.Checked = false;
            Prio8.Checked = false;
            Prio9.Checked = false;
            Prio10.Checked = false;
            Prio11.Checked = false;
            Prio12.Checked = false;
            Prio13.Checked = false;
            Prio14.Checked = false;
            Prio15.Checked = false;
            Prio16.Checked = false;

            UpdatePrioFilter(sender, e);
        }

        private void Button_SelectAll_Click(object sender, EventArgs e)
        {
            Prio1.Checked = true;
            Prio2.Checked = true;
            Prio3.Checked = true;
            Prio4.Checked = true;
            Prio5.Checked = true;
            Prio6.Checked = true;
            Prio7.Checked = true;
            Prio8.Checked = true;
            Prio9.Checked = true;
            Prio10.Checked = true;
            Prio11.Checked = true;
            Prio12.Checked = true;
            Prio13.Checked = true;
            Prio14.Checked = true;
            Prio15.Checked = true;
            Prio16.Checked = true;

            UpdatePrioFilter(sender, e);
        }
    }
}
