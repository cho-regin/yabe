
namespace GlobalCommander
{
    partial class GlobalCommander
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(GlobalCommander));
            this.DeviceList = new System.Windows.Forms.ListBox();
            this.PointList = new System.Windows.Forms.ListBox();
            this.PropertyList = new System.Windows.Forms.ListBox();
            this.cmdPopulateDevices = new System.Windows.Forms.Button();
            this.cmdPopulatePoints = new System.Windows.Forms.Button();
            this.cmdPopulateProperties = new System.Windows.Forms.Button();
            this.cmdCommand = new System.Windows.Forms.Button();
            this.lblCmdVal = new System.Windows.Forms.Label();
            this.txtCmdVal = new System.Windows.Forms.TextBox();
            this.o1 = new System.Windows.Forms.RadioButton();
            this.o2 = new System.Windows.Forms.RadioButton();
            this.o3 = new System.Windows.Forms.RadioButton();
            this.o4 = new System.Windows.Forms.RadioButton();
            this.o5 = new System.Windows.Forms.RadioButton();
            this.o6 = new System.Windows.Forms.RadioButton();
            this.o7 = new System.Windows.Forms.RadioButton();
            this.o10 = new System.Windows.Forms.RadioButton();
            this.o8 = new System.Windows.Forms.RadioButton();
            this.o11 = new System.Windows.Forms.RadioButton();
            this.o9 = new System.Windows.Forms.RadioButton();
            this.o12 = new System.Windows.Forms.RadioButton();
            this.o14 = new System.Windows.Forms.RadioButton();
            this.o15 = new System.Windows.Forms.RadioButton();
            this.o13 = new System.Windows.Forms.RadioButton();
            this.o16 = new System.Windows.Forms.RadioButton();
            this.progBar = new System.Windows.Forms.ProgressBar();
            this.cmdViewProps = new System.Windows.Forms.Button();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.splitContainer2 = new System.Windows.Forms.SplitContainer();
            this.splitContainer3 = new System.Windows.Forms.SplitContainer();
            this.txtDeviceFilter = new System.Windows.Forms.TextBox();
            this.splitContainer4 = new System.Windows.Forms.SplitContainer();
            this.txtPointFilter = new System.Windows.Forms.TextBox();
            this.splitContainer5 = new System.Windows.Forms.SplitContainer();
            this.PatienceLabel = new System.Windows.Forms.Label();
            this.PatienceTimer = new System.Windows.Forms.Timer(this.components);
            this.radComObj = new System.Windows.Forms.RadioButton();
            this.radJoinObj = new System.Windows.Forms.RadioButton();
            this.lblPtDelimLabel = new System.Windows.Forms.Label();
            this.grpPointNames = new System.Windows.Forms.GroupBox();
            this.chkHyphen = new System.Windows.Forms.CheckBox();
            this.chkUnderscore = new System.Windows.Forms.CheckBox();
            this.chkComma = new System.Windows.Forms.CheckBox();
            this.chkColon = new System.Windows.Forms.CheckBox();
            this.chkDot = new System.Windows.Forms.CheckBox();
            this.chkApostrophe = new System.Windows.Forms.CheckBox();
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
            this.grpPointNames.SuspendLayout();
            this.SuspendLayout();
            // 
            // DeviceList
            // 
            this.DeviceList.Dock = System.Windows.Forms.DockStyle.Fill;
            this.DeviceList.FormattingEnabled = true;
            this.DeviceList.Location = new System.Drawing.Point(0, 0);
            this.DeviceList.Name = "DeviceList";
            this.DeviceList.SelectionMode = System.Windows.Forms.SelectionMode.MultiExtended;
            this.DeviceList.Size = new System.Drawing.Size(285, 560);
            this.DeviceList.TabIndex = 0;
            this.DeviceList.SelectedIndexChanged += new System.EventHandler(this.DeviceList_SelectedIndexChanged);
            // 
            // PointList
            // 
            this.PointList.Dock = System.Windows.Forms.DockStyle.Fill;
            this.PointList.FormattingEnabled = true;
            this.PointList.Location = new System.Drawing.Point(0, 0);
            this.PointList.Name = "PointList";
            this.PointList.SelectionMode = System.Windows.Forms.SelectionMode.MultiExtended;
            this.PointList.Size = new System.Drawing.Size(391, 560);
            this.PointList.TabIndex = 1;
            this.PointList.SelectedIndexChanged += new System.EventHandler(this.PointList_SelectedIndexChanged);
            // 
            // PropertyList
            // 
            this.PropertyList.Dock = System.Windows.Forms.DockStyle.Fill;
            this.PropertyList.FormattingEnabled = true;
            this.PropertyList.Location = new System.Drawing.Point(0, 0);
            this.PropertyList.MinimumSize = new System.Drawing.Size(100, 4);
            this.PropertyList.Name = "PropertyList";
            this.PropertyList.Size = new System.Drawing.Size(329, 560);
            this.PropertyList.TabIndex = 2;
            // 
            // cmdPopulateDevices
            // 
            this.cmdPopulateDevices.Location = new System.Drawing.Point(0, 2);
            this.cmdPopulateDevices.Name = "cmdPopulateDevices";
            this.cmdPopulateDevices.Size = new System.Drawing.Size(140, 36);
            this.cmdPopulateDevices.TabIndex = 3;
            this.cmdPopulateDevices.Text = "Populate Devices";
            this.cmdPopulateDevices.UseVisualStyleBackColor = true;
            this.cmdPopulateDevices.Click += new System.EventHandler(this.cmdPopulateDevices_Click);
            // 
            // cmdPopulatePoints
            // 
            this.cmdPopulatePoints.Location = new System.Drawing.Point(0, 2);
            this.cmdPopulatePoints.Name = "cmdPopulatePoints";
            this.cmdPopulatePoints.Size = new System.Drawing.Size(140, 36);
            this.cmdPopulatePoints.TabIndex = 5;
            this.cmdPopulatePoints.Text = "Populate Points";
            this.cmdPopulatePoints.UseVisualStyleBackColor = true;
            this.cmdPopulatePoints.Click += new System.EventHandler(this.cmdPopulatePoints_Click);
            // 
            // cmdPopulateProperties
            // 
            this.cmdPopulateProperties.Location = new System.Drawing.Point(0, 2);
            this.cmdPopulateProperties.Name = "cmdPopulateProperties";
            this.cmdPopulateProperties.Size = new System.Drawing.Size(140, 36);
            this.cmdPopulateProperties.TabIndex = 7;
            this.cmdPopulateProperties.Text = "Populate Properties";
            this.cmdPopulateProperties.UseVisualStyleBackColor = true;
            this.cmdPopulateProperties.Click += new System.EventHandler(this.cmdPopulateProperties_Click);
            // 
            // cmdCommand
            // 
            this.cmdCommand.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.cmdCommand.Location = new System.Drawing.Point(1032, 420);
            this.cmdCommand.Name = "cmdCommand";
            this.cmdCommand.Size = new System.Drawing.Size(214, 36);
            this.cmdCommand.TabIndex = 8;
            this.cmdCommand.Text = "Globally Command";
            this.cmdCommand.UseVisualStyleBackColor = true;
            this.cmdCommand.Click += new System.EventHandler(this.cmdCommand_Click);
            // 
            // lblCmdVal
            // 
            this.lblCmdVal.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.lblCmdVal.AutoSize = true;
            this.lblCmdVal.Location = new System.Drawing.Point(1032, 394);
            this.lblCmdVal.Name = "lblCmdVal";
            this.lblCmdVal.Size = new System.Drawing.Size(87, 13);
            this.lblCmdVal.TabIndex = 7;
            this.lblCmdVal.Text = "Command Value:";
            // 
            // txtCmdVal
            // 
            this.txtCmdVal.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.txtCmdVal.Location = new System.Drawing.Point(1125, 391);
            this.txtCmdVal.Name = "txtCmdVal";
            this.txtCmdVal.Size = new System.Drawing.Size(120, 20);
            this.txtCmdVal.TabIndex = 28;
            // 
            // o1
            // 
            this.o1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.o1.AutoSize = true;
            this.o1.Location = new System.Drawing.Point(1124, 16);
            this.o1.Name = "o1";
            this.o1.Size = new System.Drawing.Size(65, 17);
            this.o1.TabIndex = 110;
            this.o1.Text = "Priority 1";
            this.o1.UseVisualStyleBackColor = true;
            // 
            // o2
            // 
            this.o2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.o2.AutoSize = true;
            this.o2.Location = new System.Drawing.Point(1124, 39);
            this.o2.Name = "o2";
            this.o2.Size = new System.Drawing.Size(65, 17);
            this.o2.TabIndex = 111;
            this.o2.Text = "Priority 2";
            this.o2.UseVisualStyleBackColor = true;
            // 
            // o3
            // 
            this.o3.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.o3.AutoSize = true;
            this.o3.Location = new System.Drawing.Point(1124, 62);
            this.o3.Name = "o3";
            this.o3.Size = new System.Drawing.Size(65, 17);
            this.o3.TabIndex = 112;
            this.o3.Text = "Priority 3";
            this.o3.UseVisualStyleBackColor = true;
            // 
            // o4
            // 
            this.o4.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.o4.AutoSize = true;
            this.o4.Location = new System.Drawing.Point(1124, 85);
            this.o4.Name = "o4";
            this.o4.Size = new System.Drawing.Size(65, 17);
            this.o4.TabIndex = 113;
            this.o4.Text = "Priority 4";
            this.o4.UseVisualStyleBackColor = true;
            // 
            // o5
            // 
            this.o5.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.o5.AutoSize = true;
            this.o5.Location = new System.Drawing.Point(1124, 108);
            this.o5.Name = "o5";
            this.o5.Size = new System.Drawing.Size(65, 17);
            this.o5.TabIndex = 114;
            this.o5.Text = "Priority 5";
            this.o5.UseVisualStyleBackColor = true;
            // 
            // o6
            // 
            this.o6.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.o6.AutoSize = true;
            this.o6.Location = new System.Drawing.Point(1124, 131);
            this.o6.Name = "o6";
            this.o6.Size = new System.Drawing.Size(65, 17);
            this.o6.TabIndex = 115;
            this.o6.Text = "Priority 6";
            this.o6.UseVisualStyleBackColor = true;
            // 
            // o7
            // 
            this.o7.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.o7.AutoSize = true;
            this.o7.Location = new System.Drawing.Point(1124, 154);
            this.o7.Name = "o7";
            this.o7.Size = new System.Drawing.Size(65, 17);
            this.o7.TabIndex = 116;
            this.o7.Text = "Priority 7";
            this.o7.UseVisualStyleBackColor = true;
            // 
            // o10
            // 
            this.o10.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.o10.AutoSize = true;
            this.o10.Location = new System.Drawing.Point(1124, 223);
            this.o10.Name = "o10";
            this.o10.Size = new System.Drawing.Size(71, 17);
            this.o10.TabIndex = 119;
            this.o10.Text = "Priority 10";
            this.o10.UseVisualStyleBackColor = true;
            // 
            // o8
            // 
            this.o8.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.o8.AutoSize = true;
            this.o8.Checked = true;
            this.o8.Location = new System.Drawing.Point(1124, 177);
            this.o8.Name = "o8";
            this.o8.Size = new System.Drawing.Size(65, 17);
            this.o8.TabIndex = 117;
            this.o8.TabStop = true;
            this.o8.Text = "Priority 8";
            this.o8.UseVisualStyleBackColor = true;
            // 
            // o11
            // 
            this.o11.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.o11.AutoSize = true;
            this.o11.Location = new System.Drawing.Point(1124, 246);
            this.o11.Name = "o11";
            this.o11.Size = new System.Drawing.Size(71, 17);
            this.o11.TabIndex = 121;
            this.o11.Text = "Priority 11";
            this.o11.UseVisualStyleBackColor = true;
            // 
            // o9
            // 
            this.o9.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.o9.AutoSize = true;
            this.o9.Location = new System.Drawing.Point(1124, 200);
            this.o9.Name = "o9";
            this.o9.Size = new System.Drawing.Size(65, 17);
            this.o9.TabIndex = 118;
            this.o9.Text = "Priority 9";
            this.o9.UseVisualStyleBackColor = true;
            // 
            // o12
            // 
            this.o12.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.o12.AutoSize = true;
            this.o12.Location = new System.Drawing.Point(1124, 269);
            this.o12.Name = "o12";
            this.o12.Size = new System.Drawing.Size(71, 17);
            this.o12.TabIndex = 123;
            this.o12.Text = "Priority 12";
            this.o12.UseVisualStyleBackColor = true;
            // 
            // o14
            // 
            this.o14.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.o14.AutoSize = true;
            this.o14.Location = new System.Drawing.Point(1124, 315);
            this.o14.Name = "o14";
            this.o14.Size = new System.Drawing.Size(71, 17);
            this.o14.TabIndex = 124;
            this.o14.Text = "Priority 14";
            this.o14.UseVisualStyleBackColor = true;
            // 
            // o15
            // 
            this.o15.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.o15.AutoSize = true;
            this.o15.Location = new System.Drawing.Point(1124, 338);
            this.o15.Name = "o15";
            this.o15.Size = new System.Drawing.Size(71, 17);
            this.o15.TabIndex = 125;
            this.o15.Text = "Priority 15";
            this.o15.UseVisualStyleBackColor = true;
            // 
            // o13
            // 
            this.o13.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.o13.AutoSize = true;
            this.o13.Location = new System.Drawing.Point(1124, 292);
            this.o13.Name = "o13";
            this.o13.Size = new System.Drawing.Size(71, 17);
            this.o13.TabIndex = 126;
            this.o13.Text = "Priority 13";
            this.o13.UseVisualStyleBackColor = true;
            // 
            // o16
            // 
            this.o16.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.o16.AutoSize = true;
            this.o16.Location = new System.Drawing.Point(1124, 361);
            this.o16.Name = "o16";
            this.o16.Size = new System.Drawing.Size(71, 17);
            this.o16.TabIndex = 127;
            this.o16.Text = "Priority 16";
            this.o16.UseVisualStyleBackColor = true;
            // 
            // progBar
            // 
            this.progBar.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.progBar.Location = new System.Drawing.Point(12, 636);
            this.progBar.Name = "progBar";
            this.progBar.Size = new System.Drawing.Size(1228, 16);
            this.progBar.TabIndex = 10;
            // 
            // cmdViewProps
            // 
            this.cmdViewProps.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.cmdViewProps.Location = new System.Drawing.Point(1032, 465);
            this.cmdViewProps.Name = "cmdViewProps";
            this.cmdViewProps.Size = new System.Drawing.Size(213, 36);
            this.cmdViewProps.TabIndex = 9;
            this.cmdViewProps.Text = "View Properties in Scope";
            this.cmdViewProps.UseVisualStyleBackColor = true;
            this.cmdViewProps.Click += new System.EventHandler(this.cmdViewProps_Click);
            // 
            // splitContainer1
            // 
            this.splitContainer1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.splitContainer1.Location = new System.Drawing.Point(12, 12);
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
            this.splitContainer1.Size = new System.Drawing.Size(1013, 604);
            this.splitContainer1.SplitterDistance = 680;
            this.splitContainer1.TabIndex = 28;
            // 
            // splitContainer2
            // 
            this.splitContainer2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer2.Location = new System.Drawing.Point(0, 0);
            this.splitContainer2.Name = "splitContainer2";
            // 
            // splitContainer2.Panel1
            // 
            this.splitContainer2.Panel1.Controls.Add(this.splitContainer3);
            // 
            // splitContainer2.Panel2
            // 
            this.splitContainer2.Panel2.Controls.Add(this.splitContainer4);
            this.splitContainer2.Size = new System.Drawing.Size(680, 604);
            this.splitContainer2.SplitterDistance = 285;
            this.splitContainer2.TabIndex = 0;
            // 
            // splitContainer3
            // 
            this.splitContainer3.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer3.FixedPanel = System.Windows.Forms.FixedPanel.Panel2;
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
            this.splitContainer3.Panel2.Controls.Add(this.txtDeviceFilter);
            this.splitContainer3.Panel2.Controls.Add(this.cmdPopulateDevices);
            this.splitContainer3.Size = new System.Drawing.Size(285, 604);
            this.splitContainer3.SplitterDistance = 560;
            this.splitContainer3.TabIndex = 1;
            // 
            // txtDeviceFilter
            // 
            this.txtDeviceFilter.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtDeviceFilter.Location = new System.Drawing.Point(152, 11);
            this.txtDeviceFilter.MinimumSize = new System.Drawing.Size(50, 4);
            this.txtDeviceFilter.Name = "txtDeviceFilter";
            this.txtDeviceFilter.Size = new System.Drawing.Size(105, 20);
            this.txtDeviceFilter.TabIndex = 4;
            this.txtDeviceFilter.TextChanged += new System.EventHandler(this.txtDeviceFilter_TextChanged);
            // 
            // splitContainer4
            // 
            this.splitContainer4.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer4.FixedPanel = System.Windows.Forms.FixedPanel.Panel2;
            this.splitContainer4.IsSplitterFixed = true;
            this.splitContainer4.Location = new System.Drawing.Point(0, 0);
            this.splitContainer4.Margin = new System.Windows.Forms.Padding(0);
            this.splitContainer4.Name = "splitContainer4";
            this.splitContainer4.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainer4.Panel1
            // 
            this.splitContainer4.Panel1.Controls.Add(this.PointList);
            // 
            // splitContainer4.Panel2
            // 
            this.splitContainer4.Panel2.Controls.Add(this.txtPointFilter);
            this.splitContainer4.Panel2.Controls.Add(this.cmdPopulatePoints);
            this.splitContainer4.Size = new System.Drawing.Size(391, 604);
            this.splitContainer4.SplitterDistance = 560;
            this.splitContainer4.TabIndex = 2;
            // 
            // txtPointFilter
            // 
            this.txtPointFilter.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtPointFilter.Location = new System.Drawing.Point(146, 11);
            this.txtPointFilter.MinimumSize = new System.Drawing.Size(100, 4);
            this.txtPointFilter.Name = "txtPointFilter";
            this.txtPointFilter.Size = new System.Drawing.Size(200, 20);
            this.txtPointFilter.TabIndex = 6;
            this.txtPointFilter.TextChanged += new System.EventHandler(this.txtPointFilter_TextChanged);
            // 
            // splitContainer5
            // 
            this.splitContainer5.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer5.FixedPanel = System.Windows.Forms.FixedPanel.Panel2;
            this.splitContainer5.IsSplitterFixed = true;
            this.splitContainer5.Location = new System.Drawing.Point(0, 0);
            this.splitContainer5.Margin = new System.Windows.Forms.Padding(0);
            this.splitContainer5.Name = "splitContainer5";
            this.splitContainer5.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainer5.Panel1
            // 
            this.splitContainer5.Panel1.Controls.Add(this.PropertyList);
            // 
            // splitContainer5.Panel2
            // 
            this.splitContainer5.Panel2.Controls.Add(this.cmdPopulateProperties);
            this.splitContainer5.Size = new System.Drawing.Size(329, 604);
            this.splitContainer5.SplitterDistance = 560;
            this.splitContainer5.TabIndex = 3;
            // 
            // PatienceLabel
            // 
            this.PatienceLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.PatienceLabel.AutoSize = true;
            this.PatienceLabel.Location = new System.Drawing.Point(59, 620);
            this.PatienceLabel.Name = "PatienceLabel";
            this.PatienceLabel.Size = new System.Drawing.Size(852, 13);
            this.PatienceLabel.TabIndex = 29;
            this.PatienceLabel.Text = "Please be patient - the global commander has absolutely no prior knowledge of the" +
    " nework, or the duplicity of bacnet objects in each device, and must poll each d" +
    "evice individually.";
            this.PatienceLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.PatienceLabel.Visible = false;
            // 
            // PatienceTimer
            // 
            this.PatienceTimer.Interval = 1000;
            this.PatienceTimer.Tick += new System.EventHandler(this.PatienceTimer_Tick);
            // 
            // radComObj
            // 
            this.radComObj.AutoSize = true;
            this.radComObj.Location = new System.Drawing.Point(6, 19);
            this.radComObj.Name = "radComObj";
            this.radComObj.Size = new System.Drawing.Size(198, 17);
            this.radComObj.TabIndex = 128;
            this.radComObj.Text = "Common Objects (Selected Devices)";
            this.delimToolTip.SetToolTip(this.radComObj, resources.GetString("radComObj.ToolTip"));
            this.radComObj.UseVisualStyleBackColor = true;
            this.radComObj.CheckedChanged += new System.EventHandler(this.radComObj_CheckedChanged);
            // 
            // radJoinObj
            // 
            this.radJoinObj.AutoSize = true;
            this.radJoinObj.Checked = true;
            this.radJoinObj.Location = new System.Drawing.Point(6, 42);
            this.radJoinObj.Name = "radJoinObj";
            this.radJoinObj.Size = new System.Drawing.Size(136, 17);
            this.radJoinObj.TabIndex = 129;
            this.radJoinObj.TabStop = true;
            this.radJoinObj.Text = "Join All Objects into List";
            this.delimToolTip.SetToolTip(this.radJoinObj, "In \"Join All Objects\" mode, all the objects of all the selected controllers are a" +
        "ppended\r\ninto one long list.");
            this.radJoinObj.UseVisualStyleBackColor = true;
            this.radJoinObj.CheckedChanged += new System.EventHandler(this.radJoinObj_CheckedChanged);
            // 
            // lblPtDelimLabel
            // 
            this.lblPtDelimLabel.AutoSize = true;
            this.lblPtDelimLabel.Location = new System.Drawing.Point(3, 69);
            this.lblPtDelimLabel.Name = "lblPtDelimLabel";
            this.lblPtDelimLabel.Size = new System.Drawing.Size(59, 13);
            this.lblPtDelimLabel.TabIndex = 131;
            this.lblPtDelimLabel.Text = "Delimeters:";
            // 
            // grpPointNames
            // 
            this.grpPointNames.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.grpPointNames.Controls.Add(this.chkHyphen);
            this.grpPointNames.Controls.Add(this.chkUnderscore);
            this.grpPointNames.Controls.Add(this.chkComma);
            this.grpPointNames.Controls.Add(this.chkColon);
            this.grpPointNames.Controls.Add(this.chkDot);
            this.grpPointNames.Controls.Add(this.chkApostrophe);
            this.grpPointNames.Controls.Add(this.radComObj);
            this.grpPointNames.Controls.Add(this.lblPtDelimLabel);
            this.grpPointNames.Controls.Add(this.radJoinObj);
            this.grpPointNames.Location = new System.Drawing.Point(1034, 511);
            this.grpPointNames.Name = "grpPointNames";
            this.grpPointNames.Size = new System.Drawing.Size(212, 119);
            this.grpPointNames.TabIndex = 132;
            this.grpPointNames.TabStop = false;
            this.grpPointNames.Text = "\"Populate Points\" Method";
            // 
            // chkHyphen
            // 
            this.chkHyphen.AutoSize = true;
            this.chkHyphen.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.chkHyphen.Location = new System.Drawing.Point(99, 89);
            this.chkHyphen.Name = "chkHyphen";
            this.chkHyphen.Size = new System.Drawing.Size(33, 24);
            this.chkHyphen.TabIndex = 132;
            this.chkHyphen.Text = "-";
            this.delimToolTip.SetToolTip(this.chkHyphen, resources.GetString("chkHyphen.ToolTip"));
            this.chkHyphen.UseVisualStyleBackColor = true;
            this.chkHyphen.Visible = false;
            this.chkHyphen.CheckedChanged += new System.EventHandler(this.chkHyphen_CheckedChanged);
            // 
            // chkUnderscore
            // 
            this.chkUnderscore.AutoSize = true;
            this.chkUnderscore.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.chkUnderscore.Location = new System.Drawing.Point(68, 92);
            this.chkUnderscore.Name = "chkUnderscore";
            this.chkUnderscore.Size = new System.Drawing.Size(33, 19);
            this.chkUnderscore.TabIndex = 132;
            this.chkUnderscore.Text = "_";
            this.delimToolTip.SetToolTip(this.chkUnderscore, resources.GetString("chkUnderscore.ToolTip"));
            this.chkUnderscore.UseVisualStyleBackColor = true;
            this.chkUnderscore.Visible = false;
            this.chkUnderscore.CheckedChanged += new System.EventHandler(this.chkUnderscore_CheckedChanged);
            // 
            // chkComma
            // 
            this.chkComma.AutoSize = true;
            this.chkComma.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.chkComma.Location = new System.Drawing.Point(172, 63);
            this.chkComma.Name = "chkComma";
            this.chkComma.Size = new System.Drawing.Size(32, 24);
            this.chkComma.TabIndex = 132;
            this.chkComma.Text = ",";
            this.delimToolTip.SetToolTip(this.chkComma, resources.GetString("chkComma.ToolTip"));
            this.chkComma.UseVisualStyleBackColor = true;
            this.chkComma.Visible = false;
            this.chkComma.CheckedChanged += new System.EventHandler(this.chkComma_CheckedChanged);
            // 
            // chkColon
            // 
            this.chkColon.AutoSize = true;
            this.chkColon.Checked = true;
            this.chkColon.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkColon.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.chkColon.Location = new System.Drawing.Point(137, 63);
            this.chkColon.Name = "chkColon";
            this.chkColon.Size = new System.Drawing.Size(32, 24);
            this.chkColon.TabIndex = 132;
            this.chkColon.Text = ":";
            this.delimToolTip.SetToolTip(this.chkColon, resources.GetString("chkColon.ToolTip"));
            this.chkColon.UseVisualStyleBackColor = true;
            this.chkColon.Visible = false;
            this.chkColon.CheckedChanged += new System.EventHandler(this.chkColon_CheckedChanged);
            // 
            // chkDot
            // 
            this.chkDot.AutoSize = true;
            this.chkDot.Checked = true;
            this.chkDot.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkDot.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.chkDot.Location = new System.Drawing.Point(99, 63);
            this.chkDot.Name = "chkDot";
            this.chkDot.Size = new System.Drawing.Size(32, 24);
            this.chkDot.TabIndex = 132;
            this.chkDot.Text = ".";
            this.delimToolTip.SetToolTip(this.chkDot, resources.GetString("chkDot.ToolTip"));
            this.chkDot.UseVisualStyleBackColor = true;
            this.chkDot.Visible = false;
            this.chkDot.CheckedChanged += new System.EventHandler(this.chkDot_CheckedChanged);
            // 
            // chkApostrophe
            // 
            this.chkApostrophe.AutoSize = true;
            this.chkApostrophe.Checked = true;
            this.chkApostrophe.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkApostrophe.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.chkApostrophe.Location = new System.Drawing.Point(68, 63);
            this.chkApostrophe.Name = "chkApostrophe";
            this.chkApostrophe.Size = new System.Drawing.Size(31, 24);
            this.chkApostrophe.TabIndex = 132;
            this.chkApostrophe.Text = "\'";
            this.delimToolTip.SetToolTip(this.chkApostrophe, resources.GetString("chkApostrophe.ToolTip"));
            this.chkApostrophe.UseVisualStyleBackColor = true;
            this.chkApostrophe.Visible = false;
            this.chkApostrophe.CheckedChanged += new System.EventHandler(this.chkApostrophe_CheckedChanged);
            // 
            // delimToolTip
            // 
            this.delimToolTip.AutoPopDelay = 30000;
            this.delimToolTip.InitialDelay = 500;
            this.delimToolTip.ReshowDelay = 100;
            this.delimToolTip.ToolTipIcon = System.Windows.Forms.ToolTipIcon.Info;
            this.delimToolTip.ToolTipTitle = "Info";
            // 
            // GlobalCommander
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1253, 659);
            this.Controls.Add(this.grpPointNames);
            this.Controls.Add(this.PatienceLabel);
            this.Controls.Add(this.splitContainer1);
            this.Controls.Add(this.progBar);
            this.Controls.Add(this.o16);
            this.Controls.Add(this.o12);
            this.Controls.Add(this.o6);
            this.Controls.Add(this.o13);
            this.Controls.Add(this.o9);
            this.Controls.Add(this.o15);
            this.Controls.Add(this.o3);
            this.Controls.Add(this.o11);
            this.Controls.Add(this.o5);
            this.Controls.Add(this.o8);
            this.Controls.Add(this.o14);
            this.Controls.Add(this.o2);
            this.Controls.Add(this.o10);
            this.Controls.Add(this.o4);
            this.Controls.Add(this.o7);
            this.Controls.Add(this.o1);
            this.Controls.Add(this.txtCmdVal);
            this.Controls.Add(this.lblCmdVal);
            this.Controls.Add(this.cmdViewProps);
            this.Controls.Add(this.cmdCommand);
            this.Name = "GlobalCommander";
            this.Text = "Yabe Global Commander";
            this.Load += new System.EventHandler(this.GlobalCommander_Load);
            this.Shown += new System.EventHandler(this.GlobalCommander_Shown);
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
            this.splitContainer3.Panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer3)).EndInit();
            this.splitContainer3.ResumeLayout(false);
            this.splitContainer4.Panel1.ResumeLayout(false);
            this.splitContainer4.Panel2.ResumeLayout(false);
            this.splitContainer4.Panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer4)).EndInit();
            this.splitContainer4.ResumeLayout(false);
            this.splitContainer5.Panel1.ResumeLayout(false);
            this.splitContainer5.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer5)).EndInit();
            this.splitContainer5.ResumeLayout(false);
            this.grpPointNames.ResumeLayout(false);
            this.grpPointNames.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ListBox DeviceList;
        private System.Windows.Forms.ListBox PointList;
        private System.Windows.Forms.ListBox PropertyList;
        private System.Windows.Forms.Button cmdPopulateDevices;
        private System.Windows.Forms.Button cmdPopulatePoints;
        private System.Windows.Forms.Button cmdPopulateProperties;
        private System.Windows.Forms.Button cmdCommand;
        private System.Windows.Forms.Label lblCmdVal;
        private System.Windows.Forms.TextBox txtCmdVal;
        private System.Windows.Forms.RadioButton o1;
        private System.Windows.Forms.RadioButton o2;
        private System.Windows.Forms.RadioButton o3;
        private System.Windows.Forms.RadioButton o4;
        private System.Windows.Forms.RadioButton o5;
        private System.Windows.Forms.RadioButton o6;
        private System.Windows.Forms.RadioButton o7;
        private System.Windows.Forms.RadioButton o10;
        private System.Windows.Forms.RadioButton o8;
        private System.Windows.Forms.RadioButton o11;
        private System.Windows.Forms.RadioButton o9;
        private System.Windows.Forms.RadioButton o12;
        private System.Windows.Forms.RadioButton o14;
        private System.Windows.Forms.RadioButton o15;
        private System.Windows.Forms.RadioButton o13;
        private System.Windows.Forms.RadioButton o16;
        private System.Windows.Forms.ProgressBar progBar;
        private System.Windows.Forms.Button cmdViewProps;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.SplitContainer splitContainer2;
        private System.Windows.Forms.SplitContainer splitContainer3;
        private System.Windows.Forms.SplitContainer splitContainer4;
        private System.Windows.Forms.SplitContainer splitContainer5;
        private System.Windows.Forms.TextBox txtDeviceFilter;
        private System.Windows.Forms.TextBox txtPointFilter;
        private System.Windows.Forms.Label PatienceLabel;
        private System.Windows.Forms.Timer PatienceTimer;
        private System.Windows.Forms.RadioButton radComObj;
        private System.Windows.Forms.RadioButton radJoinObj;
        private System.Windows.Forms.Label lblPtDelimLabel;
        private System.Windows.Forms.GroupBox grpPointNames;
        private System.Windows.Forms.CheckBox chkHyphen;
        private System.Windows.Forms.CheckBox chkUnderscore;
        private System.Windows.Forms.CheckBox chkComma;
        private System.Windows.Forms.CheckBox chkColon;
        private System.Windows.Forms.CheckBox chkDot;
        private System.Windows.Forms.CheckBox chkApostrophe;
        private System.Windows.Forms.ToolTip delimToolTip;
    }
}