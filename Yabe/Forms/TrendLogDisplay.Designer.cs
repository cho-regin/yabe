﻿namespace Yabe
{
    partial class TrendLogDisplay
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(TrendLogDisplay));
            this.m_progressBar = new System.Windows.Forms.ProgressBar();
            this.m_zedGraphCtl = new ZedGraph.ZedGraphControl();
            this.m_progresslabel = new System.Windows.Forms.Label();
            this.m_list = new System.Windows.Forms.ListView();
            this.columnHeader1 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader3 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader2 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader4 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader5 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.SuspendLayout();
            // 
            // m_progressBar
            // 
            this.m_progressBar.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.m_progressBar.Location = new System.Drawing.Point(227, 149);
            this.m_progressBar.Name = "m_progressBar";
            this.m_progressBar.Size = new System.Drawing.Size(231, 23);
            this.m_progressBar.TabIndex = 0;
            this.m_progressBar.Click += new System.EventHandler(this.m_progressBar_Click);
            // 
            // m_zedGraphCtl
            // 
            this.m_zedGraphCtl.Dock = System.Windows.Forms.DockStyle.Fill;
            this.m_zedGraphCtl.IsShowPointValues = true;
            this.m_zedGraphCtl.Location = new System.Drawing.Point(0, 0);
            this.m_zedGraphCtl.Name = "m_zedGraphCtl";
            this.m_zedGraphCtl.ScrollGrace = 0D;
            this.m_zedGraphCtl.ScrollMaxX = 0D;
            this.m_zedGraphCtl.ScrollMaxY = 0D;
            this.m_zedGraphCtl.ScrollMaxY2 = 0D;
            this.m_zedGraphCtl.ScrollMinX = 0D;
            this.m_zedGraphCtl.ScrollMinY = 0D;
            this.m_zedGraphCtl.ScrollMinY2 = 0D;
            this.m_zedGraphCtl.Size = new System.Drawing.Size(700, 317);
            this.m_zedGraphCtl.TabIndex = 1;
            // 
            // m_progresslabel
            // 
            this.m_progresslabel.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.m_progresslabel.BackColor = System.Drawing.SystemColors.Window;
            this.m_progresslabel.Location = new System.Drawing.Point(227, 125);
            this.m_progresslabel.Name = "m_progresslabel";
            this.m_progresslabel.Size = new System.Drawing.Size(231, 21);
            this.m_progresslabel.TabIndex = 2;
            this.m_progresslabel.Text = "Downloads in Progress (0%)";
            this.m_progresslabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // m_list
            // 
            this.m_list.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader1,
            this.columnHeader3,
            this.columnHeader2,
            this.columnHeader4,
            this.columnHeader5});
            this.m_list.Dock = System.Windows.Forms.DockStyle.Fill;
            this.m_list.FullRowSelect = true;
            this.m_list.GridLines = true;
            this.m_list.Location = new System.Drawing.Point(0, 0);
            this.m_list.Name = "m_list";
            this.m_list.Size = new System.Drawing.Size(285, 317);
            this.m_list.TabIndex = 3;
            this.m_list.UseCompatibleStateImageBehavior = false;
            this.m_list.View = System.Windows.Forms.View.Details;
            // 
            // columnHeader1
            // 
            this.columnHeader1.Text = "Sequence no";
            this.columnHeader1.Width = 32;
            // 
            // columnHeader3
            // 
            this.columnHeader3.Text = "Time";
            this.columnHeader3.Width = 96;
            // 
            // columnHeader2
            // 
            this.columnHeader2.Text = "Type";
            this.columnHeader2.Width = 110;
            // 
            // columnHeader4
            // 
            this.columnHeader4.Text = "Value";
            // 
            // columnHeader5
            // 
            this.columnHeader5.Text = "Status";
            // 
            // splitContainer1
            // 
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.Location = new System.Drawing.Point(0, 0);
            this.splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.m_progresslabel);
            this.splitContainer1.Panel1.Controls.Add(this.m_progressBar);
            this.splitContainer1.Panel1.Controls.Add(this.m_zedGraphCtl);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.m_list);
            this.splitContainer1.Size = new System.Drawing.Size(989, 317);
            this.splitContainer1.SplitterDistance = 700;
            this.splitContainer1.TabIndex = 0;
            // 
            // TrendLogDisplay
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(989, 317);
            this.Controls.Add(this.splitContainer1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "TrendLogDisplay";
            this.Text = "Yabe TrendLog Display";
            this.Shown += new System.EventHandler(this.TrendLogDisplay_Shown);
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.ProgressBar m_progressBar;
        private ZedGraph.ZedGraphControl m_zedGraphCtl;
        private System.Windows.Forms.Label m_progresslabel;
        private System.Windows.Forms.ListView m_list;
        private System.Windows.Forms.ColumnHeader columnHeader1;
        private System.Windows.Forms.ColumnHeader columnHeader3;
        private System.Windows.Forms.ColumnHeader columnHeader2;
        private System.Windows.Forms.ColumnHeader columnHeader4;
        private System.Windows.Forms.ColumnHeader columnHeader5;
        private System.Windows.Forms.SplitContainer splitContainer1;
    }
}