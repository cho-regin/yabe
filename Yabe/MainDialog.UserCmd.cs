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
using System.IO;
using System.IO.BACnet;
using System.Linq;
using System.Windows.Forms;

namespace Yabe
{ 
    class DummyMainDialogUserCmd { } // A dummy class required to avoid opening an empty Form with the designer  

    public partial class YabeMainDialog
    {
        string[] CommandList = { "none", "launch", "send_iam", "send_whois", "send_whoisto", "leave_devices" };

        Keys ShortCut(String s) // Must contains Control or Alt 
        {
            if (s.Length==1)
                return Keys.Control | (Keys)s[0];
            else
            {
                int Ret = (int)s[1];
                Int32.TryParse(s[0].ToString(), out int ModifierInt);

                if (((ModifierInt & 1) == 1) || ((ModifierInt & 3) == 0))
                    Ret = Ret + (int)Keys.Control;
                if ((ModifierInt & 2) == 2)
                    Ret = Ret + (int)Keys.Alt;
                if ((ModifierInt & 4) == 4)
                    Ret = Ret + (int)Keys.Shift;

                return (Keys)Ret;
            }
        }
        void InitUserCmd()
        {
            if (File.Exists("YabeMenuCmd.txt"))
            {
                
                int LineCount = 0;
                try
                {
                    using (StreamReader sr = new StreamReader("YabeMenuCmd.txt"))
                    {
                        while (!sr.EndOfStream)
                        {
                            string l = sr.ReadLine();
                            LineCount++;
                            if (!l.StartsWith("#"))
                            {
                                String[] MenuCommand = l.Split(';');
                                Tuple<int, string> Cmd = null;

                                if (MenuCommand.Length == 3)
                                    Cmd = new Tuple<int, string>(Array.IndexOf(CommandList, MenuCommand[2].ToLower()), null);
                                else if (MenuCommand.Length == 4)
                                    Cmd = new Tuple<int, string>(Array.IndexOf(CommandList, MenuCommand[2].ToLower()), MenuCommand[3]);

                                if (Cmd!=null)
                                {
                                    if ((MenuCommand[0] != "Sep") && (Cmd.Item1 >= 0))
                                    {
                                        // Creates the Menu Item
                                        ToolStripMenuItem MenuItem = new ToolStripMenuItem();
                                        MenuItem.Text = MenuCommand[0];
                                        MenuItem.Tag = Cmd;

                                        if (MenuCommand[1].Length >= 1)
                                            MenuItem.ShortcutKeys = ShortCut(MenuCommand[1]);
                                        
                                        if (Cmd.Item1 > 0)
                                            MenuItem.Click += new EventHandler(UserMenuItem_Click);

                                        yabeFrm.userCommandToolStripMenuItem.DropDownItems.Add(MenuItem);
                                    }
                                    else 
                                        yabeFrm.userCommandToolStripMenuItem.DropDownItems.Add(new ToolStripSeparator());

                                }
                            }
                        }
                    }
                }
                catch { Trace.WriteLine("Error file YabeMenuCmd.txt line " + LineCount); }

                if (userCommandToolStripMenuItem.DropDownItems.Count == 0) userCommandToolStripMenuItem.Visible = false;

            }

        }

        void UserMenuItem_Click(object sender, EventArgs e)
        {
            Tuple<int, string> Cmd = (Tuple<int, string>)(sender as ToolStripMenuItem).Tag;

            if (Cmd.Item1 == 1) // Launch
                UserCmd_Launch(Cmd);

            if ((Cmd.Item1==2)&&(Properties.Settings.Default.YabeDeviceId>=0))   // Iam
                UserCmd_Iam(Cmd);

            if (Cmd.Item1 == 3) // WhoIs with interval
                UserCmd_WhoIs1(Cmd);

            if (Cmd.Item1 == 4) // WhoIs to several
                UserCmd_WhoIs2(Cmd);

            if (Cmd.Item1 == 5)
                UserCmd_RemoveDevices(Cmd);
        }
        void UserCmd_Launch(Tuple<int, string> Cmd)
        {
            if (Cmd.Item2 == null) return;

            String[] P = Cmd.Item2.Split(',');

            if (P.Length <2) return;

            try
            {
                ProcessStartInfo ps = new ProcessStartInfo(P[1]);

                ps.WorkingDirectory = P[0];
                if (P.Length > 2)
                    ps.Arguments= P[2];

                if (P.Length == 2)
                    Process.Start(ps);
                else
                    Process.Start(ps);
            }
            catch { }
            
            return;
        }

