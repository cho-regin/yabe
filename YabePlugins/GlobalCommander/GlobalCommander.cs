using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;
using Yabe;
using System.IO.BACnet;
using Newtonsoft.Json;
using System.IO;
using System.Reflection;

namespace GlobalCommander
{
    public partial class GlobalCommander : Form
    {
        private const int PATIENCE_INTERVAL = 7500;

        private readonly object _lockObject = new object();

        private YabeMainDialog _yabeFrm;

        private List<BacnetDeviceExport> _selectedDevices;
        private List<BacnetDeviceExport> _commonDevices;

        private List<BacnetPointExport> _selectedPoints;
        private List<BacnetPointExport> _allSelectedPoints;
        private List<BacnetPointExport> _pointListForDisplay;

        private BacnetPropertyExport _selectedProperty;
        private List<BacnetPropertyExport> _allSelectedProperties;
        private List<BacnetPropertyExport> _commonProperties;

        private Dictionary<Tuple<String, BacnetObjectId>, String> DevicesObjectsName { get { return _yabeFrm.DevicesObjectsName; } }
        private bool ObjectNamesChangedFlag { get { return _yabeFrm.objectNamesChangedFlag; } set { _yabeFrm.objectNamesChangedFlag = value; } }
        public IEnumerable<KeyValuePair<BacnetClient, YabeMainDialog.BacnetDeviceLine>> YabeDiscoveredDevices { get { return _yabeFrm.DiscoveredDevices; } }

        public GlobalCommander(YabeMainDialog yabeFrm)
        {
            Cursor.Current = Cursors.WaitCursor;
            this._yabeFrm = yabeFrm;

            Icon = yabeFrm.Icon; // gets Yabe Icon
            InitializeComponent();
            

        }

        private uint DetermineWritePriority()
        {
            uint writePriority = (uint)8;

            if (o1.Checked)
                writePriority = (uint)1;
            else if (o2.Checked)
                writePriority = (uint)2;
            else if (o3.Checked)
                writePriority = (uint)3;
            else if (o4.Checked)
                writePriority = (uint)4;
            else if (o5.Checked)
                writePriority = (uint)5;
            else if (o6.Checked)
                writePriority = (uint)6;
            else if (o7.Checked)
                writePriority = (uint)7;
            else if (o8.Checked)
                writePriority = (uint)8;
            else if (o9.Checked)
                writePriority = (uint)9;
            else if (o10.Checked)
                writePriority = (uint)10;
            else if (o11.Checked)
                writePriority = (uint)11;
            else if (o12.Checked)
                writePriority = (uint)12;
            else if (o13.Checked)
                writePriority = (uint)13;
            else if (o14.Checked)
                writePriority = (uint)14;
            else if (o15.Checked)
                writePriority = (uint)15;
            else if (o16.Checked)
                writePriority = (uint)16;

            return writePriority;
        }

