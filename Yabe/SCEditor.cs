using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.BACnet;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Serialization;

namespace Yabe
{
    public partial class SCEditor : Form
    {
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

            if (sender==SelYabeCert)
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

    }
}
