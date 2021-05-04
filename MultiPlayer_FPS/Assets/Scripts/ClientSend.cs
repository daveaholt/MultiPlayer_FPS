using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClientSend : MonoBehaviour
{

    public static void WelcomeReceived()
    {
        using(Packet p = new Packet((int)ClientPackets.welcomeReceived))
        {
            p.Write(Client.instance.id);
            p.Write(UIManager.instance.usernameField.text);

            SendTCPData(p);
        }
    }

    public static void UDPTestReceived()
    {
        using(var p = new Packet((int)ClientPackets.udpTestReveived))
        {
            p.Write("Reveived a UDP packet.");
            SendUDPData(p);
        }
    }

    private static void SendUDPData(Packet p)
    {
        p.WriteLength();
        Client.instance.udp.SendData(p);
    }

    private static void SendTCPData(Packet p)
    {
        p.WriteLength();
        Client.instance.tcp.SendData(p);
    }
}
