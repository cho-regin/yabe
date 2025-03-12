/**************************************************************************
*                           MIT License
* 
* Copyright (C) 2014 Morten Kvistgaard <mk@pch-engineering.dk>
*
* Permission is hereby granted, free of charge, to any person obtaining
* a copy of this software and associated documentation files (the
* "Software"), to deal in the Software without restriction, including
* without limitation the rights to use, copy, modify, merge, publish,
* distribute, sublicense, and/or sell copies of the Software, and to
* permit persons to whom the Software is furnished to do so, subject to
* the following conditions:
*
* The above copyright notice and this permission notice shall be included
* in all copies or substantial portions of the Software.
*
* THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
* EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
* MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
* IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY
* CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
* TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
* SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*
*********************************************************************/

using System;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO.BACnet;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using ZedGraph;

namespace Yabe
{
    public partial class SettingsDialog : Form
    {
        internal enum SplitterOrientation
        {
            Horizontal,
            Vertical
        }
        internal enum BaudRate
        {
            [Description("9600 Baud")]
            Rate9600 = 9600,
            [Description("19200 Baud")]
            Rate19200 = 19200,
            [Description("38400 Baud")]
            Rate38400 = 38400,
            [Description("57600 Baud")]
            Rate57600 = 57600,
            [Description("76800 Baud")]
            Rate76800 = 76800,
            [Description("115000 Baud")]
            Rate115000 = 115000
        }

        public class EnumConverter : System.ComponentModel.EnumConverter
        {
            public EnumConverter(Type type) : base(type)
            {
                enumType = type;
            }


            public override bool CanConvertTo(ITypeDescriptorContext context, Type destType) => (destType == typeof(string));
            public override object ConvertTo(ITypeDescriptorContext context, CultureInfo info, object value, Type tDestType) => UtlEnum.GetEnumDescription(enumType.GetField(Enum.GetName(enumType, value)));
            public override bool CanConvertFrom(ITypeDescriptorContext context, Type srcType) => (srcType == typeof(string));
            public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo info, object value) => UtlEnum.GetEnumValue((String)value, enumType);


            private Type enumType;
        }
        public class UtlEnum
        {
            public static string GetEnumDescription(FieldInfo info)
            {
                var attr = (DescriptionAttribute[])info.GetCustomAttributes(typeof(DescriptionAttribute), false);
                if ((attr != null) && (attr.Length > 0))
                    return (attr[0].Description);
                else
                    return (info.Name);
            }
            public static int GetEnumValue(string text, Type enumType, bool exact = true)
            {
                foreach (int iVal in Enum.GetValues(enumType))
                {
                    if (GetEnumDescription(enumType.GetField(Enum.GetName(enumType, iVal))) == text)
                        return (iVal);
                }
                return (-1);
            }
        }

        /// <summary>
        /// Wrapper class to provide some description of each <see cref="Properties.Settings">settings</see> property to the user. 
        /// </summary>
        internal class SettingsDescriptor
        {
            #region Constants.Category
            const string CAT_GENERAL = "General";
            const string CAT_GUI = "Graphical User Interface";
            const string CAT_EDEEXPORT = "EDE Export";
            const string CAT_COV = "Change of Value";
            const string CAT_IP = "BACnet/IP";
            const string CAT_MSTP = "MS/TP";
            const string CAT_SC = "BACnet/SC";
            #endregion


            public SettingsDescriptor(Properties.Settings instance)
            {
                this.instance = instance;

                if (Debugger.IsAttached)
                {
                    // Detect missing descriptions:
                    var thisProp = this.GetType().GetProperties();
                    var expectProp = instance.GetType().GetProperties(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance);
                    var missingProp = expectProp
                        .Where(exProp => !thisProp.Any(thProp => (thProp.Name == exProp.Name)))
                        .Select(exProp => $"- {exProp.Name}");
                    if (missingProp.Any())
                        // Dear developer:
                        // You throw this exception because you probably have added or removed some setting!?
                        // > If so, please ensure to add one "descriptive property" per settings-property and fill in some details to show to the user.
                        throw new NotImplementedException($"Detected missing property descriptions for the following settings:\n{string.Join("\n", missingProp)}");
                }
            }

