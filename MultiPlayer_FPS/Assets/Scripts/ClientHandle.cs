using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net;

public class ClientHandle : MonoBehaviour
{
    public static void Welcome(Packet p)
    {
        var msg = p.ReadString();
        var id = p.ReadInt();

        Debug.Log($"Message from server: {msg}");
        Client.instance.id = id;

        ClientSend.WelcomeReceived();

        Client.instance.udp.Connect(((IPEndPoint)Client.instance.tcp.socket.Client.LocalEndPoint).Port);
    }

    public static void UDPTest(Packet p)
    {
        var msg = p.ReadString();

        Debug.Log($"Received packet via UDP. Contains message: {msg}");

        ClientSend.UDPTestReceived();
    }
}
