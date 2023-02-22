/**************************************************************************
*                           MIT License
* 
* Copyright (C) 2023 Frederic Chaxel <fchaxel@free.fr> 
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
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using WebSocketSharp;
using WebSocketSharp.Server;

namespace System.IO.BACnet
{
    // Copyright(C) 2023 Frederic Chaxel<fchaxel@free.fr>
    // Yabe SourceForge Explorer and Full Open source BACnet stack
    public class bscHub
    {
        WebSocketServer Websocket;
        WebSocketServer WebsocketLoopBackWiresharkServer;
        WebSocket WebsocketLoopBackWiresharkClient;

        X509Certificate2 ownCertificate; // with private key
        X509Certificate2Collection rejectedCertificates =new X509Certificate2Collection();
        X509Certificate2Collection trustedCertificates = new X509Certificate2Collection();
        string pkiDirectory;

        X509Chain ExtraChain = new X509Chain();
        public bscHub(string URI, String pkiDirectory = null, String ownCertificatePassword = null)
        {
            if (pkiDirectory != null)
            {
                this.pkiDirectory = pkiDirectory;
                try
                {
                    ownCertificate = new X509Certificate2(pkiDirectory + "\\own\\Hub.p12", ownCertificatePassword);
                    if (!ownCertificate.HasPrivateKey)
                        // error if it's wss://
                        Trace.WriteLine("BACnet/SC : Warning the HUB own certificate is without a private key");
                }
                catch
                {
                    // error if it's wss://
                    Trace.WriteLine("BACnet/SC : Warning no HUB certificate found");
                }

                RefreshRejectedAndTrustedCertificatesLists();
            }

            Websocket = new WebSocketServer(URI);
            Websocket.SslConfiguration.EnabledSslProtocols = SslProtocols.Tls13;
            Websocket.SslConfiguration.ClientCertificateRequired = true;
            Websocket.SslConfiguration.ClientCertificateValidationCallback = RemoteCertificateValidationCallback;
            Websocket.SslConfiguration.ServerCertificate = ownCertificate;
            Websocket.AddWebSocketService<HubListener>("/");

            // Disable WebsocketServer log in the console
            Websocket.Log.Output = (_, __) => { };

            Websocket.Start();

        }

        public void ActivateSnifferForWireshark(UInt16 LoopbackWiresharkPort)
        {
            // Open a ws channel in loopback then connect to it.
            // It's used to re-send each receive frame in a unciphered channel for debug purpose
            // when a device don't allows ws communication. Wireshark (npcap in fact) can capture loopback.

            WebsocketLoopBackWiresharkServer = new WebSocketServer(IPAddress.Loopback, LoopbackWiresharkPort);
            WebsocketLoopBackWiresharkServer.AddWebSocketService<HubListenerLoopBack>("/");
            WebsocketLoopBackWiresharkServer.Log.Output = (_,__) => { };
            WebsocketLoopBackWiresharkServer.Start();

            WebsocketLoopBackWiresharkClient = new WebSocket("ws://127.0.0.1:" + LoopbackWiresharkPort.ToString(), new string[] { "hub.bsc.bacnet.org" });
            WebsocketLoopBackWiresharkClient.ConnectAsync();
            HubListener.WebsocketLoopBack = WebsocketLoopBackWiresharkClient;
        }

        public void RefreshRejectedAndTrustedCertificatesLists()
        {

            lock (rejectedCertificates)
            {
                rejectedCertificates.Clear();
                string[] fileEntries = Directory.GetFiles(pkiDirectory + "\\rejected");
                foreach (string fileName in fileEntries)
                    try { rejectedCertificates.Add(new X509Certificate2(fileName)); } catch { }
            }
            lock (trustedCertificates)
            {
                trustedCertificates.Clear();
                string[] fileEntries = Directory.GetFiles(pkiDirectory + "\\trusted");
                foreach (string fileName in fileEntries)
                    try 
                    {
                        X509Certificate2 cert = new X509Certificate2(fileName);
                        if ((DateTime.Parse(cert.GetExpirationDateString()) >= DateTime.Now) && (DateTime.Parse(cert.GetEffectiveDateString()) <= DateTime.Now))
                            trustedCertificates.Add(cert);
                        else
                            rejectedCertificates.Add(cert); // Checked by Build( ) later, so could be left in trustedCertificates
                    } 
                    catch { }
               
            }

            lock (ExtraChain)
            { 
                ExtraChain.ChainPolicy.ExtraStore.Clear();
                ExtraChain.ChainPolicy.ExtraStore.AddRange(rejectedCertificates);
                ExtraChain.ChainPolicy.ExtraStore.AddRange(trustedCertificates);
                ExtraChain.ChainPolicy.VerificationFlags = X509VerificationFlags.AllowUnknownCertificateAuthority;
                ExtraChain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
            }

        }

        // Here we accept all certificates known by the computer : SslPolicyErrors.None
        // also all certificates in thrusted directory
        // if it's the remote certificate or one of it's signing CA in the chain (if the chain is given)
        // and we reject all certificates in rejected directory
        // if it's the remote certificate or one of it's signing CA in the chain (if the chain is given)

        // In fact here a lot of things a done without a real knowledge. You are an expert, tell me what to do

        private bool IsCertificateRejected(X509Chain chain)
        {
            if (chain == null) // normaly not
                return true;

            // explicitely rejected : : the cert itself or one of the CA in the CA chain
            lock (rejectedCertificates)
                foreach (X509ChainElement chainElement in chain.ChainElements)
                    foreach (X509Certificate2 rejectedcert in rejectedCertificates)
                        if (chainElement.Certificate.Thumbprint == rejectedcert.Thumbprint)
                        {
                            Trace.WriteLine("\tCertificate explicitely rejected");
                            return true;
                        }
            return false;
        }

        private bool IsCertificateThrusted(X509Chain chain)
        {
            if (chain == null) // normaly not
                return false;

            // explicitely accepted : the cert itself or one of the CA in the CA chain
            lock (trustedCertificates)
                foreach (X509ChainElement chainElement in chain.ChainElements)
                    foreach (X509Certificate2 trustedcert in trustedCertificates)
                        if (chainElement.Certificate.Thumbprint == trustedcert.Thumbprint)
                        {
                            Trace.WriteLine("\tCertificate explicitely accepted");
                            return true;

                        }
            return false;
        }
        private bool RemoteCertificateValidationCallback(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            if (chain == null) // normaly not
                return false;

            Trace.WriteLine("Connection with certificate name : " + certificate.Subject);

            // The root CA is system accepted
            if (sslPolicyErrors == SslPolicyErrors.None)
            {
                Trace.WriteLine("\tThrusted certificate due to underlying system security policy");
                return true;
            }

            if (IsCertificateRejected(chain)) { return false; }
            if (IsCertificateThrusted(chain)) { return true; }

            // The chain is certainly not given, so we try to build it with our own PKI content
            lock (ExtraChain)
            { 
                if (ExtraChain.Build(certificate as X509Certificate2)==false) // All Dates in the chain are verified here
                {
                    String Status = ""; foreach (var e in ExtraChain.ChainStatus) Status += e.Status.ToString()+" ";
                    Trace.WriteLine("\tRejected certificate : " + Status);
                    return false;

                }
                if (IsCertificateRejected(ExtraChain)) { return false; }
                if (IsCertificateThrusted(ExtraChain)) { return true; }
            }

            // save the untrusted certificate in the issuers directory so the user can copy it after 
            try
            {
                File.WriteAllBytes(pkiDirectory + "\\issuers\\" + certificate.Subject + ".cer", certificate.Export(X509ContentType.Cert));
                Trace.WriteLine("\tUnknown certificate written in PKI\\issuers");
            } 
            catch { Trace.WriteLine("\tWrite Error : Unknown certificate NOT written in PKI\\issuers"); }

            return false;
        }
    }
    public class HubListenerLoopBack : WebSocketBehavior
    {
        // do nothing, just here to allows a unciphered capture in loopback mode
        public HubListenerLoopBack()
        {
            this.Protocol = "hub.bsc.bacnet.org";
        }
    }

    // Copyright(C) 2023 Frederic Chaxel<fchaxel@free.fr>
    // Yabe SourceForge Explorer and Full Open source BACnet stack
    public class HubListener : WebSocketBehavior
    {
        static List<HubListener> listeners= new List<HubListener>();
        static byte[] myVMAC;       // The HUB VMac
        static Guid myGuid;         // The HUB GUID

        static readonly byte[] broadcastVMAC = new byte[] { 255, 255, 255, 255, 255, 255 };

        public static WebSocket WebsocketLoopBack;

        byte[] RemoteVMac = new byte[6];    // The BACnet Node connected 
        bool IsConnected = false;

        static HubListener()
        {           
            new Random().NextBytes(myVMAC = new byte[6]);
            myVMAC[0] = (byte)((myVMAC[0] & 0xF0) | 0x02 ); // xxxx0010
            myGuid = new Guid("deadbeaf-faed-bac0-0cab-de1e7ede1e7e");

        }
         
        public HubListener() 
        {
            this.Protocol = "hub.bsc.bacnet.org";
        }
   
        protected override void OnOpen()
        {
            // basically if not, the client will also do not accept the proposal made in the ctor
            if (!this.Context.SecWebSocketProtocols.Contains(this.Protocol))
            { 
                this.Context.WebSocket.CloseAsync();
                return;
            }

            lock (listeners)
                listeners.Add(this);
            base.OnOpen();
        }
        protected override void OnClose(CloseEventArgs e)
        {
            IsConnected = false;
            lock (listeners)
                listeners.Remove(this);
            base.OnClose(e);
        }

        protected override void OnError(WebSocketSharp.ErrorEventArgs e)
        {
            IsConnected = false;
            lock (listeners)
                listeners.Remove(this);
        }
        protected override void OnMessage(MessageEventArgs e)
        {
            try { WebsocketLoopBack?.SendAsync(e.RawData, null); } catch { WebsocketLoopBack = null; }

            try
            {
                int ret = BVLC_SC_Decode(e.RawData, out BacnetBvlcSCMessage function, out byte[] DestVmac);

                // On this very basic implementation all messages other than CONNECT & HEARTBEAT are to be distributed
                if ((ret != 0) && (IsConnected==true))
                {

                    if (((e.RawData[1] & 4) != 0) && ((e.RawData[1] & 8) == 0)) // A Dest VMAC but no source VMAC
                    {
                        // Replace the destination VMAC by the Source Vmac
                        e.RawData[1] = (byte)((e.RawData[1] & ~4) | 8);
                        Array.Copy(RemoteVMac, 0, e.RawData, 4, 6);

                        // Dispatch for everybody
                        if (DestVmac.SequenceEqual(broadcastVMAC))
                        {
                            lock (listeners)
                                foreach (var listener in listeners)
                                    if ((listener != this)&&(listener.IsConnected)) 
                                        listener.SendAsync(e.RawData, null);    // send everywhere expect sender & unconnected
                        }
                        // or send only to the destination
                        else
                        {
                            lock (listeners)
                                foreach (var listener in listeners)
                                    if ((listener.RemoteVMac.SequenceEqual(DestVmac))&&(listener.IsConnected))
                                    {
                                        listener.SendAsync(e.RawData, null);    // send to the target if already connected
                                        break;
                                    }
                        }
                    }
                }
            }
            catch { }
        }
        private void BVLC_SC_SendConnectAccept(byte MsgId1, byte MsgId2)
        {

            byte[] b = new byte[4 + 6 + 16 + 2 + 2];
            b[0] = (byte)BacnetBvlcSCMessage.BVLC_CONNECT_ACCEPT;
            b[1] = 0;           // No VMAC, No Option
            b[2] = MsgId1;           // Initial Message ID
            b[3] = MsgId2;
            // Originating HUB Virtual Address
            Array.Copy(myVMAC, 0, b, 4, 6);
            // Originating HUB UUID
            byte[] bUUID = myGuid.ToByteArray();
           
            Array.Copy(bUUID, 0, b, 10, 16);

            // Max BVLC size 1600
            b[26] = 0x06;
            b[27] = 0x40;

            // Max NPDU size 1497
            b[28] = 0x05;
            b[29] = 0xD9;

            SendAsync(b, null);
            try { WebsocketLoopBack?.SendAsync(b, null); } catch { WebsocketLoopBack = null; }
        }

        //  Disconnect-ACK, Heartbeat-ACK
        private void BVLC_SC_SendSimpleACK(BacnetBvlcSCMessage Msg, byte MsgId1, byte MsgId2)
        {
            byte[] b = new byte[4];

            b[0] = (byte)Msg;
            b[1] = 0;
            b[2] = MsgId1;
            b[3] = MsgId2;

            SendAsync(b, null);
            try { WebsocketLoopBack?.SendAsync(b, null); } catch { WebsocketLoopBack = null; }
        }
        // return 0 if the message is only for the HUB service (not to be re-transmitted on another channel)
        private int BVLC_SC_Decode(byte[] buffer , out BacnetBvlcSCMessage function, out byte[] destVMac)
        {
            destVMac = null;
            int offset = 0;
            // offset always 0, we are the first after TCP
            // and a previous test by the caller guaranteed at least 4 bytes into the buffer

            function = (BacnetBvlcSCMessage)buffer[0];
            byte controlFlag = buffer[1];
            uint messageId = (uint)((buffer[2] << 8) | buffer[3]);

            offset = 4;
            if ((controlFlag & 8) != 0) // Source VMAC should be not present, since I'am a HUB
            {
                // SendError
                offset += 6;
            }

            if ((controlFlag & 4) != 0) // Dest VMAC can be absent (connect, ...)
            {
                destVMac = new byte[6];
                Array.Copy(buffer, offset, destVMac, 0, 6);
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

            // BVLC function see page 29 Addentum
            switch (function)
            {
                case BacnetBvlcSCMessage.BVLC_CONNECT_REQUEST:
                    Array.Copy(buffer, offset, RemoteVMac, 0, 6); // get the VMac for later
                    offset += 6;
                    // remoteGuid are the next 16 bytes, don't get it today
                    BVLC_SC_SendConnectAccept(buffer[2], buffer[3]);
                    IsConnected = true;
                    return 0;
                case BacnetBvlcSCMessage.BVLC_DISCONNECT_REQUEST:
                    IsConnected = false;
                    BVLC_SC_SendSimpleACK(BacnetBvlcSCMessage.BVLC_DISCONNECT_ACK, buffer[2], buffer[3]);
                    return 0;
                case BacnetBvlcSCMessage.BVLC_HEARTBEAT_REQUEST:
                    if (destVMac.SequenceEqual(myVMAC))
                    {
                        BVLC_SC_SendSimpleACK(BacnetBvlcSCMessage.BVLC_HEARTBEAT_ACK, buffer[2], buffer[3]);
                        return 0;       // Only for BVLC 
                    }
                    else
                        return offset;
                default:
                    return offset;
            }
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

}
