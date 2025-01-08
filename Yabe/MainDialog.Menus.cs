using Mstp.BacnetCapture;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.BACnet;
using System.Media;
using System.Net;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using System.Windows.Forms;
using WebSocketSharp;
using ZedGraph;

namespace Yabe
{
    // Main user interactions are in this file
    // Menu click, Item selection, Drag/Drop
    // Not for files Export (EXE, XML), nor DeviceClassView
    class DummyYabeMenu { } // A dummy class required to avoid opening an empty Form with the designer  
    public partial class YabeMainDialog : Form
    {
        #region Menu File, 5 methods
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
        private void cleanToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DialogResult res = MessageBox.Show(this, "Clean all " + BACnetDevice.DevicesObjectsName.Count.ToString() + " entries from \"" + Properties.Settings.Default.Auto_Store_Object_Names_File + "\", really?", "Name database suppression", MessageBoxButtons.OKCancel, MessageBoxIcon.Information);
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
                            if (entryNullable != null)
                            {
                                BACnetDevice entry = entryNullable;

                                node.Text = "Device " + entry.deviceId + " - " + entry.BacAdr.ToString(false);
                                node.ToolTipText = "";
                            }

                        }
                        catch (Exception)
                        {

                        }

                        //Enumerate routed nodes
                        foreach (TreeNode subNode in node.Nodes)
                        {
                            try
                            {
                                BACnetDevice entryNullable2 = subNode.Tag as BACnetDevice;
                                if (entryNullable2 != null)
                                {
                                    BACnetDevice entry2 = entryNullable2;
                                    subNode.Text = "Device " + entry2.deviceId + " - " + entry2.BacAdr.ToString(true);
                                    subNode.ToolTipText = "";
                                }

                            }
                            catch (Exception)
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

        private void restartToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Restart();
        }

