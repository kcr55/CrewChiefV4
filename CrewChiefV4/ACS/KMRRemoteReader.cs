﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace CrewChiefV4.ACS
{
    public class KMRRemoteReader : RemoteDataReader
    {
        private String remoteAddress;
        private int remotePort;
        private String appId;
        private String clientGUID;
        private String apiToken;

        private UdpClient udpListenClient;

        private Task<UdpReceiveResult> receiveResultTask;

        private UTF8Encoding encoder = new UTF8Encoding(false);

        private Boolean hasNewData = false;

        public override void activate(Object activationData)
        {
            if (active)
            {
                deactivate();
            }
            // make the UDP handshake with the remote to register
            KMRHandshakeData handshakeData = (KMRHandshakeData)activationData;
            this.remoteAddress = handshakeData.remoteAddress;
            this.remotePort = handshakeData.remotePort;
            this.apiToken = handshakeData.apiToken;
            this.appId = handshakeData.appId;
            this.clientGUID = handshakeData.clientGUID;
            sendRegisterUDPPacket(this.remoteAddress, this.remotePort, true);
            base.activate(activationData);
        }

        public override void deactivate()
        {
            sendRegisterUDPPacket(this.remoteAddress, this.remotePort, false);
            base.deactivate();
        }

        private void sendRegisterUDPPacket(String address, int port, Boolean start)
        {
            byte[] contents = getRegisterDatagram(start);
            UdpClient client = new UdpClient();
            client.Send(contents, contents.Length, new IPEndPoint(IPAddress.Parse(this.remoteAddress), this.remotePort));
            client.Close();

            if (this.receiveResultTask != null)
            {
                // proper way to cancel this task?
                receiveResultTask.Dispose();
            }
            if (this.udpListenClient != null)
            {
                this.udpListenClient.Close();
            }

            if (start) {
                // initialise the receiving socket
                this.udpListenClient = new UdpClient();
                IPEndPoint remoteAddress = new IPEndPoint(IPAddress.Parse(this.remoteAddress), this.remotePort);
                udpListenClient.Connect(remoteAddress);
                this.receiveResultTask = this.udpListenClient.ReceiveAsync();
                // TODO: add callbacks to the receive task to populate the local data
            }
        }

        private byte[] getRegisterDatagram(Boolean start)
        {
            // construct from member vars + start / stop flag
            String fullRegisterStr = this.appId + this.clientGUID + this.apiToken;
            byte[] registerBytes = encoder.GetBytes(fullRegisterStr);
            byte[] registerDatagramContent = new byte[registerBytes.Length + 1];
            Array.Copy(registerBytes, registerDatagramContent, registerBytes.Length);
            registerDatagramContent[registerDatagramContent.Length - 1] = start ? (byte)(uint)1 : (byte)(uint)0;
            return registerDatagramContent;
        }

        public override GameState.RemoteData getRemoteDataInternal(GameState.RemoteData remoteData, object rawGameData)
        {
            if (hasNewData)
            {
                // move received data into the remote object
                hasNewData = false;
            }
            return remoteData;
        }
    }

    public class KMRHandshakeData
    {
        public String remoteAddress;
        public int remotePort;
        public String appId;
        public String clientGUID;
        public String apiToken;
    }
}
