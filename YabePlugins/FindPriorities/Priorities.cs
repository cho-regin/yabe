/**************************************************************************
*                           MIT License
* 
* Copyright (C) 2019 Frank Schubert 
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

namespace FindPriorities
{
    public partial class Priorities : Form
    {
        YabeMainDialog yabeFrm;
        BacnetObjectId objId;
        BACnetDevice device;

        public Priorities(YabeMainDialog yabeFrm)
        {
            this.yabeFrm = yabeFrm;
            Icon = yabeFrm.Icon; // gets Yabe Icon
            InitializeComponent();
        }

        private void FindPriorities_Load(object sender, EventArgs e)
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
                yabeFrm.GetObjectLink(out device, out objId, BacnetObjectTypes.MAX_BACNET_OBJECT_TYPE);
                Devicename.Text = device.BacAdr.ToString();

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

                try
                {
                    IList<BacnetValue> value;
                    // read PriorityArray property on all objects (maybe a test could be done to avoid call without interest)   
                    bool ret = device.channel.ReadPropertyRequest(device.BacAdr, object_id, BacnetPropertyIds.PROP_PRIORITY_ARRAY, out value);

                    if (ret)
                    {
                        int i;
                        bool bFirst = true;
                        bool bFound = false;
                        string sOutput = "";
                        TreeNode N;
                        string name = object_id.ToString();

                        if (name.StartsWith("OBJECT_"))
                            name = name.Substring(7);

                        for (i = 0; i < 16; i++)
                        {
                            if (null != value[i].Value)
                            {
                                IsEmpty = false;
                                bFound = true;
                                if( bFirst )
                                {
                                    bFirst = false;
                                    sOutput = name;
                                    sOutput = string.Concat( sOutput, ":", "     " );
                                }
                                sOutput = string.Concat( sOutput, " ", i + 1 );
                            }
                        }

                        if (bFound)
                        {
                            N = treeView1.Nodes.Add( sOutput );
                        }
                    }
                }
                catch
                {
                }

                if (tn.Nodes != null)   // go deap into the tree
                    CheckAllObjects(tn.Nodes);
            }
        }

        private void EmptyList_Click(object sender, EventArgs e)
        {

        }
    }
}
