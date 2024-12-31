using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.BACnet;
using System.Linq;
using System.Windows.Forms;

namespace Yabe
{
    /// <summary>
    /// Common used stuff.
    /// </summary>
    internal static class Common
    {
        static Common()
        {
            var unitLoader = new CsvLoader<BacnetUnitsId>(Properties.Resources.Units, 0);
            Common.Unit_Shortcuts = unitLoader.CreateDictionary(1);
            Common.Unit_EdeTexts = unitLoader.CreateDictionary(5);

            var objTypeLoader = new CsvLoader<BacnetObjectTypes>(Properties.Resources.ObjectTypes, 0);
            Common.ObjectType_EdeTexts = objTypeLoader.CreateDictionary(3);
        }


        public static IReadOnlyDictionary<BacnetUnitsId, string> Unit_Shortcuts { get; }
        public static IReadOnlyDictionary<BacnetUnitsId, string> Unit_EdeTexts { get; }
        public static IReadOnlyDictionary<BacnetObjectTypes, string> ObjectType_EdeTexts { get; }
    }


    /// <summary>
    /// Loader for reading text from *.csv files.
    /// Containing data can be obtained per row. Each cell will be associated with an enum value.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal class CsvLoader<T>
        where T : struct, Enum
    {
        /// <summary>
        /// Creates a new loader instance.
        /// </summary>
        /// <param name="content">Content to load.</param>
        /// <param name="primaryKeyColumn">Index of column that provides primary keys to associate with the enum type <typeparamref name="T"/>.</param>
        /// <param name="firstRow">Index of the first data row.</param>
        public CsvLoader(string content, int primaryKeyColumn, int firstRow = 1)
        {
            this.Lines = content.Split('\n');
            this.PrimaryKeyColumn = primaryKeyColumn;
            this.FirstRow = firstRow;
        }


        private string[] Lines { get; }
        private int PrimaryKeyColumn { get; }
        private int FirstRow { get; }


        /// <summary>
        /// Create dictionary from loaded data.
        /// </summary>
        public Dictionary<T, string> CreateDictionary(int column, bool skipEmptyCells = true)
        {
            var result = new Dictionary<T, string>();
            int i = 0;
            foreach (var line in Lines)
            {
                if (i++ < FirstRow)
                    continue;
                if (line.Length == 0)
                    continue;
                var cells = line.Split(';');
                if (PrimaryKeyColumn >= cells.Length)
                    throw new IndexOutOfRangeException($"Creating dictionary failed at reading primary key at line {i}!");
                if (!int.TryParse(cells[PrimaryKeyColumn], out var key))
                    throw new InvalidCastException($"Creating dictionary failed at parsing primary key at line {i}!");
                string val = "";
                if (column < cells.Length)
                    val = cells[column].Replace("\r", "");
                if ((val.Length > 0) || (!skipEmptyCells))
                    result.Add((T)Enum.ToObject(typeof(T), key), val);
            }
            return (result);
        }
    }

