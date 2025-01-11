﻿/**************************************************************************
*                           MIT License
* 
* Copyright (C) 2014 Morten Kvistgaard <mk@pch-engineering.dk>
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

namespace Yabe
{
    public partial class DeviceCommunicationControlDialog : Form
    {
        public DeviceCommunicationControlDialog()
        {
            InitializeComponent();
        }

        public bool IsReinitialize { get { return m_reinitializeRadio.Checked; } set { m_reinitializeRadio.Checked = value; } }
        public bool DisableCommunication { get { return m_disableCheck.Checked; } set { m_disableCheck.Checked = value; } }
        public uint Duration { get { return (uint)m_durationValue.Value; } set { m_durationValue.Value = value; } }
        public string Password { get { return m_passwordText.Text; } set { m_passwordText.Text = value; } }
        public System.IO.BACnet.BacnetReinitializedStates ReinitializeState { get { return (System.IO.BACnet.BacnetReinitializedStates)Enum.Parse(typeof(System.IO.BACnet.BacnetReinitializedStates), "BACNET_REINIT_" + m_StateCombo.Text); } set { m_StateCombo.Text = value.ToString(); } }

        private void reinitializeRadio_CheckedChanged(object sender, EventArgs e)
        {
            if (m_reinitializeRadio.Checked)
            {
                m_reinitializeGroup.Enabled = true;
                m_communicationGroup.Enabled = false;
            }
            else
            {
                m_reinitializeGroup.Enabled = false;
                m_communicationGroup.Enabled = true;
            }
        }

        private void DeviceCommunicationControlDialog_Load(object sender, EventArgs e)
        {
            string[] names = Enum.GetNames(typeof(System.IO.BACnet.BacnetReinitializedStates));

            for (int i = 0; i < names.Length - 1; i++) // BACNET_REINIT_IDLE is not puts into the combo list
                m_StateCombo.Items.Add(names[i].Replace("BACNET_REINIT_", ""));

            m_StateCombo.Text = m_StateCombo.Items[0].ToString();
        }
    }
}
