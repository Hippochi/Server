using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using System.IO;
using UnityEngine.UI;

public class NetworkedServer : MonoBehaviour
{
    const int maxConnections = 1000;
    int reliableChannelID;
    int unreliableChannelID;
    int hostID;
    const int socketPort = 5491;

    LinkedList<PlayerAccount> playerAccounts;

    void Start()
    {
        NetworkTransport.Init();
        ConnectionConfig config = new ConnectionConfig();

        reliableChannelID = config.AddChannel(QosType.Reliable);
        unreliableChannelID = config.AddChannel(QosType.Unreliable);
        HostTopology topology = new HostTopology(config, maxConnections);
        hostID = NetworkTransport.AddHost(topology, socketPort, null); //Server does not need to know IP, so this is NULL in this case

        playerAccounts = new LinkedList<PlayerAccount>();
        //read in player accounts

    }

    // Update is called once per frame
    void Update()
    {

        int recHostID;
        int recConnectionID;
        int recChannelID;
        byte[] recBuffer = new byte[1024];
        int bufferSize = 1024;
        int dataSize;
        byte error = 0;

        NetworkEventType recNetworkEvent = NetworkTransport.Receive(out recHostID, out recConnectionID, out recChannelID, recBuffer, bufferSize, out dataSize, out error);

        switch (recNetworkEvent)
        {
            case NetworkEventType.Nothing:
                break;
            case NetworkEventType.ConnectEvent:
                Debug.Log("Connection, " + recConnectionID);
                break;
            case NetworkEventType.DataEvent:
                string msg = Encoding.Unicode.GetString(recBuffer, 0, dataSize); //recBuffer is a byte array being converted to string via Encoding.Unicode
                ProcessRecievedMsg(msg, recConnectionID);
                break;
            case NetworkEventType.DisconnectEvent:
                Debug.Log("Disconnection, " + recConnectionID);
                break;
        }

    }

    public void SendMessageToClient(string msg, int id)
    {
        byte error = 0;
        byte[] buffer = Encoding.Unicode.GetBytes(msg);
        NetworkTransport.Send(hostID, id, reliableChannelID, buffer, msg.Length * sizeof(char), out error);
    }

    private void ProcessRecievedMsg(string msg, int id)
    {
        Debug.Log("msg recieved = " + msg + ".  connection id = " + id);

        string[] csv = msg.Split(',');

        int signifier = int.Parse(csv[0]);

        if (signifier == ClientToServerSignifiers.CreateAccount)
        {
            Debug.Log("create account");

            string n = csv[1];
            string p = csv[2];
            bool nameIsInUse = false;

            //check if player account name already exists,
            foreach (PlayerAccount pa in playerAccounts)
            {
                if (pa.name == n)
                    nameIsInUse = true;
                break;
            }

            if (nameIsInUse)
            {
                SendMessageToClient(ServerToClientSignifiers.AccountCreationFailed + "", id);
            }
            else
            {

                PlayerAccount newPlayerAccount = new PlayerAccount(n, p);
                playerAccounts.AddLast(newPlayerAccount);
                SendMessageToClient(ServerToClientSignifiers.AccountCreationComplete + "", id);

                //add to list, and save list to HD
            }


            //send to client to success/failure
        }
        else if (signifier == ClientToServerSignifiers.Login)
        {
            Debug.Log("login to account");
            //check if player account name already exists, 
            //send to client to success/failure
        }

    }

    public class PlayerAccount
    {
        public string name, password;

        public PlayerAccount(string Name, string Password)
        {
            name = Name;
            password = Password;
        }
    }



    public static class ClientToServerSignifiers
    {
        public const int CreateAccount = 1;

        public const int Login = 2;
    }

    public static class ServerToClientSignifiers
    {

        public const int LoginComplete = 1;

        public const int LoginFailed = 2;

        public const int AccountCreationComplete = 3;

        public const int AccountCreationFailed = 4;

    }

}