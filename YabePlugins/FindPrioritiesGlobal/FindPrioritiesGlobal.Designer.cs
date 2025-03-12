
namespace FindPrioritiesGlobal
{
    partial class FindPrioritiesGlobal
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
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FindPrioritiesGlobal));
            this.DeviceList = new System.Windows.Forms.ListBox();
            this.cmdPopulateDevices = new System.Windows.Forms.Button();
            this.cmdPopulatePoints = new System.Windows.Forms.Button();
            this.progBar = new System.Windows.Forms.ProgressBar();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.splitContainer2 = new System.Windows.Forms.SplitContainer();
            this.splitContainer3 = new System.Windows.Forms.SplitContainer();
            this.splitContainer4 = new System.Windows.Forms.SplitContainer();
            this.treeView1 = new System.Windows.Forms.TreeView();
            this.button_expand = new System.Windows.Forms.Button();
            this.button_collapse = new System.Windows.Forms.Button();
            this.splitContainer5 = new System.Windows.Forms.SplitContainer();
            this.label1 = new System.Windows.Forms.Label();
            this.PropertyFilter = new System.Windows.Forms.GroupBox();
            this.button_SelectNone = new System.Windows.Forms.Button();
            this.button_SelectAll = new System.Windows.Forms.Button();
            this.Prio14 = new System.Windows.Forms.CheckBox();
            this.Prio13 = new System.Windows.Forms.CheckBox();
            this.Prio16 = new System.Windows.Forms.CheckBox();
            this.Prio15 = new System.Windows.Forms.CheckBox();
            this.Prio10 = new System.Windows.Forms.CheckBox();
            this.Prio9 = new System.Windows.Forms.CheckBox();
            this.Prio12 = new System.Windows.Forms.CheckBox();
            this.Prio11 = new System.Windows.Forms.CheckBox();
            this.Prio8 = new System.Windows.Forms.CheckBox();
            this.Prio7 = new System.Windows.Forms.CheckBox();
            this.Prio6 = new System.Windows.Forms.CheckBox();
            this.Prio5 = new System.Windows.Forms.CheckBox();
            this.Prio4 = new System.Windows.Forms.CheckBox();
            this.Prio3 = new System.Windows.Forms.CheckBox();
            this.Prio2 = new System.Windows.Forms.CheckBox();
            this.Prio1 = new System.Windows.Forms.CheckBox();
            this.Options = new System.Windows.Forms.GroupBox();
            this.PrintValues = new System.Windows.Forms.CheckBox();
            this.IncludePriorityLevelNames = new System.Windows.Forms.CheckBox();
            this.buttonExport = new System.Windows.Forms.Button();
            this.PatienceLabel = new System.Windows.Forms.Label();
            this.PatienceTimer = new System.Windows.Forms.Timer(this.components);
            this.delimToolTip = new System.Windows.Forms.ToolTip(this.components);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer2)).BeginInit();
            this.splitContainer2.Panel1.SuspendLayout();
            this.splitContainer2.Panel2.SuspendLayout();
            this.splitContainer2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer3)).BeginInit();
            this.splitContainer3.Panel1.SuspendLayout();
            this.splitContainer3.Panel2.SuspendLayout();
            this.splitContainer3.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer4)).BeginInit();
            this.splitContainer4.Panel1.SuspendLayout();
            this.splitContainer4.Panel2.SuspendLayout();
            this.splitContainer4.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer5)).BeginInit();
            this.splitContainer5.Panel1.SuspendLayout();
            this.splitContainer5.Panel2.SuspendLayout();
            this.splitContainer5.SuspendLayout();
            this.PropertyFilter.SuspendLayout();
            this.Options.SuspendLayout();
            this.SuspendLayout();
            // 
            // DeviceList
            // 
            this.DeviceList.Dock = System.Windows.Forms.DockStyle.Fill;
            this.DeviceList.FormattingEnabled = true;
            this.DeviceList.Location = new System.Drawing.Point(0, 0);
            this.DeviceList.Margin = new System.Windows.Forms.Padding(0);
            this.DeviceList.MaximumSize = new System.Drawing.Size(296, 620);
            this.DeviceList.Name = "DeviceList";
            this.DeviceList.SelectionMode = System.Windows.Forms.SelectionMode.MultiExtended;
            this.DeviceList.Size = new System.Drawing.Size(296, 620);
            this.DeviceList.TabIndex = 0;
            // 
            // cmdPopulateDevices
            // 
            this.cmdPopulateDevices.Location = new System.Drawing.Point(0, 1);
            this.cmdPopulateDevices.Margin = new System.Windows.Forms.Padding(2, 3, 2, 3);
            this.cmdPopulateDevices.Name = "cmdPopulateDevices";
            this.cmdPopulateDevices.Size = new System.Drawing.Size(140, 36);
            this.cmdPopulateDevices.TabIndex = 3;
            this.cmdPopulateDevices.Text = "Search Devices";
            this.delimToolTip.SetToolTip(this.cmdPopulateDevices, "Populate the list with all currently discovered\r\ndevices that are showing in the " +
        "main window.");
            this.cmdPopulateDevices.UseVisualStyleBackColor = true;
            this.cmdPopulateDevices.Click += new System.EventHandler(this.cmdSearchDevices_Click);
            // 
            // cmdPopulatePoints
            // 
            this.cmdPopulatePoints.Location = new System.Drawing.Point(0, 1);
            this.cmdPopulatePoints.Margin = new System.Windows.Forms.Padding(2, 3, 2, 3);
            this.cmdPopulatePoints.Name = "cmdPopulatePoints";
            this.cmdPopulatePoints.Size = new System.Drawing.Size(140, 36);
            this.cmdPopulatePoints.TabIndex = 5;
            this.cmdPopulatePoints.Text = "Search Priorities";
            this.cmdPopulatePoints.UseVisualStyleBackColor = true;
            this.cmdPopulatePoints.Click += new System.EventHandler(this.cmdSearchPriorities_Click);
            // 
            // progBar
            // 
            this.progBar.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.progBar.Location = new System.Drawing.Point(12, 695);
            this.progBar.Margin = new System.Windows.Forms.Padding(2, 3, 2, 3);
            this.progBar.Name = "progBar";
            this.progBar.Size = new System.Drawing.Size(980, 16);
            this.progBar.TabIndex = 10;
            // 
            // splitContainer1
            // 
            this.splitContainer1.AccessibleRole = System.Windows.Forms.AccessibleRole.None;
            this.splitContainer1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.splitContainer1.IsSplitterFixed = true;
            this.splitContainer1.Location = new System.Drawing.Point(12, 12);
            this.splitContainer1.Margin = new System.Windows.Forms.Padding(0);
            this.splitContainer1.MaximumSize = new System.Drawing.Size(1200, 665);
            this.splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.splitContainer2);
            this.splitContainer1.Panel1MinSize = 200;
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.splitContainer5);
            this.splitContainer1.Panel2MinSize = 100;
            this.splitContainer1.Size = new System.Drawing.Size(1200, 665);
            this.splitContainer1.SplitterDistance = 764;
            this.splitContainer1.TabIndex = 28;
            // 
            // splitContainer2
            // 
            this.splitContainer2.AccessibleRole = System.Windows.Forms.AccessibleRole.None;
            this.splitContainer2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer2.FixedPanel = System.Windows.Forms.FixedPanel.Panel2;
            this.splitContainer2.IsSplitterFixed = true;
            this.splitContainer2.Location = new System.Drawing.Point(0, 0);
            this.splitContainer2.Margin = new System.Windows.Forms.Padding(0);
            this.splitContainer2.Name = "splitContainer2";
            // 
            // splitContainer2.Panel1
            // 
            this.splitContainer2.Panel1.Controls.Add(this.splitContainer3);
            // 
            // splitContainer2.Panel2
            // 
            this.splitContainer2.Panel2.Controls.Add(this.splitContainer4);
            this.splitContainer2.Panel2MinSize = 100;
            this.splitContainer2.Size = new System.Drawing.Size(764, 665);
            this.splitContainer2.SplitterDistance = 296;
            this.splitContainer2.TabIndex = 0;
            // 
            // splitContainer3
            // 
            this.splitContainer3.AccessibleRole = System.Windows.Forms.AccessibleRole.None;
            this.splitContainer3.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer3.FixedPanel = System.Windows.Forms.FixedPanel.Panel1;
            this.splitContainer3.IsSplitterFixed = true;
            this.splitContainer3.Location = new System.Drawing.Point(0, 0);
            this.splitContainer3.Margin = new System.Windows.Forms.Padding(0);
            this.splitContainer3.Name = "splitContainer3";
            this.splitContainer3.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainer3.Panel1
            // 
            this.splitContainer3.Panel1.Controls.Add(this.DeviceList);
            // 
            // splitContainer3.Panel2
            // 
            this.splitContainer3.Panel2.Controls.Add(this.cmdPopulateDevices);
            this.splitContainer3.Panel2MinSize = 100;
            this.splitContainer3.Size = new System.Drawing.Size(296, 665);
            this.splitContainer3.SplitterDistance = 620;
            this.splitContainer3.TabIndex = 1;
            // 
            // splitContainer4
            // 
            this.splitContainer4.AccessibleRole = System.Windows.Forms.AccessibleRole.None;
            this.splitContainer4.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer4.FixedPanel = System.Windows.Forms.FixedPanel.Panel1;
            this.splitContainer4.IsSplitterFixed = true;
            this.splitContainer4.Location = new System.Drawing.Point(0, 0);
            this.splitContainer4.Margin = new System.Windows.Forms.Padding(0);
            this.splitContainer4.Name = "splitContainer4";
            this.splitContainer4.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainer4.Panel1
            // 
            this.splitContainer4.Panel1.Controls.Add(this.treeView1);
            // 
            // splitContainer4.Panel2
            // 
            this.splitContainer4.Panel2.Controls.Add(this.button_expand);
            this.splitContainer4.Panel2.Controls.Add(this.button_collapse);
            this.splitContainer4.Panel2.Controls.Add(this.cmdPopulatePoints);
            this.splitContainer4.Panel2MinSize = 100;
            this.splitContainer4.Size = new System.Drawing.Size(464, 665);
            this.splitContainer4.SplitterDistance = 620;
            this.splitContainer4.TabIndex = 2;
            // 
            // treeView1
            // 
            this.treeView1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.treeView1.Location = new System.Drawing.Point(0, 0);
            this.treeView1.Margin = new System.Windows.Forms.Padding(0);
            this.treeView1.MaximumSize = new System.Drawing.Size(464, 620);
            this.treeView1.Name = "treeView1";
            this.treeView1.Size = new System.Drawing.Size(464, 620);
            this.treeView1.TabIndex = 1;
            // 
            // button_expand
            // 
            this.button_expand.Font = new System.Drawing.Font("Microsoft Sans Serif", 6F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.button_expand.Location = new System.Drawing.Point(175, 8);
            this.button_expand.Margin = new System.Windows.Forms.Padding(2, 3, 2, 3);
            this.button_expand.Name = "button_expand";
            this.button_expand.Size = new System.Drawing.Size(25, 19);
            this.button_expand.TabIndex = 8;
            this.button_expand.Text = "o";
            this.delimToolTip.SetToolTip(this.button_expand, "expand all");
            this.button_expand.UseVisualStyleBackColor = true;
            this.button_expand.Click += new System.EventHandler(this.button_expand_Click);
            // 
            // button_collapse
            // 
            this.button_collapse.Font = new System.Drawing.Font("Microsoft Sans Serif", 6F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.button_collapse.Location = new System.Drawing.Point(146, 8);
            this.button_collapse.Margin = new System.Windows.Forms.Padding(2, 3, 2, 3);
            this.button_collapse.Name = "button_collapse";
            this.button_collapse.Size = new System.Drawing.Size(25, 19);
            this.button_collapse.TabIndex = 7;
            this.button_collapse.Text = "x";
            this.delimToolTip.SetToolTip(this.button_collapse, "collapse all");
            this.button_collapse.UseVisualStyleBackColor = true;
            this.button_collapse.Click += new System.EventHandler(this.button1_Click);
            // 
            // splitContainer5
            // 
            this.splitContainer5.AccessibleRole = System.Windows.Forms.AccessibleRole.None;
            this.splitContainer5.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer5.FixedPanel = System.Windows.Forms.FixedPanel.Panel1;
            this.splitContainer5.IsSplitterFixed = true;
            this.splitContainer5.Location = new System.Drawing.Point(0, 0);
            this.splitContainer5.Margin = new System.Windows.Forms.Padding(0);
            this.splitContainer5.Name = "splitContainer5";
            this.splitContainer5.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainer5.Panel1
            // 
            this.splitContainer5.Panel1.Controls.Add(this.label1);
            this.splitContainer5.Panel1.Controls.Add(this.PropertyFilter);
            this.splitContainer5.Panel1.Controls.Add(this.Options);
            // 
            // splitContainer5.Panel2
            // 
            this.splitContainer5.Panel2.Controls.Add(this.buttonExport);
            this.splitContainer5.Panel2MinSize = 100;
            this.splitContainer5.Size = new System.Drawing.Size(432, 665);
            this.splitContainer5.SplitterDistance = 620;
            this.splitContainer5.TabIndex = 3;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(229, 12);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(183, 442);
            this.label1.TabIndex = 70;
            this.label1.Text = resources.GetString("label1.Text");
            // 
            // PropertyFilter
            // 
            this.PropertyFilter.Controls.Add(this.button_SelectNone);
            this.PropertyFilter.Controls.Add(this.button_SelectAll);
            this.PropertyFilter.Controls.Add(this.Prio14);
            this.PropertyFilter.Controls.Add(this.Prio13);
            this.PropertyFilter.Controls.Add(this.Prio16);
            this.PropertyFilter.Controls.Add(this.Prio15);
            this.PropertyFilter.Controls.Add(this.Prio10);
            this.PropertyFilter.Controls.Add(this.Prio9);
            this.PropertyFilter.Controls.Add(this.Prio12);
            this.PropertyFilter.Controls.Add(this.Prio11);
            this.PropertyFilter.Controls.Add(this.Prio8);
            this.PropertyFilter.Controls.Add(this.Prio7);
            this.PropertyFilter.Controls.Add(this.Prio6);
            this.PropertyFilter.Controls.Add(this.Prio5);
            this.PropertyFilter.Controls.Add(this.Prio4);
            this.PropertyFilter.Controls.Add(this.Prio3);
            this.PropertyFilter.Controls.Add(this.Prio2);
            this.PropertyFilter.Controls.Add(this.Prio1);
            this.PropertyFilter.Location = new System.Drawing.Point(16, 3);
            this.PropertyFilter.Name = "PropertyFilter";
            this.PropertyFilter.Size = new System.Drawing.Size(180, 434);
            this.PropertyFilter.TabIndex = 69;
            this.PropertyFilter.TabStop = false;
            this.PropertyFilter.Text = "Priority levels to check";
            // 
            // button_SelectNone
            // 
            this.button_SelectNone.Location = new System.Drawing.Point(96, 399);
            this.button_SelectNone.Margin = new System.Windows.Forms.Padding(2, 3, 2, 3);
            this.button_SelectNone.Name = "button_SelectNone";
            this.button_SelectNone.Size = new System.Drawing.Size(74, 23);
            this.button_SelectNone.TabIndex = 86;
            this.button_SelectNone.Text = "Select None";
            this.button_SelectNone.UseVisualStyleBackColor = true;
            this.button_SelectNone.Click += new System.EventHandler(this.Button_SelectNone_Click);
            // 
            // button_SelectAll
            // 
            this.button_SelectAll.Location = new System.Drawing.Point(15, 399);
            this.button_SelectAll.Margin = new System.Windows.Forms.Padding(2, 3, 2, 3);
            this.button_SelectAll.Name = "button_SelectAll";
            this.button_SelectAll.Size = new System.Drawing.Size(74, 23);
            this.button_SelectAll.TabIndex = 85;
            this.button_SelectAll.Text = "Select All";
            this.button_SelectAll.UseVisualStyleBackColor = true;
            this.button_SelectAll.Click += new System.EventHandler(this.Button_SelectAll_Click);
            // 
            // Prio14
            // 
            this.Prio14.AutoSize = true;
            this.Prio14.Checked = true;
            this.Prio14.CheckState = System.Windows.Forms.CheckState.Checked;
            this.Prio14.Location = new System.Drawing.Point(15, 330);
            this.Prio14.Margin = new System.Windows.Forms.Padding(2, 3, 2, 3);
            this.Prio14.Name = "Prio14";
            this.Prio14.Size = new System.Drawing.Size(90, 17);
            this.Prio14.TabIndex = 83;
            this.Prio14.Text = "14 - Available";
            this.Prio14.UseVisualStyleBackColor = true;
            this.Prio14.CheckedChanged += new System.EventHandler(this.Prio14_CheckedChanged);
            // 
            // Prio13
            // 
            this.Prio13.AutoSize = true;
            this.Prio13.Checked = true;
            this.Prio13.CheckState = System.Windows.Forms.CheckState.Checked;
            this.Prio13.Location = new System.Drawing.Point(15, 307);
            this.Prio13.Margin = new System.Windows.Forms.Padding(2, 3, 2, 3);
            this.Prio13.Name = "Prio13";
            this.Prio13.Size = new System.Drawing.Size(90, 17);
            this.Prio13.TabIndex = 82;
            this.Prio13.Text = "13 - Available";
            this.Prio13.UseVisualStyleBackColor = true;
            this.Prio13.CheckedChanged += new System.EventHandler(this.Prio13_CheckedChanged);
            // 
            // Prio16
            // 
            this.Prio16.AutoSize = true;
            this.Prio16.Checked = true;
            this.Prio16.CheckState = System.Windows.Forms.CheckState.Checked;
            this.Prio16.Location = new System.Drawing.Point(15, 376);
            this.Prio16.Margin = new System.Windows.Forms.Padding(2, 3, 2, 3);
            this.Prio16.Name = "Prio16";
            this.Prio16.Size = new System.Drawing.Size(90, 17);
            this.Prio16.TabIndex = 81;
            this.Prio16.Text = "16 - Available";
            this.Prio16.UseVisualStyleBackColor = true;
            this.Prio16.CheckedChanged += new System.EventHandler(this.Prio16_CheckedChanged);
            // 
            // Prio15
            // 
            this.Prio15.AutoSize = true;
            this.Prio15.Checked = true;
            this.Prio15.CheckState = System.Windows.Forms.CheckState.Checked;
            this.Prio15.Location = new System.Drawing.Point(15, 353);
            this.Prio15.Margin = new System.Windows.Forms.Padding(2, 3, 2, 3);
            this.Prio15.Name = "Prio15";
            this.Prio15.Size = new System.Drawing.Size(90, 17);
            this.Prio15.TabIndex = 80;
            this.Prio15.Text = "15 - Available";
            this.Prio15.UseVisualStyleBackColor = true;
            this.Prio15.CheckedChanged += new System.EventHandler(this.Prio15_CheckedChanged);
            // 
            // Prio10
            // 
            this.Prio10.AutoSize = true;
            this.Prio10.Checked = true;
            this.Prio10.CheckState = System.Windows.Forms.CheckState.Checked;
            this.Prio10.Location = new System.Drawing.Point(15, 237);
            this.Prio10.Margin = new System.Windows.Forms.Padding(2, 3, 2, 3);
            this.Prio10.Name = "Prio10";
            this.Prio10.Size = new System.Drawing.Size(90, 17);
            this.Prio10.TabIndex = 79;
            this.Prio10.Text = "10 - Available";
            this.Prio10.UseVisualStyleBackColor = true;
            this.Prio10.CheckedChanged += new System.EventHandler(this.Prio10_CheckedChanged);
            // 
            // Prio9
            // 
            this.Prio9.AutoSize = true;
            this.Prio9.Checked = true;
            this.Prio9.CheckState = System.Windows.Forms.CheckState.Checked;
            this.Prio9.Location = new System.Drawing.Point(15, 214);
            this.Prio9.Margin = new System.Windows.Forms.Padding(2, 3, 2, 3);
            this.Prio9.Name = "Prio9";
            this.Prio9.Size = new System.Drawing.Size(84, 17);
            this.Prio9.TabIndex = 78;
            this.Prio9.Text = "9 - Available";
            this.Prio9.UseVisualStyleBackColor = true;
            this.Prio9.CheckedChanged += new System.EventHandler(this.Prio9_CheckedChanged);
            // 
            // Prio12
            // 
            this.Prio12.AutoSize = true;
            this.Prio12.Checked = true;
            this.Prio12.CheckState = System.Windows.Forms.CheckState.Checked;
            this.Prio12.Location = new System.Drawing.Point(15, 284);
            this.Prio12.Margin = new System.Windows.Forms.Padding(2, 3, 2, 3);
            this.Prio12.Name = "Prio12";
            this.Prio12.Size = new System.Drawing.Size(90, 17);
            this.Prio12.TabIndex = 77;
            this.Prio12.Text = "12 - Available";
            this.Prio12.UseVisualStyleBackColor = true;
            this.Prio12.CheckedChanged += new System.EventHandler(this.Prio12_CheckedChanged);
            // 
            // Prio11
            // 
            this.Prio11.AutoSize = true;
            this.Prio11.Checked = true;
            this.Prio11.CheckState = System.Windows.Forms.CheckState.Checked;
            this.Prio11.Location = new System.Drawing.Point(15, 261);
            this.Prio11.Margin = new System.Windows.Forms.Padding(2, 3, 2, 3);
            this.Prio11.Name = "Prio11";
            this.Prio11.Size = new System.Drawing.Size(90, 17);
            this.Prio11.TabIndex = 76;
            this.Prio11.Text = "11 - Available";
            this.Prio11.UseVisualStyleBackColor = true;
            this.Prio11.CheckedChanged += new System.EventHandler(this.Prio11_CheckedChanged);
            // 
            // Prio8
            // 
            this.Prio8.AutoSize = true;
            this.Prio8.Location = new System.Drawing.Point(15, 193);
            this.Prio8.Margin = new System.Windows.Forms.Padding(2, 3, 2, 3);
            this.Prio8.Name = "Prio8";
            this.Prio8.Size = new System.Drawing.Size(120, 17);
            this.Prio8.TabIndex = 75;
            this.Prio8.Text = "8 - Manual Operator";
            this.Prio8.UseVisualStyleBackColor = true;
            this.Prio8.CheckedChanged += new System.EventHandler(this.Prio8_CheckedChanged);
            // 
            // Prio7
            // 
            this.Prio7.AutoSize = true;
            this.Prio7.Checked = true;
            this.Prio7.CheckState = System.Windows.Forms.CheckState.Checked;
            this.Prio7.Location = new System.Drawing.Point(15, 170);
            this.Prio7.Margin = new System.Windows.Forms.Padding(2, 3, 2, 3);
            this.Prio7.Name = "Prio7";
            this.Prio7.Size = new System.Drawing.Size(84, 17);
            this.Prio7.TabIndex = 74;
            this.Prio7.Text = "7 - Available";
            this.Prio7.UseVisualStyleBackColor = true;
            this.Prio7.CheckedChanged += new System.EventHandler(this.Prio7_CheckedChanged);
            // 
            // Prio6
            // 
            this.Prio6.AutoSize = true;
            this.Prio6.Checked = true;
            this.Prio6.CheckState = System.Windows.Forms.CheckState.Checked;
            this.Prio6.Location = new System.Drawing.Point(15, 146);
            this.Prio6.Margin = new System.Windows.Forms.Padding(2, 3, 2, 3);
            this.Prio6.Name = "Prio6";
            this.Prio6.Size = new System.Drawing.Size(118, 17);
            this.Prio6.TabIndex = 73;
            this.Prio6.Text = "6 - Minimum On/Off";
            this.Prio6.UseVisualStyleBackColor = true;
            this.Prio6.CheckedChanged += new System.EventHandler(this.Prio6_CheckedChanged);
            // 
            // Prio5
            // 
            this.Prio5.AutoSize = true;
            this.Prio5.Checked = true;
            this.Prio5.CheckState = System.Windows.Forms.CheckState.Checked;
            this.Prio5.Location = new System.Drawing.Point(15, 123);
            this.Prio5.Margin = new System.Windows.Forms.Padding(2, 3, 2, 3);
            this.Prio5.Name = "Prio5";
            this.Prio5.Size = new System.Drawing.Size(161, 17);
            this.Prio5.TabIndex = 72;
            this.Prio5.Text = "5 - Critical Equipment Control";
            this.Prio5.UseVisualStyleBackColor = true;
            this.Prio5.CheckedChanged += new System.EventHandler(this.Prio5_CheckedChanged);
            // 
            // Prio4
            // 
            this.Prio4.AutoSize = true;
            this.Prio4.Checked = true;
            this.Prio4.CheckState = System.Windows.Forms.CheckState.Checked;
            this.Prio4.Location = new System.Drawing.Point(15, 100);
            this.Prio4.Margin = new System.Windows.Forms.Padding(2, 3, 2, 3);
            this.Prio4.Name = "Prio4";
            this.Prio4.Size = new System.Drawing.Size(84, 17);
            this.Prio4.TabIndex = 71;
            this.Prio4.Text = "4 - Available";
            this.Prio4.UseVisualStyleBackColor = true;
            this.Prio4.CheckedChanged += new System.EventHandler(this.Prio4_CheckedChanged);
            // 
            // Prio3
            // 
            this.Prio3.AutoSize = true;
            this.Prio3.Checked = true;
            this.Prio3.CheckState = System.Windows.Forms.CheckState.Checked;
            this.Prio3.Location = new System.Drawing.Point(15, 77);
            this.Prio3.Margin = new System.Windows.Forms.Padding(2, 3, 2, 3);
            this.Prio3.Name = "Prio3";
            this.Prio3.Size = new System.Drawing.Size(84, 17);
            this.Prio3.TabIndex = 70;
            this.Prio3.Text = "3 - Available";
            this.Prio3.UseVisualStyleBackColor = true;
            this.Prio3.CheckedChanged += new System.EventHandler(this.Prio3_CheckedChanged);
            // 
            // Prio2
            // 
            this.Prio2.AutoSize = true;
            this.Prio2.Checked = true;
            this.Prio2.CheckState = System.Windows.Forms.CheckState.Checked;
            this.Prio2.Location = new System.Drawing.Point(15, 54);
            this.Prio2.Margin = new System.Windows.Forms.Padding(2, 3, 2, 3);
            this.Prio2.Name = "Prio2";
            this.Prio2.Size = new System.Drawing.Size(141, 17);
            this.Prio2.TabIndex = 69;
            this.Prio2.Text = "2 - Automatic-Life Safety";
            this.Prio2.UseVisualStyleBackColor = true;
            this.Prio2.CheckedChanged += new System.EventHandler(this.Prio2_CheckedChanged);
            // 
            // Prio1
            // 
            this.Prio1.AutoSize = true;
            this.Prio1.Checked = true;
            this.Prio1.CheckState = System.Windows.Forms.CheckState.Checked;
            this.Prio1.Location = new System.Drawing.Point(15, 31);
            this.Prio1.Margin = new System.Windows.Forms.Padding(2, 3, 2, 3);
            this.Prio1.Name = "Prio1";
            this.Prio1.Size = new System.Drawing.Size(129, 17);
            this.Prio1.TabIndex = 68;
            this.Prio1.Text = "1 - Manual-Life Safety";
            this.Prio1.UseVisualStyleBackColor = true;
            this.Prio1.CheckedChanged += new System.EventHandler(this.Prio1_CheckedChanged);
            // 
            // Options
            // 
            this.Options.Controls.Add(this.PrintValues);
            this.Options.Controls.Add(this.IncludePriorityLevelNames);
            this.Options.Location = new System.Drawing.Point(16, 443);
            this.Options.Name = "Options";
            this.Options.Size = new System.Drawing.Size(180, 70);
            this.Options.TabIndex = 68;
            this.Options.TabStop = false;
            this.Options.Text = "Options";
            // 
            // PrintValues
            // 
            this.PrintValues.AutoSize = true;
            this.PrintValues.Checked = true;
            this.PrintValues.CheckState = System.Windows.Forms.CheckState.Checked;
            this.PrintValues.Location = new System.Drawing.Point(6, 42);
            this.PrintValues.Name = "PrintValues";
            this.PrintValues.Size = new System.Drawing.Size(82, 17);
            this.PrintValues.TabIndex = 66;
            this.PrintValues.Text = "Print Values";
            this.PrintValues.UseVisualStyleBackColor = true;
            // 
            // IncludePriorityLevelNames
            // 
            this.IncludePriorityLevelNames.AutoSize = true;
            this.IncludePriorityLevelNames.Location = new System.Drawing.Point(6, 19);
            this.IncludePriorityLevelNames.Name = "IncludePriorityLevelNames";
            this.IncludePriorityLevelNames.Size = new System.Drawing.Size(160, 17);
            this.IncludePriorityLevelNames.TabIndex = 65;
            this.IncludePriorityLevelNames.Text = "Include Priority Level Names";
            this.IncludePriorityLevelNames.UseVisualStyleBackColor = true;
            // 
            // buttonExport
            // 
            this.buttonExport.Location = new System.Drawing.Point(0, 1);
            this.buttonExport.Margin = new System.Windows.Forms.Padding(2, 3, 2, 3);
            this.buttonExport.Name = "buttonExport";
            this.buttonExport.Size = new System.Drawing.Size(140, 36);
            this.buttonExport.TabIndex = 6;
            this.buttonExport.Text = "Export";
            this.buttonExport.UseVisualStyleBackColor = true;
            this.buttonExport.Click += new System.EventHandler(this.buttonExport_Click);
            // 
            // PatienceLabel
            // 
            this.PatienceLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.PatienceLabel.AutoSize = true;
            this.PatienceLabel.Location = new System.Drawing.Point(59, 680);
            this.PatienceLabel.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.PatienceLabel.Name = "PatienceLabel";
            this.PatienceLabel.Size = new System.Drawing.Size(559, 13);
            this.PatienceLabel.TabIndex = 29;
            this.PatienceLabel.Text = "Please be patient - the plugin has absolutely no prior knowledge of the nework an" +
    "d must poll each device individually.";
            this.PatienceLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.PatienceLabel.Visible = false;
            // 
            // PatienceTimer
            // 
            this.PatienceTimer.Interval = 1000;
            this.PatienceTimer.Tick += new System.EventHandler(this.PatienceTimer_Tick);
            // 
            // delimToolTip
            // 
            this.delimToolTip.AutoPopDelay = 30000;
            this.delimToolTip.InitialDelay = 500;
            this.delimToolTip.ReshowDelay = 100;
            this.delimToolTip.ToolTipIcon = System.Windows.Forms.ToolTipIcon.Info;
            this.delimToolTip.ToolTipTitle = "Info";
            // 
            // FindPrioritiesGlobal
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1234, 721);
            this.Controls.Add(this.PatienceLabel);
            this.Controls.Add(this.splitContainer1);
            this.Controls.Add(this.progBar);
            this.Margin = new System.Windows.Forms.Padding(2, 3, 2, 3);
            this.MinimumSize = new System.Drawing.Size(1250, 760);
            this.Name = "FindPrioritiesGlobal";
            this.Text = "Yabe FindPrioritiesGlobal";
            this.Load += new System.EventHandler(this.FindPrioritiesGlobal_Load);
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            this.splitContainer2.Panel1.ResumeLayout(false);
            this.splitContainer2.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer2)).EndInit();
            this.splitContainer2.ResumeLayout(false);
            this.splitContainer3.Panel1.ResumeLayout(false);
            this.splitContainer3.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer3)).EndInit();
            this.splitContainer3.ResumeLayout(false);
            this.splitContainer4.Panel1.ResumeLayout(false);
            this.splitContainer4.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer4)).EndInit();
            this.splitContainer4.ResumeLayout(false);
            this.splitContainer5.Panel1.ResumeLayout(false);
            this.splitContainer5.Panel1.PerformLayout();
            this.splitContainer5.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer5)).EndInit();
            this.splitContainer5.ResumeLayout(false);
            this.PropertyFilter.ResumeLayout(false);
            this.PropertyFilter.PerformLayout();
            this.Options.ResumeLayout(false);
            this.Options.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ListBox DeviceList;
        private System.Windows.Forms.Button cmdPopulateDevices;
        private System.Windows.Forms.Button cmdPopulatePoints;
        private System.Windows.Forms.ProgressBar progBar;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.SplitContainer splitContainer2;
        private System.Windows.Forms.SplitContainer splitContainer3;
        private System.Windows.Forms.SplitContainer splitContainer4;
        private System.Windows.Forms.SplitContainer splitContainer5;
        private System.Windows.Forms.Label PatienceLabel;
        private System.Windows.Forms.Timer PatienceTimer;
        private System.Windows.Forms.ToolTip delimToolTip;
        private System.Windows.Forms.TreeView treeView1;
        private System.Windows.Forms.Button button_expand;
        private System.Windows.Forms.Button button_collapse;
        private System.Windows.Forms.GroupBox PropertyFilter;
        private System.Windows.Forms.Button button_SelectNone;
        private System.Windows.Forms.Button button_SelectAll;
        private System.Windows.Forms.CheckBox Prio14;
        private System.Windows.Forms.CheckBox Prio13;
        private System.Windows.Forms.CheckBox Prio16;
        private System.Windows.Forms.CheckBox Prio15;
        private System.Windows.Forms.CheckBox Prio10;
        private System.Windows.Forms.CheckBox Prio9;
        private System.Windows.Forms.CheckBox Prio12;
        private System.Windows.Forms.CheckBox Prio11;
        private System.Windows.Forms.CheckBox Prio8;
        private System.Windows.Forms.CheckBox Prio7;
        private System.Windows.Forms.CheckBox Prio6;
        private System.Windows.Forms.CheckBox Prio5;
        private System.Windows.Forms.CheckBox Prio4;
        private System.Windows.Forms.CheckBox Prio3;
        private System.Windows.Forms.CheckBox Prio2;
        private System.Windows.Forms.CheckBox Prio1;
        private System.Windows.Forms.GroupBox Options;
        private System.Windows.Forms.CheckBox IncludePriorityLevelNames;
        private System.Windows.Forms.Button buttonExport;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.CheckBox PrintValues;
    }
}