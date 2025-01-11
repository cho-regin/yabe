﻿namespace Yabe
{
    partial class AlarmSummary
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(AlarmSummary));
            this.AckText = new System.Windows.Forms.TextBox();
            this.LblInfo = new System.Windows.Forms.Label();
            this.AckBt = new System.Windows.Forms.Button();
            this.TAlarmList = new CodersLab.Windows.Controls.TreeView();
            this.label1 = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // AckText
            // 
            this.AckText.Enabled = false;
            this.AckText.Location = new System.Drawing.Point(288, 405);
            this.AckText.Name = "AckText";
            this.AckText.Size = new System.Drawing.Size(144, 20);
            this.AckText.TabIndex = 2;
            this.AckText.Text = "Ack by Yabe";
            // 
            // LblInfo
            // 
            this.LblInfo.BackColor = System.Drawing.SystemColors.ButtonHighlight;
            this.LblInfo.Location = new System.Drawing.Point(36, 109);
            this.LblInfo.Name = "LblInfo";
            this.LblInfo.Size = new System.Drawing.Size(302, 15);
            this.LblInfo.TabIndex = 3;
            this.LblInfo.Text = "Loading alarms, please wait ...";
            this.LblInfo.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // AckBt
            // 
            this.AckBt.Enabled = false;
            this.AckBt.Location = new System.Drawing.Point(12, 403);
            this.AckBt.Name = "AckBt";
            this.AckBt.Size = new System.Drawing.Size(145, 23);
            this.AckBt.TabIndex = 4;
            this.AckBt.Text = "Ack selected alarm(s)";
            this.AckBt.UseVisualStyleBackColor = true;
            this.AckBt.Click += new System.EventHandler(this.AckBt_Click);
            // 
            // TAlarmList
            // 
            this.TAlarmList.Location = new System.Drawing.Point(12, 31);
            this.TAlarmList.Name = "TAlarmList";
            this.TAlarmList.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            this.TAlarmList.SelectionMode = CodersLab.Windows.Controls.TreeViewSelectionMode.MultiSelect;
            this.TAlarmList.ShowNodeToolTips = true;
            this.TAlarmList.Size = new System.Drawing.Size(420, 357);
            this.TAlarmList.TabIndex = 1;
            this.TAlarmList.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.TAlarmList_AfterSelect);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.ForeColor = System.Drawing.SystemColors.ControlText;
            this.label1.Location = new System.Drawing.Point(12, 9);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(87, 13);
            this.label1.TabIndex = 5;
            this.label1.Text = "Key F4 to refresh";
            // 
            // AlarmSummary
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(444, 440);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.AckBt);
            this.Controls.Add(this.LblInfo);
            this.Controls.Add(this.AckText);
            this.Controls.Add(this.TAlarmList);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.KeyPreview = true;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "AlarmSummary";
            this.Text = "Active Alarms on Device";
            this.Shown += new System.EventHandler(this.AlarmSummary_Shown);
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.AlarmSummary_KeyDown);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private CodersLab.Windows.Controls.TreeView TAlarmList;
        private System.Windows.Forms.TextBox AckText;
        private System.Windows.Forms.Label LblInfo;
        private System.Windows.Forms.Button AckBt;
        private System.Windows.Forms.Label label1;
    }
}