            #region Properties

            [DisplayName("Device Class Structure")]
            [Description("Activates/Describes the Class View in parallel with the Network View. Value is a semicon string with Groups name and device Ids inside like HVAC(3,9);Lighting(9,23);Building(HVAC,Lighting,40,27)")]
            [Category(CAT_GUI)]
            public string DeviceClassStructure { set { instance.DeviceClassStructure = value; } get { return instance.DeviceClassStructure; } }

            [DisplayName("Device Mode View Not Affected name")]
            [Description("Name of the Folder for no affected Devices. Hidden if name is left blank")]
            [Category(CAT_GUI)]
            public string NotAffectedFolderName { set { instance.NotAffectedFolderName = value; } get { return instance.NotAffectedFolderName; } }

            [DisplayName("Device Mode View")]
            [Description("Specify how the Device view should be organised")]
            [Category(CAT_GUI)]
            public DeviceTreeViewType DeviceViewMode { set { instance.DeviceViewMode = value; } get { return instance.DeviceViewMode; } }

            [DisplayName("Object splitter orientation")]
            [Description("Global windows split organisation in the GUI interface.")]
            [Category(CAT_GUI)]
            public SplitterOrientation Vertical_Object_Splitter_Orientation { set { instance.Vertical_Object_Splitter_Orientation = value.Equals(SplitterOrientation.Vertical); } get { return (SplitterOrientation)Convert.ToInt32(instance.Vertical_Object_Splitter_Orientation); } }
            [DisplayName("Auto-store objectnames file")]
            [Description("Name of the objectnames database file.")]
            [Category(CAT_GUI)]
            public string Auto_Store_Object_Names_File { set { instance.Auto_Store_Object_Names_File = value; } get { return instance.Auto_Store_Object_Names_File; } }
            [DisplayName("Show description when useful")]
            [Description("Displays the description of objects in some places (requires more network activities to get it).")]
            [Category(CAT_GUI)]
            public bool ShowDescriptionWhenUseful { set { instance.ShowDescriptionWhenUseful = value; } get { return instance.ShowDescriptionWhenUseful; } }
            [DisplayName("Address space view")]
            [Description("The Addendum 135d defines a 'Structured View' entry in the address space. This enables a hierarchical address space (selection 'Structured'). Though if you like the flat model better, set this to 'List'. The option both combines both display modes. FieldTechnician view is a flat view with filter described in the file SimplifiedViewFilter.xml")]
            [Category(CAT_GUI)]
            public AddressTreeViewType Address_Space_Structured_View { set { instance.Address_Space_Structured_View = value; } get { return instance.Address_Space_Structured_View; } }
            [DisplayName("Display ID with name")]
            [Description("Leaves properties Id (such as ANALOG_INPUT:0) along with the properties name or hides this Id.")]
            [Category(CAT_GUI)]
            public bool DisplayIdWithName { set { instance.DisplayIdWithName = value; } get { return instance.DisplayIdWithName; } }
            [DisplayName("Dot style")]
            [Description("Allows to select the style of the plotter dots.")]
            [Category(CAT_GUI)]
            public SymbolType GraphDotStyle { set { instance.GraphDotStyle = value; } get { return instance.GraphDotStyle; } }
            [DisplayName("Line step")]
            [Description("Used by the graph to show or not a stair to link two successive values on a line.")]
            [Category(CAT_GUI)]
            public bool GraphLineStep { set { instance.GraphLineStep = value; } get { return instance.GraphLineStep; } }
            [DisplayName("Auto-store objectnames")]
            [Description("Automatic update of the objectnames database.")]
            [Category(CAT_GUI)]
            public bool Auto_Store_Object_Names { set { instance.Auto_Store_Object_Names = value; } get { return instance.Auto_Store_Object_Names; } }
            [DisplayName("Auto-store period minutes")]
            [Description("Objectnames database file flush period when new associations are available.")]
            [Category(CAT_GUI)]
            public int Auto_Store_Period_Minutes { set { instance.Auto_Store_Period_Minutes = value; } get { return instance.Auto_Store_Period_Minutes; } }
            [DisplayName("Show highlight filter")]
            [Description("Allows to highlight items which match the filter. Helpful to select specific items in a larger device or object list.")]
            [Category(CAT_GUI)]
            public bool ShowHighLightFilter { set { instance.ShowHighLightFilter = value; } get { return instance.ShowHighLightFilter; } }

