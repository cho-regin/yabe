using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.BACnet;
using System.Windows.Forms;

namespace Yabe
{
    class Subscription
    {
        public readonly BACnetDevice device;

        public readonly BacnetObjectId object_id;
        public readonly string sub_key;
        public readonly uint subscribe_id;
        public bool is_COV_subscription = true; // false if subscription is refused (fallback to polling) or polling is specified explicitly.
        public int Periode;
        public bool IsActive = false;
        public Subscription(BACnetDevice device, BacnetObjectId object_id, string sub_key, uint subscribe_id, int periode)
        {
            this.device = device;
            this.object_id = object_id;
            this.sub_key = sub_key;
            this.subscribe_id = subscribe_id;
            Periode = periode;
        }
    }

    #region " Trace Listner "
    class MyTraceListener : TraceListener
    {
        private TextBox m_LogText;
        public MyTraceListener(TextBox txtLog)
            : base("MyListener")
        {
            m_LogText = txtLog;
        }

        public override void TraceEvent(TraceEventCache eventCache, string source, TraceEventType eventType, int id, string message)
        {
            if ((this.Filter != null) && !this.Filter.ShouldTrace(eventCache, source, eventType, id, message, null, null, null)) return;

            ConsoleColor color;
            switch (eventType)
            {
                case TraceEventType.Error:
                    color = ConsoleColor.Red;
                    break;
                case TraceEventType.Warning:
                    color = ConsoleColor.Yellow;
                    break;
                case TraceEventType.Information:
                    color = ConsoleColor.DarkGreen;
                    break;
                default:
                    color = ConsoleColor.Gray;
                    break;
            }

            WriteColor(message + Environment.NewLine, color);
        }

        public override void TraceEvent(TraceEventCache eventCache, string source, TraceEventType eventType, int id, string format, params object[] args)
        {
            if ((this.Filter != null) && !this.Filter.ShouldTrace(eventCache, source, eventType, id, format, args, null, null)) return;

            ConsoleColor color;
            switch (eventType)
            {
                case TraceEventType.Error:
                    color = ConsoleColor.Red;
                    break;
                case TraceEventType.Warning:
                    color = ConsoleColor.Yellow;
                    break;
                case TraceEventType.Information:
                    color = ConsoleColor.DarkGreen;
                    break;
                default:
                    color = ConsoleColor.Gray;
                    break;
            }

            WriteColor(string.Format(format, args) + Environment.NewLine, color);
        }

        public override void Write(string message)
        {
            WriteColor(message, ConsoleColor.Gray);
        }
        public override void WriteLine(string message)
        {
            WriteColor(message + Environment.NewLine, ConsoleColor.Gray);
        }

        private void WriteColor(string message, ConsoleColor color)
        {
            if (!m_LogText.IsHandleCreated)
                return;

            m_LogText.BeginInvoke((MethodInvoker)delegate { m_LogText.AppendText(message); });
        }
    }
    #endregion
    // Used to sort the devices Tree
    public class NodeSorter : IComparer
    {
        bool NetworkBeforeClass;
        public NodeSorter(bool NetworkBeforeClass)
        {
            this.NetworkBeforeClass = NetworkBeforeClass;
        }
        public int Compare(object x, object y)
        {
            TreeNode tx = x as TreeNode;
            TreeNode ty = y as TreeNode;

            // Two Devices node, orderd by deviceId
            if ((tx.Tag is BACnetDevice txdev) && (ty.Tag is BACnetDevice tydev))
                return txdev.deviceId.CompareTo(tydev.deviceId);

            // Two Folder Nodes in Device Class View, use Name (equal Text apart Not affected Folder)
            if ((tx.Tag is List<int> txList) && (ty.Tag is List<int> tyList))
                return tx.Name.CompareTo(ty.Name);

            // A Folder and a device, device first
            if ((tx.Tag is BACnetDevice) && (ty.Tag is List<int>))
                return -1;
            if ((tx.Tag is List<int>) && (ty.Tag is BACnetDevice))
                return 1;

            // The Two Root nodes ordered according to the user choice
            if ((tx.Tag is int txidev) && (ty.Tag is int tyidev))
                if (NetworkBeforeClass)
                    return tyidev.CompareTo(txidev);
                else
                    return txidev.CompareTo(tyidev);

            return 0;   // Don't know, don't care, equal
        }
    }

    public enum AddressTreeViewType
    {
        List,
        Structured,
        Both,
        FieldTechnician
    }

    public enum DeviceTreeViewType
    {
        Network,
        DeviceClass,
        NetworkThenDeviceClass,
        DeviceClassThenNetwork
    }

    public enum BackGroundOperationType
    {
        None,
        GetObjectsList,
        GetObjectsName,
        GetObjectsListIncludeMstp,
        GetObjectsNameIncludeMstp,
        GetAbsolutelyAll
    }
}
