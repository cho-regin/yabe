﻿/**************************************************************************
*                           MIT License
* 
* Copyright (C) 2018 Frederic Chaxel <fchaxel@free.fr>
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
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Yabe;
using System.IO.BACnet;
using System.Diagnostics;
using System.IO.BACnet.Serialize;

namespace ListCOV_Increment
{
    public partial class COV_Increment : Form
    {
        public BacnetObjectTypes[] Filter; // Filtering list

        YabeMainDialog yabeFrm;
        BacnetClient client; BacnetAddress adr; BacnetObjectId objId; uint deviceId;

        public COV_Increment(YabeMainDialog yabeFrm)
        {
            this.yabeFrm = yabeFrm;
            Icon = yabeFrm.Icon; // gets Yabe Icon
            InitializeComponent();
        }

        private void COV_Increment_Load(object sender, EventArgs e)
        {
            BeginInvoke(new Action(RunReadAll)); // Leave Windows displaying the form before processing
        }

        bool IsEmpty = true;

        void RunReadAll()
        {
            Application.UseWaitCursor = true;

            // Gets all elements concerning the selected device into the DeviceTree
            // and optionnaly the object into the AddressSpaceTree treeview
            // return false if objId is not OK (but got the value ANALOG:0 !)
            // BacnetClient & BacnetAddress could be null if nothing is selected into the DeviceTree

            // a lot of Error in the Trace due to Read property not existing, remove listerner, then add it back
            TraceListener trace = Trace.Listeners[1];
            Trace.Listeners.Remove(Trace.Listeners[1].Name);

            try
            {
                yabeFrm.GetObjectLink(out client, out adr, out deviceId, out objId, BacnetObjectTypes.MAX_BACNET_OBJECT_TYPE);
                Devicename.Text = adr.ToString();

                CheckAllObjects(yabeFrm.m_AddressSpaceTree.Nodes);
                EmptyList.Visible = IsEmpty;
            }
            catch
            { }

            Trace.Listeners.Add(trace);

            treeView1.ExpandAll();

            Application.UseWaitCursor=false;
            
        }

        void CheckAllObjects(TreeNodeCollection tncol)
        {

            foreach (TreeNode tn in tncol) // gets all nodes into the AddressSpaceTree
            {
                Application.DoEvents();

                BacnetObjectId object_id = (BacnetObjectId)tn.Tag;

                if (Filter.Contains(object_id.type)) // Only for some objects
                {

                    String Identifier = null;

                    lock (yabeFrm.DevicesObjectsName) // translate to it's name if already known
                        yabeFrm.DevicesObjectsName.TryGetValue(new Tuple<String, BacnetObjectId>(adr.FullHashString(deviceId), object_id), out Identifier);

                    try
                    {

                        IList<BacnetValue> value;
                        // read COV_Increment property on all objects (maybe a test could be done to avoid call without interest)   
                        bool ret = client.ReadPropertyRequest(adr, object_id, BacnetPropertyIds.PROP_COV_INCREMENT, out value);
                        string Increment = value[0].Value.ToString();

                        if (ret)
                        {

                            string Units = "";
                            try
                            {
                                // read Units property on all objects (maybe a test could be done to avoid call without interest)   
                                ret = client.ReadPropertyRequest(adr, object_id, BacnetPropertyIds.PROP_UNITS, out value);
                                Units = ((BacnetUnitsId)((uint)value[0].Value)).ToString();
                                if (Units.StartsWith("UNITS_"))
                                    Units = Units.Substring(6);
                                Units = Units.Replace("_", " ");
                                Units = System.Threading.Thread.CurrentThread.CurrentCulture.TextInfo.ToTitleCase(Units.ToLower());
                            }
                            catch { };


                            IsEmpty = false;

                            string name = object_id.ToString();
                            if (name.StartsWith("OBJECT_"))
                                name = name.Substring(7);

                            TreeNode N;
                            if (Identifier != null)
                                N = treeView1.Nodes.Add(Identifier + " (" + System.Threading.Thread.CurrentThread.CurrentCulture.TextInfo.ToTitleCase(name.ToLower()) + ")");
                            else
                                N = treeView1.Nodes.Add(name);
                            ret = client.ReadPropertyRequest(adr, object_id, BacnetPropertyIds.PROP_DESCRIPTION, out value); // with Description
                            if (ret)
                                N.Nodes.Add(value[0].Value.ToString());
                            N.Nodes.Add(("Value = ") + Increment + " " + Units);

                        }
                    }
                    catch
                    {
                    }
                }

                if (tn.Nodes != null)   // go deap into the tree
                    CheckAllObjects(tn.Nodes);
            }
        }

        private void treeView1_AfterSelect(object sender, TreeViewEventArgs e)
        {

        }
    }
}
