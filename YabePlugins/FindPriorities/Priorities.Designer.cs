﻿namespace FindPriorities
{
    partial class Priorities
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
            this.treeView1 = new System.Windows.Forms.TreeView();
            this.Devicename = new System.Windows.Forms.Label();
            this.EmptyList = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // treeView1
            // 
            this.treeView1.Location = new System.Drawing.Point(16, 32);
            this.treeView1.Name = "treeView1";
            this.treeView1.Size = new System.Drawing.Size(472, 266);
            this.treeView1.TabIndex = 0;
            // 
            // Devicename
            // 
            this.Devicename.AutoSize = true;
            this.Devicename.Location = new System.Drawing.Point(16, 13);
            this.Devicename.Name = "Devicename";
            this.Devicename.Size = new System.Drawing.Size(35, 13);
            this.Devicename.TabIndex = 1;
            this.Devicename.Text = "label1";
            // 
            // EmptyList
            // 
            this.EmptyList.AutoSize = true;
            this.EmptyList.BackColor = System.Drawing.SystemColors.Window;
            this.EmptyList.Location = new System.Drawing.Point(86, 148);
            this.EmptyList.Name = "EmptyList";
            this.EmptyList.Size = new System.Drawing.Size(156, 13);
            this.EmptyList.TabIndex = 2;
            this.EmptyList.Text = "No Objects with priorities found.";
            this.EmptyList.Visible = false;
            this.EmptyList.Click += new System.EventHandler(this.EmptyList_Click);
            // 
            // Priorities
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(509, 315);
            this.Controls.Add(this.EmptyList);
            this.Controls.Add(this.Devicename);
            this.Controls.Add(this.treeView1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "Priorities";
            this.Text = "Priorities";
            this.Load += new System.EventHandler(this.FindPriorities_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TreeView treeView1;
        private System.Windows.Forms.Label Devicename;
        private System.Windows.Forms.Label EmptyList;
    }
}