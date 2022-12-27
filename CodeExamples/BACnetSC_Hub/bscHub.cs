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
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Claims;
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
        X509Certificate2 ownCertificate; // with private key
        X509Certificate2Collection rejectedCertificates;
        X509Certificate2Collection trustedCertificates;
        String pkiDirectory;

        X509Chain ExtraChain = new X509Chain();
        public bscHub(string URI, String pkiDirectory=null, String ownCertificatePassword=null)
        {
            if (pkiDirectory != null)
            { 
                this.pkiDirectory= pkiDirectory;    
                PKI_Init(ownCertificatePassword);
            }

            Websocket = new WebSocketServer(URI);
            Websocket.SslConfiguration.EnabledSslProtocols = SslProtocols.Tls13;
            Websocket.SslConfiguration.ClientCertificateRequired= true;
            Websocket.SslConfiguration.ClientCertificateValidationCallback = RemoteCertificateValidationCallback;
            Websocket.SslConfiguration.ServerCertificate = ownCertificate;
            Websocket.AddWebSocketService<HubListener>("/");

            // Disable WebsocketServer log
            Websocket.Log.Output = (_, __) => { };

            Websocket.Start();
        }
        private void PKI_Init(String ownCertificatePassword)
        {
            try 
            {
                ownCertificate = new X509Certificate2(pkiDirectory + "\\own\\Hub.p12", ownCertificatePassword);
                if (!ownCertificate.HasPrivateKey)
                    // error if it's wss://
                    Trace.TraceWarning("BACnet/SC : Warning the HUB own certificate is without a private key");
            }
            catch 
            { 
                // error if it's wss://
                Trace.TraceWarning("BACnet/SC : Warning no HUB certificate found"); 
            }
            
            rejectedCertificates = new X509Certificate2Collection();
            trustedCertificates = new X509Certificate2Collection();
            RefreshRejectedAndTrustedCertificatesLists();

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
                    try { trustedCertificates.Add(new X509Certificate2(fileName)); } catch { }
            }

            ExtraChain.ChainPolicy.ExtraStore.Clear();
            ExtraChain.ChainPolicy.ExtraStore.AddRange(trustedCertificates);
            ExtraChain.ChainPolicy.VerificationFlags = X509VerificationFlags.AllowUnknownCertificateAuthority; // not suffisant !!

        }

        // Here we accept all certificates known by the computer : SslPolicyErrors.None
        // also all certificates in cert directory
        // if it's the remote certificate or one of it's signing CA in the chain (if the chain is given)

        // In fact here a lot of things a done without a real knowledge. You a expert, tell me what to do
        private bool IsCertificateThrusted(X509Chain chain)
        {
            if (chain == null) // normaly not
                return false;

            // explicitely rejected : : the cert itself or one of the CA in the CA chain
            lock (rejectedCertificates)
                foreach (X509ChainElement chainElement in chain.ChainElements)
                    foreach (X509Certificate2 rejectedcert in rejectedCertificates)
                        if (chainElement.Certificate.Thumbprint == rejectedcert.Thumbprint)
                        {
                            Trace.TraceInformation("\tCertificate explicitely rejected");
                            return false;
                        }

            // explicitely accepted : the cert itself or one of the CA in the CA chain
            lock (trustedCertificates)
                foreach (X509ChainElement chainElement in chain.ChainElements)
                    foreach (X509Certificate2 trustedcert in trustedCertificates)
                        if (chainElement.Certificate.Thumbprint == trustedcert.Thumbprint)
                        {
                            Trace.TraceInformation("\tCertificate explicitely accepted");
                            return true;

                        }
            return false;
        }
        private bool RemoteCertificateValidationCallback(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            if (chain == null) // normaly not
                return false;

            Trace.TraceInformation("Connection with certificate name : " + certificate.Subject);

            // Maybe the root CA is system accepted
            if (sslPolicyErrors == SslPolicyErrors.None)
            {
                Trace.TraceInformation("\tThrusted certificate respecting underlying system security policy");
                return true;
            }

            if (IsCertificateThrusted(chain)) { return true; }

            // The chain is certainly not given, so we try to build it with the trusted certificate
            ExtraChain.Build(certificate as X509Certificate2);

            if (IsCertificateThrusted(ExtraChain)) { return true; }

            // save the untrusted certificate in the issuers directory so the user can copy it after 
            try
            {
                File.WriteAllBytes(pkiDirectory + "\\issuers\\" + certificate.Subject + ".cer", certificate.Export(X509ContentType.Cert));
                Trace.TraceInformation("\tUnknown certificate written in PKI\\issuers");
            } 
            catch { Trace.TraceInformation("\tUnknown certificate NOT written in PKI\\issuers"); }


            return false;
        }
    }

    // Copyright(C) 2023 Frederic Chaxel<fchaxel@free.fr>
    // Yabe SourceForge Explorer and Full Open source BACnet stack
    public class HubListener : WebSocketBehavior
    {
        static List<HubListener> listeners= new List<HubListener>();
        static byte[] myVMAC;       // The HUB VMac
        static Guid myGuid;         // The HUB GUID

        static byte[] broadcastVMAC = new byte[] { 255, 255, 255, 255, 255, 255 };

        byte[] RemoteVMac = new byte[6];    // The End-device connected 
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
            try
            {
                int ret = BVLC_SC_Decode(e.RawData, out BacnetBvlcSCMessage function, out byte[] DestVmac);

                // On this very basic implementation all messages other than CONNECT & HEARTBEAT are to be distributed
                if ((ret != 0) && (IsConnected==true))
                {

                    if (((e.RawData[1] & 4) != 0) && ((e.RawData[1] & 8) == 0)) // A Dest VMAC but no source VMAC
                    {
                        e.RawData[1] = (byte)((e.RawData[1] & ~4) | 8);
                        Array.Copy(RemoteVMac, 0, e.RawData, 4, 6); // Replace the destination VMAC by the Source Vmac
                        if (DestVmac.SequenceEqual(broadcastVMAC))
                        {
                            lock (listeners)
                                foreach (var listener in listeners)
                                    if ((listener != this)&&(listener.IsConnected)) 
                                        listener.SendAsync(e.RawData, null);    // send everywhere expect sender & unconnected
                        }
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
                    BVLC_SC_SendSimpleACK(BacnetBvlcSCMessage.BVLC_DISCONNECT_ACK, buffer[2], buffer[3]);
                    IsConnected = false;
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