        private bool SelectAllPointsAndProperties(bool selectObjInsFromAllDevices, bool doProgBar = false)
        {
            _selectedPoints = new List<BacnetPointExport>();
            if (PointList.SelectedItem != null)
            {
                progBar.Value = 0;
                Application.DoEvents();
                Cursor.Current = Cursors.WaitCursor;

                foreach (ListViewItemBetterString item in PointList.SelectedItems)
                {
                    _selectedPoints.Add((BacnetPointExport)item.Tag);
                }
            }
            else
            {
                MessageBox.Show("No point(s) selected.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            bool propertiesSelected = true;
            if (PropertyList.SelectedItem == null)
            {
                propertiesSelected = false;
                /*MessageBox.Show("No property selected.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;*/
            }

            _allSelectedProperties = new List<BacnetPropertyExport>();
            _allSelectedPoints = new List<BacnetPointExport>();
            
            if (propertiesSelected)
            {
                _selectedProperty = (BacnetPropertyExport)((ListViewItemBetterString)PropertyList.SelectedItem).Tag;
            }

            if (propertiesSelected && _selectedProperty == null)
            {
                MessageBox.Show("Property selected is not valid.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            if (propertiesSelected && _selectedProperty.Values.Count <= 0)
            {
                MessageBox.Show("Property selected does not have a value attached to it. Unable to read/command property value.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            int prog = 0;
            int progTotal = _selectedPoints.Count * _selectedDevices.Count + 2;

            if(doProgBar)
            {
                progBar.Value = (int)(100 * prog / progTotal);
                Application.DoEvents();
            }
            
            if(selectObjInsFromAllDevices)
            {
                // Used for the "Common Objects between Devices" setting
                foreach (BacnetDeviceExport device in _selectedDevices)
                {
                    foreach (BacnetPointExport point in _selectedPoints)
                    {
                        BacnetPointExport actualPoint = device.Points.Find(x => x.ObjectID.Equals(point.ObjectID));
                        if (actualPoint != null)
                        {
                            _allSelectedPoints.Add(actualPoint);
                            if (propertiesSelected)
                            {
                                BacnetPropertyExport match = actualPoint.Properties.Find(x => x.PropertyID == _selectedProperty.PropertyID);
                                if (match != null)
                                {
                                    _allSelectedProperties.Add(match);
                                }
                            }
                        }

                        if (doProgBar)
                        {
                            prog++;
                            progBar.Value = (int)(100 * prog / progTotal);
                            Application.DoEvents();
                        }
                    }
                }
            }
            else
            {
                // Used for the "Join All Objects" setting
                foreach (BacnetPointExport point in _selectedPoints)
                {
                    _allSelectedPoints.Add(point);
                    if (propertiesSelected)
                    {
                        BacnetPropertyExport match = point.Properties.Find(x => x.PropertyID == _selectedProperty.PropertyID);
                        if (match != null)
                        {
                            _allSelectedProperties.Add(match);
                        }
                    }
                    

                    if (doProgBar)
                    {
                        prog++;
                        progBar.Value = (int)(100 * prog / progTotal);
                        Application.DoEvents();
                    }
                }
            }
            
            return true;
        }

        private void GlobalCommander_Load(object sender, EventArgs e)
        {
            Cursor.Current = Cursors.WaitCursor;
            /*try
            {
                
            }
            catch
            {
                Cursor.Current = Cursors.Default;
                throw;
            }*/
        }

        private void GlobalCommander_Shown(object sender, EventArgs e)
        {
            Cursor.Current = Cursors.Default;
            cmdCommand.Enabled = false;
            cmdViewProps.Enabled = false;
            txtPointFilter.Enabled = false;
            txtPointFilter.Text = "";
            txtDeviceFilter.Enabled = false;
            txtDeviceFilter.Text = "";
        }

        private void cmdPopulateDevices_Click(object sender, EventArgs e)
        {

            Cursor.Current = Cursors.WaitCursor;
            StartPatienceTimer();
            progBar.Value = 0;
            Application.DoEvents();
            _selectedProperty = null;
            _allSelectedProperties = null;
            PropertyList.SelectedItem = null;
            PropertyList.Items.Clear();

            _selectedPoints = null;
            _allSelectedPoints = null;
            PointList.SelectedItem = null;
            PointList.Items.Clear();

            _commonDevices = PopulateDevicesWithNames(true);
            _commonDevices.Sort();

            _selectedDevices = null;
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

            cmdCommand.Enabled = false;
            cmdViewProps.Enabled = false;
            txtPointFilter.Enabled = false;
            txtPointFilter.Text = "";
            txtDeviceFilter.Enabled = true;
        }

        private string GetNameWithOnlyLastPartShown(string input)
        {
            //char[] delims = new char[] {'\'',':','.' };
            char[] delims = GetDelims();
            string output = string.Empty;
            bool containedDelim = false;

            for (int delimIndex = 0;delimIndex<delims.Length;delimIndex++)
            {
                if (input.Contains(delims[delimIndex]))
                {
                    containedDelim = true;
                    int hyphenA = input.LastIndexOf(delims[delimIndex]);
                    string[] parts = input.Split(delims[delimIndex]);

                    for (int i = 0; i < parts.Length; i++)
                    {
                        if (i == (parts.Length - 1))
                        {
                            output = output + parts[i];
                            break;
                        }
                        else
                        {
                            output = output + "*" + delims[delimIndex];
                        }
                    }
                    break;
                }
            }

            if(containedDelim)
            {
                return output;
            }
            else
            {
                return input;
            }
        }

        private bool CheckLastPartOfNameMatch(string a, string b)
        {
            if(a.Equals(b))
            {
                return true;
            }

            //char[] delims = new char[] { '\'', ':', '.' };
            char[] delims = GetDelims();
            string output = string.Empty;

            for (int delimIndex = 0; delimIndex < delims.Length; delimIndex++)
            {
                if (a.Contains(delims[delimIndex]) && b.Contains(delims[delimIndex]))
                {
                    int hyphenA = a.LastIndexOf(delims[delimIndex]);
                    int hyphenB = b.LastIndexOf(delims[delimIndex]);
                    if (a.Substring(hyphenA).Equals(b.Substring(hyphenB)))
                    {
                        return true;
                    }
                    else
                    {
                        break;
                    }
                }
            }

            return false;
        }

        private void ResetFromObjectsList()
        {
            _selectedProperty = null;
            _allSelectedProperties = null;
            PropertyList.SelectedItem = null;
            PropertyList.Items.Clear();

            _selectedPoints = null;
            _allSelectedPoints = null;
            PointList.SelectedItem = null;
            PointList.Items.Clear();

            cmdCommand.Enabled = false;
            cmdViewProps.Enabled = false;
            txtPointFilter.Enabled = false;
            txtPointFilter.Text = "";
        }

        private void cmdPopulatePoints_Click(object sender, EventArgs e)
        {
            ResetFromObjectsList();

            _selectedDevices = new List<BacnetDeviceExport>();

            if (DeviceList.SelectedItem!=null)
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
                    if(radComObj.Checked)
                    {
                        // Display only the points that are common to all controllers.
                        // To determine if the point name is the same, we only compare the part of the name after the LAST delimeter specified.
                        foreach (BacnetPointExport point in _selectedDevices[0].Points)
                        {
                            _pointListForDisplay.Add(point);
                        }

                        for (int i = 0; i < _pointListForDisplay.Count; i++)
                        {
                            foreach (BacnetDeviceExport device in _selectedDevices)
                            {
                                BacnetPointExport match = device.Points.Find(x => x.ObjectID.Equals(_pointListForDisplay[i].ObjectID) && CheckLastPartOfNameMatch(x.ObjectName, _pointListForDisplay[i].ObjectName));
                                if (match == null)
                                {
                                    _pointListForDisplay.RemoveAt(i--);
                                    break;
                                }
                            }
                        }

                    }
                    else if(radJoinObj.Checked)
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

            PointList.Items.Clear();
            foreach (BacnetPointExport point in _pointListForDisplay)
            {
                ListViewItemBetterString item = new ListViewItemBetterString();
                if(_selectedDevices.Count>1 && radComObj.Checked)
                { 
                    item.Text = GetNameWithOnlyLastPartShown(point.ObjectName) + " [" + point.ObjectID.ToString() + "] ";
                }
                else
                {
                    item.Text = point.Name;
                }
                item.Name = item.Text;
                item.Tag = point;
                PointList.Items.Add(item);
            }
            progBar.Value = 100;
            ResetPatience();

            Application.DoEvents();
            Cursor.Current = Cursors.Default;

            cmdCommand.Enabled = false;
            cmdViewProps.Enabled = false;
            txtPointFilter.Enabled = true;
        }

        private void cmdPopulateProperties_Click(object sender, EventArgs e)
        {
            _selectedProperty = null;
            _allSelectedProperties = null;
            PropertyList.SelectedItem = null;
            PropertyList.Items.Clear();

            if (!SelectAllPointsAndProperties(radComObj.Checked, false))
            {
                progBar.Value = 0;
                Application.DoEvents();
                Cursor.Current = Cursors.Default;
                MessageBox.Show("Failed to retrieve a comprehensive list of selected points/properties.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            Cursor.Current = Cursors.WaitCursor;
            StartPatienceTimer();

            bool result = PopulatePropertyRefsForPoints(_allSelectedPoints, true);

            _commonProperties = new List<BacnetPropertyExport>();

            if (_selectedPoints != null && _selectedPoints.Count>0 && result)
            {
                if (result)
                {
                    _commonProperties = new List<BacnetPropertyExport>();
                    foreach (BacnetPropertyExport property in _selectedPoints[0].Properties)
                    {
                        _commonProperties.Add(property);
                    }

                    if (_selectedPoints.Count > 1)
                    {
                        _commonProperties.RemoveAll(x =>
                                !_selectedPoints.All(y =>
                                        y.Properties.Exists(z =>
                                                z.PropertyID==x.PropertyID && x.Values.Count == z.Values.Count && (x.Values.Count == 0 ? true : (x.Values[0].Tag == z.Values[0].Tag)))));

                        /*
                        // This seems to have threading issues with large numbers of points 
                        for (int i = 1; i < _commonProperties.Count; i++)
                        {
                            foreach (BacnetPointExport point in _selectedPoints)
                            {
                                BacnetPropertyExport match = point.Properties.Find(x => x.PropertyID == _commonProperties[i].PropertyID && x.Values.Count == _commonProperties[i].Values.Count && (x.Values.Count == 0 ? true : (x.Values[0].Tag == _commonProperties[i].Values[0].Tag)));
                                if (match == null)
                                {
                                    _commonProperties.RemoveAt(i--);
                                }
                            }
                        }
                        */
                    }
                }
                else
                {
                    progBar.Value = 0;
                    ResetPatience();
                    Application.DoEvents();
                    Cursor.Current = Cursors.Default;
                    MessageBox.Show("Failed to retrieve a comprehensive list of selected points/properties.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }
            else
            {
                progBar.Value = 0;
                ResetPatience();
                Application.DoEvents();
                Cursor.Current = Cursors.Default;
                MessageBox.Show("No points selected.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            PropertyList.Items.Clear();
            foreach (BacnetPropertyExport property in _commonProperties)
            {
                ListViewItemBetterString item = new ListViewItemBetterString();
                if(property.Values.Count<=0)
                {
                    item.Text = String.Format("{0} [{1}]", property.Name, "NO VALUE PRESENT");
                }
                else
                {
                    item.Text = String.Format("{0} [{1}]", property.Name, property.Values[0].Tag.ToString());
                }
                item.Name = item.Text;
                item.Tag = property;
                PropertyList.Items.Add(item);
            }

            progBar.Value = 100;
            ResetPatience();
            Application.DoEvents();
            Cursor.Current = Cursors.Default;

            cmdCommand.Enabled = true;
            cmdViewProps.Enabled = true;
        }

        private void cmdCommand_Click(object sender, EventArgs e)
        {
            if(_selectedPoints==null || _selectedPoints.Count<=0)
            {
                MessageBox.Show("No points found to command.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

            uint writePriority = DetermineWritePriority();

            if (PropertyList.SelectedItem==null)
            {
                MessageBox.Show("No property selected.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _selectedProperty = (BacnetPropertyExport)((ListViewItemBetterString)PropertyList.SelectedItem).Tag;
            if(_selectedProperty==null)
            {
                MessageBox.Show("Property selected is not valid.","Error",MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (_selectedProperty.Values.Count <= 0)
            {
                MessageBox.Show("Property selected does not have a value attached to it for some reason. Unable to command.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (_selectedProperty.Values.Count>1)
            {
                MessageBox.Show("Unfortunately, globally commanding value arrays is not supported - however you can command arrays from the \"View Properties in Scope\" window.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            Cursor.Current = Cursors.WaitCursor;
            progBar.Value = 0;
            Application.DoEvents();
            int prog = 0;
            int progTotal = _selectedPoints.Count * _selectedDevices.Count + 3;

            BacnetValue b_value = _selectedProperty.Values[0];
            BacnetApplicationTags dataType = b_value.Tag;

            string enteredValue = txtCmdVal.Text.Trim();
            BacnetValue[] new_b_vals = new BacnetValue[1];

            if (dataType != BacnetApplicationTags.BACNET_APPLICATION_TAG_NULL)
            {
                BacnetValue new_b_val = ConvertVal(enteredValue, dataType);
                new_b_vals[0] = new_b_val;
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
                        o = Convert.ChangeType(enteredValue, Type.GetType("System." + typename));
                        break;
                    }
                    catch { }
                }

                if (o == null)
                    new_b_vals[0] = new BacnetValue(enteredValue);
                else
                    new_b_vals[0] = new BacnetValue(o);
            }

            prog++;
            progBar.Value = (int)(100 * prog / progTotal);
            Application.DoEvents();

            if (!SelectAllPointsAndProperties(radComObj.Checked, false))
            {
                progBar.Value = 0;
                Application.DoEvents();
                Cursor.Current = Cursors.Default;
                MessageBox.Show("Failed to retrieve a comprehensive list of selected properties to be commanded.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if(_allSelectedProperties==null || _allSelectedProperties.Count<=0)
            {
                MessageBox.Show("Points to command could not be resolved.","Error",MessageBoxButtons.OK, MessageBoxIcon.Warning);
                progBar.Value = 0;
                Application.DoEvents();
                return;
            }

            if(_allSelectedProperties.Count > 10)
            {
                int maxMessageBoxLines = 40;

                StringBuilder question = new StringBuilder();
                question.AppendLine("Are you sure you want to globally command all objects in the scope of the selection?");
                question.AppendLine();
                question.AppendLine("The Following objects will be commanded:");

                int count = 0;

                foreach (BacnetPropertyExport propertyToCommand in _allSelectedProperties)
                {
                    if(count < maxMessageBoxLines)
                    {
                        question.AppendLine(String.Format("Device \"{0}\"; object \"{1}\"; property \"{2}\".", propertyToCommand.ParentPoint.ParentDevice.Name, propertyToCommand.ParentPoint.Name, _selectedProperty.Name));
                        count++;
                    }
                    else
                    {
                        question.AppendLine();
                        question.AppendLine(String.Format("and {0} more...", _allSelectedProperties.Count - count));
                        break;
                    }
                    
                }
                string finalQuestion = question.ToString();

                DialogResult continueResult = MessageBox.Show(finalQuestion, "Confirm Global Command", MessageBoxButtons.YesNo, MessageBoxIcon.Information);
                if (continueResult != DialogResult.Yes)
                {
                    return;
                }
            }

            prog++;
            progBar.Value = (int)(100 * prog / progTotal);
            Application.DoEvents();

            //write
            BacnetClient comm;
            BacnetAddress adr;
            BacnetObjectId object_id;

            foreach (BacnetPropertyExport propertyToCommand in _allSelectedProperties)
            {
                comm = propertyToCommand.ParentPoint.ParentDevice.Comm;
                adr = propertyToCommand.ParentPoint.ParentDevice.DeviceAddress;
                object_id = propertyToCommand.ParentPoint.ObjectID;

                try
                {
                    comm.WritePriority = writePriority;
                    if (!comm.WritePropertyRequest(adr, object_id, propertyToCommand.PropertyID, new_b_vals))
                    {
                        MessageBox.Show(String.Format("Couldn't write property {0} to device {1}, object {2}. Commanding will not continue", _selectedProperty.PropertyID.ToString(), propertyToCommand.ParentPoint.ParentDevice.Name, propertyToCommand.ParentPoint.Name), "Communication Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        progBar.Value = 0;
                        Application.DoEvents();
                        Cursor.Current = Cursors.Default;
                        return;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(String.Format("Error writing property {0} to device {1}, object {2}. Exception: {3}. Commanding will not continue", _selectedProperty.PropertyID.ToString(), propertyToCommand.ParentPoint.ParentDevice.Name, propertyToCommand.ParentPoint.Name, ex.GetType().ToString() + " - " + ex.Message), "Communication Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    progBar.Value = 0;
                    Application.DoEvents();
                    Cursor.Current = Cursors.Default;
                    return;
                }

                prog++;
                progBar.Value = (int)(100 * prog / progTotal);
                Application.DoEvents();
            }

            progBar.Value = 100;
            Application.DoEvents();
            Cursor.Current = Cursors.Default;
        }

        private void cmdViewProps_Click(object sender, EventArgs e)
        {
            Cursor.Current = Cursors.WaitCursor;
            progBar.Value = 0;
            Application.DoEvents();

            // Once, to get the right points:
            if (!SelectAllPointsAndProperties(radComObj.Checked, false))
            {
                progBar.Value = 0;
                Application.DoEvents();
                Cursor.Current = Cursors.Default;
                MessageBox.Show("Failed to retrieve a comprehensive list of selected points/properties.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            bool result = PopulatePropertyRefsForPoints(_allSelectedPoints, true);

            // Repeat, to get the updated point values (properties):
            if (!SelectAllPointsAndProperties(radComObj.Checked, false))
            {
                progBar.Value = 0;
                Application.DoEvents();
                Cursor.Current = Cursors.Default;
                MessageBox.Show("Failed to retrieve a comprehensive list of selected points/properties.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (_allSelectedProperties == null || _allSelectedProperties.Count <= 0)
            {
                progBar.Value = 0;
                Application.DoEvents();
                Cursor.Current = Cursors.Default;
                MessageBox.Show("Points to view could not be resolved.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                PropertyValueListForm viewPropsFrm = new PropertyValueListForm(_allSelectedProperties, _selectedProperty.Name);
                viewPropsFrm.Show();
            }
            catch (Exception ex)
            {
                Cursor.Current = Cursors.Default;
                progBar.Value = 0;
                Application.DoEvents();
                MessageBox.Show(String.Format("Failed to load property value list. {0} - {1}.", ex.GetType().ToString(), ex.Message), "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            progBar.Value = 100;
            Application.DoEvents();
            Cursor.Current = Cursors.Default;
        }

        private void PointList_SelectedIndexChanged(object sender, EventArgs e)
        {
            _selectedProperty = null;
            _allSelectedProperties = null;
            PropertyList.SelectedItem = null;
            PropertyList.Items.Clear();

            cmdCommand.Enabled = false;
            cmdViewProps.Enabled = false;
        }

        private void DeviceList_SelectedIndexChanged(object sender, EventArgs e)
        {
            _selectedProperty = null;
            _allSelectedProperties = null;
            PropertyList.SelectedItem = null;
            PropertyList.Items.Clear();

            _selectedPoints = null;
            _allSelectedPoints = null;
            PointList.SelectedItem = null;
            PointList.Items.Clear();

            cmdCommand.Enabled = false;
            cmdViewProps.Enabled = false;
            txtPointFilter.Enabled = false;
            txtPointFilter.Text = "";
        }

        private static BacnetValue ConvertVal(string value, BacnetApplicationTags type)
        {
            if(value==null)
            {
                return new BacnetValue(type, null);
            }

            try
            {
                switch (type)
                {
                    case BacnetApplicationTags.BACNET_APPLICATION_TAG_NULL:
                        return new BacnetValue(value);
                    case BacnetApplicationTags.BACNET_APPLICATION_TAG_BOOLEAN:
                        int vint;
                        if (int.TryParse(value, out vint)) return new BacnetValue(type, (vint != 0)); // allows 0 and 1 to be used
                        return new BacnetValue(type, bool.Parse(value));
                    case BacnetApplicationTags.BACNET_APPLICATION_TAG_UNSIGNED_INT:
                        return new BacnetValue(type, uint.Parse(value));
                    case BacnetApplicationTags.BACNET_APPLICATION_TAG_SIGNED_INT:
                        return new BacnetValue(type, int.Parse(value));
                    case BacnetApplicationTags.BACNET_APPLICATION_TAG_REAL:
                        return new BacnetValue(type, float.Parse(value, System.Globalization.CultureInfo.InvariantCulture));
                    case BacnetApplicationTags.BACNET_APPLICATION_TAG_DOUBLE:
                        return new BacnetValue(type, double.Parse(value, System.Globalization.CultureInfo.InvariantCulture));
                    case BacnetApplicationTags.BACNET_APPLICATION_TAG_OCTET_STRING:
                        try
                        {
                            return new BacnetValue(type, Convert.FromBase64String(value));
                        }
                        catch
                        {
                            return new BacnetValue(type, value);
                        }
                    case BacnetApplicationTags.BACNET_APPLICATION_TAG_CONTEXT_SPECIFIC_DECODED:
                        try
                        {
                            return new BacnetValue(type, Convert.FromBase64String(value));
                        }
                        catch
                        {
                            return new BacnetValue(type, value);
                        }
                    case BacnetApplicationTags.BACNET_APPLICATION_TAG_CHARACTER_STRING:
                        return new BacnetValue(type, value);
                    case BacnetApplicationTags.BACNET_APPLICATION_TAG_BIT_STRING:
                        return new BacnetValue(type, BacnetBitString.Parse(value));
                    case BacnetApplicationTags.BACNET_APPLICATION_TAG_ENUMERATED:
                        return new BacnetValue(type, uint.Parse(value));
                    case BacnetApplicationTags.BACNET_APPLICATION_TAG_DATE:
                        return new BacnetValue(type, DateTime.Parse(value));
                    case BacnetApplicationTags.BACNET_APPLICATION_TAG_TIME:
                        return new BacnetValue(type, DateTime.Parse(value));
                    case BacnetApplicationTags.BACNET_APPLICATION_TAG_OBJECT_ID:
                        return new BacnetValue(type, BacnetObjectId.Parse(value));
                    case BacnetApplicationTags.BACNET_APPLICATION_TAG_READ_ACCESS_SPECIFICATION:
                        return new BacnetValue(type, BacnetReadAccessSpecification.Parse(value));
                    default:
                        return new BacnetValue(type, null);
                }
            }
            catch
            {
                return new BacnetValue(type, null);
            }
        }

        public List<BacnetDeviceExport> PopulateDevicesWithNames(bool commandProgBar = false)
        {
            int progTotal = YabeDiscoveredDevices.Count() + 1;
            int prog = 0;
            List<BacnetDeviceExport> deviceList = new List<BacnetDeviceExport>();
            foreach (KeyValuePair<BacnetClient, YabeMainDialog.BacnetDeviceLine> transport in YabeDiscoveredDevices)
            {
                foreach (KeyValuePair<BacnetAddress, uint> address in transport.Value.Devices)
                {
                    BacnetAddress deviceAddress = address.Key;
                    uint deviceID = address.Value;
                    BacnetClient comm = transport.Key;
                    BacnetDeviceExport device = new BacnetDeviceExport(comm, this, deviceID, deviceAddress);

                    bool Prop_Object_NameOK = false;
                    BacnetObjectId deviceObjectID = new BacnetObjectId(BacnetObjectTypes.OBJECT_DEVICE, deviceID);
                    string identifier = null;

                    lock (DevicesObjectsName)
                    {
                        Prop_Object_NameOK = DevicesObjectsName.TryGetValue(new Tuple<String, BacnetObjectId>(deviceAddress.FullHashString(), deviceObjectID), out identifier);
                    }

                    if (Prop_Object_NameOK)
                    {
                        identifier = identifier + " [" + deviceObjectID.Instance.ToString() + "] ";
                    }
                    else
                    {
                        try
                        {
                            IList<BacnetValue> values;
                            if (comm.ReadPropertyRequest(deviceAddress, deviceObjectID, BacnetPropertyIds.PROP_OBJECT_NAME, out values))
                            {
                                identifier = values[0].ToString();
                                lock (DevicesObjectsName)
                                {
                                    Tuple<String, BacnetObjectId> t = new Tuple<String, BacnetObjectId>(deviceAddress.FullHashString(), deviceObjectID);
                                    DevicesObjectsName.Remove(t);
                                    DevicesObjectsName.Add(t, identifier);
                                    ObjectNamesChangedFlag = true;
                                }
                                identifier = identifier + " [" + deviceObjectID.Instance.ToString() + "] ";
                            }
                        }
                        catch { }
                    }

                    if (identifier != null)
                    {
                        device.Name = identifier;
                    }

                    if (deviceList.Find(item => item.DeviceID == deviceID) == null)
                    {
                        deviceList.Add(device);
                    }

                }
                if(commandProgBar)
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

        public bool PopulatePointsForDevice(BacnetDeviceExport device)
        {
            BacnetClient comm = device.Comm;
            BacnetAddress adr = device.DeviceAddress;
            uint device_id = device.DeviceID;

            //int old_timeout = comm.Timeout;
            IList<BacnetValue> value_list = null;
            try
            {
                if (!comm.ReadPropertyRequest(adr, new BacnetObjectId(BacnetObjectTypes.OBJECT_DEVICE, device_id), BacnetPropertyIds.PROP_OBJECT_LIST, out value_list))
                {
                    Trace.TraceWarning(String.Format("Didn't get response from 'Object List' for device {0}.", device.Name));
                    value_list = null;
                }
            }
            catch (Exception)
            {
                Trace.TraceWarning(String.Format("Got exception from 'Object List' for device {0}", device.Name));
                value_list = null;
            }


            //fetch list one-by-one
            if (value_list == null)
            {
                IList<BacnetValue> temp_value_list;
                try
                {
                    //fetch object list count
                    if (!comm.ReadPropertyRequest(adr, new BacnetObjectId(BacnetObjectTypes.OBJECT_DEVICE, device_id), BacnetPropertyIds.PROP_OBJECT_LIST, out temp_value_list, 0, 0))
                    {
                        ResetPatience();
                        MessageBox.Show(this, String.Format("Couldn't fetch objects for device {0}.", device.Name), "Communication Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    ResetPatience();
                    MessageBox.Show(this, String.Format("Failed to read object list of device {0}: {1}", device.Name, ex.Message), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }

                if (temp_value_list != null && temp_value_list.Count == 1 && temp_value_list[0].Value is ulong)
                {
                    uint list_count = (uint)(ulong)temp_value_list[0].Value;
                    value_list = temp_value_list;
                    value_list.Clear();
                    try
                    {
                        for (int i = 1; i <= list_count; i++)
                        {
                            temp_value_list = null;
                            if (comm.ReadPropertyRequest(adr, new BacnetObjectId(BacnetObjectTypes.OBJECT_DEVICE, device_id), BacnetPropertyIds.PROP_OBJECT_LIST, out temp_value_list, 0, (uint)i))
                            {
                                //add to actual list for returning
                                foreach (BacnetValue value in temp_value_list)
                                {
                                    value_list.Add(value);
                                }
                            }
                            else
                            {
                                ResetPatience();
                                MessageBox.Show(this, String.Format("Couldn't fetch object list index {1} for device {0}.", device.Name, i), "Communication Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                return false;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        ResetPatience();
                        MessageBox.Show(this, String.Format("Failed to read object list for device {0}: {1}", device.Name, ex.Message), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return false;
                    }
                }
                else
                {
                    ResetPatience();
                    MessageBox.Show(this, String.Format("Couldn't read object list for device {0}.", device.Name), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }
            }

            List<BacnetObjectId> objectList = SortBacnetObjects(value_list);
            device.Points.Clear();
            foreach (BacnetObjectId bobj_id in objectList)
            {
                if(true) // if (bobj_id.type != BacnetObjectTypes.OBJECT_DEVICE || bobj_id.instance != device.DeviceID)
                {

                    BacnetPointExport point = new BacnetPointExport(device, bobj_id);

                    // If the Device name not set, try to update it
                    bool Prop_Object_NameOK = false;
                    string identifier = null;
                    string objectName = null;

                    lock (DevicesObjectsName)
                    {
                        Prop_Object_NameOK = DevicesObjectsName.TryGetValue(new Tuple<String, BacnetObjectId>(adr.FullHashString(), bobj_id), out objectName);
                    }
                    if (Prop_Object_NameOK)
                    {
                        identifier = objectName + " [" + bobj_id.ToString() + "] ";
                    }
                    else
                    {
                        try
                        {
                            IList<BacnetValue> values;
                            if (comm.ReadPropertyRequest(adr, bobj_id, BacnetPropertyIds.PROP_OBJECT_NAME, out values))
                            {
                                objectName = values[0].ToString();
                                lock (DevicesObjectsName)
                                {
                                    Tuple<String, BacnetObjectId> t = new Tuple<String, BacnetObjectId>(adr.FullHashString(), bobj_id);
                                    //DevicesObjectsName.Remove(t);
                                    DevicesObjectsName[t]=objectName;
                                    ObjectNamesChangedFlag = true;
                                }
                                identifier = objectName + " [" + bobj_id.ToString() + "] ";
                            }
                        }
                        catch { }
                    }

                    if (identifier != null)
                    {
                        point.Name = identifier;
                        point.ObjectName = objectName;
                    }

                    if (device.Points.Find(item => item.ObjectID.Equals(bobj_id)) == null)
                    {
                        device.Points.Add(point);
                    }
                }
            }

            return true;
        }

        public bool PopulatePropertyRefsForPoints(List<BacnetPointExport> points, bool commandProgBar = false)
        {
            int progTotal = points.Count + 2;
            int prog = 0;
            bool result = true;
            for (int i = 0; i < points.Count; i++)
            {
                bool particularResult = PopulatePropertiesForPoint(points[i]);
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

        public bool PopulatePropertiesForPoint(BacnetPointExport point)
        {
            BacnetClient comm = point.ParentDevice.Comm;
            BacnetAddress adr = point.ParentDevice.DeviceAddress;
            uint device_id = point.ParentDevice.DeviceID;

            BacnetObjectId object_id = point.ObjectID;
            BacnetPropertyReference[] properties = new BacnetPropertyReference[] { new BacnetPropertyReference((uint)BacnetPropertyIds.PROP_ALL, System.IO.BACnet.Serialize.ASN1.BACNET_ARRAY_ALL) };
            IList<BacnetReadAccessResult> multi_value_list;
            try
            {
                //fetch properties. This might not be supported (ReadMultiple) or the response might be too long.
                if (!comm.ReadPropertyMultipleRequest(adr, object_id, properties, out multi_value_list))
                {
                    Trace.TraceWarning(String.Format("Couldn't perform ReadPropertyMultiple for property list on device {0}, object {1} ... Trying ReadProperty instead", point.ParentDevice.Name, point.Name));
                    if (!ReadAllPropertiesBySingle(comm, adr, object_id, out multi_value_list))
                    {
                        ResetPatience();
                        MessageBox.Show(this, String.Format("Couldn't get property list using ReadProperty loop (single properties at a time) of device {0}, object {1} ... Trying ReadProperty instead", point.ParentDevice.Name, point.Name), "Communication Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return false;
                    }
                }
            }
            catch (Exception)
            {
                Trace.TraceWarning(String.Format("Couldn't perform ReadPropertyMultiple for property list on device {0}, object {1} ... Trying ReadProperty instead", point.ParentDevice.Name, point.Name));
                try
                {
                    //fetch properties with single calls
                    if (!ReadAllPropertiesBySingle(comm, adr, object_id, out multi_value_list))
                    {
                        ResetPatience();
                        MessageBox.Show(this, String.Format("Couldn't get property list using ReadProperty loop (single properties at a time) of device {0}, object {1} ... Trying ReadProperty instead", point.ParentDevice.Name, point.Name), "Communication Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    ResetPatience();
                    MessageBox.Show(this, String.Format("Error reading properties one-at-a-time from device {1}, object {2}: {0}", ex.Message, point.ParentDevice.Name, point.Name), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }
            }

            point.Properties.Clear();
            foreach (BacnetPropertyValue p_value in multi_value_list[0].values)
            {
                List<BacnetValue> b_values = null;
                BacnetPropertyIds propertyID = (BacnetPropertyIds)p_value.property.propertyIdentifier;
                BacnetPropertyExport property = new BacnetPropertyExport(point, propertyID);

                property.Name = _yabeFrm.GetNiceName(propertyID, true);

                if (p_value.value != null)
                {
                    b_values = p_value.value.ToList<BacnetValue>();
                    property.Values.AddRange(b_values);
                }

                if (point.Properties.Find(item => item.PropertyID.Equals(propertyID)) == null)
                {
                    point.Properties.Add(property);
                }
            }
            return true;
        }

        // ----- The below methods were exposed and stolen from Yabe -------
        private List<BacnetObjectId> SortBacnetObjects(IList<BacnetValue> value_list)
        {
            return _yabeFrm.SortBacnetObjects(value_list);
        }

        private bool ReadAllPropertiesBySingle(BacnetClient comm, BacnetAddress adr, BacnetObjectId object_id, out IList<BacnetReadAccessResult> multi_value_list)
        {
            return _yabeFrm.ReadAllPropertiesBySingle(comm, adr, object_id, out multi_value_list);
        }
        // ------------------------------------------------------------------

        public class BacnetDeviceExport : IEquatable<BacnetDeviceExport>, IComparable<BacnetDeviceExport>
        {
            public uint DeviceID { get; }
            private string _name;
            public string Name { get { return _name; } set { _nameIsSet = true; _name = value; } }
            private bool _nameIsSet;
            public bool NameIsSet { get { return _nameIsSet; } }
            public BacnetAddress DeviceAddress { get; }
            public BacnetClient Comm { get; }
            public GlobalCommander ParentWindow { get; }
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

            public BacnetDeviceExport(BacnetClient comm, GlobalCommander parentWindow, uint deviceID, BacnetAddress deviceAddress)
            {
                Comm = comm;
                ParentWindow = parentWindow;
                DeviceID = deviceID;
                BacnetObjectId deviceObjectID = new BacnetObjectId(BacnetObjectTypes.OBJECT_DEVICE, deviceID);
                _name = deviceObjectID.ToString();
                _nameIsSet = false;
                DeviceAddress = deviceAddress;
                Points = new List<BacnetPointExport>();
            }
        }

        public class BacnetPointExport : IEquatable<BacnetPointExport>, IComparable<BacnetPointExport>
        {
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
                    if(arrayObj[i]!=null)
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

            public string ParentDeviceName { get { return ParentPoint.ParentDevice.Name; } }
            public string ParentPointName { get { return ParentPoint.Name; } }
            public BacnetPropertyIds PropertyID { get; }
            public BacnetPointExport ParentPoint { get; }
            public uint ArrayIndex { get; }
            private string _name;
            public string Name { get { return _name; } set { _nameIsSet = true; _name = value; } }
            private bool _nameIsSet;
            public bool NameIsSet { get { return _nameIsSet; } }
            public bool IsPrintableValue(BacnetValue bv)
            {
                object t = bv.Value;

                if (t == null)
                    return true;
                else if (t is string)
                    return true;
                else if (t is long || t is int || t is short || t is sbyte)
                    return true;
                else if (t is ulong || t is uint || t is ushort || t is byte)
                    return true;
                else if (t is bool)
                    return true;
                else if (t is float || t is double)
                    return true;
                else if (t is BacnetBitString)
                    return true;
                else if (t is BacnetObjectId)
                    return true;
                else
                    return false;
                    
            }
            public bool ExportAsJSON
            {
                get
                {
                    foreach(BacnetValue bv in Values)
                    {
                        if(!IsPrintableValue(bv))
                        {
                            return true;
                        }
                    }

                    return false;
                }
            }
            public List<BacnetValue> Values { get; private set; }
            public object ValueObject
            {
                get
                {
                    if (Values.Count > 1)
                    {
                        object[] arr = new object[Values.Count];
                        for (int j = 0; j < arr.Length; j++)
                            arr[j] = Values[j].Value;
                        return arr;
                    }
                    else if (Values.Count == 1)
                    {
                        return Values[0].Value;
                    }
                    else
                    {
                        return null;
                    }
                }
            }
            public string ValueObjectString
            {
                get
                {
                    object ValueObjectCopy = ValueObject;
                    if (ValueObjectCopy != null)
                    {
                        if(ExportAsJSON)
                        {
                            return "JSON (NOT EDITABLE): " + JsonConvert.SerializeObject(ValueObjectCopy);
                        }
                        else
                        {
                            if (Values.Count > 1)
                            {
                                return ArrayToString((object[])ValueObjectCopy);
                            }
                            else
                            {
                                return ValueObjectCopy.ToString();

                            }
                        }
                        
                    }
                    else
                    {
                        return "";
                    }
                }
                set
                {
                    BacnetClient comm = ParentPoint.ParentDevice.Comm;
                    BacnetAddress adr = ParentPoint.ParentDevice.DeviceAddress;
                    BacnetObjectId object_id = ParentPoint.ObjectID;

                    if (ExportAsJSON)
                    {
                        MessageBox.Show(String.Format("Unfortunately, JSON is not editable at this point in time."), "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                    else
                    {
                        uint writePriority = (uint)8;
                        writePriority = ParentPoint.ParentDevice.ParentWindow.DetermineWritePriority();

                        if (Values.Count < 1)
                        {
                            MessageBox.Show(String.Format("Couldn't write property {0} of device {1}, object {2}.", PropertyID.ToString(), ParentPoint.ParentDevice.Name, ParentPoint.Name), "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            Cursor.Current = Cursors.Default;
                            return;
                        }
                        Cursor.Current = Cursors.WaitCursor;

                        string enteredValue = null;
                        if (value != null)
                        {
                            enteredValue = value.Trim();
                        }

                        string[] enteredValues = null;
                        string[] existingStringValues = null;

                        bool valueTypesDeterminedByJSON = false;
                        int dataLength = 0;

                        BacnetValue[] new_b_vals = null;

                        if (Values.Count > 1)
                        {
                            //if (PropertyID == BacnetPropertyIds.PROP_PRIORITY_ARRAY)
                            //{
                            if (!(enteredValue.StartsWith("{") && enteredValue.EndsWith("}")))
                            {
                                MessageBox.Show(String.Format("Couldn't write property {0} of device {1}, object {2}. The array format was not correct. Please enter the array as follows: {x1,x2,x3,x4,...}", PropertyID.ToString(), ParentPoint.ParentDevice.Name, ParentPoint.Name), "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                Cursor.Current = Cursors.Default;
                                return;
                            }
                            string enteredValueWithoutBrackets = enteredValue.Replace("{", "").Replace("}", "");
                            enteredValues = enteredValueWithoutBrackets.Split(',');
                            dataLength = enteredValues.Length;
                            if (dataLength != Values.Count)
                            {
                                MessageBox.Show(String.Format("Couldn't write property {0} of device {1}, object {2}. The array format was not correct (incorrect number of elements).", PropertyID.ToString(), ParentPoint.ParentDevice.Name, ParentPoint.Name), "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                Cursor.Current = Cursors.Default;
                                return;
                            }

                            for (int i = 0; i < dataLength; i++)
                            {
                                enteredValues[i] = enteredValues[i].Trim();
                            }

                            string existingPriorityArray = ArrayToString((object[])ValueObject);
                            string existingPriorityArrayWithoutBrackets = existingPriorityArray.Replace("{", "").Replace("}", "");
                            existingStringValues = existingPriorityArrayWithoutBrackets.Split(',');
                            for (int i = 0; i < existingStringValues.Length; i++)
                            {
                                existingStringValues[i] = existingStringValues[i].Trim();
                            }
                            //}
                            /*else
                            {
                                // BROKEN and doesn't work, how naive of me...
                                try
                                {
                                    List<BacnetValue> enteredParsed = JsonConvert.DeserializeObject<List<BacnetValue>>(enteredValue);
                                    valueTypesDeterminedByJSON = true;

                                    if (Values.Count == 1)
                                    {
                                        new_b_vals = new BacnetValue[1];
                                        new_b_vals[0] = new BacnetValue();

                                        new_b_vals[0].Value = enteredParsed.ToArray();
                                        // Never gonna work...
                                    }
                                    else
                                    {
                                        new_b_vals = enteredParsed.ToArray();
                                    }

                                    dataLength = enteredParsed.Count;
                                
                                }
                                catch(Exception ex)
                                {
                                    MessageBox.Show(String.Format("Couldn't write property {0} of device {1}, object {2}. The JSON format was not correct: {3} - {4}.", PropertyID.ToString(), ParentPoint.ParentDevice.Name, ParentPoint.Name, ex.GetType().Name, ex.Message), "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                    return;
                                }
                            }*/

                        }
                        else
                        {
                            enteredValues = new string[1] { enteredValue };
                            dataLength = 1;
                            // Not needed for anything other than the priority array really, just to avoid sending 16 commands every time.
                            existingStringValues = null;
                        }



                        if (!valueTypesDeterminedByJSON)
                        {
                            // ------------------------------------------------------------------
                            // Below is only relevant if we are NOT entering the value as JSON.
                            // ------------------------------------------------------------------

                            new_b_vals = new BacnetValue[dataLength];
                            for (int i = 0; i < dataLength; i++)
                            {
                                BacnetValue b_value = Values[i];
                                BacnetApplicationTags dataType = b_value.Tag;

                                if (string.IsNullOrWhiteSpace(enteredValues[i]) || enteredValues[i].Equals("null", StringComparison.OrdinalIgnoreCase))
                                {
                                    new_b_vals[i] = new BacnetValue(BacnetApplicationTags.BACNET_APPLICATION_TAG_NULL, null);
                                }
                                else
                                {
                                    bool gotDatatype = false;

                                    if (dataType != BacnetApplicationTags.BACNET_APPLICATION_TAG_NULL)
                                    {
                                        gotDatatype = true;
                                        new_b_vals[i] = ConvertVal(enteredValues[i], dataType);
                                    }

                                    if (!gotDatatype)
                                    {
                                        try
                                        {
                                            if (!comm.ReadPropertyRequest(adr, object_id, BacnetPropertyIds.PROP_PRESENT_VALUE, out IList<BacnetValue> presentValueList))
                                            {
                                                Trace.TraceError(String.Format("Couldn't read the data type of the present value of device {0}, object {1}", ParentPoint.ParentDevice.Name, ParentPoint.Name));
                                            }
                                            else
                                            {
                                                dataType = presentValueList[0].Tag;
                                                if (dataType != BacnetApplicationTags.BACNET_APPLICATION_TAG_NULL)
                                                {
                                                    gotDatatype = true;
                                                    new_b_vals[i] = ConvertVal(enteredValues[i], dataType);
                                                }
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            Trace.TraceError(String.Format("Couldn't read the data type of the present value of device {0}, object {1}. Exception: {2}.", ParentPoint.ParentDevice.Name, ParentPoint.Name, ex.GetType().ToString() + " - " + ex.Message));
                                            gotDatatype = false;
                                        }
                                    }

                                    if (!gotDatatype)
                                    {
                                        object o = null;
                                        TypeConverter t = new TypeConverter();
                                        // try to convert to the simplest type
                                        String[] typelist = { "Boolean", "UInt32", "Int32", "Single", "Double" };

                                        foreach (String typename in typelist)
                                        {
                                            try
                                            {
                                                o = Convert.ChangeType(enteredValues[i], Type.GetType("System." + typename));
                                                gotDatatype = true;
                                                break;
                                            }
                                            catch { }
                                        }

                                        if (o == null)
                                            new_b_vals[i] = new BacnetValue(enteredValue);
                                        else
                                            new_b_vals[i] = new BacnetValue(o);
                                    }
                                }
                            }

                        } // End of valueTypesDetermined==false block

                        try
                        {
                            if (PropertyID == BacnetPropertyIds.PROP_PRIORITY_ARRAY)
                            {
                                for (int i = 0; i < enteredValues.Length; i++)
                                {
                                    uint priority = (uint)i + 1;
                                    if (enteredValues[i].Equals(existingStringValues[i]))
                                    {
                                        continue;
                                    }
                                    try
                                    {
                                        comm.WritePriority = priority;
                                        if (!comm.WritePropertyRequest(adr, object_id, BacnetPropertyIds.PROP_PRESENT_VALUE, new BacnetValue[1] { new_b_vals[i] }))
                                        {
                                            MessageBox.Show(String.Format("Couldn't write property {0} at priority {1} of device {2}, object {3}", PropertyID.ToString(), priority, ParentPoint.ParentDevice.Name, ParentPoint.Name), "Communication Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                        }

                                    }
                                    catch (Exception ex)
                                    {
                                        MessageBox.Show(String.Format("Error writing property {0} at priority {1} to device {2}, object {3}. Exception: {4}.", PropertyID.ToString(), priority, ParentPoint.ParentDevice.Name, ParentPoint.Name, ex.GetType().ToString() + " - " + ex.Message), "Communication Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                    }
                                }
                            }
                            else
                            {
                                comm.WritePriority = writePriority;
                                if (!comm.WritePropertyRequest(adr, object_id, PropertyID, new_b_vals))
                                {
                                    MessageBox.Show(String.Format("Couldn't write property {0} of device {1}, object {2}", PropertyID.ToString(), ParentPoint.ParentDevice.Name, ParentPoint.Name), "Communication Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                    Cursor.Current = Cursors.Default;
                                    return;
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show(String.Format("Error writing property {0} to device {1}, object {2}. Exception: {3}.", PropertyID.ToString(), ParentPoint.ParentDevice.Name, ParentPoint.Name, ex.GetType().ToString() + " - " + ex.Message), "Communication Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            Cursor.Current = Cursors.Default;
                            return;
                        }
                    }

                    try
                    {
                        if(!comm.ReadPropertyRequest(adr, object_id, PropertyID, out IList<BacnetValue> valueList))
                        {
                            MessageBox.Show(String.Format("Couldn't read back property {0} of device {1}, object {2}", PropertyID.ToString(), ParentPoint.ParentDevice.Name, ParentPoint.Name), "Communication Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            Cursor.Current = Cursors.Default;
                            return;
                        }
                        Values = valueList.ToList<BacnetValue>();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(String.Format("Error reading back property {0} from device {1}, object {2}. Exception: {3}.", PropertyID.ToString(), ParentPoint.ParentDevice.Name, ParentPoint.Name, ex.GetType().ToString() + " - " + ex.Message), "Communication Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        Cursor.Current = Cursors.Default;
                        return;
                    }
                    Cursor.Current = Cursors.Default;
                }
            }

            public override string ToString()
            {
                return Name;
            }

            public BacnetPropertyExport(BacnetPointExport parentPoint, BacnetPropertyIds propertyID, uint arrayIndex = System.IO.BACnet.Serialize.ASN1.BACNET_ARRAY_ALL)
            {
                PropertyID = propertyID;
                ParentPoint = parentPoint;
                ArrayIndex = arrayIndex;
                Name = propertyID.ToString();
                Values = new List<BacnetValue>();
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
                else if (Tag!=null)
                {
                    return Tag.ToString();
                }
                else
                {
                    return base.ToString();
                }
            }
        }

        private void txtDeviceFilter_TextChanged(object sender, EventArgs e)
        {
            if (!txtDeviceFilter.Enabled)
                return;

            Cursor.Current = Cursors.WaitCursor;
            //progBar.Value = 0;
            //Application.DoEvents();
            _selectedProperty = null;
            _allSelectedProperties = null;
            PropertyList.SelectedItem = null;
            PropertyList.Items.Clear();

            _selectedPoints = null;
            _allSelectedPoints = null;
            PointList.SelectedItem = null;
            PointList.Items.Clear();

            _selectedDevices = null;
            DeviceList.Items.Clear();

            List<BacnetDeviceExport> filteredDevices = null;
            if (_commonDevices != null && _commonDevices.Count > 0)
            {
                filteredDevices = FilterCommonDevices(txtDeviceFilter.Text);
                foreach (BacnetDeviceExport device in filteredDevices)
                {
                    ListViewItemBetterString item = new ListViewItemBetterString();
                    item.Text = device.Name;
                    item.Name = item.Text;
                    item.Tag = device;
                    DeviceList.Items.Add(item);
                }
            }
            
            //progBar.Value = 100;
            //Application.DoEvents();
            Cursor.Current = Cursors.Default;

            cmdCommand.Enabled = false;
            cmdViewProps.Enabled = false;
            txtPointFilter.Enabled = false;
            txtPointFilter.Text = "";
        }

        private void txtPointFilter_TextChanged(object sender, EventArgs e)
        {
            if (!txtPointFilter.Enabled)
                return;

            Cursor.Current = Cursors.WaitCursor;
            //progBar.Value = 0;
            //Application.DoEvents();
            _selectedProperty = null;
            _allSelectedProperties = null;
            PropertyList.SelectedItem = null;
            PropertyList.Items.Clear();

            _selectedPoints = null;
            _allSelectedPoints = null;
            PointList.SelectedItem = null;
            PointList.Items.Clear();

            List<BacnetPointExport> filteredPoints = null;
            if (_pointListForDisplay != null && _pointListForDisplay.Count > 0)
            {
                filteredPoints = FilterCommonPoints(txtPointFilter.Text);
                foreach (BacnetPointExport point in filteredPoints)
                {
                    ListViewItemBetterString item = new ListViewItemBetterString();
                    item.Text = point.Name;
                    item.Name = item.Text;
                    item.Tag = point;
                    PointList.Items.Add(item);
                }
            }

            //progBar.Value = 100;
            //Application.DoEvents();
            Cursor.Current = Cursors.Default;

            cmdCommand.Enabled = false;
            cmdViewProps.Enabled = false;
        }

        private List<BacnetDeviceExport> FilterCommonDevices(string filterString)
        {
            List<BacnetDeviceExport> filteredList = new List<BacnetDeviceExport>();
            if(string.IsNullOrWhiteSpace(filterString))
            {
                return _commonDevices;
            }

            foreach (BacnetDeviceExport device in _commonDevices)
            {
                if(device.Name.Contains(filterString,StringComparison.OrdinalIgnoreCase))
                {
                    filteredList.Add(device);
                }
            }

            return filteredList;
        }

        private List<BacnetPointExport> FilterCommonPoints(string filterString)
        {
            List<BacnetPointExport> filteredList = new List<BacnetPointExport>();
            if (string.IsNullOrWhiteSpace(filterString))
            {
                return _pointListForDisplay;
            }

            foreach (BacnetPointExport point in _pointListForDisplay)
            {
                if (point.Name.Contains(filterString, StringComparison.OrdinalIgnoreCase))
                {
                    filteredList.Add(point);
                }
            }

            return filteredList;
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

        private void radComObj_CheckedChanged(object sender, EventArgs e)
        {
            if(radComObj.Checked)
            {
                CheckDelimVisibility();
                ResetFromObjectsList();
            }
        }

        private void CheckDelimVisibility()
        {
            bool visible = false;
            if(radComObj.Checked)
            {
                visible = true;
            }
            chkApostrophe.Visible = visible;
            chkColon.Visible = visible;
            chkComma.Visible = visible;
            chkDot.Visible = visible;
            chkHyphen.Visible = visible;
            chkUnderscore.Visible = visible;
            lblPtDelimLabel.Visible = visible;
        }

        private char[] GetDelims()
        {
            char[] delims = new char[6];
            int count = 0;
            if(chkApostrophe.Checked)
            {
                delims[count++] = '\'';
            }
            if(chkColon.Checked)
            {
                delims[count++] = ':';
            }
            if(chkComma.Checked)
            {
                delims[count++] = ',';
            }
            if(chkDot.Checked)
            {
                delims[count++] = '.';
            }
            if(chkHyphen.Checked)
            {
                delims[count++] = '-';
            }
            if(chkUnderscore.Checked)
            {
                delims[count++] = '_';
            }
            Array.Resize(ref delims, count);
            return delims;
        }

        private void chkApostrophe_CheckedChanged(object sender, EventArgs e)
        {
            ResetFromObjectsList();
        }

        private void chkDot_CheckedChanged(object sender, EventArgs e)
        {
            ResetFromObjectsList();
        }

        private void chkColon_CheckedChanged(object sender, EventArgs e)
        {
            ResetFromObjectsList();
        }

        private void chkComma_CheckedChanged(object sender, EventArgs e)
        {
            ResetFromObjectsList();
        }

        private void chkUnderscore_CheckedChanged(object sender, EventArgs e)
        {
            ResetFromObjectsList();
        }

        private void chkHyphen_CheckedChanged(object sender, EventArgs e)
        {
            ResetFromObjectsList();
        }

        private void radJoinObj_CheckedChanged(object sender, EventArgs e)
        {
            if (radJoinObj.Checked)
            {
                CheckDelimVisibility();
                ResetFromObjectsList();
            }
        }
    }
}