            [DisplayName("Auto Expand Grid Array Max Size")]
            [Description("Maximum number of elements in Array to automatically expanded it in the Properties Grid. Zero to disable the option")]
            [Category(CAT_GUI)]
            public int GridArrayExpandMaxSize { set { instance.GridArrayExpandMaxSize = value; } get { return instance.GridArrayExpandMaxSize; } }

            [DisplayName("Always Expanded Properties")]
            [Description("Always expand the Properties in Grid even with a large array (comma separated names, case sensible, no space)")]
            [Category(CAT_GUI)]
            public String GridAlwaysExpandProperties { set { instance.GridAlwaysExpandProperties = value; } get { return instance.GridAlwaysExpandProperties; } }

            [DisplayName("Single file")]
            [Description("Export multiple devices into a single EDE file.")]
            [Category(CAT_EDEEXPORT)]
            public bool EDE_SingleFile { set { instance.EDE_SingleFile = value; } get { return instance.EDE_SingleFile; } }
            [DisplayName("Common files")]
            [Description("Generate files for objecttypes and units (known as common files).")]
            [Category(CAT_EDEEXPORT)]
            public bool EDE_CommonFiles { set { instance.EDE_CommonFiles = value; } get { return instance.EDE_CommonFiles; } }
            [DisplayName("EDE Separator")]
            [Description("EDE Columns separator")]
            [Category(CAT_EDEEXPORT)]
            public char EDE_Separator { set { instance.EDE_Separator = value; } get { return instance.EDE_Separator; } }

            [DisplayName("Issue confirmed notifies")]
            [Description("By default notifications will be sent 'unconfirmed'. If you think your notifications are important set this to 'true' instead.")]
            [Category(CAT_COV)]
            public bool Subscriptions_IssueConfirmedNotifies { set { instance.Subscriptions_IssueConfirmedNotifies = value; } get { return instance.Subscriptions_IssueConfirmedNotifies; } }
            [DisplayName("Lifetime")]
            [Description("Subscriptions will be created with this lifetime. E.g. after 120 seconds the subscription will be removed by device. Set to 0 to disable.")]
            [Category(CAT_COV)]
            public uint Subscriptions_Lifetime { set { instance.Subscriptions_Lifetime = value; } get { return instance.Subscriptions_Lifetime; } }
            [DisplayName("Replacement polling period")]
            [Description("Default time in milliseconds for the polling interval in case COV fails.")]
            [Category(CAT_COV)]
            public uint Subscriptions_ReplacementPollingPeriod { set { instance.Subscriptions_ReplacementPollingPeriod = value; } get { return instance.Subscriptions_ReplacementPollingPeriod; } }
            [DisplayName("COV time formater")]
            [Description("Time format to display COV timestamps.")]
            [Category(CAT_COV)]
            public string COVTimeFormater { set { instance.COVTimeFormater = value; } get { return instance.COVTimeFormater; } }
            [DisplayName("COV export path")]
            [Description("Default pathname to save COV graph export files.")]
            [Category(CAT_COV)]
            public string COV_Export_Path { set { instance.COV_Export_Path = value; } get { return instance.COV_Export_Path; } }
            [DisplayName("Use polling by default")]
            [Description("Do not try COV subscription but polling with a predefined period (see replacement polling period).")]
            [Category(CAT_COV)]
            public bool UsePollingByDefault { set { instance.UsePollingByDefault = value; } get { return instance.UsePollingByDefault; } }

