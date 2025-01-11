﻿namespace Yabe
{
    partial class ScheduleDisplay
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
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ScheduleDisplay));
            this.TxtStartDate = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.TxtEndDate = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.Schedule = new CodersLab.Windows.Controls.TreeView();
            this.contextMenuStrip = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.modifyToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.deleteToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.addToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.copyToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.pasteToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.imageList = new System.Windows.Forms.ImageList(this.components);
            this.UpdateButton = new System.Windows.Forms.Button();
            this.StartDatePicker = new System.Windows.Forms.Button();
            this.EndDatePicker = new System.Windows.Forms.Button();
            this.label4 = new System.Windows.Forms.Label();
            this.listReferences = new System.Windows.Forms.ListView();
            this.label5 = new System.Windows.Forms.Label();
            this.TxtScheduleDefault = new System.Windows.Forms.TextBox();
            this.label6 = new System.Windows.Forms.Label();
            this.ScheduleDataType = new System.Windows.Forms.ComboBox();
            this.label7 = new System.Windows.Forms.Label();
            this.contextMenuStrip.SuspendLayout();
            this.SuspendLayout();
            // 
            // TxtStartDate
            // 
            this.TxtStartDate.Location = new System.Drawing.Point(22, 35);
            this.TxtStartDate.Name = "TxtStartDate";
            this.TxtStartDate.Size = new System.Drawing.Size(100, 20);
            this.TxtStartDate.TabIndex = 1;
            this.TxtStartDate.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.TxtDate_KeyPress);
            this.TxtStartDate.Validated += new System.EventHandler(this.TxtDate_Validated);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(19, 19);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(91, 13);
            this.label1.TabIndex = 2;
            this.label1.Text = "Validity Start Date";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(226, 19);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(88, 13);
            this.label2.TabIndex = 3;
            this.label2.Text = "Validity End Date";
            // 
            // TxtEndDate
            // 
            this.TxtEndDate.Location = new System.Drawing.Point(229, 35);
            this.TxtEndDate.Name = "TxtEndDate";
            this.TxtEndDate.Size = new System.Drawing.Size(100, 20);
            this.TxtEndDate.TabIndex = 4;
            this.TxtEndDate.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.TxtDate_KeyPress);
            this.TxtEndDate.Validated += new System.EventHandler(this.TxtDate_Validated);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(19, 132);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(96, 13);
            this.label3.TabIndex = 6;
            this.label3.Text = "Weekly Schedules";
            // 
            // Schedule
            // 
            this.Schedule.ContextMenuStrip = this.contextMenuStrip;
            this.Schedule.ImageIndex = 0;
            this.Schedule.ImageList = this.imageList;
            this.Schedule.Location = new System.Drawing.Point(21, 148);
            this.Schedule.Name = "Schedule";
            this.Schedule.SelectedImageIndex = 0;
            this.Schedule.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            this.Schedule.SelectionMode = CodersLab.Windows.Controls.TreeViewSelectionMode.MultiSelect;
            this.Schedule.Size = new System.Drawing.Size(340, 269);
            this.Schedule.TabIndex = 7;
            this.Schedule.AfterLabelEdit += new System.Windows.Forms.NodeLabelEditEventHandler(this.Schedule_AfterLabelEdit);
            this.Schedule.DoubleClick += new System.EventHandler(this.modifyToolStripMenuItem_Click);
            this.Schedule.MouseDown += new System.Windows.Forms.MouseEventHandler(this.Schedule_MouseDown);
            // 
            // contextMenuStrip
            // 
            this.contextMenuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.modifyToolStripMenuItem,
            this.deleteToolStripMenuItem,
            this.addToolStripMenuItem,
            this.copyToolStripMenuItem,
            this.pasteToolStripMenuItem});
            this.contextMenuStrip.Name = "contextMenuStrip";
            this.contextMenuStrip.Size = new System.Drawing.Size(113, 114);
            // 
            // modifyToolStripMenuItem
            // 
            this.modifyToolStripMenuItem.Name = "modifyToolStripMenuItem";
            this.modifyToolStripMenuItem.Size = new System.Drawing.Size(112, 22);
            this.modifyToolStripMenuItem.Text = "Modify";
            this.modifyToolStripMenuItem.Click += new System.EventHandler(this.modifyToolStripMenuItem_Click);
            // 
            // deleteToolStripMenuItem
            // 
            this.deleteToolStripMenuItem.Name = "deleteToolStripMenuItem";
            this.deleteToolStripMenuItem.ShortcutKeys = System.Windows.Forms.Keys.Delete;
            this.deleteToolStripMenuItem.ShowShortcutKeys = false;
            this.deleteToolStripMenuItem.Size = new System.Drawing.Size(112, 22);
            this.deleteToolStripMenuItem.Text = "Delete";
            this.deleteToolStripMenuItem.Click += new System.EventHandler(this.deleteToolStripMenuItem_Click);
            // 
            // addToolStripMenuItem
            // 
            this.addToolStripMenuItem.Name = "addToolStripMenuItem";
            this.addToolStripMenuItem.ShortcutKeys = System.Windows.Forms.Keys.Insert;
            this.addToolStripMenuItem.ShowShortcutKeys = false;
            this.addToolStripMenuItem.Size = new System.Drawing.Size(112, 22);
            this.addToolStripMenuItem.Text = "Add";
            this.addToolStripMenuItem.Click += new System.EventHandler(this.addToolStripMenuItem_Click);
            // 
            // copyToolStripMenuItem
            // 
            this.copyToolStripMenuItem.Name = "copyToolStripMenuItem";
            this.copyToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.C)));
            this.copyToolStripMenuItem.ShowShortcutKeys = false;
            this.copyToolStripMenuItem.Size = new System.Drawing.Size(112, 22);
            this.copyToolStripMenuItem.Text = "Copy";
            this.copyToolStripMenuItem.Click += new System.EventHandler(this.copyToolStripMenuItem_Click);
            // 
            // pasteToolStripMenuItem
            // 
            this.pasteToolStripMenuItem.Name = "pasteToolStripMenuItem";
            this.pasteToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.V)));
            this.pasteToolStripMenuItem.ShowShortcutKeys = false;
            this.pasteToolStripMenuItem.Size = new System.Drawing.Size(112, 22);
            this.pasteToolStripMenuItem.Text = "Paste";
            this.pasteToolStripMenuItem.Click += new System.EventHandler(this.pasteToolStripMenuItem_Click);
            // 
            // imageList
            // 
            this.imageList.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("imageList.ImageStream")));
            this.imageList.TransparentColor = System.Drawing.Color.Transparent;
            this.imageList.Images.SetKeyName(0, "time_go.png");
            // 
            // UpdateButton
            // 
            this.UpdateButton.AutoSize = true;
            this.UpdateButton.Location = new System.Drawing.Point(233, 546);
            this.UpdateButton.Name = "UpdateButton";
            this.UpdateButton.Size = new System.Drawing.Size(128, 28);
            this.UpdateButton.TabIndex = 8;
            this.UpdateButton.Text = "Update && Read back";
            this.UpdateButton.UseVisualStyleBackColor = true;
            this.UpdateButton.Click += new System.EventHandler(this.Update_Click);
            // 
            // StartDatePicker
            // 
            this.StartDatePicker.AutoSize = true;
            this.StartDatePicker.Image = global::Yabe.Properties.Resources.calendar_view_week1;
            this.StartDatePicker.Location = new System.Drawing.Point(128, 33);
            this.StartDatePicker.Name = "StartDatePicker";
            this.StartDatePicker.Size = new System.Drawing.Size(27, 22);
            this.StartDatePicker.TabIndex = 9;
            this.StartDatePicker.UseVisualStyleBackColor = true;
            this.StartDatePicker.Click += new System.EventHandler(this.StartDatePicker_Click);
            // 
            // EndDatePicker
            // 
            this.EndDatePicker.AutoSize = true;
            this.EndDatePicker.Image = global::Yabe.Properties.Resources.calendar_view_week1;
            this.EndDatePicker.Location = new System.Drawing.Point(335, 33);
            this.EndDatePicker.Name = "EndDatePicker";
            this.EndDatePicker.Size = new System.Drawing.Size(27, 22);
            this.EndDatePicker.TabIndex = 10;
            this.EndDatePicker.UseVisualStyleBackColor = true;
            this.EndDatePicker.Click += new System.EventHandler(this.EndDatePicker_Click);
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(-1, 582);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(224, 13);
            this.label4.TabIndex = 11;
            this.label4.Text = "Optional Exception Schedule not implemented";
            // 
            // listReferences
            // 
            this.listReferences.ContextMenuStrip = this.contextMenuStrip;
            this.listReferences.Location = new System.Drawing.Point(21, 454);
            this.listReferences.Name = "listReferences";
            this.listReferences.Size = new System.Drawing.Size(340, 75);
            this.listReferences.TabIndex = 12;
            this.listReferences.UseCompatibleStateImageBehavior = false;
            this.listReferences.View = System.Windows.Forms.View.SmallIcon;
            this.listReferences.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.modifyToolStripMenuItem_Click);
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(18, 438);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(145, 13);
            this.label5.TabIndex = 13;
            this.label5.Text = "Objects properties references";
            // 
            // TxtScheduleDefault
            // 
            this.TxtScheduleDefault.Location = new System.Drawing.Point(22, 85);
            this.TxtScheduleDefault.Name = "TxtScheduleDefault";
            this.TxtScheduleDefault.Size = new System.Drawing.Size(100, 20);
            this.TxtScheduleDefault.TabIndex = 14;
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(19, 69);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(118, 13);
            this.label6.TabIndex = 15;
            this.label6.Text = "Schedule Default value";
            // 
            // ScheduleDataType
            // 
            this.ScheduleDataType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.ScheduleDataType.FormattingEnabled = true;
            this.ScheduleDataType.Location = new System.Drawing.Point(230, 85);
            this.ScheduleDataType.Name = "ScheduleDataType";
            this.ScheduleDataType.Size = new System.Drawing.Size(98, 21);
            this.ScheduleDataType.TabIndex = 16;
            this.ScheduleDataType.SelectedIndexChanged += new System.EventHandler(this.ScheduleDataType_SelectedIndexChanged);
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(226, 69);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(57, 13);
            this.label7.TabIndex = 17;
            this.label7.Text = "Data Type";
            // 
            // ScheduleDisplay
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(386, 604);
            this.Controls.Add(this.label7);
            this.Controls.Add(this.ScheduleDataType);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.TxtScheduleDefault);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.listReferences);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.EndDatePicker);
            this.Controls.Add(this.StartDatePicker);
            this.Controls.Add(this.UpdateButton);
            this.Controls.Add(this.Schedule);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.TxtEndDate);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.TxtStartDate);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "ScheduleDisplay";
            this.Text = "Simple Schedule Editor";
            this.contextMenuStrip.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox TxtStartDate;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox TxtEndDate;
        private System.Windows.Forms.Label label3;
        private CodersLab.Windows.Controls.TreeView Schedule;
        private System.Windows.Forms.Button UpdateButton;
        private System.Windows.Forms.ContextMenuStrip contextMenuStrip;
        private System.Windows.Forms.ToolStripMenuItem modifyToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem deleteToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem addToolStripMenuItem;
        private System.Windows.Forms.Button StartDatePicker;
        private System.Windows.Forms.Button EndDatePicker;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.ImageList imageList;
        private System.Windows.Forms.ListView listReferences;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.TextBox TxtScheduleDefault;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.ComboBox ScheduleDataType;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.ToolStripMenuItem copyToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem pasteToolStripMenuItem;
    }
}