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

    private static void SendTCPData(Packet p)
    {
        p.WriteLength();
        Client.instance.tcp.SendData(p);
    }
}
