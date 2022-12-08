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
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.UUID = new System.Windows.Forms.TextBox();
            this.HubURI = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.SelYabeCert = new System.Windows.Forms.Button();
            this.viewYabeCert = new System.Windows.Forms.Button();
            this.btSave = new System.Windows.Forms.Button();
            this.YabeCert = new System.Windows.Forms.TextBox();
            this.HubCert = new System.Windows.Forms.TextBox();
            this.viewHubCert = new System.Windows.Forms.Button();
            this.SelHubCert = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // chk_VerifyHubCert
            // 
            this.chk_VerifyHubCert.AutoSize = true;
            this.chk_VerifyHubCert.Location = new System.Drawing.Point(23, 255);
            this.chk_VerifyHubCert.Name = "chk_VerifyHubCert";
            this.chk_VerifyHubCert.Size = new System.Drawing.Size(125, 17);
            this.chk_VerifyHubCert.TabIndex = 0;
            this.chk_VerifyHubCert.Text = "Verify Hub Certificate";
            this.chk_VerifyHubCert.UseVisualStyleBackColor = true;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(20, 36);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(49, 13);
            this.label1.TabIndex = 1;
            this.label1.Text = "Hub URI";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(20, 77);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(62, 13);
            this.label2.TabIndex = 2;
            this.label2.Text = "Yabe UUID";
            // 
            // UUID
            // 
            this.UUID.Location = new System.Drawing.Point(111, 74);
            this.UUID.Name = "UUID";
            this.UUID.Size = new System.Drawing.Size(210, 20);
            this.UUID.TabIndex = 3;
            // 
            // HubURI
            // 
            this.HubURI.Location = new System.Drawing.Point(111, 33);
            this.HubURI.Name = "HubURI";
            this.HubURI.Size = new System.Drawing.Size(210, 20);
            this.HubURI.TabIndex = 4;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(20, 136);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(181, 13);
            this.label3.TabIndex = 5;
            this.label3.Text = "Yabe Certificate File, with private key";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(20, 188);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(99, 13);
            this.label4.TabIndex = 6;
            this.label4.Text = "HUB Certificate File";
            // 
            // SelYabeCert
            // 
            this.SelYabeCert.Location = new System.Drawing.Point(210, 125);
            this.SelYabeCert.Name = "SelYabeCert";
            this.SelYabeCert.Size = new System.Drawing.Size(52, 22);
            this.SelYabeCert.TabIndex = 9;
            this.SelYabeCert.Text = "Select";
            this.SelYabeCert.UseVisualStyleBackColor = true;
            this.SelYabeCert.Click += new System.EventHandler(this.SelCert_Click);
            // 
            // viewYabeCert
            // 
            this.viewYabeCert.Location = new System.Drawing.Point(270, 125);
            this.viewYabeCert.Name = "viewYabeCert";
            this.viewYabeCert.Size = new System.Drawing.Size(51, 22);
            this.viewYabeCert.TabIndex = 10;
            this.viewYabeCert.Text = "View";
            this.viewYabeCert.UseVisualStyleBackColor = true;
            this.viewYabeCert.Click += new System.EventHandler(this.viewCert_Click);
            // 
            // btSave
            // 
            this.btSave.Location = new System.Drawing.Point(124, 299);
            this.btSave.Name = "btSave";
            this.btSave.Size = new System.Drawing.Size(77, 22);
            this.btSave.TabIndex = 13;
            this.btSave.Text = "Save";
            this.btSave.UseVisualStyleBackColor = true;
            this.btSave.Click += new System.EventHandler(this.btSave_Click);
            // 
            // YabeCert
            // 
            this.YabeCert.Location = new System.Drawing.Point(23, 152);
            this.YabeCert.Name = "YabeCert";
            this.YabeCert.ReadOnly = true;
            this.YabeCert.Size = new System.Drawing.Size(298, 20);
            this.YabeCert.TabIndex = 14;
            // 
            // HubCert
            // 
            this.HubCert.Location = new System.Drawing.Point(23, 206);
            this.HubCert.Name = "HubCert";
            this.HubCert.ReadOnly = true;
            this.HubCert.Size = new System.Drawing.Size(298, 20);
            this.HubCert.TabIndex = 15;
            // 
            // viewHubCert
            // 
            this.viewHubCert.Location = new System.Drawing.Point(270, 183);
            this.viewHubCert.Name = "viewHubCert";
            this.viewHubCert.Size = new System.Drawing.Size(51, 22);
            this.viewHubCert.TabIndex = 17;
            this.viewHubCert.Text = "View";
            this.viewHubCert.UseVisualStyleBackColor = true;
            this.viewHubCert.Click += new System.EventHandler(this.viewCert_Click);
            // 
            // SelHubCert
            // 
            this.SelHubCert.Location = new System.Drawing.Point(210, 183);
            this.SelHubCert.Name = "SelHubCert";
            this.SelHubCert.Size = new System.Drawing.Size(52, 22);
            this.SelHubCert.TabIndex = 16;
            this.SelHubCert.Text = "Select";
            this.SelHubCert.UseVisualStyleBackColor = true;
            this.SelHubCert.Click += new System.EventHandler(this.SelCert_Click);
            // 
            // SCEditor
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(333, 343);
            this.Controls.Add(this.viewHubCert);
            this.Controls.Add(this.SelHubCert);
            this.Controls.Add(this.HubCert);
            this.Controls.Add(this.YabeCert);
            this.Controls.Add(this.btSave);
            this.Controls.Add(this.viewYabeCert);
            this.Controls.Add(this.SelYabeCert);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.HubURI);
            this.Controls.Add(this.UUID);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.chk_VerifyHubCert);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "SCEditor";
            this.ShowIcon = false;
            this.Text = "BACnet/SC Channel Configuration";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.CheckBox chk_VerifyHubCert;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox UUID;
        private System.Windows.Forms.TextBox HubURI;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Button SelYabeCert;
        private System.Windows.Forms.Button viewYabeCert;
        private System.Windows.Forms.Button btSave;
        private System.Windows.Forms.TextBox YabeCert;
        private System.Windows.Forms.TextBox HubCert;
        private System.Windows.Forms.Button viewHubCert;
        private System.Windows.Forms.Button SelHubCert;
    }
}