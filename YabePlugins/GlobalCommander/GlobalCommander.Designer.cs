
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
            this.btnSyncTime = new System.Windows.Forms.Button();
            this.oComProp = new System.Windows.Forms.RadioButton();
            this.oAnyProp = new System.Windows.Forms.RadioButton();
            this.cboPriority = new System.Windows.Forms.ComboBox();
            this.lblPrio = new System.Windows.Forms.Label();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.lblGlobal1 = new System.Windows.Forms.Label();
            this.groupBox3 = new System.Windows.Forms.GroupBox();
            this.groupBox4 = new System.Windows.Forms.GroupBox();
            this.grpDevices = new System.Windows.Forms.GroupBox();
            this.label2 = new System.Windows.Forms.Label();
            this.txtWhoIsHigh = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.txtWhoIsLow = new System.Windows.Forms.TextBox();
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
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.groupBox3.SuspendLayout();
            this.groupBox4.SuspendLayout();
            this.grpDevices.SuspendLayout();
            this.SuspendLayout();
            // 
            // DeviceList
            // 
            this.DeviceList.Dock = System.Windows.Forms.DockStyle.Fill;
            this.DeviceList.FormattingEnabled = true;
            this.DeviceList.Location = new System.Drawing.Point(0, 0);
            this.DeviceList.Name = "DeviceList";
            this.DeviceList.SelectionMode = System.Windows.Forms.SelectionMode.MultiExtended;
            this.DeviceList.Size = new System.Drawing.Size(312, 592);
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
            this.PointList.Size = new System.Drawing.Size(375, 592);
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
            this.PropertyList.Size = new System.Drawing.Size(356, 592);
            this.PropertyList.TabIndex = 2;
            this.PropertyList.DoubleClick += new System.EventHandler(this.PropertyList_DoubleClick);
            // 
            // cmdPopulateDevices
            // 
            this.cmdPopulateDevices.Location = new System.Drawing.Point(0, 2);
            this.cmdPopulateDevices.Name = "cmdPopulateDevices";
            this.cmdPopulateDevices.Size = new System.Drawing.Size(140, 36);
            this.cmdPopulateDevices.TabIndex = 3;
            this.cmdPopulateDevices.Text = "Populate Devices";
            this.delimToolTip.SetToolTip(this.cmdPopulateDevices, resources.GetString("cmdPopulateDevices.ToolTip"));
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
            this.delimToolTip.SetToolTip(this.cmdPopulatePoints, "Populate the objects from the selected devices.\r\nThe behaviour of this button is " +
        "dependant on\r\nthe \"Populate Points Method\" setting to the right.");
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
            this.delimToolTip.SetToolTip(this.cmdPopulateProperties, "Populate the relevant properties from the selected\r\nobjects. The behaviour of thi" +
        "s button is dependant\r\non the \"Populate Properties Method\" setting to the\r\nright" +
        ".\r\n");
            this.cmdPopulateProperties.UseVisualStyleBackColor = true;
            this.cmdPopulateProperties.Click += new System.EventHandler(this.cmdPopulateProperties_Click);
            // 
            // cmdCommand
            // 
            this.cmdCommand.Location = new System.Drawing.Point(6, 79);
            this.cmdCommand.Name = "cmdCommand";
            this.cmdCommand.Size = new System.Drawing.Size(214, 36);
            this.cmdCommand.TabIndex = 26;
            this.cmdCommand.Text = "Globally Command";
            this.delimToolTip.SetToolTip(this.cmdCommand, "Command/relinquish the selected property\r\non all selected objects/devices to the " +
        "specified\r\nvalue.");
            this.cmdCommand.UseVisualStyleBackColor = true;
            this.cmdCommand.Click += new System.EventHandler(this.cmdCommand_Click);
            // 
            // lblCmdVal
            // 
            this.lblCmdVal.AutoSize = true;
            this.lblCmdVal.Location = new System.Drawing.Point(6, 23);
            this.lblCmdVal.Name = "lblCmdVal";
            this.lblCmdVal.Size = new System.Drawing.Size(87, 13);
            this.lblCmdVal.TabIndex = 21;
            this.lblCmdVal.Text = "Command Value:";
            // 
            // txtCmdVal
            // 
            this.txtCmdVal.Location = new System.Drawing.Point(99, 20);
            this.txtCmdVal.Name = "txtCmdVal";
            this.txtCmdVal.Size = new System.Drawing.Size(120, 20);
            this.txtCmdVal.TabIndex = 22;
            // 
            // progBar
            // 
            this.progBar.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.progBar.Location = new System.Drawing.Point(12, 669);
            this.progBar.Name = "progBar";
            this.progBar.Size = new System.Drawing.Size(1276, 16);
            this.progBar.TabIndex = 10;
            // 
            // cmdViewProps
            // 
            this.cmdViewProps.Location = new System.Drawing.Point(6, 19);
            this.cmdViewProps.Name = "cmdViewProps";
            this.cmdViewProps.Size = new System.Drawing.Size(214, 36);
            this.cmdViewProps.TabIndex = 31;
            this.cmdViewProps.Text = "View Properties in Scope";
            this.delimToolTip.SetToolTip(this.cmdViewProps, "View all the curent value of the selected property\r\non all selected objects/devic" +
        "es that would be globally\r\ncommanded. The values can also be commanded\r\none-by-o" +
        "ne from here.");
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
            this.splitContainer1.Size = new System.Drawing.Size(1051, 637);
            this.splitContainer1.SplitterDistance = 691;
            this.splitContainer1.TabIndex = 101;
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
            this.splitContainer2.Size = new System.Drawing.Size(691, 637);
            this.splitContainer2.SplitterDistance = 312;
            this.splitContainer2.TabIndex = 100;
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
            this.splitContainer3.Size = new System.Drawing.Size(312, 637);
            this.splitContainer3.SplitterDistance = 592;
            this.splitContainer3.TabIndex = 1;
            // 
            // txtDeviceFilter
            // 
            this.txtDeviceFilter.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtDeviceFilter.Location = new System.Drawing.Point(152, 11);
            this.txtDeviceFilter.MinimumSize = new System.Drawing.Size(50, 4);
            this.txtDeviceFilter.Name = "txtDeviceFilter";
            this.txtDeviceFilter.Size = new System.Drawing.Size(160, 20);
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
            this.splitContainer4.Size = new System.Drawing.Size(375, 637);
            this.splitContainer4.SplitterDistance = 592;
            this.splitContainer4.TabIndex = 2;
            // 
            // txtPointFilter
            // 
            this.txtPointFilter.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtPointFilter.Location = new System.Drawing.Point(146, 11);
            this.txtPointFilter.MinimumSize = new System.Drawing.Size(100, 4);
            this.txtPointFilter.Name = "txtPointFilter";
            this.txtPointFilter.Size = new System.Drawing.Size(219, 20);
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
            this.splitContainer5.Size = new System.Drawing.Size(356, 637);
            this.splitContainer5.SplitterDistance = 592;
            this.splitContainer5.TabIndex = 3;
            // 
            // PatienceLabel
            // 
            this.PatienceLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.PatienceLabel.AutoSize = true;
            this.PatienceLabel.Location = new System.Drawing.Point(59, 653);
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
            this.radComObj.TabIndex = 51;
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
            this.radJoinObj.TabIndex = 52;
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
            this.lblPtDelimLabel.TabIndex = 53;
            this.lblPtDelimLabel.Text = "Delimeters:";
            this.lblPtDelimLabel.Visible = false;
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
            this.grpPointNames.Location = new System.Drawing.Point(1069, 392);
            this.grpPointNames.Name = "grpPointNames";
            this.grpPointNames.Size = new System.Drawing.Size(226, 119);
            this.grpPointNames.TabIndex = 50;
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
            this.chkHyphen.TabIndex = 59;
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
            this.chkUnderscore.TabIndex = 58;
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
            this.chkComma.TabIndex = 57;
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
            this.chkColon.TabIndex = 56;
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
            this.chkDot.TabIndex = 55;
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
            this.chkApostrophe.TabIndex = 54;
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
            // btnSyncTime
            // 
            this.btnSyncTime.Location = new System.Drawing.Point(6, 19);
            this.btnSyncTime.Name = "btnSyncTime";
            this.btnSyncTime.Size = new System.Drawing.Size(214, 36);
            this.btnSyncTime.TabIndex = 9;
            this.btnSyncTime.Text = "Sync Time";
            this.delimToolTip.SetToolTip(this.btnSyncTime, "Synchronize the time of the selected devices\r\nwith the PC\'s time. This will be ei" +
        "ther local\r\ntime or UTC time, depending on the setting\r\n\"TimeSynchronize_UTC\".");
            this.btnSyncTime.UseVisualStyleBackColor = true;
            this.btnSyncTime.Click += new System.EventHandler(this.btnSyncTime_Click);
            // 
            // oComProp
            // 
            this.oComProp.AutoSize = true;
            this.oComProp.Checked = true;
            this.oComProp.Location = new System.Drawing.Point(6, 20);
            this.oComProp.Name = "oComProp";
            this.oComProp.Size = new System.Drawing.Size(150, 30);
            this.oComProp.TabIndex = 61;
            this.oComProp.TabStop = true;
            this.oComProp.Text = "Properties Common to ALL\r\nSelected Points";
            this.delimToolTip.SetToolTip(this.oComProp, resources.GetString("oComProp.ToolTip"));
            this.oComProp.UseVisualStyleBackColor = true;
            this.oComProp.CheckedChanged += new System.EventHandler(this.oComProp_CheckedChanged);
            // 
            // oAnyProp
            // 
            this.oAnyProp.AutoSize = true;
            this.oAnyProp.Location = new System.Drawing.Point(6, 59);
            this.oAnyProp.Name = "oAnyProp";
            this.oAnyProp.Size = new System.Drawing.Size(154, 30);
            this.oAnyProp.TabIndex = 62;
            this.oAnyProp.Text = "Properties that Exist in ANY\r\nSelected Point";
            this.delimToolTip.SetToolTip(this.oAnyProp, resources.GetString("oAnyProp.ToolTip"));
            this.oAnyProp.UseVisualStyleBackColor = true;
            this.oAnyProp.CheckedChanged += new System.EventHandler(this.oAnyProp_CheckedChanged);
            // 
            // cboPriority
            // 
            this.cboPriority.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboPriority.FormattingEnabled = true;
            this.cboPriority.Items.AddRange(new object[] {
            "1 (Manual Life Safety)",
            "2 (Auto Life Safety)",
            "3",
            "4",
            "5 (Critical Equipment Protection)",
            "6 (Minimum On/Off Time)",
            "7",
            "8 (Manual Operator)",
            "9",
            "10",
            "11",
            "12",
            "13",
            "14",
            "15",
            "16"});
            this.cboPriority.Location = new System.Drawing.Point(52, 49);
            this.cboPriority.Name = "cboPriority";
            this.cboPriority.Size = new System.Drawing.Size(167, 21);
            this.cboPriority.TabIndex = 24;
            // 
            // lblPrio
            // 
            this.lblPrio.AutoSize = true;
            this.lblPrio.Location = new System.Drawing.Point(6, 52);
            this.lblPrio.Name = "lblPrio";
            this.lblPrio.Size = new System.Drawing.Size(41, 13);
            this.lblPrio.TabIndex = 23;
            this.lblPrio.Text = "Priority:";
            // 
            // groupBox1
            // 
            this.groupBox1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox1.Controls.Add(this.oComProp);
            this.groupBox1.Controls.Add(this.oAnyProp);
            this.groupBox1.Location = new System.Drawing.Point(1069, 517);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(226, 98);
            this.groupBox1.TabIndex = 60;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "\"Populate Properties\" Method";
            // 
            // groupBox2
            // 
            this.groupBox2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox2.Controls.Add(this.btnSyncTime);
            this.groupBox2.Location = new System.Drawing.Point(1069, 12);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(226, 69);
            this.groupBox2.TabIndex = 8;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Device Commands";
            // 
            // lblGlobal1
            // 
            this.lblGlobal1.AutoSize = true;
            this.lblGlobal1.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblGlobal1.ForeColor = System.Drawing.Color.Maroon;
            this.lblGlobal1.Location = new System.Drawing.Point(6, 79);
            this.lblGlobal1.Name = "lblGlobal1";
            this.lblGlobal1.Size = new System.Drawing.Size(214, 39);
            this.lblGlobal1.TabIndex = 25;
            this.lblGlobal1.Text = "The \"Populate Properties\" method\r\nneeds to be set to \"Common to ALL\"\r\nin order to" +
    " globally command.";
            this.lblGlobal1.Visible = false;
            // 
            // groupBox3
            // 
            this.groupBox3.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox3.Controls.Add(this.lblGlobal1);
            this.groupBox3.Controls.Add(this.cmdCommand);
            this.groupBox3.Controls.Add(this.lblCmdVal);
            this.groupBox3.Controls.Add(this.lblPrio);
            this.groupBox3.Controls.Add(this.txtCmdVal);
            this.groupBox3.Controls.Add(this.cboPriority);
            this.groupBox3.Location = new System.Drawing.Point(1069, 95);
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.Size = new System.Drawing.Size(226, 130);
            this.groupBox3.TabIndex = 20;
            this.groupBox3.TabStop = false;
            this.groupBox3.Text = "Global Commanding";
            // 
            // groupBox4
            // 
            this.groupBox4.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox4.Controls.Add(this.cmdViewProps);
            this.groupBox4.Location = new System.Drawing.Point(1069, 231);
            this.groupBox4.Name = "groupBox4";
            this.groupBox4.Size = new System.Drawing.Size(226, 69);
            this.groupBox4.TabIndex = 30;
            this.groupBox4.TabStop = false;
            this.groupBox4.Text = "Viewing";
            // 
            // grpDevices
            // 
            this.grpDevices.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.grpDevices.Controls.Add(this.label2);
            this.grpDevices.Controls.Add(this.txtWhoIsHigh);
            this.grpDevices.Controls.Add(this.label1);
            this.grpDevices.Controls.Add(this.txtWhoIsLow);
            this.grpDevices.Location = new System.Drawing.Point(1069, 306);
            this.grpDevices.Name = "grpDevices";
            this.grpDevices.Size = new System.Drawing.Size(226, 80);
            this.grpDevices.TabIndex = 40;
            this.grpDevices.TabStop = false;
            this.grpDevices.Text = "\"Populate Devices\" Settings";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(6, 49);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(100, 13);
            this.label2.TabIndex = 43;
            this.label2.Text = "\"WhoIs\" Limit High:";
            // 
            // txtWhoIsHigh
            // 
            this.txtWhoIsHigh.Location = new System.Drawing.Point(137, 46);
            this.txtWhoIsHigh.Name = "txtWhoIsHigh";
            this.txtWhoIsHigh.Size = new System.Drawing.Size(82, 20);
            this.txtWhoIsHigh.TabIndex = 44;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(6, 25);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(98, 13);
            this.label1.TabIndex = 41;
            this.label1.Text = "\"WhoIs\" Limit Low:";
            // 
            // txtWhoIsLow
            // 
            this.txtWhoIsLow.Location = new System.Drawing.Point(137, 22);
            this.txtWhoIsLow.Name = "txtWhoIsLow";
            this.txtWhoIsLow.Size = new System.Drawing.Size(82, 20);
            this.txtWhoIsLow.TabIndex = 42;
            // 
            // GlobalCommander
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1301, 692);
            this.Controls.Add(this.groupBox4);
            this.Controls.Add(this.grpDevices);
            this.Controls.Add(this.groupBox3);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.grpPointNames);
            this.Controls.Add(this.PatienceLabel);
            this.Controls.Add(this.splitContainer1);
            this.Controls.Add(this.progBar);
            this.MinimumSize = new System.Drawing.Size(1000, 694);
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
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.groupBox3.ResumeLayout(false);
            this.groupBox3.PerformLayout();
            this.groupBox4.ResumeLayout(false);
            this.grpDevices.ResumeLayout(false);
            this.grpDevices.PerformLayout();
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
        private System.Windows.Forms.Button btnSyncTime;
        private System.Windows.Forms.ComboBox cboPriority;
        private System.Windows.Forms.Label lblPrio;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.RadioButton oComProp;
        private System.Windows.Forms.RadioButton oAnyProp;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.Label lblGlobal1;
        private System.Windows.Forms.GroupBox groupBox3;
        private System.Windows.Forms.GroupBox groupBox4;
        private System.Windows.Forms.GroupBox grpDevices;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox txtWhoIsHigh;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox txtWhoIsLow;
    }
}