        private void quitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }
        #endregion
        #region Menu Help and Tools, Plugins & User Menu are activated with code in Form_Load
        private void helpToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            string readme_path = Path.GetDirectoryName(Application.ExecutablePath) + "/README.txt";
            System.Diagnostics.Process.Start(readme_path);
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
                "\nReferences:" +
                "\nhttp://bacnet.sourceforge.net/" +
                "\nhttp://www.unified-automation.com/products/development-tools/uaexpert.html" +
                "\nhttp://www.famfamfam.com/" +
                "\nhttp://sourceforge.net/projects/zedgraph/" +
                "\nhttp://www.codeproject.com/Articles/38699/A-Professional-Calendar-Agenda-View-That-You-Will" +
                "\nhttps://github.com/chmorgan/sharppcap" +
                "\nhttps://sourceforge.net/projects/mstreeview" +
                "\nhttps://github.com/sta/websocket-sharp"
                , "About", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void settingsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            bool prevVertOrientation = Properties.Settings.Default.Vertical_Object_Splitter_Orientation;

            new SettingsDialog(Properties.Settings.Default).ShowDialog(this);

            bool changedOrientation = prevVertOrientation ^ Properties.Settings.Default.Vertical_Object_Splitter_Orientation;

            if (changedOrientation)
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
        #endregion
        #region Menu Add/Remove Channel/Device
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
                catch { return; }

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
                        comm.OnWhoIs += (_, __, ___, ____) => { };    // Ignore OnWhois

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
                if (MessageBox.Show(m_DeviceTree.SelectedNode.Text + "\r\nDelete this device from everywhere?", "Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == System.Windows.Forms.DialogResult.Yes)
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

                    if (m_Server != null)
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
        #endregion


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

        #region IPServices Menu :  4 Menus
        private void foreignDeviceRegistrationToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //fetch end point
            BacnetClient comm = FetchTransportClientEndPoint(typeof(BacnetIpUdpProtocolTransport));
            if (comm == null) return;

            Form F = new ForeignRegistry(comm);
            F.ShowDialog();

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
                    string[] entry = Input.genericInput.Text.Split('-');
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
        private void editBBMDTablesToolStripMenuItem_Click(object sender, EventArgs e)
        {

            BACnetDevice device = FetchEndPoint();

            if ((device != null) && (device.channel.Transport is BacnetIpUdpProtocolTransport) && (device.BacAdr != null) && (device.BacAdr.RoutedSource == null))
                new BBMDEditor(device.channel, device.BacAdr).ShowDialog();
            else
                MessageBox.Show("An IPv4 device is required", "Wrong device", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        #endregion

        #region Several Menus : File Up-download, Com control, Laucnh different editor (Shedule, Calendar, Trendlog, ...)
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
            if (Properties.Settings.Default.TimeSynchronize_UTC)
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

        private void createObjectToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //fetch end point
            BACnetDevice device = FetchEndPoint();

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
                    device.CreateObjectRequest(new BacnetObjectId((BacnetObjectTypes)F.ObjectType.SelectedIndex, (uint)F.ObjectId.Value), initialvalues);

                    m_DeviceTree_AfterSelect("ObjNewDelete", new TreeViewEventArgs(this._selectedDevice));
                }
                catch (Exception ex)
                {
                    Trace.TraceError("Error : " + ex.Message);
                    MessageBox.Show("Fail to Create Object", "CreateObject", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }

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

                new TrendLogDisplay(device, object_id).ShowDialog();

            }
            catch (Exception ex)
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

                new ScheduleDisplay(m_AddressSpaceTree.ImageList, device, object_id).ShowDialog();

            }
            catch (Exception ex) { Trace.TraceError("Error loading Schedule : " + ex.Message); }
        }

        private void deleteObjectToolStripMenuItem_Click(object sender, EventArgs e)
        {

            try
            {
                //fetch end point
                BACnetDevice device;
                BacnetObjectId object_id;

                GetObjectLink(out device, out object_id);

                if (MessageBox.Show("Are you sure you want to delete this object ?", object_id.ToString(), MessageBoxButtons.OKCancel, MessageBoxIcon.Question) == DialogResult.OK)
                {
                    device.DeleteObjectRequest(object_id);
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

                new CalendarEditor(device, object_id).ShowDialog();

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

                new NotificationEditor(device, object_id).ShowDialog();

            }
            catch (Exception ex) { Trace.TraceError("Error loading Notification : " + ex.Message); }
        }

        private void subscribeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //fetch end point
            if (m_DeviceTree.SelectedNode == null) return;
            else if (m_DeviceTree.SelectedNode.Tag == null) return;
            else if (!(m_DeviceTree.SelectedNode.Tag is BACnetDevice)) return;
            BACnetDevice entry = (BACnetDevice)m_DeviceTree.SelectedNode.Tag;

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
        #endregion
        private void btnExport_Click(object sender, EventArgs e)
        {
            ExportCovGraph();
        }

        private void btnPlay_Click(object sender, EventArgs e)
        {
            TogglePlotter();
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

        private void ack_icon_Click(object sender, EventArgs e)
        {
            if (sender == ack_offnormal)
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
            GenericInputBox<TextBox> search = new GenericInputBox<TextBox>("Search object", "Name", (o) =>
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

        private void m_DeviceTree_AfterSelect(object sender, TreeViewEventArgs e)
        {
            SetSimplifiedLabels();

            TreeNode node = e.Node;
            if (node == null) return; // Toogle Simplified View without selected node

            BACnetDevice device = node.Tag as BACnetDevice;
            if (device == null) return;  // Not a TreeNode linked to a device

            if ((sender as String == "ObjRename") || (sender == manual_refresh_objects) || (sender as String == "ObjNewDelete"))
                _selectedDevice = null; // invalidate the current selection to force a renew

            if (_selectedDevice != null)
                if ((_selectedDevice.Tag == device)) return;  // Same device, on a clone TreeNode

            _selectedDevice = node;

            AsynchRequestId++; // disabled a possible thread pool work (update) on the AddressSpaceTree

            if ((sender == manual_refresh_objects) || (sender as String == "ObjNewDelete") || (!Properties.Settings.Default.UseObjectsCache))
                device.ClearCache();

            m_AddressSpaceTree.Nodes.Clear();   // clear Objects dictionnary, the device is changed
            AddSpaceLabel.Text = AddrSpaceTxt;

            BacnetClient comm = device.channel;
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
                if (dlg.ShowDialog(this) == System.Windows.Forms.DialogResult.Cancel) return;
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
                    AddSpaceLabel.Text = AddrSpaceTxt + " : " + CountStructuredObjects.ToString() + " Items";
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
                        AddSpaceLabel.Text = AddrSpaceTxt + " : 0 Items / " + list_count.ToString() + " expected";
                        AddObjectListOneByOneAsync(device, list_count, AsynchRequestId);
                        _selectedDevice = node;
                        return;
                    }

                    if (device.SortableDictionnary)
                        objectList.Sort();

                    //add to tree
                    int Count = 0;
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
                        Count+=AddObjectEntry(device, null, bobj_id, m_AddressSpaceTree.Nodes);
                    }
                    AddSpaceLabel.Text = AddrSpaceTxt + " : " + Count.ToString() + " Items";
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

        private void m_AddressSpaceTree_ItemDrag(object sender, ItemDragEventArgs e)
        {
            m_AddressSpaceTree.DoDragDrop(e.Item, DragDropEffects.Move);
        }

        private void m_SubscriptionView_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Move;
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
                        string line = sr.ReadLine();
                        if ((line.Length > 0) && (line[0] != '#'))
                        {

                            string[] description = line.Split(';');
                            if ((description.Length == 3) || (description.Length == 4))
                            {
                                try
                                {
                                    uint deviceId;
                                    deviceId = Convert.ToUInt32(description[1]);
                                    string objectIdString = description[2];
                                    if (!objectIdString.StartsWith("OBJECT_"))
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
                                                    if (description.Length == 4)
                                                        CreateSubscription(deviceEntry, objectId, description[2] == "P", Int32.Parse(description[3]));
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

        // Change the WritePriority Value & Simplified view
        private void MainDialog_KeyDown(object sender, KeyEventArgs e)
        {
            if ((e.Modifiers == (Keys.Control | Keys.Alt)))
            {
                if (e.KeyValue == (int)Keys.S)
                    ToggleViewMenuItem_Click(null, null);

                if (e.KeyValue == (int)Keys.N)
                    readPropertiesNameToolStripMenuItem_Click(null, null);

                if ((e.KeyCode >= Keys.D0 && e.KeyCode <= Keys.D9) || (e.KeyCode >= Keys.NumPad0 && e.KeyCode <= Keys.NumPad9))
                {
                    string s = e.KeyCode.ToString();
                    int i = Convert.ToInt32(s[s.Length - 1]) - 48;

                    Properties.Settings.Default.DefaultWritePriority = (BacnetWritePriority)i;
                    SystemSounds.Beep.Play();
                    Trace.WriteLine("WritePriority change to level " + i.ToString() + " : " + ((BacnetWritePriority)i).ToString());
                }
            }
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

                if ((selectedSubscriptions == null) || (selectedSubscriptions.Count == 0))
                    return;

                this.m_AddressSpaceTree.SelectedNode = null;
                this.m_AddressSpaceTree.SelectedNodes.Clear();

                ListViewItem itm = selectedSubscriptions[0];

                if (!(itm.Tag is Subscription subscription))
                    return;
                else
                    UpdateGrid(subscription);

            }
            catch { }

        }

        private void m_SubscriptionView_ItemChecked(object sender, ItemCheckedEventArgs e)
        {
            if (e.Item.Tag is Subscription sub)
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

        private void pollRateSelector_ValueChanged(object sender, EventArgs e)
        {
            uint period = Math.Max(Math.Min((uint)((NumericUpDown)sender).Value, MAX_POLL_PERIOD), MIN_POLL_PERIOD);
            Properties.Settings.Default.Subscriptions_ReplacementPollingPeriod = period;
        }

        private void PollOpn_CheckedChanged(object sender, EventArgs e)
        {
            if (PollOpn.Checked)
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

 
        private void TbxHighlightTreeView_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode != Keys.Return) return;

            TbxHighlightTreeView_Update(sender, null);
        }


        private void menuStrip1_MenuActivate(object sender, EventArgs e)
        {
            // Updates controls enabled state.
            FetchEndPoints(out var endPoints);
            exportEDEFilesSelDeviceToolStripMenuItem.Enabled = (FetchEndPoint() != null);
            exportEDEFilesAllDevicesToolStripMenuItem.Enabled = (endPoints.Count >= 1);
        }


    }
}