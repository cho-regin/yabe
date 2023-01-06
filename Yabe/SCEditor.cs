/**************************************************************************
*                           MIT License
* 
* Copyright (C) 2022 Frederic Chaxel <fchaxel@free.fr>
* Yabe SourceForge Explorer and Full Open source BACnet stack
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
using System.Diagnostics;
using System.IO;
using System.IO.BACnet;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Windows.Forms;
using System.Xml.Serialization;

namespace Yabe
{
    public partial class SCEditor : Form
    {
        // Password not saved in the XML file between Yabe session
        public static String YabeCertificateFilePassword;

        BACnetSCConfigChannel config;
        String FileName;
        XmlSerializer Xmlser = new XmlSerializer(typeof(BACnetSCConfigChannel));
        public SCEditor(String FileName)
        {
            InitializeComponent();

            this.FileName = FileName;

            try
            {
                using (StreamReader sr = new StreamReader(FileName))
                    config = (BACnetSCConfigChannel)Xmlser.Deserialize(sr);

                UUID.Text = config.UUID;
                HubURI.Text = config.primaryHubURI;
                HubCert.Text = config.HubCertificateFile;
                YabeCert.Text = config.OwnCertificateFile;
                chk_VerifyHubCert.Checked = config.ValidateHubCertificate;
                chk_DirectConnect.Checked = config.DirectConnect;

                YabeCertPassword.Text = YabeCertificateFilePassword;

            }
            catch
            {
                Trace.TraceError("Error with BACnet/SC configuration file");
            }
        }

        private void btSave_Click(object sender, EventArgs e)
        {
            config.UUID = UUID.Text;
            config.primaryHubURI = HubURI.Text;
            config.HubCertificateFile = HubCert.Text;

            config.OwnCertificateFile = YabeCert.Text;
            config.ValidateHubCertificate = chk_VerifyHubCert.Checked;
            config.DirectConnect = chk_DirectConnect.Checked;

            // Password is put into the config object but not saved in the Xml file.
            YabeCertificateFilePassword = YabeCertPassword.Text; 

            try
            {
                using (StreamWriter sw = new StreamWriter(FileName))
                    Xmlser.Serialize(sw, config);

                this.Close();
            }
            catch { Trace.TraceError("Error with BACnet/SC configuration file"); }
        }

        private void viewCert_Click(object sender, EventArgs e)
        {
            try
            {
                if (sender == viewYabeCert)
                    X509Certificate2UI.DisplayCertificate(new X509Certificate2(YabeCert.Text));
                else
                    X509Certificate2UI.DisplayCertificate(new X509Certificate2(HubCert.Text));
            }
            catch
            {
                Trace.TraceError("Error with certificate file");
            }
        }

        private void SelCert_Click(object sender, EventArgs e)
        {

            OpenFileDialog dlg = new OpenFileDialog();
            dlg.InitialDirectory = Path.GetDirectoryName(Properties.Settings.Default.Auto_Store_Object_Names_File);

            if (sender == SelYabeCert)
            {
                dlg.DefaultExt = "p12";
                dlg.Filter = "PKCS#12 (*.p12)|*.p12|PEM + key (*.pem)|*.pem|All files (*.*)|*.*";
                if (dlg.ShowDialog(this) != System.Windows.Forms.DialogResult.OK) return;
                YabeCert.Text = dlg.FileName;
            }
            else
            {
                dlg.DefaultExt = "crt";
                dlg.Filter = "PEM (*.crt)|*.crt|DER (*.der)|*.der|PEM (*.pem)|*.pem|All files (*.*)|*.*";
                if (dlg.ShowDialog(this) != System.Windows.Forms.DialogResult.OK) return;
                HubCert.Text = dlg.FileName;
            }
        }

        private void chk_DirectConnect_CheckedChanged(object sender, EventArgs e)
        {
            if (chk_DirectConnect.Checked)
            {
                lblRemoteCertificate.Text = "Device or CA Certificate File";
                lblUri.Text = "Device URI";
            }
            else
            {
                lblRemoteCertificate.Text = "Hub or CA Certificate File";
                lblUri.Text = "Hub URI";
            }
        }

        private void GetRemoteCertificate_Click(object sender, EventArgs e)
        {
            try
            {
                Uri uri = new Uri(HubURI.Text); // WebSocket ctor fail if the string contains spaces, so use this to clean the path
                WebSocketSharp.WebSocket Websocket = new WebSocketSharp.WebSocket(uri.ToString());
                Websocket.SslConfiguration.EnabledSslProtocols = SslProtocols.Tls13;
                Websocket.SslConfiguration.ServerCertificateValidationCallback = GetServerCertificate;

                Websocket.ConnectAsync();
            }
            catch{}

        }
        public bool GetServerCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            // Only here to get and Display the certificate
            X509Certificate2UI.DisplayCertificate((X509Certificate2)certificate);
            return false; // reject, so close the connection
        }
    }

}