            [DisplayName("Exclusive use of socket")]
            [Description("Set this to 'true' to force single socket usage on port 0xBAC0. A value of 'false' will create an extra unicast socket and allow multiple clients on same IP address / machine.")]
            [Category(CAT_IP)]
            public bool Udp_ExclusiveUseOfSocket { set { instance.Udp_ExclusiveUseOfSocket = value; } get { return instance.Udp_ExclusiveUseOfSocket; } }
            [DisplayName("Default Port")]
            [Description("Port number of the UDP port used. 47808 (0xBAC0) is reserved for BACnet traffic.")]
            [Category(CAT_IP)]
            public decimal DefaultUdpPort { set { instance.DefaultUdpPort = value; } get { return instance.DefaultUdpPort; } }
            [DisplayName("Do not fragment")]
            [Description("This will enforce (if set to 'true') no fragmentation on the udp. It ought to be enforced, but it turns out that MTU is a bit tricky.")]
            [Category(CAT_IP)]
            public bool Udp_DontFragment { set { instance.Udp_DontFragment = value; } get { return instance.Udp_DontFragment; } }
            [DisplayName("Max. payload")]
            [Description("The maximum payload for UDP seems to differ from the expectations of BACnet. The most common payload is 1472. Which is 1500 when added with the 28 bytes IP headers. This number is determined by your local switch / router though.")]
            [Category(CAT_IP)]
            public int Udp_MaxPayload { set { instance.Udp_MaxPayload = value; } get { return instance.Udp_MaxPayload; } }
            [DisplayName("Default UDP/IP")]
            [Description("IP address used as default for BACnet/IP communication.")]
            [Category(CAT_IP)]
            public string DefaultUdpIp { set { instance.DefaultUdpIp = value; } get { return instance.DefaultUdpIp; } }
            [DisplayName("IPv6 support")]
            [Description("Enables or disables IPv6 communication.")]
            [Category(CAT_IP)]
            public bool IPv6_Support { set { instance.IPv6_Support = value; } get { return instance.IPv6_Support; } }
            [DisplayName("Default BBMD")]
            [Description("IP address and port of the BBMD to connect to as a foreign device.")]
            [Category(CAT_IP)]
            public string DefaultBBMD { set { instance.DefaultBBMD = value; } get { return instance.DefaultBBMD; } }

            [DisplayName("Display free addresses")]
            [Description("By default a MS/TP connection will display all 'free' addresses in the 'Device' tree. This can help select a source address for the program. If you don't want to see the 'free' entries, set this option to 'false'")]
            [Category(CAT_MSTP)]
            public bool MSTP_DisplayFreeAddresses { set { instance.MSTP_DisplayFreeAddresses = value; } get { return instance.MSTP_DisplayFreeAddresses; } }
            [DisplayName("Log state machine")]
            [Description("The MS/TP code is able to display all state changes in log. This is very verbose. It may help you understand the MS/TP better though.")]
            [Category(CAT_MSTP)]
            public bool MSTP_LogStateMachine { set { instance.MSTP_LogStateMachine = value; } get { return instance.MSTP_LogStateMachine; } }
            [DisplayName("Default baudrate")]
            [Description("Default Baudrate used for MS/TP communication")]
            [Category(CAT_MSTP)]
            [TypeConverter(typeof(EnumConverter))]
            public BaudRate DefaultBaudrate { set { instance.DefaultBaudrate = (decimal)value; } get { return (BaudRate)instance.DefaultBaudrate; } }
            [DisplayName("Default source address")]
            [Description("Master source address of YABE. Must be unique in the MS/TP network and shall not be assigned to another MS/TP device.")]
            [Category(CAT_MSTP)]
            public decimal DefaultSourceAddress { set { instance.DefaultSourceAddress = value; } get { return instance.DefaultSourceAddress; } }
            [DisplayName("Default MaxMaster")]
            [Description("'Max_Master' specifies the highest master station address. Must at least be set in the node with the highest station number to avoid unnecessary 'PollForMaster' requests.")]
            [Category(CAT_MSTP)]
            public decimal DefaultMaxMaster { set { instance.DefaultMaxMaster = value; } get { return instance.DefaultMaxMaster; } }
            [DisplayName("Default MaxInfoFrames")]
            [Description("Specifies the number of packets YABE is allowed to send after receiving the token. This allows a load balancing on the MS/TP network.")]
            [Category(CAT_MSTP)]
            public decimal DefaultMaxInfoFrames { set { instance.DefaultMaxInfoFrames = value; } get { return instance.DefaultMaxInfoFrames; } }

