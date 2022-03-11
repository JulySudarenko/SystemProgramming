using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace Code.ThirdLesson
{
    internal class Server : MonoBehaviour
    {
        private const int MAX_CONNECTIONS = 10;
        private int _port = 5805;
        private int _hostID;
        private int _reliableChannel;
        private bool _isStarted = false;
        private byte _error;

        List<int> _connectionIDs = new List<int>();

        public void StartServer()
        {
            NetworkTransport.Init();

            ConnectionConfig cc = new ConnectionConfig();
            _reliableChannel = cc.AddChannel(QosType.Reliable);

            HostTopology topology = new HostTopology(cc, MAX_CONNECTIONS);
            _hostID = NetworkTransport.AddHost(topology, _port);

            _isStarted = true;
        }

        private void Update()
        {
            if (_isStarted)
            {
                int recHostID;
                int connectionID;
                int channelID;
                byte[] recBuffer = new byte[1024];
                int bufferSize = 1024;
                int dataSize;

                NetworkEventType recData = NetworkTransport.Receive(out recHostID, out connectionID, out channelID,
                    recBuffer, bufferSize, out dataSize, out _error);

                while (recData != NetworkEventType.Nothing)
                {
                    switch (recData)
                    {
                        case NetworkEventType.DataEvent:
                            break;
                        case NetworkEventType.ConnectEvent:
                            break;
                        case NetworkEventType.DisconnectEvent:
                            break;
                        case NetworkEventType.Nothing:
                            break;
                        case NetworkEventType.BroadcastEvent:
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
            }
        }

        public void ShutDownServer()
        {
        }
    }
}
