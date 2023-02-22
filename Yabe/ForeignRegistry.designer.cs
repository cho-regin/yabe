﻿namespace Yabe
{
    partial class ForeignRegistry
    {
        /// <summary>
        /// Variable nécessaire au concepteur.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Nettoyage des ressources utilisées.
        /// </summary>
        /// <param name="disposing">true si les ressources managées doivent être supprimées ; sinon, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Code généré par le Concepteur Windows Form

        /// <summary>
        /// Méthode requise pour la prise en charge du concepteur - ne modifiez pas
        /// le contenu de cette méthode avec l'éditeur de code.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ForeignRegistry));
            this.BBMD_IP = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.sendFDR = new System.Windows.Forms.Button();
            this.SendWhois = new System.Windows.Forms.Button();
            this.BBMD_Port = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.TTL_Input = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // BBMD_IP
            // 
            this.BBMD_IP.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.BBMD_IP.Location = new System.Drawing.Point(21, 41);
            this.BBMD_IP.Name = "BBMD_IP";
            this.BBMD_IP.Size = new System.Drawing.Size(114, 20);
            this.BBMD_IP.TabIndex = 0;
            this.BBMD_IP.KeyDown += new System.Windows.Forms.KeyEventHandler(this.BBMD_IP_KeyDown);
            // 
            // label1
            // 
            this.label1.Location = new System.Drawing.Point(14, 17);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(187, 21);
            this.label1.TabIndex = 1;
            this.label1.Text = "Remote BBMD IPv4, IPv6 Endpoint";
            this.label1.TextAlign = System.Drawing.ContentAlignment.TopCenter;
            // 
            // sendFDR
            // 
            this.sendFDR.Location = new System.Drawing.Point(21, 108);
            this.sendFDR.Name = "sendFDR";
            this.sendFDR.Size = new System.Drawing.Size(171, 27);
            this.sendFDR.TabIndex = 2;
            this.sendFDR.Text = "Register";
            this.sendFDR.UseVisualStyleBackColor = true;
            this.sendFDR.Click += new System.EventHandler(this.sendFDR_Click);
            // 
            // SendWhois
            // 
            this.SendWhois.Enabled = false;
            this.SendWhois.Location = new System.Drawing.Point(21, 152);
            this.SendWhois.Name = "SendWhois";
            this.SendWhois.Size = new System.Drawing.Size(171, 27);
            this.SendWhois.TabIndex = 3;
            this.SendWhois.Text = "Send Remote Whois";
            this.SendWhois.UseVisualStyleBackColor = true;
            this.SendWhois.Click += new System.EventHandler(this.SendWhois_Click);
            // 
            // BBMD_Port
            // 
            this.BBMD_Port.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.BBMD_Port.Location = new System.Drawing.Point(141, 41);
            this.BBMD_Port.Name = "BBMD_Port";
            this.BBMD_Port.Size = new System.Drawing.Size(51, 20);
            this.BBMD_Port.TabIndex = 4;
            this.BBMD_Port.Text = "47808";
            this.BBMD_Port.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // label2
            // 
            this.label2.Location = new System.Drawing.Point(14, 73);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(103, 21);
            this.label2.TabIndex = 5;
            this.label2.Text = "Registration Time:";
            // 
            // TTL_Input
            // 
            this.TTL_Input.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.TTL_Input.Location = new System.Drawing.Point(109, 71);
            this.TTL_Input.Name = "TTL_Input";
            this.TTL_Input.Size = new System.Drawing.Size(38, 20);
            this.TTL_Input.TabIndex = 4;
            this.TTL_Input.Text = "30";
            this.TTL_Input.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // label3
            // 
            this.label3.Location = new System.Drawing.Point(151, 73);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(44, 21);
            this.label3.TabIndex = 6;
            this.label3.Text = "minutes";
            // 
            // ForeignRegistry
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(223, 201);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.TTL_Input);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.BBMD_Port);
            this.Controls.Add(this.SendWhois);
            this.Controls.Add(this.sendFDR);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.BBMD_IP);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "ForeignRegistry";
            this.ShowInTaskbar = false;
            this.Text = "ForeignRegistry";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox BBMD_IP;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button sendFDR;
        private System.Windows.Forms.Button SendWhois;
        private System.Windows.Forms.TextBox BBMD_Port;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox TTL_Input;
        private System.Windows.Forms.Label label3;
    }
}