    public partial class YabeMainDialog
    {
        private void exportEDEFilesSelDeviceToolStripMenuItem_Click(object sender, EventArgs e) => exportDeviceEDEFile(true);
        private void exportEDEFilesAllDevicesToolStripMenuItem_Click(object sender, EventArgs e) => exportDeviceEDEFile(false);
        private const string EDE_EXPORT_TITLE = "EDE file export";
        private const string EDE_EXPORT_EXT = "csv";
        private void exportDeviceEDEFile(bool selDeviceOnly)
        {
            // Fetch endpoints:
            var endPoints = new List<BACnetDevice>();
            if (selDeviceOnly)
                FetchEndPoint(out endPoints);
            else
                FetchEndPoints(out endPoints);
            if (endPoints.Count == 0)
                return;

            string file;
            var singleFile = ((endPoints.Count == 1) || (Properties.Settings.Default.EDE_SingleFile));
            if (singleFile)
            {
                // Export device(s) in into single file:
                var endPoint = endPoints.First();
                if (endPoints.Count == 1)
                    file = $"Device{endPoint.deviceId}.{EDE_EXPORT_EXT}";
                else
                    file = $"Devices.{EDE_EXPORT_EXT}";
                var dlg = new SaveFileDialog()
                {
                    Filter = "csv|*.csv",
                    FileName = file
                };
                if (dlg.ShowDialog(this) != DialogResult.OK)
                    return;

                file = dlg.FileName.Remove(dlg.FileName.Length - 4, 4);
                exportDeviceEDEFile(endPoints, file);
            }
            else
            {
                // Export devices in into separate files:
                var dlg = new FolderBrowserDialog()
                {
                    Description = $"Select output folder to export {endPoints.Count} EDE files to."
                };
                if (dlg.ShowDialog(this) != DialogResult.OK)
                    return;

                foreach (var endPoint in endPoints)
                {
                    file = Path.Combine(dlg.SelectedPath, $"Device{endPoint.deviceId}.{EDE_EXPORT_EXT}");
                    exportDeviceEDEFile(endPoint, file);
                }
            }

            MessageBox.Show(this, $"Exported {endPoints.Count} device(s).", EDE_EXPORT_TITLE, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        /// <summary>
        /// Exports a single device to EDE file.
        /// </summary>
        private void exportDeviceEDEFile(BACnetDevice device, String fileName) => exportDeviceEDEFile(Enumerable.Repeat((device), 1), fileName);
        /// <summary>
        /// Exports one or more devices to EDE file.
        /// </summary>
        /// <remarks>
        /// This will download all values from a given device and store it in 2 csv files: EDE and StateText (for Binary and Multistate objects).
        /// Ede files for Units and ObjectTypes are common when all values are coming from the standard
        /// </remarks>
        /// <example>
        /// Base on https://www.big-eu.org/s/big_ede_2_3.zip
        /// </example>
        private void exportDeviceEDEFile(IEnumerable<BACnetDevice> endPoints, String fileName)
        {
            var stateTextReferences = new List<string>();

            char Sep = Properties.Settings.Default.EDE_Separator;

            using (var edeWriter = new StreamWriter($"{fileName}_EDE.csv"))
            {
                edeWriter.WriteLine("#Engineering-Data-Exchange - B.I.G.-EU");
                edeWriter.WriteLine("PROJECT_NAME");
                edeWriter.WriteLine("VERSION_OF_REFERENCEFILE");
                edeWriter.WriteLine("TIMESTAMP_OF_LAST_CHANGE" + Sep + DateTime.Now.ToShortDateString());
                edeWriter.WriteLine("AUTHOR_OF_LAST_CHANGE" + Sep + "YABE Yet Another Bacnet Explorer");
                edeWriter.WriteLine("VERSION_OF_LAYOUT" + Sep + "2.3");
                edeWriter.WriteLine("#mandatory;mandator;mandatory;mandatory;mandatory;optional;optional;optional;optional;optional;optional;optional;optional;optional;optional;optional".Replace(';', Sep));
                edeWriter.WriteLine("# keyname;device obj.-instance;object-name;object-type;object-instance;description;present-value-default;min-present-value;max-present-value;settable;supports COV;hi-limit;low-limit;state-text-reference;unit-code;vendor-specific-addres".Replace(';', Sep));

                foreach (var device in endPoints)
                    exportDeviceEDEFile(device, edeWriter, stateTextReferences);
            }
            using (var stateTextWriter = new StreamWriter($"{fileName}_StateTexts.csv"))
            {
                stateTextWriter.WriteLine("#State Text Reference");
                if (stateTextReferences.Count > 0)
                {
                    var maxStates = stateTextReferences
                        .Select(stateRef => stateRef.Count(c => c.Equals(Sep)) + 1)
                        .Max();
                    var columns = Enumerable
                        .Range(0, maxStates + 1)
                        .Select(col => $"Text {col}")
                        .ToArray();
                    if (maxStates >= 0) columns[0] = "Reference Number";
                    if (maxStates >= 1) columns[1] += " or Inactive-Text";
                    if (maxStates >= 2) columns[2] += " or Active-Text";
                    stateTextWriter.WriteLine("#" + string.Join(Sep.ToString(), columns));

                    int i = 0;
                    foreach (var stateRef in stateTextReferences)
                    {
                        stateTextWriter.Write($"{i++}" + Sep);
                        stateTextWriter.WriteLine(stateRef);
                    }
                }
            }

            if (Properties.Settings.Default.EDE_CommonFiles)
            {
                using (var objTypesWriter = new StreamWriter($"{fileName}_ObjTypes.csv"))
                {
                    objTypesWriter.WriteLine("#Encoding of BACnet Object Types" + Sep);
                    objTypesWriter.WriteLine("#Code;Object Type;".Replace(';', Sep));
                    foreach (var objType in Common.ObjectType_EdeTexts)
                        objTypesWriter.WriteLine($"{(int)objType.Key};{objType.Value}" + Sep);
                }
                using (var unitsWriter = new StreamWriter($"{fileName}_Units.csv"))
                {
                    unitsWriter.WriteLine("#Encoding of BACnet Engineering Units" + Sep);
                    unitsWriter.WriteLine("#Code;Unit Text;".Replace(';', Sep));
                    foreach (var unit in Common.Unit_EdeTexts)
                        unitsWriter.WriteLine($"{(int)unit.Key};{unit.Value};".Replace(';', Sep));
                }
            }
        }
        /// <summary>
        /// Gathers a devices EDE data and writes it to a file stream.
        /// </summary>
        private void exportDeviceEDEFile(BACnetDevice device, StreamWriter edeWriter, List<string> stateTextReferences)
        {
            char Sep = Properties.Settings.Default.EDE_Separator;

            device.ReadObjectList(out _, out uint ObjCount);    // Get the dictionary (already in cache or puts in cache)

            this.Cursor = Cursors.WaitCursor;

            try
            {
                // Read 6 properties even if not existing in the given object
                BacnetPropertyReference[] propertiesWithText = new BacnetPropertyReference[6] {
                    new BacnetPropertyReference((uint)BacnetPropertyIds.PROP_OBJECT_NAME, System.IO.BACnet.Serialize.ASN1.BACNET_ARRAY_ALL),
                    new BacnetPropertyReference((uint)BacnetPropertyIds.PROP_DESCRIPTION, System.IO.BACnet.Serialize.ASN1.BACNET_ARRAY_ALL),
                    new BacnetPropertyReference((uint)BacnetPropertyIds.PROP_UNITS, System.IO.BACnet.Serialize.ASN1.BACNET_ARRAY_ALL),
                    new BacnetPropertyReference((uint)BacnetPropertyIds.PROP_STATE_TEXT, System.IO.BACnet.Serialize.ASN1.BACNET_ARRAY_ALL),
                    new BacnetPropertyReference((uint)BacnetPropertyIds.PROP_INACTIVE_TEXT, System.IO.BACnet.Serialize.ASN1.BACNET_ARRAY_ALL),
                    new BacnetPropertyReference((uint)BacnetPropertyIds.PROP_ACTIVE_TEXT, System.IO.BACnet.Serialize.ASN1.BACNET_ARRAY_ALL),
                };

                for (uint i = 1; i <= ObjCount; i++)
                {
                    Application.DoEvents();

                    BacnetObjectId Bacobj;
                    device.ReadObjectListItem(out Bacobj, i); // From the cache or not
                    string Identifier = "";
                    string Description = "";
                    String UnitCode = "";
                    String InactiveText = "";
                    String ActiveText = "";

                    IList<BacnetValue> State_Text = null;

                    if (device.ReadMultiple != BACnetDevice.ReadPopertyMultipleStatus.NotSupported)
                    {
                        try
                        {

                            IList<BacnetReadAccessResult> multi_value_list;
                            device.ReadPropertyMultipleRequest(Bacobj, propertiesWithText, out multi_value_list);
                            BacnetReadAccessResult br = multi_value_list[0];

                            foreach (BacnetPropertyValue pv in br.values)
                            {

                                if ((BacnetPropertyIds)pv.property.propertyIdentifier == BacnetPropertyIds.PROP_OBJECT_NAME)
                                    Identifier = pv.value[0].Value.ToString();
                                if ((BacnetPropertyIds)pv.property.propertyIdentifier == BacnetPropertyIds.PROP_DESCRIPTION)
                                    if (!(pv.value[0].Value is BacnetError))
                                        Description = pv.value[0].Value.ToString();
                                if ((BacnetPropertyIds)pv.property.propertyIdentifier == BacnetPropertyIds.PROP_UNITS)
                                    if (!(pv.value[0].Value is BacnetError))
                                        UnitCode = pv.value[0].Value.ToString();
                                if ((BacnetPropertyIds)pv.property.propertyIdentifier == BacnetPropertyIds.PROP_STATE_TEXT)
                                    if (!(pv.value[0].Value is BacnetError))
                                        State_Text = pv.value;
                                if ((BacnetPropertyIds)pv.property.propertyIdentifier == BacnetPropertyIds.PROP_INACTIVE_TEXT)
                                    if (!(pv.value[0].Value is BacnetError))
                                        InactiveText = pv.value[0].Value.ToString();
                                if ((BacnetPropertyIds)pv.property.propertyIdentifier == BacnetPropertyIds.PROP_ACTIVE_TEXT)
                                    if (!(pv.value[0].Value is BacnetError))
                                        ActiveText = pv.value[0].Value.ToString();
                            }

                            device.ReadMultiple = BACnetDevice.ReadPopertyMultipleStatus.Accepted;
                        }
                        catch
                        {
                            if (device.ReadMultiple != BACnetDevice.ReadPopertyMultipleStatus.Accepted)
                                device.ReadMultiple = BACnetDevice.ReadPopertyMultipleStatus.NotSupported; // assume the error is due to that 
                        }
                    }
                    if (device.ReadMultiple == BACnetDevice.ReadPopertyMultipleStatus.NotSupported)
                    {
                        IList<BacnetValue> out_value;

                        Identifier = device.ReadObjectName(Bacobj);

                        try
                        {
                            // OBJECT_MULTI_STATE_INPUT, OBJECT_MULTI_STATE_OUTPUT, OBJECT_MULTI_STATE_VALUE
                            if ((Bacobj.type >= BacnetObjectTypes.OBJECT_MULTI_STATE_INPUT) && (Bacobj.type <= BacnetObjectTypes.OBJECT_MULTI_STATE_INPUT + 2))
                            {
                                device.ReadPropertyRequest(Bacobj, BacnetPropertyIds.PROP_STATE_TEXT, out State_Text);
                                if (State_Text[0].Value is BacnetError) State_Text = null;
                            }
                            // OBJECT_BINARY_INPUT, OBJECT_BINARY_OUTPUT, OBJECT_BINARY_VALUE
                            if ((Bacobj.type >= BacnetObjectTypes.OBJECT_BINARY_INPUT) && (Bacobj.type <= BacnetObjectTypes.OBJECT_BINARY_INPUT + 2))
                            {
                                device.ReadPropertyRequest(Bacobj, BacnetPropertyIds.PROP_INACTIVE_TEXT, out out_value);
                                if (!(out_value[0].Value is BacnetError))
                                    InactiveText = out_value[0].Value.ToString();
                                device.ReadPropertyRequest(Bacobj, BacnetPropertyIds.PROP_ACTIVE_TEXT, out out_value);
                                if (!(out_value[0].Value is BacnetError))
                                    ActiveText = out_value[0].Value.ToString();
                            }

                            device.ReadPropertyRequest(Bacobj, BacnetPropertyIds.PROP_DESCRIPTION, out out_value);
                            if (!(out_value[0].Value is BacnetError))
                                Description = out_value[0].Value.ToString();

                        }
                        catch { }
                    }

                    // Write state texts:
                    int? stateTextIdx = null;
                    IEnumerable<string> stateTexts = null;
                    if (State_Text != null)
                        stateTexts = State_Text.Select(sta => sta.Value.ToString());
                    else if ((InactiveText != "") && (ActiveText != ""))
                        stateTexts = new string[] { InactiveText, ActiveText };

                    if (stateTexts != null)
                    {
                        var line = string.Join(Sep.ToString(), stateTexts);
                        stateTextIdx = stateTextReferences.IndexOf(line);
                        if (stateTextIdx == -1)
                        {
                            stateTextIdx = stateTextReferences.Count;
                            stateTextReferences.Add(line);
                        }
                    }
                    edeWriter.WriteLine(Bacobj.ToString() + Sep + device.deviceId.ToString() + Sep + Identifier + Sep + ((int)Bacobj.type).ToString() + Sep + Bacobj.instance.ToString() + Sep + Description + Sep + Sep + Sep + Sep + Sep + Sep + Sep + Sep + stateTextIdx + Sep + UnitCode);

                    // Update also the Dictionary of known object name
                    device.UpdateObjectNameMapping(Bacobj, Identifier);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Export failed:\r\n{ex.Message}", EDE_EXPORT_TITLE, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                this.Cursor = Cursors.Default;
            }
        }

        /// <summary>
        /// This will download all values from a given device and store it in a xml format, fit for the DemoServer
        /// This can be a good way to test serializing
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void exportDeviceDBToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //fetch end point
            BACnetDevice device = FetchEndPoint();

            if (device == null)
            {
                MessageBox.Show(this, "Please select a device node", "Wrong node", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }


            //select file to store
            SaveFileDialog dlg = new SaveFileDialog();
            dlg.Filter = "xml|*.xml";
            if (dlg.ShowDialog(this) != System.Windows.Forms.DialogResult.OK) return;

            this.Cursor = Cursors.WaitCursor;
            Application.DoEvents();

            bool removeObject = false;

            try
            {
                //get all objects
                System.IO.BACnet.Storage.DeviceStorage storage = new System.IO.BACnet.Storage.DeviceStorage();
                IList<BacnetValue> value_list;
                device.ReadPropertyRequest(new BacnetObjectId(BacnetObjectTypes.OBJECT_DEVICE, device.deviceId), BacnetPropertyIds.PROP_OBJECT_LIST, out value_list);
                LinkedList<BacnetObjectId> object_list = new LinkedList<BacnetObjectId>();
                foreach (BacnetValue value in value_list)
                {
                    if (Enum.IsDefined(typeof(BacnetObjectTypes), ((BacnetObjectId)value.Value).Type))
                        object_list.AddLast((BacnetObjectId)value.Value);
                    else
                        removeObject = true;
                }

                foreach (BacnetObjectId object_id in object_list)
                {
                    //read all properties
                    IList<BacnetReadAccessResult> multi_value_list;
                    BacnetPropertyReference[] properties = new BacnetPropertyReference[] { new BacnetPropertyReference((uint)BacnetPropertyIds.PROP_ALL, System.IO.BACnet.Serialize.ASN1.BACNET_ARRAY_ALL) };
                    device.ReadPropertyMultipleRequest(object_id, properties, out multi_value_list);

                    //store
                    foreach (BacnetPropertyValue value in multi_value_list[0].values)
                    {
                        try
                        {
                            storage.WriteProperty(object_id, (BacnetPropertyIds)value.property.propertyIdentifier, value.property.propertyArrayIndex, value.value, true);
                        }
                        catch { }
                    }
                }

                //save to disk
                storage.Save(dlg.FileName);

                //display
                MessageBox.Show(this, "Done", "Export done", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Error during export: " + ex.Message, "Export Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                this.Cursor = Cursors.Default;
                if (removeObject == true)
                    Trace.TraceWarning("All proprietary Objects removed from export");
            }
        }

    }
}
