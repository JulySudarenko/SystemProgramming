using System;
using System.Collections.Generic;
using System.Text;
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
                            string message = Encoding.Unicode.GetString(recBuffer, 0, dataSize);

                            SendMessageToAll($"Player {connectionID} : {message}");
                            Debug.Log($"Player {connectionID} : {message}");
                            break;
                        case NetworkEventType.ConnectEvent:
                            _connectionIDs.Add(connectionID);

                            SendMessageToAll($"Player {connectionID} has connected.");
                            Debug.Log($"Player {connectionID} has connected.");
                            break;
                        case NetworkEventType.DisconnectEvent:
                            _connectionIDs.Remove(connectionID);

                            SendMessageToAll($"Player {connectionID} has disconnected.");
                            Debug.Log($"Player {connectionID} has disconnected.");
                            break;
                        case NetworkEventType.Nothing:
                            break;
                        case NetworkEventType.BroadcastEvent:
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }

                    recData = NetworkTransport.Receive(out recHostID, out connectionID, out channelID,
                        recBuffer, bufferSize, out dataSize, out _error);
                }
            }
        }

        public void ShutDownServer()
        {
            if (_isStarted)
            {
                NetworkTransport.RemoveHost(_hostID);
                NetworkTransport.Shutdown();
                _isStarted = false;
            }
        }

        public void SendMessage(string message, int connectionID)
        {
            byte[] buffer = Encoding.Unicode.GetBytes(message);

            NetworkTransport.Send(_hostID, connectionID, _reliableChannel, buffer, message.Length * sizeof(char),
                out _error);
            if ((NetworkError) _error != NetworkError.Ok)
            {
                Debug.Log((NetworkError) _error);
            }
        }

        public void SendMessageToAll(string message)
        {
            for (int i = 0; i < _connectionIDs.Count; i++)
            {
                SendMessage(message, _connectionIDs[i]);
            }
        }
    }
}
