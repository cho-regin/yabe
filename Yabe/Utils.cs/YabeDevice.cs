using System;
using System.Collections.Generic;
using System.IO.BACnet;
using System.IO.BACnet.Storage;

namespace Yabe
{
    class YabeDevice   // Yabe as a BACnet server
    {
        DeviceStorage m_storage;
        uint myId;

        public YabeDevice(uint DeviceId)
        {
            myId = DeviceId;

            // Load descriptor from the embedded xml resource
            m_storage = m_storage = DeviceStorage.Load("Yabe.Common_Files.YabeDeviceDescriptor.xml", (uint)Properties.Settings.Default.YabeDeviceId);
            // A fast way to change the PROP_OBJECT_LIST
            Property Prop = Array.Find<Property>(m_storage.Objects[0].Properties, p => p.Id == BacnetPropertyIds.PROP_OBJECT_LIST);
            Prop.Value[0] = "OBJECT_DEVICE:" + Properties.Settings.Default.YabeDeviceId.ToString();
            // change PROP_FIRMWARE_REVISION
            Prop = Array.Find<Property>(m_storage.Objects[0].Properties, p => p.Id == BacnetPropertyIds.PROP_FIRMWARE_REVISION);
            Prop.Value[0] = this.GetType().Assembly.GetName().Version.ToString();
            // change PROP_APPLICATION_SOFTWARE_VERSION
            Prop = Array.Find<Property>(m_storage.Objects[0].Properties, p => p.Id == BacnetPropertyIds.PROP_APPLICATION_SOFTWARE_VERSION);
            Prop.Value[0] = this.GetType().Assembly.GetName().Version.ToString();
        }
        public void Iam(BacnetClient client)
        {
            if ((client.Transport is BacnetMstpProtocolTransport Transport) && (Transport.SourceAddress == -1))
                return;

            client.Iam(myId, BacnetSegmentations.SEGMENTATION_BOTH, 61440);
        }
        public void AddCom(BacnetClient client)
        {
            client.OnWhoIs += OnWhoIs;
            client.OnReadPropertyRequest += OnReadPropertyRequest;
            client.OnReadPropertyMultipleRequest += OnReadPropertyMultipleRequest;
        }
        public void RemoveCom(BacnetClient client)
        {
            client.OnWhoIs -= OnWhoIs;
            client.OnReadPropertyRequest -= OnReadPropertyRequest;
            client.OnReadPropertyMultipleRequest -= OnReadPropertyMultipleRequest;

        }

        void OnWhoIs(BacnetClient sender, BacnetAddress adr, int low_limit, int high_limit)
        {
            if (low_limit != -1 && myId < low_limit) return;
            else if (high_limit != -1 && myId > high_limit) return;
            sender.Iam(myId, BacnetSegmentations.SEGMENTATION_BOTH, 61440);
        }

        private void OnReadPropertyRequest(BacnetClient sender, BacnetAddress adr, byte invoke_id, BacnetObjectId object_id, BacnetPropertyReference property, BacnetMaxSegments max_segments)
        {
            lock (m_storage)
            {
                try
                {
                    IList<BacnetValue> value;
                    DeviceStorage.ErrorCodes code = m_storage.ReadProperty(object_id, (BacnetPropertyIds)property.propertyIdentifier, property.propertyArrayIndex, out value);
                    if (code == DeviceStorage.ErrorCodes.Good)
                        sender.ReadPropertyResponse(adr, invoke_id, sender.GetSegmentBuffer(max_segments), object_id, property, value);
                    else
                        sender.ErrorResponse(adr, BacnetConfirmedServices.SERVICE_CONFIRMED_READ_PROPERTY, invoke_id, BacnetErrorClasses.ERROR_CLASS_DEVICE, BacnetErrorCodes.ERROR_CODE_OTHER);
                }
                catch (Exception)
                {
                    sender.ErrorResponse(adr, BacnetConfirmedServices.SERVICE_CONFIRMED_READ_PROPERTY, invoke_id, BacnetErrorClasses.ERROR_CLASS_DEVICE, BacnetErrorCodes.ERROR_CODE_OTHER);
                }
            }
        }

        private void OnReadPropertyMultipleRequest(BacnetClient sender, BacnetAddress adr, byte invoke_id, IList<BacnetReadAccessSpecification> properties, BacnetMaxSegments max_segments)
        {
            lock (m_storage)
            {
                try
                {
                    IList<BacnetPropertyValue> value;
                    List<BacnetReadAccessResult> values = new List<BacnetReadAccessResult>();
                    foreach (BacnetReadAccessSpecification p in properties)
                    {
                        if (p.propertyReferences.Count == 1 && p.propertyReferences[0].propertyIdentifier == (uint)BacnetPropertyIds.PROP_ALL)
                        {
                            if (!m_storage.ReadPropertyAll(p.objectIdentifier, out value))
                            {
                                sender.ErrorResponse(adr, BacnetConfirmedServices.SERVICE_CONFIRMED_READ_PROP_MULTIPLE, invoke_id, BacnetErrorClasses.ERROR_CLASS_OBJECT, BacnetErrorCodes.ERROR_CODE_UNKNOWN_OBJECT);
                                return;
                            }
                        }
                        else
                            m_storage.ReadPropertyMultiple(p.objectIdentifier, p.propertyReferences, out value);
                        values.Add(new BacnetReadAccessResult(p.objectIdentifier, value));
                    }

                    sender.ReadPropertyMultipleResponse(adr, invoke_id, sender.GetSegmentBuffer(max_segments), values);

                }
                catch (Exception)
                {
                    sender.ErrorResponse(adr, BacnetConfirmedServices.SERVICE_CONFIRMED_READ_PROP_MULTIPLE, invoke_id, BacnetErrorClasses.ERROR_CLASS_DEVICE, BacnetErrorCodes.ERROR_CODE_OTHER);
                }
            }
        }
    }
}
