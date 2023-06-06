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
using System.Text;
using System.Security.Authentication;

// based on Addendum 135-2016 bj
// and with the help of sample applications from https://sourceforge.net/projects/bacnet-sc-reference-stack/

namespace System.IO.BACnet
{
    public enum BACnetSCState { IDLE, AWAITING_WEBSOCKET, AWAITING_REQUEST, AWAITING_ACCEPT, CONNECTED, DISCONNECTING }

    // MIT License Copyright (C) 2022 Frederic Chaxel <fchaxel@free.fr>
    // Copyright (C) https://sourceforge.net/projects/yetanotherbacnetexplorer/
    public class BACnetTransportSecureConnect : IBacnetTransport, IDisposable
    {
        private BACnetSCState state = BACnetSCState.IDLE;
        public BACnetSCState State { get { return state; } }

        // A very good and simple lib for secure and unsecure websocket communication
        // https://github.com/sta/websocket-sharp
        private WebSocketSharp.WebSocket Websocket; 

        private BACnetSCConfigChannel configuration;

        private byte[] myVMAC = new byte[6];          // my random VMAC
        private byte[] RemoteVMAC = new byte[6];    // HUB or Direct connected device VMAC 

        // Several frames type
        // resize will be done after, if needed
        // by default this is the value used by NPDU with 1 VMAC : 10
        public int HeaderLength { get { return BVLC_HEADER_LENGTH; } }

        public const byte BVLC_HEADER_LENGTH = 10; // Not all the time (from 4 to a lot), but 10 for all NPDUs sent

        public const BacnetMaxAdpu BVLC_MAX_APDU = BacnetMaxAdpu.MAX_APDU1476;
        public BacnetMaxAdpu MaxAdpuLength { get { return BVLC_MAX_APDU; } }
        public byte MaxInfoFrames { get { return 0xff; } set { /* ignore */ } }     // the TCP doesn't have max info frames
        public int MaxBufferLength { get { return m_max_payload; } }
        public BacnetAddressTypes Type { get { return BacnetAddressTypes.SC; } }

        private int m_max_payload = 1500;

        public event MessageRecievedHandler MessageRecieved;
        public event EventHandler OnChannelConnected;
        public event EventHandler OnChannelDisconnected;

        private List<byte[]> AwaitingFrames = new List<byte[]>();

        private bool ConfigOK = false;

        public BACnetTransportSecureConnect(BACnetSCConfigChannel config)
        {
            configuration = config;

            if (configuration.UseTLS)
            {
                if ((configuration.OwnCertificateFile != null)&&(configuration.OwnCertificate==null))
                    try
                    {
                        // could be not given or with error if the remote device do not verify it (wrong idea)
                        configuration.OwnCertificate = new X509Certificate2(configuration.OwnCertificateFile, configuration.OwnCertificateFilePassword);
                    }
                    catch (Exception e)
                    {
                        Trace.TraceError("Error with App own certificate file : " + e.Message);
                    }

                if (configuration.OwnCertificate == null)
                    Trace.TraceWarning("BACnet/SC : Warning App without certificate ");

                if ((configuration.OwnCertificate!=null)&&(!configuration.OwnCertificate.HasPrivateKey))
                    Trace.TraceWarning("BACnet/SC : Warning the App own certificate is without a private key");

                if ((configuration.ValidateHubCertificate)&&(configuration.HubCertificate == null))
                    try
                    {
                        // could be not given if the root CA is in the default computer store
                        configuration.HubCertificate = new X509Certificate2(configuration.HubCertificateFile);
                    }
                    catch
                    {
                        // Error may or not occur later during connection
                        return;
                    }
            }

            ConfigOK = true;
        }
        public override int GetHashCode()
        {
            return configuration.primaryHubURI.GetHashCode();
        }

        public override string ToString()
        {
            return "Secure Connect : " + configuration.primaryHubURI;
        }

