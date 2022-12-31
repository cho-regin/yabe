using System;
using System.Windows.Forms;
using System.Security.Cryptography.X509Certificates;

namespace CertViewer
{
    public partial class Form1 : Form
    {
        public Form1(String CertFile)
        {
            InitializeComponent();
            if (CertFile != null) GetCert(CertFile);
        }

        private void Form1_DragDrop(object sender, DragEventArgs e)
        {
            certTree.Nodes.Clear();
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files.Length < 1) return;
            GetCert(files[0]);
        }

        private void GetCert(string File)
        {

            try
            {
                X509Certificate2 cert = new X509Certificate2(File);
                X509Chain chain = new X509Chain();
                chain.Build(cert);

                TreeNodeCollection tnc = certTree.Nodes;
                TreeNode tn = null;

                for (int i = chain.ChainElements.Count - 1; i >= 0; i--)
                {
                    X509ChainElement elem = chain.ChainElements[i];

                    string s = elem.Certificate.Subject.Split(',')[0].Remove(0, 3); // delete CN=

                    tn = new TreeNode(s); 
                    tn.Tag = elem.Certificate;
                    tnc.Add(tn);
                    tnc = tn.Nodes;

                }

                certTree.ExpandAll();
            }
            catch
            {
                tmrError.Enabled = true;
                lblError.Visible = true;
            }

        }
        private void Form1_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop)) e.Effect = DragDropEffects.Copy;
        }

        private void tmrError_Tick(object sender, EventArgs e)
        {
            lblCopyright.Text = "Drop a Certificate File to Edit it";
            tmrError.Enabled = false;
            lblError.Visible = false;
        }

        private void certTree_BeforeCollapse(object sender, TreeViewCancelEventArgs e)
        {
            e.Cancel = true;
        }

        private void certTree_MouseUp(object sender, MouseEventArgs e)
        {
            TreeNode tn=certTree.GetNodeAt(e.Location);

            if (tn!=null)
            {
                X509Certificate2 cert = tn.Tag as X509Certificate2;
                X509Certificate2UI.DisplayCertificate(cert);
            }
        }
    }
}
