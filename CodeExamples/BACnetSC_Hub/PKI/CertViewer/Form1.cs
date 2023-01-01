using System;
using System.Windows.Forms;
using System.Security.Cryptography.X509Certificates;
using System.Drawing;
using System.Linq;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Net.Security;
using System.Threading;
using System.Net;
using System.Text;
using Asn1;

namespace CertViewer
{
    public partial class Form1 : Form
    {
        readonly Font FontBold = new Font("Tahoma", 10, FontStyle.Bold);
        readonly Font FontRegular = new Font("Tahoma", 10, FontStyle.Regular);

        public Form1(String CertFile, String Password)
        {           
            InitializeComponent();
            richTextBox1.SelectionTabs = new int[] { 10, 150, 250 };
            txtPasswd.Text = Password;
            if (CertFile != null) GetCert(CertFile);
        }

        private void Form1_DragDrop(object sender, DragEventArgs e)
        {
            certTree.Nodes.Clear();
            richTextBox1.Clear();
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files.Length < 1) return;
            GetCert(files[0]);
        }

        private bool ValidateServerCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            Invoke(new Action<X509Chain>(DisplayChain), chain); // BeginInvoke is not working, the ref on chain is modified after
            return false;
        }     
        private void GetCert(Uri uri)
        {
            ThreadPool.QueueUserWorkItem( (_) =>
            {
                try
                {
                    TcpClient client = new TcpClient();
                    client.Connect(uri.Host, uri.Port);
                    SslStream sslStream = new SslStream(
                        client.GetStream(),
                        false,
                        new RemoteCertificateValidationCallback(ValidateServerCertificate)
                      );
                   
                    sslStream.AuthenticateAsClient(uri.Host);
                }
                catch { }
            }
            );
            return;
        }

        private void GetCert(string File)
        {

            try
            {
                X509Certificate2 cert = new X509Certificate2(File, txtPasswd.Text);
                X509Chain chain = new X509Chain();
                chain.Build(cert);  // Why we do not get the full chain even if the p12 file contains it ???

                DisplayChain(chain);
            }
            catch
            {
                tmrError.Enabled = true;
                lblError.Visible = true;
            }
        }

        private void DisplayChain(X509Chain chain)
        {
            TreeNodeCollection tnc = certTree.Nodes;
            TreeNode tn = null;

            for (int i = chain.ChainElements.Count - 1; i >= 0; i--)
            {
                X509ChainElement elem = chain.ChainElements[i];

                string s = elem.Certificate.Subject.Split(',').First((st) => st.Contains("CN=")).Remove(0, 3);

                tn = new TreeNode(s);
                tn.Tag = elem.Certificate;
                tnc.Add(tn);
                tnc = tn.Nodes;

            }

            certTree.ExpandAll();

            certTree.SelectedNode = tn;
        }

        private void Form1_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop)) e.Effect = DragDropEffects.Copy;
        }

        private void tmrError_Tick(object sender, EventArgs e)
        {
            tmrError.Enabled = false;
            lblError.Visible = false;
        }

        private void certTree_BeforeCollapse(object sender, TreeViewCancelEventArgs e)
        {
            e.Cancel = true;
        }

        // https://docs.oracle.com/cd/E24191_01/common/tutorials/authz_cert_attributes.html
        Dictionary<String, String> X509Fields = new Dictionary<string, string>
        {
            {"O",  "Organisation Name"},
            {"CN", "Common Name" },
            {"L",  "Locality Name"},
            {"C",  "Country Name"},
            {"OU", "Organisation Unit Name" },
            {"S", "State, Province Name"}
        };
      
        private List<KeyValuePair<string, string>> DecompSubject(String subject)
        {
            var ret = new List<KeyValuePair<string, string>>();

            String[] elems = subject.Split(',');

            foreach (var elem in elems)
            {
                String[] content = elem.Split('=');

                content[0] = content[0].TrimStart(' ').TrimEnd(' ');

                string ValName;
                if (X509Fields.TryGetValue(content[0], out ValName) == true)
                    content[0] = ValName;

                ret.Add(new KeyValuePair<string, string>(content[0], content[1]));
            }

            return ret;
        }

        private void DisplayCertificate(X509Certificate2 cert)
        {
            X509Chain ch = new X509Chain();
            ch.Build(cert);
            richTextBox1.SelectionFont = FontBold;
            richTextBox1.AppendText("Certificate Status\r\n");
            richTextBox1.SelectionFont = FontRegular;

            if (ch.ChainStatus.Length==0)
                richTextBox1.AppendText("\tCertificate is Valid according to the system policy\r\n");
            else
            { 
                foreach (var st in ch.ChainStatus)
                    richTextBox1.AppendText("\t"+st.StatusInformation);
            }

            richTextBox1.AppendText("   __________________________________________________________________________________\r\n\r\n");

            richTextBox1.SelectionFont = FontBold;
            richTextBox1.AppendText("Subject Name\r\n");
            richTextBox1.SelectionFont = FontRegular;
            var Subject = DecompSubject(cert.Subject);

            foreach (var e in Subject)
                richTextBox1.AppendText("\t"+e.Key+"\t "+e.Value+"\r\n");

            richTextBox1.AppendText("   __________________________________________________________________________________\r\n\r\n");
            richTextBox1.SelectionFont = FontBold;
            richTextBox1.AppendText("Issuer Name\r\n");
            richTextBox1.SelectionFont = FontRegular;

            var Issuer = DecompSubject(cert.Issuer);
            foreach (var e in Issuer)
                richTextBox1.AppendText("\t" + e.Key + "\t " + e.Value + "\r\n");

            richTextBox1.AppendText("   __________________________________________________________________________________\r\n\r\n");
            richTextBox1.SelectionFont = FontBold;
            richTextBox1.AppendText("Validity\r\n");
            richTextBox1.SelectionFont = FontRegular;
            richTextBox1.AppendText("\tNot Before\t " + cert.NotBefore + "\r\n");
            richTextBox1.AppendText("\tNot After\t " + cert.NotAfter + "\r\n");

            richTextBox1.AppendText("   __________________________________________________________________________________\r\n\r\n");
            richTextBox1.SelectionFont = FontBold;
            richTextBox1.AppendText("Public Key Informations\r\n");
            richTextBox1.SelectionFont = FontRegular;
            richTextBox1.AppendText("\tAlgorithm\t " + cert.PublicKey.EncodedParameters.Oid.FriendlyName + "\r\n");
            richTextBox1.AppendText("\tPublic value\t " + cert.GetPublicKeyString().Substring(0,30)+ "...\r\n");

            richTextBox1.AppendText("   __________________________________________________________________________________\r\n\r\n");
            richTextBox1.SelectionFont = FontBold;
            richTextBox1.AppendText("Various Informations\r\n");
            richTextBox1.SelectionFont = FontRegular;
            richTextBox1.AppendText("\tSerial Number\t " + cert.GetSerialNumberString() + "\r\n");
            richTextBox1.AppendText("\tSignature Algorithm\t " + cert.SignatureAlgorithm.FriendlyName + "\r\n");
            richTextBox1.AppendText("\tVersion\t " + cert.Version.ToString() + "\r\n");
            richTextBox1.AppendText("\tHas private Key\t " + cert.HasPrivateKey.ToString() + "\r\n");
            richTextBox1.AppendText("\tSHA1 Thumbprint\t " + cert.Thumbprint + "\r\n");

            if (cert.Extensions.Count > 0)
            {
                richTextBox1.AppendText("   __________________________________________________________________________________\r\n\r\n");
                richTextBox1.SelectionFont = FontBold;
                richTextBox1.AppendText("Certificate Extensions\r\n");
                richTextBox1.SelectionFont = FontRegular;

                foreach (X509Extension ex in cert.Extensions)
                {
                    if (ex is X509KeyUsageExtension)
                    {
                        var ex2 = (ex as X509KeyUsageExtension);
                        richTextBox1.AppendText("\tKey Usages\t " + ex2.KeyUsages + "\r\n");
                    }
                    else
                    if (ex is X509BasicConstraintsExtension)
                    {
                        var ex2 = (ex as X509BasicConstraintsExtension);
                        if (ex2.Oid.Value=="2.5.29.19")
                            richTextBox1.AppendText("\tCertificate Autority\t " + ex2.CertificateAuthority + "\r\n");
                    }
                    else
                    {
                        if (ex.Oid.Value == "2.5.29.17") // Alternative Name, https://www.alvestrand.no/objectid/2.5.29.17.html
                        {
                            richTextBox1.AppendText("\tAlternative Names\r\n");

                            try
                            {
                                var ret = Asn1.Asn1Sequence.ReadFrom(ex.RawData);
                                foreach (Asn1Node node in ret.Nodes[0].Nodes)
                                {
                                    // Tag value 1 for rfc822Name, 2 for DnsName, 6 for Uri : ISO 646, quite ASCII
                                    if (node.Is(Asn1TagClass.ContextDefined, 1) || (node.Is(Asn1TagClass.ContextDefined, 2)) || (node.Is(Asn1TagClass.ContextDefined, 6)))
                                    {
                                        string name = Encoding.ASCII.GetString((node as Asn1CustomNode).Data);
                                        richTextBox1.AppendText("\t\tDNS Name=" + name + "\r\n");
                                    }
                                    // IP v4 or v6
                                    if (node.Is(Asn1TagClass.ContextDefined, 7))
                                    {
                                        IPAddress ip = new IPAddress((node as Asn1CustomNode).Data);
                                        richTextBox1.AppendText("\t\tIP Address=" + ip.ToString() + "\r\n");
                                    }
                                }
                            }
                            catch { }
                        }
                        else
                        if (ex.Oid.Value == "2.5.29.14") // Subject key Identifier, https://www.alvestrand.no/objectid/2.5.29.14.html
                        {
                            richTextBox1.AppendText("\tSubject key Identifier\t"+ (ex as X509SubjectKeyIdentifierExtension).SubjectKeyIdentifier+"\r\n");
                        }
                        else
                            richTextBox1.AppendText("\t"+ex.Oid.Value+"\t " + ex.Oid.FriendlyName + "\r\n");
                    }
                }
            }
        }

        private void certTree_AfterSelect(object sender, TreeViewEventArgs e)
        {
            X509Certificate2 cert = e.Node.Tag as X509Certificate2;
            richTextBox1.Clear();
            DisplayCertificate(cert);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (certTree.SelectedNode == null) return;

            X509Certificate2 cert = certTree.SelectedNode.Tag as X509Certificate2;
            X509Certificate2UI.DisplayCertificate(cert);
        }

        private void contextMenuStrip1_Opening(object sender, System.ComponentModel.CancelEventArgs e)
        {
            String s=Clipboard.GetText();
            try
            {
                Uri uri = new Uri(s); // Validates the Uri
                contextMenuStrip1.Items[0].Text = "Get X509 from : "+uri.Host;
            }
            catch
            {
                contextMenuStrip1.Items[0].Text = "Get X509 from : <Put a valid URL in the Clipboard>";
            }
        }

        private void toolStripGetUri_Click(object sender, EventArgs e)
        {
            String s = Clipboard.GetText();
            try
            {
                certTree.Nodes.Clear();
                richTextBox1.Clear();

                Uri uri = new Uri(s); 
                GetCert(uri);
            }
            catch
            {
            }
        }

        private void toolStripAbout_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Copywrite F. Chaxel 2023\r\nYabe BACnet project on Soureforge","X509 Viewer",MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

    }
}