        private void Open()
        {
            if (ConfigOK == false) return;

            Uri uri = new Uri(configuration.primaryHubURI); // WebSocket ctor fail if the string contains spaces, so use this to clean the path

            if (!configuration.DirectConnect)
                Websocket = new WebSocketSharp.WebSocket(uri.ToString(), new string[] { "hub.bsc.bacnet.org" });
            else
                Websocket = new WebSocketSharp.WebSocket(uri.ToString(), new string[] { "dc.bsc.bacnet.org" });

            if (configuration.UseTLS)
            {
                if (configuration.OnlyAllowsTLS13==true)
                    Websocket.SslConfiguration.EnabledSslProtocols = SslProtocols.Tls13;
                else
                    Websocket.SslConfiguration.EnabledSslProtocols = SslProtocols.None; // The client send all available options, the server should normaly propose only Tls1.3
               
                Websocket.SslConfiguration.ServerCertificateValidationCallback = RemoteCertificateValidationCallback;
                Websocket.SslConfiguration.ClientCertificateSelectionCallback = LocalCertificateSelectionCallback;
            }
            Websocket.OnOpen += Websocket_OnOpen;
            Websocket.OnMessage += Websocket_OnMessage;
            Websocket.OnError += Websocket_OnError;
            Websocket.OnClose += Websocket_OnClose;
            Websocket.Log.Output = new Action<LogData, String>(Websocket_OnLog); // needed to get detailed information about error
            state = BACnetSCState.AWAITING_WEBSOCKET;

            Websocket.ConnectAsync();
        }
        private X509Certificate LocalCertificateSelectionCallback(object sender, string targetHost, X509CertificateCollection localCertificates, X509Certificate remoteCertificate, string[] acceptableIssuers)
        {
            return configuration.OwnCertificate;
        }
        // Here we accept all certificates known by the computer : SslPolicyErrors.None
        // also the one given as a configuration parameter in config.HubCertificateFile
        // if it's the hub certificate or one of it's signing CA in the chain (if the chain is given)
        // REQUEST : help from a PKI-TLS-X509 specialist to validate this workflow
        private bool RemoteCertificateValidationCallback(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            if (configuration.ValidateHubCertificate==false)
                return true;    // No verification requested : always OK

            // The certificate or the root CA certificate is in the default computer store and all the X509Chain is given
            if (sslPolicyErrors == System.Net.Security.SslPolicyErrors.None)
                return true;

            // We have a certificate in the chain (even self signed) : CA Root, CA Intermediate
            foreach (X509ChainElement chainElement in chain.ChainElements)
                if (chainElement.Certificate.Equals(configuration.HubCertificate)) 
                    return true;

            // We have the final certificate (even self signed) ... normaly this certificate is in the previous X509Chain
            if (certificate.Equals(configuration.HubCertificate))
                return true;   

            Trace.TraceError("BACnet/SC : Remote certificate rejected");

            return false; 

        }
        private void Websocket_OnOpen(object sender, EventArgs e)
        {
            state = BACnetSCState.AWAITING_ACCEPT;
            Trace.TraceInformation("BACnet/SC Websocket established");
            BVLC_SC_SendConnectRequest();
        }
        private void Websocket_OnMessage(object sender, MessageEventArgs e)
        {
            OnReceiveData(e.RawData);
        }
        private void Websocket_OnError(object sender, WebSocketSharp.ErrorEventArgs e)
        {
            Trace.TraceError("BACnet/SC Error : ", e.Message);
            state = BACnetSCState.IDLE;
        }
        private void Websocket_OnClose(object sender, CloseEventArgs e)
        {
            Trace.TraceInformation("BACnet/SC Close : "+ e.Reason);
            state = BACnetSCState.IDLE;
            OnChannelDisconnected?.Invoke(this, e);
        }
        private void Websocket_OnLog(LogData log, String Logmessage) 
        {
            // First line is enough
            Trace.TraceError("BACnet/SC Websocket : " + log.Message.Split(new[] { '\r', '\n' })[0]);
        }

