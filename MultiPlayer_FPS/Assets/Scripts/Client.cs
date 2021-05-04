using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System;

public class Client : MonoBehaviour
{
    public static Client instance;
    public static int dataBufferSize = 4096;

    public string ip = "127.0.0.1";
    public int port = 3074;
    public int id = 0;
    public TCP tcp;
    public UDP udp;

    private delegate void PacketHandler(Packet p);
    private static Dictionary<int, PacketHandler> packetHandlers;

    public void ConnectToServer()
    {
        InitializeClientData();

        tcp.Connect();
    }

    private void Awake()
    {
        if(instance == null)
        {
            instance = this;
        }
        else if(instance != this)
        {
            Debug.Log("Instance already exists, destroying object.");
            Destroy(this);
        }
    }

    private void Start()
    {
        tcp = new TCP();
        udp = new UDP();
    } 

    public class UDP
    {
        public UdpClient socket;
        public IPEndPoint endpoint;

        public UDP()
        {
            endpoint = new IPEndPoint(IPAddress.Parse(instance.ip), instance.port);
        }

        public void Connect(int localPort)
        {
            socket = new UdpClient(localPort);
            socket.Connect(endpoint);
            socket.BeginReceive(ReceiveCallback, null);

            using(Packet p = new Packet())
            {
                SendData(p);
            }
        } 

        public void SendData(Packet p)
        {
            try
            {
                p.InsertInt(instance.id);
                if(socket != null)
                {
                    socket.BeginSend(p.ToArray(), p.Length(), null, null);
                }
            }
            catch(Exception ex)
            {
                Debug.Log($"Error sending data to server via UDP: {ex}");
            }
        }

        private void ReceiveCallback(IAsyncResult result)
        {
            try
            {
                var data = socket.EndReceive(result, ref endpoint);
                socket.BeginReceive(ReceiveCallback, null);

                if(data.Length < 4)
                {
                    //TODO: disconnect (maybe?? partial packet?)
                    return;
                }

                HandleData(data);
            }
            catch(Exception ex)
            {
                //TODO: disconnect 
            }
        }

        private void HandleData(byte[] data)
        {
            using (Packet p = new Packet(data))
            {
                var packetLength = p.ReadInt();
                data = p.ReadBytes(packetLength);
            }

            ThreadManager.ExecuteOnMainThread(()=>
            {
                using(Packet p = new Packet(data))
                {
                    var packetId = p.ReadInt();
                    packetHandlers[packetId](p);
                }
            });
        }
    }

    public class TCP
    {
        public TcpClient socket;

        private Packet receiveData;
        private NetworkStream stream;
        private byte[] receiveBuffer;

        public void Connect()
        {
            socket = new TcpClient
            {
                ReceiveBufferSize = dataBufferSize,
                SendBufferSize = dataBufferSize
            };

            receiveBuffer = new byte[dataBufferSize];
            socket.BeginConnect(instance.ip, instance.port, ConnectCallback, socket);
        }

        public void ConnectCallback(IAsyncResult result)
        {
            socket.EndConnect(result);

            if (!socket.Connected)
            {
                return;
            }

            stream = socket.GetStream();
            receiveData = new Packet();
             
            stream.BeginRead(receiveBuffer, 0, dataBufferSize, ReceiveCallback, null);
        }

        public void SendData(Packet p)
        {
            try
            {
                if(socket != null)
                {
                    stream.BeginWrite(p.ToArray(), 0, p.Length(), null, null);
                }
            }
            catch(Exception ex)
            {
                Debug.Log($"Error sending data to server via TCP: {ex}");
            }
        }

        private void ReceiveCallback(IAsyncResult result)
        {
            try
            {
                var byteLength = stream.EndRead(result);
                if (byteLength <= 0)
                {
                    //TODO: disconnect
                    return;
                }

                var data = new byte[byteLength];
                Array.Copy(receiveBuffer, data, byteLength);

                receiveData.Reset(HandleData(data));
                stream.BeginRead(receiveBuffer, 0, dataBufferSize, ReceiveCallback, null);
            }
            catch (Exception ex)
            {
                //TODO: disconnect
            }
        }

        private bool HandleData(byte[] data)
        {
            var packetLength = 0;

            receiveData.SetBytes(data);
            if(receiveData.UnreadLength() >= 4)
            {
                packetLength = receiveData.ReadInt();
                if(packetLength <= 0)
                {
                    return true;
                }
            }

            while(packetLength > 0 && packetLength <= receiveData.UnreadLength())
            {
                var packetBytes = receiveData.ReadBytes(packetLength);
                ThreadManager.ExecuteOnMainThread(() =>
                {
                    using(Packet p = new Packet(packetBytes))
                    {
                        var packetId = p.ReadInt();
                        packetHandlers[packetId](p);
                    };
                });

                packetLength = 0;
                if (receiveData.UnreadLength() >= 4)
                {
                    packetLength = receiveData.ReadInt();
                    if (packetLength <= 0)
                    {
                        return true;
                    }
                }
            }

            if (packetLength <= 0)
            {
                return true;
            }
            return false;
        }
    }

    private void InitializeClientData()
    {
        packetHandlers = new Dictionary<int, PacketHandler>()
        {
            {(int)ServerPackets.welcome, ClientHandle.Welcome },
            {(int)ServerPackets.udpTest, ClientHandle.UDPTest }
        };
        Debug.Log("Initialized packets");
    }
}