            [DisplayName("BACnetSC config file")]
            [Description("Specifies the path and name of the configuration file used for BACnet/Secure Connect communication.")]
            [Category(CAT_SC)]
            public string BACnetSCConfigFile { set { instance.BACnetSCConfigFile = value; } get { return instance.BACnetSCConfigFile; } }

            [DisplayName("Default retries")]
            [Description("Number of APDU retries when no response is given to a request (such as read, write, ...)")]
            [Category(CAT_GENERAL)]
            public decimal DefaultRetries { set { instance.DefaultRetries = value; } get { return instance.DefaultRetries; } }
            [DisplayName("Default timeout")]
            [Description("APDU timeout in ms allowed to the remote device to give responses to requests (such as read, write, ...)")]
            [Category(CAT_GENERAL)]
            public decimal DefaultTimeout { set { instance.DefaultTimeout = value; } get { return instance.DefaultTimeout; } }
            [DisplayName("Default download speed")]
            [Description("This value sets the method for 'file download'. (This is part of the original tests). The default value of '0' will result in a standard 'send request, wait for response' sequence. This is rather efficient on UDP. Value '1' will result in a 'stacked asynchronous' sequence. This is suited for MS/TP when combined with increasing the 'max_info_frames'. Value '2' will result in a 'segmented' sequence. This is the most efficient for both UDP and MS/TP.")]
            [Category(CAT_GENERAL)]
            public int DefaultDownloadSpeed { set { instance.DefaultDownloadSpeed = value; } get { return instance.DefaultDownloadSpeed; } }
            [DisplayName("Proposed window size")]
            //[Description("[Obsolete] Not used, can be remove from code, also ProposedWindowSize in Bacnetclient.cs")]
            [Category(CAT_GENERAL)]
            public byte Segments_ProposedWindowSize { set { instance.Segments_ProposedWindowSize = value; } get { return instance.Segments_ProposedWindowSize; } }
            [DisplayName("Max. segments")]
            [Description("This value sets 'allowed max_segments' to send to the client. The client might not support segmentation though. If it gives you trouble, set this to '0' to disable.")]
            [Category(CAT_GENERAL)]
            public byte Segments_Max { set { instance.Segments_Max = value; } get { return instance.Segments_Max; } }
            [DisplayName("Time synchronize UTC")]
            [Description("BACnet allows time synchronization as local time (the devices must be located in the same timezone) or UTC (Universal Time Coordinated). UTC allows to synchronize the time across different timezones.")]
            [Category(CAT_GENERAL)]
            public bool TimeSynchronize_UTC { set { instance.TimeSynchronize_UTC = value; } get { return instance.TimeSynchronize_UTC; } }
            [DisplayName("Default write priority")]
            [Description("Priorty level used for write operation. Can be changed without reboot. <Ctrl><Alt> + '0' to '9' keys are shortcuts to change this value directly from the main form (0: no priority to priority level 9, for others... no shortcut): a sound is played. To write a NULL value simply enter an empty value to the 'PresentValue' property.")]
            [Category(CAT_GENERAL)]
            [TypeConverter(typeof(EnumConverter))]
            public BacnetWritePriority DefaultWritePriority { set { instance.DefaultWritePriority = value; } get { return instance.DefaultWritePriority; } }
            [DisplayName("Plugins")]
            [Description("List of plugins to be loaded (separated by commas).")]
            [Category(CAT_GENERAL)]
            public string Plugins { set { instance.Plugins = value; } get { return instance.Plugins; } }
            [DisplayName("Device ID")]
            [Description("If this value is positive Yabe send response to 'Who-Is' with this BACnet device ID. Can be usefull to set recipients list in notification class objects without using Yabe IP endpoint.")]
            [Category(CAT_GENERAL)]
            public int YabeDeviceId { set { instance.YabeDeviceId = value; } get { return instance.YabeDeviceId; } }
            [DisplayName("Use Objects cache when useful")]
            [Description("Queries the dictionary via a cache if available. The Refresh button forces a real reading.")]
            [Category(CAT_GENERAL)]
            public bool UseObjectsCache { set { instance.UseObjectsCache = value; } get { return instance.UseObjectsCache; } }
            [DisplayName("Show property ID numbers")]
            [Description("Displays the properties ID with there names.")]
            [Category(CAT_GENERAL)]
            public bool Show_Property_Id_Numbers { set { instance.Show_Property_Id_Numbers = value; } get { return instance.Show_Property_Id_Numbers; } }

