/**************************************************************************
*                           MIT License
* 
* Copyright (C) 2025 Frederic Chaxel <fchaxel@free.fr>
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
using System.Drawing;
using System.IO;
using System.IO.BACnet;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace Yabe
{
    class DummyUserCmd { } // A dummy class required to avoid opening an empty Form with the designer  

    public partial class YabeMainDialog
    {
        static readonly string[] CommandList = { "none", "launch", "send_iam", "send_whois", "leave_devices", "subscribe_files",
                                                "snapshot", "write_recipe", "settings", "exec_batch" };
        // Action array cannot be filled here, methods cannot be static to access simply to all Yabe elements, done in InitUserCmd
        Action<String>[] UserCmdCommands;
        Keys ShortCut(String s) // Keys Must contains Control or Alt for keyboard shortcut
        {
            if (s.Length == 1)
                return Keys.Control | (Keys)s[0];
            else
            {
                int Ret = (int)s[1];
                Int32.TryParse(s[0].ToString(), out int ModifierInt);

                // Don't accept Yabe reserved KeySortCut 
                if ((ModifierInt == 3))
                    if ((Ret == 'N') || (Ret == 'S') || ((Ret >= '0') && (Ret <= '9')))
                        return (Keys)Ret;

                // at least Ctrl or Alt is required for a Keyboard Shortcut
                if (((ModifierInt & 1) == 1) || ((ModifierInt & 3) == 0))
                    Ret = Ret + (int)Keys.Control;
                if ((ModifierInt & 2) == 2)
                    Ret = Ret + (int)Keys.Alt;
                if ((ModifierInt & 4) == 4)
                    Ret = Ret + (int)Keys.Shift;

                return (Keys)Ret;
            }
        }
        Tuple<int, string> MenuLineToCmd(String line, out String[] MenuCommand)
        {
            Tuple<int, string> Cmd = null;

            // Change escaped \; and \, with unusable symbols
            line = line.Replace("\\,", new string((char)4, 1)).Replace("\\;", new string((char)3, 1));
            // Operation will be done later to get back ; and ,

            MenuCommand = line.Split(';');

            // Parameters with , are not splited here. Maybe it will be necessary a day inside them and not as a sperator

            if (MenuCommand.Length == 3)
                Cmd = new Tuple<int, string>(Array.IndexOf(CommandList, MenuCommand[2].ToLower()), null);
            else if (MenuCommand.Length == 4)
                    Cmd = new Tuple<int, string>(Array.IndexOf(CommandList, MenuCommand[2].ToLower()), MenuCommand[3]);

            return Cmd;
        }
        void InitUserCmd()
        {
            int MenuVersion;

            if (File.Exists("YabeMenuCmd.txt"))
            {
                // Init the Array of link to Methods associated to each commands
                UserCmdCommands = new Action<String>[] { UserCmd_None, UserCmd_Launch, UserCmd_Iam, UserCmd_WhoIs,
                     UserCmd_RemoveDevices, UserCmd_SubscribeFiles, UserCmd_SnapShot, UserCmd_WriteRecipe, UserCmd_Settings, UserCmd_ExecBatch };

                if ((Debugger.IsAttached) && (UserCmdCommands.Length != CommandList.Length))
                {
                    // Dear developer:
                    // You throw this exception because you probably have added or removed User commands!?
                    // The number of commands should be the same as the number of methods linked to them
                    throw new NotImplementedException("Missing User Commands");
                }

                int LineCount = 0;
                try
                {
                    using (StreamReader sr = new StreamReader("YabeMenuCmd.txt"))
                    {

                        ToolStripMenuItem YabeUserMenu = yabeFrm.userCommandToolStripMenuItem;
                        while (!sr.EndOfStream)
                        {
                            string l = sr.ReadLine();
                            LineCount++;

                            // Maybe used a day
                            if ((LineCount == 1) && (l.Contains("YabeMenuV")))
                                Int32.TryParse(new string(l.Where(c => char.IsDigit(c)).ToArray()), out MenuVersion);

                            if (string.IsNullOrWhiteSpace(l) || l.StartsWith("#"))
                                continue;

                            String[] MenuCommand;
                            Tuple<int, string> Cmd = MenuLineToCmd(l, out MenuCommand); 

                            if (Cmd != null)
                            {
                                if (MenuCommand[0].StartsWith("SubStart"))
                                {
                                    ToolStripMenuItem MenuItem = new ToolStripMenuItem()
                                    {
                                        Text = MenuCommand[0].Substring(9),
                                        Tag = YabeUserMenu, // To remember the Parent
                                    };

                                    YabeUserMenu.DropDownItems.Add(MenuItem);
                                    YabeUserMenu = MenuItem;
                                }
                                else if (MenuCommand[0].StartsWith("SubClose"))
                                {
                                    if (YabeUserMenu.Tag is ToolStripMenuItem)
                                        YabeUserMenu = YabeUserMenu.Tag as ToolStripMenuItem;
                                }
                                else if ((MenuCommand[0] != "Sep") && (Cmd.Item1 >= 0))
                                {
                                    // Creates the Menu Item
                                    ToolStripMenuItem MenuItem = new ToolStripMenuItem()
                                    {
                                        Text = MenuCommand[0],
                                        Tag = Cmd,
                                    };

                                    if (MenuCommand[1].Length >= 1)
                                        MenuItem.ShortcutKeys = ShortCut(MenuCommand[1]);

                                    if (Cmd.Item1 > 0)
                                        MenuItem.Click += new EventHandler(UserMenuItem_Click);

                                    YabeUserMenu.DropDownItems.Add(MenuItem);
                                }
                                else
                                    YabeUserMenu.DropDownItems.Add(new ToolStripSeparator());
                            }
                        }
                    }
                }
                catch { Trace.WriteLine("Error file YabeMenuCmd.txt line " + LineCount); }
            }

            if (userCommandToolStripMenuItem.DropDownItems.Count == 0) userCommandToolStripMenuItem.Visible = false;

        }

        void UserMenuItem_Click(object sender, EventArgs e) // This indirect step could be avoided, but it's not necessary
        {
            try
            {
                Tuple<int, string> Cmd = (Tuple<int, string>)(sender as ToolStripMenuItem).Tag;
                UserCmdCommands?[Cmd.Item1](Cmd.Item2);
            }
            catch { }
        }
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////
        void UserCmd_None(String Parameters) // will be used later with an idea of batch command
        {
            if (Parameters == null) return;

            UInt32.TryParse(Parameters, out uint delay);
            Thread.Sleep((int)delay);
        }
        void UserCmd_Launch(String Parameters)
        {
            if (Parameters == null) return;
            String[] P = Parameters.Split(',');

            if (P.Length < 2) return;

            try
            {
                ProcessStartInfo ps = new ProcessStartInfo(P[1]);

                ps.WorkingDirectory = P[0];
                if (P.Length > 2)
                    ps.Arguments = P[2];

                if (P.Length == 2)
                    Process.Start(ps);
                else
                    Process.Start(ps);
            }
            catch { }

            return;
        }

        void UserCmd_Settings(String Parameters)
        {
            if (Parameters == null) return;
            String[] P = Parameters.Split(',');

            SettingsDialog.SettingsDescriptor description = new SettingsDialog.SettingsDescriptor(Properties.Settings.Default);
            PropertyInfo[] allProp = typeof(SettingsDialog.SettingsDescriptor).GetProperties();
            for (int i = 0; i < P.Length; i++)
            {

                String[] Setting = P[i].Split('=');
                if (Setting.Length == 1)    // End 
                {
                    Properties.Settings.Default.Save();
                    if ((P[i].ToLower() == "restart"))
                        Application.Restart();
                }

                foreach (PropertyInfo prop in allProp)
                {
                    object[] o = prop.GetCustomAttributes(true);
                    if ((o.Length == 3) && (o[0].GetType() == typeof(System.ComponentModel.DisplayNameAttribute)))
                    {
                        String s = (o[0] as System.ComponentModel.DisplayNameAttribute).DisplayName.ToString();
                        if (s == Setting[0].Trim()) // Bingo it's the good property
                        {
                            String ValToSet = Setting[1].Replace((char)4, ',').Replace((char)3, ';');
                            Type typeorigin = prop.PropertyType;

                            try
                            {
                                if (typeorigin.IsEnum)
                                    prop.SetValue(description, Enum.Parse(typeorigin, ValToSet, true));
                                else
                                    prop.SetValue(description, Convert.ChangeType(ValToSet, typeorigin));
                            }
                            catch { }
                            break;
                        }
                    }
                }
            }

        }

        void UserCmd_Iam(String Parameters)
        {
            if (Properties.Settings.Default.YabeDeviceId >= 0)
            {
                foreach (TreeNode Tn in NetworkViewTreeNode.Nodes)
                {
                    BacnetClient cli = Tn.Tag as BacnetClient;
                    try
                    { m_Server?.Iam(cli); }
                    catch { }
                }
            }
            else
                Trace.WriteLine("Cannot send Iam without Yabe device Id");
        }

        void UserCmd_WhoIs(String Parameters)
        {
            if (Parameters == null) return;
            String[] Param = Parameters.Split(',');

            List<Tuple<int, int>> Id = new List<Tuple<int, int>>();
            try
            {
                for (int i = 0; i < Param.Length; i++)
                {
                    int Min, Max;
                    if (Param[i].Contains(".."))
                    {
                        String[] Param2 = Param[i].Split('.');
                        Min = Convert.ToInt32(Param2[0]);
                        Max = Convert.ToInt32(Param2[2]);
                    }
                    else
                        Min = Max = Convert.ToInt32(Param[i]);

                    Id.Add(new Tuple<int, int>(Min, Max));
                }
            }
            catch { }

            lock (m_devices)
                foreach (var v in m_devices)
                {
                    BacnetClient cli = v.Key;
                    foreach (var devId in Id)
                        try { cli.WhoIs(devId.Item1, devId.Item2); } catch { }
                }
            return;
        }
        void UserCmd_RemoveDevices(String Parameters)
        {
            if (Parameters == null) return;
            String[] Param = Parameters.Split(',');

            uint[] Id = new uint[Param.Length];

            try
            {
                for (int i = 0; i < Param.Length; i++)
                    Id[i] = Convert.ToUInt32(Param[i]);
            }
            catch { return; }

            List<Tuple<BacnetClient, BACnetDevice>> tnremove = new List<Tuple<BacnetClient, BACnetDevice>>();

            // If a router is to be remove all routed nodes as to be in the removed list before
            // otherwise it is not
            // All devices are always in the NetworkViewTreeNode (even hidden) and it's 
            // the only way to know if a device is routed by another one visible in the TreeView.
            // So behaviour here is not exactly the same as in Yabe interface, but it's wanted
            foreach (TreeNode TnClient in NetworkViewTreeNode.Nodes)
            {
                BacnetClient cli = TnClient.Tag as BacnetClient;
                foreach (TreeNode Tn in TnClient.Nodes)
                {
                    int count = 0;
                    if (Tn.Nodes.Count != 0)
                    {
                        foreach (TreeNode Tn2 in Tn.Nodes)
                        {
                            BACnetDevice device = Tn2.Tag as BACnetDevice;
                            if (!Id.Contains(device.deviceId))
                            {
                                tnremove.Add(new Tuple<BacnetClient, BACnetDevice>(cli, device));
                                count++;
                            }
                        }
                    }
                    if (count == Tn.Nodes.Count)// A simple device or a router now with all devices removes
                    {
                        BACnetDevice device = Tn.Tag as BACnetDevice;
                        if (!Id.Contains(device.deviceId))
                            tnremove.Add(new Tuple<BacnetClient, BACnetDevice>(cli, device));
                    }
                }
            }

            lock (m_devices)
                foreach (var remove in tnremove)
                {
                    RemoveSubscriptions(remove.Item2, null);
                    DeleteTreeNodeDevice(remove.Item2);
                    m_devices[remove.Item1].Devices.Remove(remove.Item2);
                }

            m_AddressSpaceTree.Nodes.Clear();   //clear address space
            AddSpaceLabel.Text = AddrSpaceTxt;
            m_DataGrid.SelectedObject = null;   //clear property grid
        }

        void UserCmd_SubscribeFiles(String Parameters)
        {
            if (Parameters == null) return;
            String[] Param = Parameters.Split(',');

            foreach (String ParamStr in Param)
            {
                if (File.Exists(ParamStr))
                    try
                    {
                        DataObject dataObject = new DataObject(DataFormats.FileDrop, new string[] { ParamStr });
                        m_SubscriptionView_DragDrop(null, new DragEventArgs(dataObject, 0, 0, 0, DragDropEffects.All, DragDropEffects.All));
                    }
                    catch { }
            }
        }
        List<String> UserCmd_WriteRecipeSnapShotCommon(String Parameters, bool IsOnlySnap)
        {
            BacnetValue GetBacnetValue(BACnetDevice device, BacnetObjectId obj, BacnetPropertyIds prop, bool Snap)
            {
                // if Snap=true we always want the value
                // We need only the BacnetApplicationTags if Snap=false
                // Some a well known and maybe here a read can be avoided for them
                // eg Present Value on basic objects, several binary property such as out of service
                IList<BacnetValue> out_value;
                device.ReadPropertyRequest(obj, prop, out out_value);
                if (out_value != null)
                    return out_value[0];
                else
                    return new BacnetValue(BacnetApplicationTags.BACNET_APPLICATION_TAG_NULL, null);
            }

            List<String> Status = new List<string>();

            if (File.Exists(Parameters))
            {
                String ReadWriteStatus = null;
                String[] WriteCmds = File.ReadAllLines(Parameters);
                foreach (String WriteCmd in WriteCmds)
                {
                    String[] Commande = WriteCmd.Split(';');

                    if ((Commande.Length < 7) || (Commande[0].StartsWith("#")))
                        continue;
                    try
                    {
                        if (UInt32.TryParse(Commande[0], out uint device_id)) // fail with the first line if it's the header
                        {
                            BACnetDevice device = FetchDeviceFromDeviceId(device_id);

                            if (device == null)
                            {
                                ReadWriteStatus = Commande[0] + ";" + Commande[1] + ";" + Commande[2] + ";" + Commande[3] + ";" + Commande[4] + ";;Device not Found";
                                Status.Add(ReadWriteStatus);
                                continue;
                            }

                            if (device.deviceName != null)  // Change the Device Name if already knonw
                                Commande[1] = device.deviceName;

                            ReadWriteStatus = Commande[0] + ";" + Commande[1] + ";" + Commande[2] + ";" + Commande[3] + ";" + Commande[4] + ";";


                            BacnetObjectId boid = BacnetObjectId.Parse("OBJECT_" + Commande[2].Trim().Replace(' ', '_').ToUpper());
                            BacnetPropertyIds prop = (BacnetPropertyIds)Enum.Parse(typeof(BacnetPropertyIds), "PROP_" + Commande[4].Trim().Replace(' ', '_').ToUpper());

                            UInt32.TryParse(Commande[5], out uint WritePriority);

                            String ValStrSatus = Commande[6];
                            String ValStr = Commande[6];
                            for (int i = 7; i < Commande.Length; i++)    // Restore the possible ; from a String
                            {
                                ValStr = ValStr + ";" + Commande[i];
                                ValStrSatus = ValStrSatus + "," + Commande[i]; // Change de seprator only for the status
                            }

                            // Get the data with the right type and BacnetTag
                            BacnetValue valRead = GetBacnetValue(device, boid, prop, false);

                            if ((valRead.Tag != BacnetApplicationTags.BACNET_APPLICATION_TAG_NULL) && (!IsOnlySnap))
                            {
                                BacnetValue valtoWrite = new BacnetValue(valRead.Tag, ValStr); // The stack is able to convert the string with the good Tag value
                                uint oldpriority = device.channel.WritePriority;
                                device.channel.WritePriority = WritePriority;
                                try
                                {
                                    bool ret = device.WritePropertyRequest(boid, prop, new List<BacnetValue> { valtoWrite });
                                    if (ret == false)
                                        ReadWriteStatus += ";Write Fail";
                                    else
                                        ReadWriteStatus += ValStrSatus + ";Write Ok";
                                }
                                catch { ReadWriteStatus += ";Write Fail"; }
                                device.channel.WritePriority = oldpriority;
                            }
                            if (IsOnlySnap)
                            {
                                if (valRead.Value != null)
                                    ReadWriteStatus += valRead.Value.ToString();
                                else
                                    ReadWriteStatus += ";Read Fail";
                            }
                        }
                        else
                            continue;
                    }
                    catch
                    {
                        ReadWriteStatus += ";Syntax Error";
                    }

                    Status.Add(ReadWriteStatus);
                }
            }

            return Status;
        }

        void UserCmd_SnapShot(String Parameters)
        {
            if (Parameters == null) return;
            String[] Param = Parameters.Split(',');

            if (Param.Length >= 1)
            {
                List<String> status = UserCmd_WriteRecipeSnapShotCommon(Param[0], true);
                if (Param.Length >= 2)
                {
                    StringBuilder sb = new StringBuilder();
                    sb.AppendLine("DeviceId;DeviceName;ObjectId;ObjectName;Property;Value;Status");
                    foreach (String s in status)
                        sb.AppendLine(s);

                    String FileName = Param[1].Replace("%d", DateTime.Now.ToString().Replace('/', '_').Replace(':', '_'));

                    try
                    {
                        File.WriteAllText(FileName, sb.ToString());
                    }
                    catch 
                    {
                        Trace.WriteLine("Fail to write the snapshot File");
                    }
                }

                if ((Param.Length == 1) || (Param.Length > 2))
                    new WriteRecipeForm(this.Icon, status).ShowDialog();
            }

        }
        void UserCmd_WriteRecipe(String Parameters)
        {
            List<String> status = UserCmd_WriteRecipeSnapShotCommon(Parameters, false);
            new WriteRecipeForm(this.Icon, status).ShowDialog();
        }
        void UserCmd_ExecBatch(String Parameters) 
        {
            if (Parameters == null) return;
            if (!File.Exists(Parameters)) return;

            // The file is the same as YabeMenuCmd.txt without the 2 first columns
            String[] lines = File.ReadAllLines(Parameters);

            int LineCount = 0;
            String LineError = "";
            String Sep = "";

            foreach (String line in lines)
            {
                LineCount++;

                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
                    continue;

                String cmdline=";;"+line;   // Add the two missing items similiar to YabeMenuCmd.txt

                String[] MenuCommand;
                Tuple<int, string> Cmd = MenuLineToCmd(cmdline, out MenuCommand);

                if (Cmd == null)
                {
                    LineError = LineError + Sep + LineCount.ToString();
                    Sep = ", ";
                }
                else
                    try
                    {
                            UserCmdCommands[Cmd.Item1](Cmd.Item2);
                    }
                    catch
                    {
                        LineError = LineError + LineCount.ToString();
                        Sep = ", ";
                    }
            }

            if (LineError != "")
                Trace.WriteLine("Script error line(s) : " + LineError);
        }
    }
    class WriteRecipeForm : Form
    {
        private ListView listStatus;
        public WriteRecipeForm(Icon icon, List<String> Status)
        {
            InitializeComponent(icon);
            foreach (String s in Status)
            {
                String[] st = s.Split(';');
                ListViewItem itm = listStatus.Items.Add(st.Last());

              for (int i = 0;i<st.Length-1;i++)
                    itm.SubItems.Add(st[i]);
            }
        }

        void InitializeComponent(Icon icon)
        {
            listStatus = new ListView();
            SuspendLayout();

            listStatus.Dock = DockStyle.Fill;
            listStatus.UseCompatibleStateImageBehavior = false;
            listStatus.View = View.Details;
            listStatus.Columns.AddRange(new ColumnHeader[] 
            {
                new ColumnHeader() { Text="Status", Width=-2},
                new ColumnHeader() { Text="Device", Width = -2},
                new ColumnHeader() { Text="Device Name", Width=-2},
                new ColumnHeader() { Text="Object Id", Width=-2},
                new ColumnHeader() { Text="Object Name", Width = -2},
                new ColumnHeader() { Text="Property", Width = -2},
                new ColumnHeader() { Text="Value", Width = -2}
            }) ;

            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 450);
            Controls.Add(listStatus);
            Text = "SnapShot or Write Status";
            Icon = icon;
            ResumeLayout(false);

        }

    }
}