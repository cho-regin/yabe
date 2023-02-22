namespace Yabe
{
    partial class SCEditor
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SCEditor));
            this.chk_VerifyHubCert = new System.Windows.Forms.CheckBox();
            this.lblUri = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.UUID = new System.Windows.Forms.TextBox();
            this.HubURI = new System.Windows.Forms.TextBox();
            this.btSave = new System.Windows.Forms.Button();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.label1 = new System.Windows.Forms.Label();
            this.YabeCertPassword = new System.Windows.Forms.TextBox();
            this.viewHubCert = new System.Windows.Forms.Button();
            this.SelHubCert = new System.Windows.Forms.Button();
            this.HubCert = new System.Windows.Forms.TextBox();
            this.YabeCert = new System.Windows.Forms.TextBox();
            this.viewYabeCert = new System.Windows.Forms.Button();
            this.SelYabeCert = new System.Windows.Forms.Button();
            this.lblRemoteCertificate = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.chk_DirectConnect = new System.Windows.Forms.CheckBox();
            this.bpGetX509 = new System.Windows.Forms.Button();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // chk_VerifyHubCert
            // 
            this.chk_VerifyHubCert.AutoSize = true;
            this.chk_VerifyHubCert.Location = new System.Drawing.Point(6, 205);
            this.chk_VerifyHubCert.Name = "chk_VerifyHubCert";
            this.chk_VerifyHubCert.Size = new System.Drawing.Size(142, 17);
            this.chk_VerifyHubCert.TabIndex = 0;
            this.chk_VerifyHubCert.Text = "Verify Remote Certificate";
            this.chk_VerifyHubCert.UseVisualStyleBackColor = true;
            // 
            // lblUri
            // 
            this.lblUri.AutoSize = true;
            this.lblUri.Location = new System.Drawing.Point(20, 69);
            this.lblUri.Name = "lblUri";
            this.lblUri.Size = new System.Drawing.Size(49, 13);
            this.lblUri.TabIndex = 1;
            this.lblUri.Text = "Hub URI";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(20, 26);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(62, 13);
            this.label2.TabIndex = 2;
            this.label2.Text = "Yabe UUID";
            // 
            // UUID
            // 
            this.UUID.Location = new System.Drawing.Point(111, 23);
            this.UUID.Name = "UUID";
            this.UUID.Size = new System.Drawing.Size(204, 20);
            this.UUID.TabIndex = 3;
            // 
            // HubURI
            // 
            this.HubURI.Location = new System.Drawing.Point(111, 66);
            this.HubURI.Name = "HubURI";
            this.HubURI.Size = new System.Drawing.Size(204, 20);
            this.HubURI.TabIndex = 4;
            // 
            // btSave
            // 
            this.btSave.Location = new System.Drawing.Point(118, 397);
            this.btSave.Name = "btSave";
            this.btSave.Size = new System.Drawing.Size(77, 22);
            this.btSave.TabIndex = 13;
            this.btSave.Text = "Save";
            this.btSave.UseVisualStyleBackColor = true;
            this.btSave.Click += new System.EventHandler(this.btSave_Click);
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.label1);
            this.groupBox1.Controls.Add(this.YabeCertPassword);
            this.groupBox1.Controls.Add(this.viewHubCert);
            this.groupBox1.Controls.Add(this.SelHubCert);
            this.groupBox1.Controls.Add(this.HubCert);
            this.groupBox1.Controls.Add(this.YabeCert);
            this.groupBox1.Controls.Add(this.viewYabeCert);
            this.groupBox1.Controls.Add(this.SelYabeCert);
            this.groupBox1.Controls.Add(this.lblRemoteCertificate);
            this.groupBox1.Controls.Add(this.label3);
            this.groupBox1.Controls.Add(this.chk_VerifyHubCert);
            this.groupBox1.Location = new System.Drawing.Point(12, 143);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(309, 236);
            this.groupBox1.TabIndex = 18;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Security ... required only with wss://";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(8, 97);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(102, 13);
            this.label1.TabIndex = 29;
            this.label1.Text = "Certificate password";
            // 
            // YabeCertPassword
            // 
            this.YabeCertPassword.Location = new System.Drawing.Point(132, 94);
            this.YabeCertPassword.Name = "YabeCertPassword";
            this.YabeCertPassword.PasswordChar = '*';
            this.YabeCertPassword.Size = new System.Drawing.Size(169, 20);
            this.YabeCertPassword.TabIndex = 28;
            // 
            // viewHubCert
            // 
            this.viewHubCert.Location = new System.Drawing.Point(252, 143);
            this.viewHubCert.Name = "viewHubCert";
            this.viewHubCert.Size = new System.Drawing.Size(51, 22);
            this.viewHubCert.TabIndex = 25;
            this.viewHubCert.Text = "View";
            this.viewHubCert.UseVisualStyleBackColor = true;
            this.viewHubCert.Click += new System.EventHandler(this.viewCert_Click);
            // 
            // SelHubCert
            // 
            this.SelHubCert.Location = new System.Drawing.Point(192, 143);
            this.SelHubCert.Name = "SelHubCert";
            this.SelHubCert.Size = new System.Drawing.Size(52, 22);
            this.SelHubCert.TabIndex = 24;
            this.SelHubCert.Text = "Select";
            this.SelHubCert.UseVisualStyleBackColor = true;
            this.SelHubCert.Click += new System.EventHandler(this.SelCert_Click);
            // 
            // HubCert
            // 
            this.HubCert.Location = new System.Drawing.Point(5, 165);
            this.HubCert.Name = "HubCert";
            this.HubCert.ReadOnly = true;
            this.HubCert.Size = new System.Drawing.Size(298, 20);
            this.HubCert.TabIndex = 23;
            // 
            // YabeCert
            // 
            this.YabeCert.Location = new System.Drawing.Point(5, 55);
            this.YabeCert.Name = "YabeCert";
            this.YabeCert.ReadOnly = true;
            this.YabeCert.Size = new System.Drawing.Size(298, 20);
            this.YabeCert.TabIndex = 22;
            // 
            // viewYabeCert
            // 
            this.viewYabeCert.Location = new System.Drawing.Point(250, 33);
            this.viewYabeCert.Name = "viewYabeCert";
            this.viewYabeCert.Size = new System.Drawing.Size(51, 22);
            this.viewYabeCert.TabIndex = 21;
            this.viewYabeCert.Text = "View";
            this.viewYabeCert.UseVisualStyleBackColor = true;
            this.viewYabeCert.Click += new System.EventHandler(this.viewCert_Click);
            // 
            // SelYabeCert
            // 
            this.SelYabeCert.Location = new System.Drawing.Point(192, 33);
            this.SelYabeCert.Name = "SelYabeCert";
            this.SelYabeCert.Size = new System.Drawing.Size(52, 22);
            this.SelYabeCert.TabIndex = 20;
            this.SelYabeCert.Text = "Select";
            this.SelYabeCert.UseVisualStyleBackColor = true;
            this.SelYabeCert.Click += new System.EventHandler(this.SelCert_Click);
            // 
            // lblRemoteCertificate
            // 
            this.lblRemoteCertificate.AutoSize = true;
            this.lblRemoteCertificate.Location = new System.Drawing.Point(2, 147);
            this.lblRemoteCertificate.Name = "lblRemoteCertificate";
            this.lblRemoteCertificate.Size = new System.Drawing.Size(128, 13);
            this.lblRemoteCertificate.TabIndex = 19;
            this.lblRemoteCertificate.Text = "HUB or CA Certificate File";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label3.Location = new System.Drawing.Point(2, 39);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(181, 13);
            this.label3.TabIndex = 18;
            this.label3.Text = "Yabe Certificate File, with private key";
            // 
            // chk_DirectConnect
            // 
            this.chk_DirectConnect.AutoSize = true;
            this.chk_DirectConnect.Location = new System.Drawing.Point(23, 98);
            this.chk_DirectConnect.Name = "chk_DirectConnect";
            this.chk_DirectConnect.Size = new System.Drawing.Size(97, 17);
            this.chk_DirectConnect.TabIndex = 26;
            this.chk_DirectConnect.Text = "Direct Connect";
            this.chk_DirectConnect.UseVisualStyleBackColor = true;
            this.chk_DirectConnect.CheckedChanged += new System.EventHandler(this.chk_DirectConnect_CheckedChanged);
            // 
            // bpGetX509
            // 
            this.bpGetX509.Location = new System.Drawing.Point(234, 92);
            this.bpGetX509.Name = "bpGetX509";
            this.bpGetX509.Size = new System.Drawing.Size(79, 23);
            this.bpGetX509.TabIndex = 27;
            this.bpGetX509.Text = "Try Get X509";
            this.bpGetX509.UseVisualStyleBackColor = true;
            this.bpGetX509.Click += new System.EventHandler(this.GetRemoteCertificate_Click);
            // 
            // SCEditor
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(333, 447);
            this.Controls.Add(this.bpGetX509);
            this.Controls.Add(this.chk_DirectConnect);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.HubURI);
            this.Controls.Add(this.UUID);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.lblUri);
            this.Controls.Add(this.btSave);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "SCEditor";
            this.ShowIcon = false;
            this.Text = "BACnet/SC Channel Configuration";
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.CheckBox chk_VerifyHubCert;
        private System.Windows.Forms.Label lblUri;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox UUID;
        private System.Windows.Forms.TextBox HubURI;
        private System.Windows.Forms.Button btSave;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Button viewHubCert;
        private System.Windows.Forms.Button SelHubCert;
        private System.Windows.Forms.TextBox HubCert;
        private System.Windows.Forms.TextBox YabeCert;
        private System.Windows.Forms.Button viewYabeCert;
        private System.Windows.Forms.Button SelYabeCert;
        private System.Windows.Forms.Label lblRemoteCertificate;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.CheckBox chk_DirectConnect;
        private System.Windows.Forms.Button bpGetX509;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox YabeCertPassword;
    }
}