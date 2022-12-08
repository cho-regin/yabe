/**************************************************************************
*                           MIT License
* 
* Copyright (C) 2022 Frederic Chaxel <fchaxel@free.fr> 
* Yabe SourceForge Explorer and Full Open source BACnet stack
* 
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

using System.Collections.Generic;
using System.Diagnostics;
using System.Security.Cryptography.X509Certificates;
using WebSocketSharp;
using System.Threading;
using System.Xml.Serialization;
using System.Net.Security;
using System.Net.WebSockets;
using System.Text;
using System.Security.Authentication;

// based on Addendum 135-2016 bj
// and with the help of sample applications from https://sourceforge.net/projects/bacnet-sc-reference-stack/

namespace System.IO.BACnet
{
    // MIT License Copyright (C) 2022 Frederic Chaxel <fchaxel@free.fr> 
    public class BACnetTransportSecureConnect : IBacnetTransport, IDisposable
    {
        public enum State { IDLE, AWAITING_WEBSOCKET, AWAITING_REQUEST, AWAITING_ACCEPT, CONNECTED, DISCONNECTING }

        private State state = State.IDLE;

        private WebSocketSharp.WebSocket Websocket;

        BACnetSCConfigChannel config;

        private byte[] VMAC = new byte[6];          // my VMAC
        private byte[] RemoteVMAC = new byte[6];    // HUB VMAC 

        // Several frames type
        // resize will be done after, if needed
        // by default this is the value used by NPDU with 2 VMAC : 16
        public int HeaderLength { get { return BVLC_HEADER_LENGTH; } }

        public const byte BVLC_HEADER_LENGTH = 10; // Not all the time from 4 to a lot

        public const BacnetMaxAdpu BVLC_MAX_APDU = BacnetMaxAdpu.MAX_APDU1476;
        public BacnetMaxAdpu MaxAdpuLength { get { return BVLC_MAX_APDU; } }
        public byte MaxInfoFrames { get { return 0xff; } set { /* ignore */ } }     //the TCP doesn't have max info frames
        public int MaxBufferLength { get { return m_max_payload; } }

        public BacnetAddressTypes Type { get { return BacnetAddressTypes.SC; } }

        private int m_max_payload = 1500;

        public event MessageRecievedHandler MessageRecieved;

        List<byte[]> AwaitingFrames = new List<byte[]>();

        private bool ConfigOK = false;

        public BACnetTransportSecureConnect(Stream ConfigurationFile)
        {
            if (ConfigurationFile == null)
            {
                config = new BACnetSCConfigChannel()
                {
                    primaryHubURI = "ws://127.0.0.1:47808",
                    bUUID = new byte[16] {1,2,3,4,1,2,3,4,1,2,3,4,1,2,3,4}
                };
            }
            else
            {
                XmlSerializer ser = new XmlSerializer(typeof(BACnetSCConfigChannel));

                try
                {
                    config = (BACnetSCConfigChannel) ser.Deserialize(ConfigurationFile);
                    config.bUUID = Encoding.ASCII.GetBytes(config.UUID);
                    Array.Resize(ref config.bUUID, 16);
                    ConfigurationFile.Close();
                }
                catch 
                { 
                    Trace.TraceError("Error with BACnet/SC configuration file");
                    return;
                }

                if (config.primaryHubURI.Contains("wss://")|| config.failoverHubURI.Contains("wss://"))
                { 
                    config.UseTLS = true;
                    try
                    {
                        config.OwnCertificate=new X509Certificate2(config.OwnCertificateFile);
                        if (config.ValidateHubCertificate)
                            config.HubCertificate = new X509Certificate2(config.HubCertificateFile);
                    }
                    catch (Exception e)
                    {
                        Trace.TraceError("Error with certificate file : "+e.Message);
                        return;
                    }

                }

            }
            ConfigOK = true;
        }
        public override int GetHashCode()
        {
            return config.primaryHubURI.GetHashCode();
        }

        public override string ToString()
        {
            return "Secure Connect : " + config.primaryHubURI;
        }

        private void Open()
        {
            if (ConfigOK == false) return;

            new Random().NextBytes(this.VMAC);
            this.VMAC[0] = (byte)((this.VMAC[0] & 0xF0) | 0x02); // ensure xxxx0010, § H.7.X EUI-48 and Random-48 VMAC Address

            Websocket = new WebSocketSharp.WebSocket(config.primaryHubURI, new string[] { "hub.bsc.bacnet.org" });

            if (config.UseTLS)
            {
                Websocket.SslConfiguration.EnabledSslProtocols = SslProtocols.Tls13;
                Websocket.SslConfiguration.ClientCertificates = new X509CertificateCollection();
                Websocket.SslConfiguration.ClientCertificates.Add(config.OwnCertificate);
                Websocket.SslConfiguration.ServerCertificateValidationCallback = RemoteCertificateValidationCallback;
            }
            Websocket.OnOpen += Websocket_OnOpen;
            Websocket.OnClose += Websocket_OnClose;
            Websocket.OnError += Websocket_OnError;
            Websocket.OnMessage += Websocket_OnMessage;

            state = State.AWAITING_WEBSOCKET;

            Websocket.ConnectAsync();
        }

        private bool RemoteCertificateValidationCallback(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            
            if (config.ValidateHubCertificate==false)
                return true;    // always OK

            if (certificate.Equals(config.HubCertificate))
                return true;

            return false; 

        }
        private void Websocket_OnMessage(object sender, MessageEventArgs e)
        {
            OnReceiveData(e.RawData);
        }

        private void Websocket_OnError(object sender, WebSocketSharp.ErrorEventArgs e)
        {
            Trace.TraceError("BACnet/SC Error :", e.Message);
            state = State.IDLE;
        }

        private void Websocket_OnClose(object sender, CloseEventArgs e)
        {
            Trace.TraceInformation("BACnet/SC Close :"+ e.Reason);
            state = State.IDLE;
        }

        private void Websocket_OnOpen(object sender, EventArgs e)
        {
            state = State.AWAITING_ACCEPT;
            Trace.TraceInformation("BACnet/SC Websocket established");
            BVLC_SC_SendConnectRequest();
        }

        // Not a friendly BVLC disconnection here
        // Could be done more cleaner
        private void Close()
        {
            if (state != State.IDLE)
            {
                state = State.DISCONNECTING;
                try
                {
                    Websocket.CloseAsync();
                }
                catch { }
            }
        }

        public void Start()
        {
            Open();
        }

        private void OnReceiveData(byte[] local_buffer)
        {

            try
            {
                int rx = local_buffer.Length;

                if (rx < 4)    // Noting to do
                {
                    Trace.WriteLine("Unknow BVLC Header");
                    return;
                }

                try
                {
                    //verify message : BVLC decoding and get the VMAC back
                    BacnetAddress remote_address = new BacnetAddress(BacnetAddressTypes.SC, null);
                    BacnetBvlcSCMessage function;
                    int msg_length;
                    int HEADER_LENGTH = BVLC_SC_Decode(local_buffer, 0, out function, out msg_length, remote_address);

                    if (function == BacnetBvlcSCMessage.BVLC_CONNECT_ACCEPT)
                    {
                        Trace.TraceInformation("BACnet/SC connected");
                        state = State.CONNECTED;
                    }

                    if (HEADER_LENGTH == 0) // return value when no payload is on the frame
                        return; // only BVLC message not for upper layers

                    if (HEADER_LENGTH == -1)
                    {
                        Trace.WriteLine("Unknow BVLC Header");
                        return;
                    }

                    if (function == BacnetBvlcSCMessage.BVLC_ENCASULATED_NPDU) // Normaly the only function code with NPDU content
                        if ((MessageRecieved != null) && (rx > HEADER_LENGTH)) MessageRecieved(this, local_buffer, HEADER_LENGTH, rx - HEADER_LENGTH, remote_address);

                }
                catch (Exception ex)
                {
                    Trace.TraceError("Exception in Websocket OnReceiveData: " + ex.Message);
                }

            }
            catch (Exception ex)
            {
                Trace.TraceError("Exception in Websocket OnReceiveData: " + ex.Message);
            }
        }

        public bool WaitForAllTransmits(int timeout)
        {
            //we got no sending queue in WebSocket, so just return true
            return true;
        }
        private int Send(byte[] buffer)
        {
            try
            {
                Websocket.SendAsync(buffer, null);
                return buffer.Length;
            }
            catch
            {
                return 0;
            }
        }
        public int Send(byte[] buffer, int offset, int data_length, BacnetAddress address, bool wait_for_transmission, int timeout)
        {

            if ((state == State.IDLE) || (state == State.DISCONNECTING)) return -1; // nothing to do more

            //add header
            int full_length = data_length + HeaderLength;
            BVLC_SC_Encode(buffer, offset - BVLC_HEADER_LENGTH, BacnetBvlcSCMessage.BVLC_ENCASULATED_NPDU, ref full_length, address);

            if (state == State.CONNECTED)
            {
                Array.Resize(ref buffer, full_length);
                return Send(buffer);
            }
            else
            {
                // NPDU sent before a full SC connection (Iam, WhoIs certainly), push the Frame on a tempo buffer
                // Upper layers (using ethernet, udp or serial) have not by designed for a delay between open and send
                byte[] cpy = new byte[full_length];
                Array.Copy(buffer, cpy, full_length);
                lock (AwaitingFrames)
                {
                    AwaitingFrames.Add(cpy);
                    if (AwaitingFrames.Count == 1)
                        ThreadPool.QueueUserWorkItem(SendLaterAwaitingFrames); // Delayed sent
                }

            }

            return 0;
        }
        private void SendLaterAwaitingFrames(object o)
        {
            do
            {
                Thread.Sleep(200);
                if (state == State.IDLE) // Done before Connected, go away
                {
                    lock (AwaitingFrames)
                        AwaitingFrames.Clear();
                    return;

                }
            } while (state != State.CONNECTED);

            lock (AwaitingFrames)
            {
                foreach (var frame in AwaitingFrames) { Send(frame); }
                AwaitingFrames.Clear();
            }
        }

        private void BVLC_SC_SendConnectRequest()
        {

            byte[] b = new byte[4 + 6 + 16 + 2 + 2];
            b[0] = (byte)BacnetBvlcSCMessage.BVLC_CONNECT_REQUEST;
            b[1] = 0; // No VMAC, No Option
            b[2] = 0; // Initial Message ID
            b[3] = 0;
            // Originating Virtual Address
            Array.Copy(VMAC, 0, b, 4, 6);

            Array.Copy(config.bUUID, 0, b, 10, 16);

            // Max BVLC size
            b[26] = 0xFF;
            b[27] = 0xFF;

            // Max NPDU size
            b[28] = 0xFF;
            b[29] = 0xFF;

            Send(b);
        }
        private int BVLC_SC_Encode(byte[] buffer, int offset, BacnetBvlcSCMessage function, ref int msg_length, BacnetAddress address)
        {
    
            // offset should be zero or a resize must be done. Not tested here

            buffer[0] = (byte)function;
            buffer[1] = 4 ;             // Destination Vmac only, no source VMAC on a connected channel, without optional fields
            buffer[2] = 0xBA;           // Message Id
            buffer[3] = 0xC0;
            // Destination Virtual Address
            Array.Copy(address.VMac, 0, buffer, 4, 6);
            return 10;

        }
        // Decode is called each time a Frame is received
        private int BVLC_SC_Decode(byte[] buffer, int offset, out BacnetBvlcSCMessage function, out int msg_length, BacnetAddress remote_address)
        {
            msg_length = -1;

            // offset always 0, we are the first after TCP
            // and a previous test by the caller guaranteed at least 4 bytes into the buffer

            function = (BacnetBvlcSCMessage)buffer[0];
            byte controlFlag = buffer[1];
            uint messageId = (uint)((buffer[2] << 8) | buffer[3]);

            offset = 4;
            if ((controlFlag & 8) != 0) // Got the remote device VMAC
            {
                Array.Copy(buffer, offset, remote_address.VMac, 0, 6);
                remote_address.adr = remote_address.VMac;

                offset += 6;
            }
            if ((controlFlag & 4) != 0)
            {
                // Destination Vmac, should be my Vmac or broadcast
                // Can be tested to reject the packet since we are just a simple SC Node.
                offset += 6;
            }

            // Skip Destination Option
            if ((controlFlag & 2) != 0)
            {
                bool moreOption = true;
                while (moreOption)
                {
                    moreOption = (buffer[offset] & 0x80) != 0;
                    bool headerDataPresent = (buffer[offset] & 0x20) != 0;
                    offset++;
                    if (headerDataPresent)
                        offset += 2 + (buffer[offset] << 8) + buffer[offset + 1];
                }
            }
            // Skip Data Option
            if ((controlFlag & 1) != 0)
            {
                bool moreOption = true;
                while (moreOption)
                {
                    moreOption = (buffer[offset] & 0x80) != 0;
                    bool headerDataPresent = (buffer[offset] & 0x20) != 0;
                    offset++;
                    if (headerDataPresent)
                        offset += 2 + (buffer[offset] << 8) + buffer[offset + 1];
                }
            }

            msg_length = offset;

            // BVLC function see page 29 Addentum
            switch (function)
            {
                case BacnetBvlcSCMessage.BVLC_RESULT:
                    return 0;   // TODO
                case BacnetBvlcSCMessage.BVLC_ENCASULATED_NPDU:
                    return offset;   // only for the upper layers
                case BacnetBvlcSCMessage.BVLC_CONNECT_ACCEPT:
                    Array.Copy(buffer, 4, RemoteVMAC, 0, 6); // Hub VMAC
                    return 0;   // Only for BVLC 
                case BacnetBvlcSCMessage.BVLC_ADVERTISEMENT:
                    return 0;   // TODO

                // error encoding function or experimental one
                default:
                    return -1;
            }
        }
        public BacnetAddress GetBroadcastAddress()
        {
            BacnetAddress ret = new BacnetAddress()
            {
                VMac = new byte[] { 255, 255, 255, 255, 255, 255 },
                adr = VMAC,
                net = 0xFFFF,
            };

            return ret;
        }

        public void Dispose()
        {
            try
            {
                Close();
            }
            catch { }
        }
        private enum BacnetBvlcSCMessage : byte
        {
            BVLC_RESULT = 0,
            BVLC_ENCASULATED_NPDU = 1,
            BVLC_ADDRESS_RESOLUTION = 2,
            BVLC_ADDRESS_RESOLUTION_ACK = 3,
            BVLC_ADVERTISEMENT = 4,
            BVLC_ADVERTISEMENT_SOLICITATION = 5,
            BVLC_CONNECT_REQUEST = 6,
            BVLC_CONNECT_ACCEPT = 7,
            BVLC_DISCONNECT_REQUEST = 8,
            BVLC_DISCONNECT_ACK = 9,
            BVLC_HEARTBEAT_REQUEST = 0xA,
            BVLC_HEARTBEAT_ACK = 0xB,
            BVLC_PROPRIETARY_MESSAGE = 0xC
        };
    }

    public class BACnetSCConfigChannel
    {

        public String primaryHubURI;
        public String failoverHubURI;

        public String UUID;

        [XmlIgnore]
        public byte[] bUUID;

        public String OwnCertificateFile;

        [XmlIgnore]
        public X509Certificate2 OwnCertificate; // with private key

        public String HubCertificateFile;
        [XmlIgnore]
        public X509Certificate2 HubCertificate;

        [XmlIgnore]
        public bool UseTLS = false;

        public bool ValidateHubCertificate;
    }
}