        private void Close()
        {
            if (state != BACnetSCState.IDLE)
            {
                state = BACnetSCState.DISCONNECTING;

                BVLC_SC_SendSimpleBVLCMsg(BacnetBvlcSCMessage.BVLC_DISCONNECT_REQUEST, 0, 0);
                ThreadPool.QueueUserWorkItem( (_) =>
                {
                    try
                    {
                        // Wait a few the BVLC Disconnect Ack and the TCP FIN messages
                        // The TCP round trip time is not available so wait ... euh 2s !
                        Thread.Sleep(2000);
                        if (state == BACnetSCState.DISCONNECTING)
                            Websocket.Close();  // Not closed by the peer device, do it
                    }
                    catch { }

                    state= BACnetSCState.IDLE;

                }
                );
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
                    int HEADER_LENGTH = BVLC_SC_Decode(local_buffer, 0, out BacnetBvlcSCMessage function, out int msg_length, remote_address);

                    if (function == BacnetBvlcSCMessage.BVLC_CONNECT_ACCEPT)
                    {
                        Trace.TraceInformation("BACnet/SC connected");
                        state = BACnetSCState.CONNECTED;
                        OnChannelConnected?.Invoke(this, new EventArgs());
                    }

                    if (HEADER_LENGTH == 0) // return value when no payload is on the frame
                        return;             // only BVLC message not for upper layers

                    if (HEADER_LENGTH == -1)
                    {
                        Trace.WriteLine("Unknow BVLC Header");
                        return;
                    }

                    if (function == BacnetBvlcSCMessage.BVLC_ENCASULATED_NPDU) // Normaly the only function code with NPDU content
                    {
                        if (configuration.DirectConnect)
                        {
                            // We need the VMAC, not given in the BVLC on a direct connection because it's a known value
                            Array.Copy(RemoteVMAC, remote_address.VMac, 6);
                            remote_address.adr = remote_address.VMac;
                        }
                        if ((MessageRecieved != null) && (rx > HEADER_LENGTH)) MessageRecieved(this, local_buffer, HEADER_LENGTH, rx - HEADER_LENGTH, remote_address);
                    }
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
            // we got no sending queue in WebSocket, so just return true
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

            if ((state == BACnetSCState.IDLE) || (state == BACnetSCState.DISCONNECTING)) return -1; // nothing to do more

            // add header
            int full_length = data_length + HeaderLength;
            BVLC_SC_Encode(buffer, offset - BVLC_HEADER_LENGTH, BacnetBvlcSCMessage.BVLC_ENCASULATED_NPDU, ref full_length, address);

            if (state == BACnetSCState.CONNECTED)
            {
                Array.Resize(ref buffer, full_length);
                return Send(buffer);
            }
            else
            {
                // NPDU sent before a full SC connection (Iam, WhoIs certainly), push the Frame on a tempo buffer
                // Upper layers (using ethernet, udp or serial) were not designed for a delay between open and send
                // ... all using at least one level of queue
                byte[] cpy = new byte[full_length];
                Array.Copy(buffer, cpy, full_length);
                lock (AwaitingFrames)
                {
                    Trace.TraceInformation("BACnet/SC : Request pushed in delay queue");
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
                if (state == BACnetSCState.IDLE) // Done before Connected, go away
                {
                    lock (AwaitingFrames)
                        AwaitingFrames.Clear();
                    return;

                }
            } while (state != BACnetSCState.CONNECTED);

            lock (AwaitingFrames)
            {
                foreach (var frame in AwaitingFrames) { Send(frame); Trace.TraceInformation("BACnet/SC : Request pulled from queue"); }
                AwaitingFrames.Clear();
            }
        }

        private void BVLC_SC_SendConnectRequest()
        {
            // Random VMAC creation
            // ensure xxxx0010, § H.7.X EUI - 48 and Random-48 VMAC Address
            new Random().NextBytes(myVMAC);
            myVMAC[0] = (byte)((myVMAC[0] & 0xF0) | 0x02); // xxxx0010

            byte[] b = new byte[4 + 6 + 16 + 2 + 2];
            b[0] = (byte)BacnetBvlcSCMessage.BVLC_CONNECT_REQUEST;
            b[1] = 0;           // No VMAC, No Option
            b[2] = 0;           // Initial Message ID
            b[3] = 0;
            // Payload
            // VMAC address of the requesting node (me)
            Array.Copy(myVMAC, 0, b, 4, 6);
            // UUID
            byte[] bUUID;
            if (Guid.TryParse(configuration.UUID, out Guid uuid)==true)
                bUUID = uuid.ToByteArray();
            else
            { 
                bUUID = Encoding.ASCII.GetBytes(configuration.UUID);
                Array.Resize(ref bUUID, 16);
            }
            Array.Copy(bUUID, 0, b, 10, 16);

            // Max BVLC size 1600
            b[26] = 0x06;
            b[27] = 0x40;

            // Max NPDU size 1497
            b[28] = 0x05;
            b[29] = 0xD9;

            Send(b);
        }

        private void BVLC_SC_SendBvlcError(byte[] DestVmac, byte MsgId1, byte MsgId2, BacnetErrorClasses classe, BacnetErrorCodes code)
        {
            byte[] b=new byte[10 + (configuration.DirectConnect==true?0:6)];

            b[0] = (byte)BacnetBvlcSCMessage.BVLC_RESULT;
            b[1] = (byte)(configuration.DirectConnect == true ? 0 : 4);// Destination Vmac 4 only or not 0
            b[2] = MsgId1;          // Initial Message ID
            b[3] = MsgId2;

            int offset = 0;
            if (configuration.DirectConnect==false)
            {
                // Destination Virtual Address
                Array.Copy(DestVmac, 0, b, 4, 6);
                offset = 6;
            }

            b[4 + offset] = 0;              // No data option
            b[5 + offset] = 0x01;           // NAK
            b[6 + offset] = 0;
            b[7 + offset] = (byte)BacnetErrorClasses.ERROR_CLASS_SERVICES;
            b[8 + offset] = 0;
            b[9 + offset] = (byte)BacnetErrorCodes.ERROR_CODE_BVLC_FUNCTION_UNKNOWN;

            Send(b);
        }

        //  Disconnect-ACK, Heartbeat-ACK
        private void BVLC_SC_SendSimpleBVLCMsg(BacnetBvlcSCMessage Msg, byte MsgId1, byte MsgId2)
        {
            byte[] b = new byte[4];

            b[0] = (byte)Msg;
            b[1] = 0;
            b[2] = MsgId1;
            b[3] = MsgId2;

            Send(b);
        }

        private int BVLC_SC_Encode(byte[] buffer, int offset, BacnetBvlcSCMessage function, ref int msg_length, BacnetAddress address)
        {
    
            // offset should be zero

            if (!configuration.DirectConnect)
            {
                buffer[0] = (byte)function;
                buffer[1] = 4 ;             // Destination Vmac only, no source VMAC on a hub channel, without optional fields
                buffer[2] = 0xBA;           // Message Id
                buffer[3] = 0xC0;
                // Destination Virtual Address
                Array.Copy(address.VMac, 0, buffer, 4, 6);
                return 10;
            }
            else // No VMAC at all
            {
                Array.Copy(buffer, 10, buffer, 4, msg_length - 6); // Shift left, BVLC HEADER is 6 bytes less than the given one
                buffer[0] = (byte)function;
                buffer[1] = 0;              //  no VMACs on a direct connected channel, without optional fields
                buffer[2] = 0xBA;           // Message Id
                buffer[3] = 0xC0;

                msg_length = msg_length - 6;

                return 4;
            }

        }
        // Decode is called each time a Frame is received
        private int BVLC_SC_Decode(byte[] buffer, int offset, out BacnetBvlcSCMessage function, out int msg_length, BacnetAddress remote_address)
        {
            msg_length = -1;

            // offset always 0, we are the first after TCP
            // and a previous test by the caller guaranteed at least 4 bytes into the buffer

            function = (BacnetBvlcSCMessage)buffer[0];
            byte controlFlag = buffer[1];
            // uint messageId = (uint)((buffer[2] << 8) | buffer[3]);

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
                    if ((buffer[offset] == (byte)BacnetBvlcSCMessage.BVLC_CONNECT_REQUEST)&&(buffer[offset+1]==0X01)&&(buffer[offset + 2]==0))
                    {
                        UInt16 ErrorClass = (UInt16)((buffer[offset + 3] << 8) | (buffer[offset + 4]));
                        UInt16 ErrorCode = (UInt16)((buffer[offset + 5] << 8) | (buffer[offset + 6]));
                        // Normaly duplicate VMAC should never occur 1.7^13 values. Redo with another random number
                        // ... a good way to spend time to think, code and discuss for nothing
                        if ((ErrorClass == (byte)BacnetErrorClasses.ERROR_CLASS_COMMUNICATION)&&(ErrorCode == (byte)BacnetErrorCodes.ERROR_CODE_NODE_DUPLICATE_VMAC))
                            BVLC_SC_SendConnectRequest();
                    }
                    return 0;
                case BacnetBvlcSCMessage.BVLC_ENCASULATED_NPDU:
                    return offset;  // all bytes for the upper layers
                case BacnetBvlcSCMessage.BVLC_CONNECT_ACCEPT:
                    Array.Copy(buffer, 4, RemoteVMAC, 0, 6); // Hub or remote device VMAC ... used later for Direct Connect mode
                    return 0;       // Only for BVLC 
                case BacnetBvlcSCMessage.BVLC_DISCONNECT_REQUEST:
                    BVLC_SC_SendSimpleBVLCMsg(BacnetBvlcSCMessage.BVLC_DISCONNECT_ACK, buffer[2], buffer[3]);
                    state=BACnetSCState.IDLE;
                    return 0;       // Only for BVLC 
                case BacnetBvlcSCMessage.BVLC_DISCONNECT_ACK:
                    // Don't set BACnetSCState.IDLE ... wait for close
                    return 0;       // Only for BVLC 
                case BacnetBvlcSCMessage.BVLC_HEARTBEAT_REQUEST:
                    BVLC_SC_SendSimpleBVLCMsg(BacnetBvlcSCMessage.BVLC_HEARTBEAT_ACK, buffer[2], buffer[3]);
                    return 0;       // Only for BVLC 
                case BacnetBvlcSCMessage.BVLC_ADDRESS_RESOLUTION:
                    BVLC_SC_SendBvlcError(remote_address.VMac, buffer[2], buffer[3], BacnetErrorClasses.ERROR_CLASS_COMMUNICATION, BacnetErrorCodes.ERROR_CODE_OPTIONAL_FUNCTIONALITY_NOT_SUPPORTED);
                    return 0;       // Only for BVLC
                case BacnetBvlcSCMessage.BVLC_PROPRIETARY_MESSAGE:
                    BVLC_SC_SendBvlcError(remote_address.VMac, buffer[2], buffer[3], BacnetErrorClasses.ERROR_CLASS_COMMUNICATION, BacnetErrorCodes.ERROR_CODE_BVLC_PROPRIETARY_FUNCTION_UNKNOWN);
                    return 0;       // Only for BVLC
                case BacnetBvlcSCMessage.BVLC_ADVERTISEMENT_SOLICITATION:
                default:
                    BVLC_SC_SendBvlcError(remote_address.VMac, buffer[2], buffer[3], BacnetErrorClasses.ERROR_CLASS_COMMUNICATION, BacnetErrorCodes.ERROR_CODE_BVLC_FUNCTION_UNKNOWN);
                    return 0;       // Only for BVLC
            }
        }
        public BacnetAddress GetBroadcastAddress()
        {
            BacnetAddress ret = new BacnetAddress()
            {
                VMac = new byte[] { 255, 255, 255, 255, 255, 255 },
                net = 0xFFFF,
            };

            ret.adr = ret.VMac; // normaly not used

            return ret;
        }

        public void Dispose()
        {
            try
            {
                ConfigOK = false;
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
        public String primaryHubURI="";
        public String failoverHubURI;

        public String UUID="If Forgot !";

        public String OwnCertificateFile;
        public String HubCertificateFile;

        public bool ValidateHubCertificate=false;
        public bool DirectConnect=false;

        public bool OnlyAllowsTLS13 = false;
        public bool UseTLS { get { return primaryHubURI.Contains("wss://"); } }

        [XmlIgnore]
        public String OwnCertificateFilePassword;

        [XmlIgnore]
        public X509Certificate2 OwnCertificate; // with private key
        [XmlIgnore]
        public X509Certificate2 HubCertificate;

    }
}
