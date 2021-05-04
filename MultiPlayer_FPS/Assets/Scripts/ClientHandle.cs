using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClientHandle : MonoBehaviour
{
    public static void Welcome(Packet p)
    {
        var msg = p.ReadString();
        var id = p.ReadInt();

        Debug.Log($"Message from server: {msg}");
        Client.instance.id = id;

        ClientSend.WelcomeReceived();
    }
}