        void UserCmd_Iam(Tuple<int, string> Cmd)
        {
            foreach (TreeNode Tn in m_DeviceTree.Nodes[0].Nodes)
            {
                BacnetClient cli = Tn.Tag as BacnetClient;
                try
                { m_Server?.Iam(cli); } catch { }
            }
            return;
        }

        void UserCmd_WhoIs1(Tuple<int, string> Cmd)
        {
            if (Cmd.Item2 == null) return;

            String[] P = Cmd.Item2.Split(',');
            int Min, Max;
            try
            {
                Min = Convert.ToInt32(P[0]);
                Max = Convert.ToInt32(P[1]);
            }
            catch { return; }

            foreach (TreeNode Tn in m_DeviceTree.Nodes[0].Nodes)
            {
                BacnetClient cli = Tn.Tag as BacnetClient;
                try { cli.WhoIs(Min, Max); } catch { }
            }
            return;
        }

        void UserCmd_WhoIs2(Tuple<int, string> Cmd)
        {
            if (Cmd.Item2 == null) return;

            String[] Param = Cmd.Item2.Split(',');
            int[] Id = new int[Param.Length];
            try
            {
                for (int i = 0; i < Param.Length; i++)
                    Id[i] = Convert.ToInt32(Param[i]);
            }
            catch { return; }

            foreach (TreeNode Tn in m_DeviceTree.Nodes[0].Nodes)
            {
                BacnetClient cli = Tn.Tag as BacnetClient;
                foreach (var devId in Id)
                    try { cli.WhoIs(devId, devId); } catch { }
            }
            return;
        }
        void UserCmd_RemoveDevices(Tuple<int, string> Cmd)
        {
            if (Cmd.Item2 == null) return;

            String[] Param = Cmd.Item2.Split(',');
            uint[] Id = new uint[Param.Length];

            try
            {
                for (int i = 0; i < Param.Length; i++)
                    Id[i] = Convert.ToUInt32(Param[i]);
            }
            catch { return; }

            List<Tuple<TreeNode, BacnetClient, BACnetDevice>> tnremove = new List<Tuple<TreeNode, BacnetClient, BACnetDevice>>();
            foreach (TreeNode TnClient in m_DeviceTree.Nodes[0].Nodes)
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
                                tnremove.Add(new Tuple<TreeNode, BacnetClient, BACnetDevice>(Tn2, cli, device));
                                count++;
                            }
                        }
                    }
                    if (count == Tn.Nodes.Count)// A simple device or a router now with all devices removes
                    {
                        BACnetDevice device = Tn.Tag as BACnetDevice;
                        if (!Id.Contains(device.deviceId))
                            tnremove.Add(new Tuple<TreeNode, BacnetClient, BACnetDevice>(Tn, cli, device));
                    }
                }
            }

            lock (m_devices)
                foreach (var remove in tnremove)
                {
                    RemoveSubscriptions(remove.Item3, null);
                    m_DeviceTree.Nodes.Remove(remove.Item1);
                    m_devices[remove.Item2].Devices.Remove(remove.Item3);
                }

            m_AddressSpaceTree.Nodes.Clear();   //clear address space
            AddSpaceLabel.Text = "Address Space";
            m_DataGrid.SelectedObject = null;   //clear property grid
        }
    }
}