            [DisplayName("Background requests on IAm receptions")]
            [Description("Background queries of the Object Dictionary with or without the Names (twice bandwidth consuming), also or not on MSTP slow networks. GetAbsolutlyAll do the same even with a lot of single requests if required (highly time and bandwidth consuming, should be avoided) : experimental.")]
            [Category(CAT_GENERAL)]
            public BackGroundOperationType BackGroundOperations { set { instance.BackGroundOperations = value; } get { return instance.BackGroundOperations; } }

            [DisplayName("Background requests : Number of Tasks")]
            [Description("Number of parallel Background Tasks,max 10. Do not use more than 1 thread on direct network other than BACnet/IP.")]
            [Category(CAT_GENERAL)]
            public int BackGroundThreadNumber { set { instance.BackGroundThreadNumber = value; } get { return instance.BackGroundThreadNumber; } }



            #endregion
            #region Properties.Hidden
            [Browsable(false)]
            public string GUI_FormState { set { instance.GUI_FormState = value; } get { return instance.GUI_FormState; } }
            [Browsable(false)]
            public Size GUI_FormSize { set { instance.GUI_FormSize = value; } get { return instance.GUI_FormSize; } }
            [Browsable(false)]
            public int GUI_SplitterLeft { set { instance.GUI_SplitterLeft = value; } get { return instance.GUI_SplitterLeft; } }
            [Browsable(false)]
            public int GUI_SplitterMiddle { set { instance.GUI_SplitterMiddle = value; } get { return instance.GUI_SplitterMiddle; } }
            [Browsable(false)]
            public int GUI_SplitterRight { set { instance.GUI_SplitterRight = value; } get { return instance.GUI_SplitterRight; } }
            [Browsable(false)]
            public int GUI_SplitterButtom { set { instance.GUI_SplitterButtom = value; } get { return instance.GUI_SplitterButtom; } }
            [Browsable(false)]
            public string GUI_LastFilename { set { instance.GUI_LastFilename = value; } get { return instance.GUI_LastFilename; } }
            [Browsable(false)]
            public string GUI_SubscriptionColumns { set { instance.GUI_SubscriptionColumns = value; } get { return instance.GUI_SubscriptionColumns; } }

            [Browsable(false)]
            public bool SettingsUpgradeRequired { set { instance.SettingsUpgradeRequired = value; } get { return instance.SettingsUpgradeRequired; } }

            #endregion
            Properties.Settings instance;
        }

        internal SettingsDialog(Properties.Settings instance)
        {
            InitializeComponent();
            // Adjust the grid by writing into a private field, no need to try catch
            try
            {
                // Encapsulation principle violation, but why labelRatio or size is not accessible ?
                Control view = (Control)m_SettingsGrid.GetType().GetField("gridView", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(m_SettingsGrid);
                FieldInfo fi = view.GetType().GetField("labelRatio", BindingFlags.Instance | BindingFlags.Public);
                fi.SetValue(view, 2.5);
            }
            catch { }
            // Create settings wrapper
            m_SettingsGrid.SelectedObject = new SettingsDescriptor(instance);

        }

        private void SettingsDialog_Load(object sender, EventArgs e)
        {
        }
    }
}
