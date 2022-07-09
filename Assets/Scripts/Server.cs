using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net.Sockets;
using System.Net;
using System;
using System.IO;
using TMPro;
using UnityEngine.UI;

public class Server : MonoBehaviour
{
    private bool serverStartedFlag = false;

    private TcpListener server;

    private string ip;
    private int port;

    public List<ServerClient> clientList;
    public List<ServerClient> disconnectedClientList;

    [SerializeField]
    private TMP_InputField ipInput;
    [SerializeField]
    private TMP_InputField portInput;

    public void StartServer()
    {
        clientList = new List<ServerClient>();
        disconnectedClientList = new List<ServerClient>();

        try
        {
            //Get Server Info
            ip = "127.0.0.1";
            if (!string.IsNullOrEmpty(ipInput.text))
                ip = ipInput.text;
            port = Int32.Parse(portInput.text);
            if (port <= 0)
                port = 8888;

            server = new TcpListener(IPAddress.Parse(ip), port);

            server.Start();

            server.BeginAcceptTcpClient(AcceptTcpClient, server);

            serverStartedFlag = true;

            Debug.Log($"SERVER INFO: Server has been started on port: {port}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"SERVER ERROR: Server couldn't be started. Error Info: {ex.Message}");
        }
    }

    private void Update()
    {
        if (!serverStartedFlag)
            return;

        for (int i = 0; i < clientList.Count; i++)
        {
            if (IsConnected(clientList[i].Tcp) == false)
            {
                clientList[i].Tcp.Close();

                disconnectedClientList.Add(clientList[i]);

                continue;
            }

            NetworkStream stream = clientList[i].Tcp.GetStream();

            if (!stream.DataAvailable)
                continue;

            StreamReader reader = new StreamReader(stream, true);

            string data = reader.ReadLine();

            if (data != null)
                OnIncomingData(clientList[i], data);
        }

        for (int i = 0; i < disconnectedClientList.Count; i++)
        {
            clientList.Remove(disconnectedClientList[i]);

            SendClientNames();
        }
        disconnectedClientList.Clear();
    }

    private void OnIncomingData(ServerClient _client, string _data)
    {
        if (_data.Contains("&NAME"))
        {
            string name = _data.Split('|')[1];

            if (string.IsNullOrEmpty(name))
                name = "Guest";

            int userNumber = 0;
            string tempName = name;

            for (int i = 0; i < clientList.Count; i++)
            {
                if (tempName == clientList[i].clientName)
                {
                    userNumber += 1;
                    tempName = name + userNumber;
                    i = 0;
                }
            }
            name = tempName;

            _client.clientName = name;

            SendClientNames();

            return;
        }
        if (_data.Substring(0, 1) == "/")
        {
            if (_data.Split(' ')[0] == "/whisper")
            {
                if (ErrorCheck(_data == "/whisper", "Try this => /whisper {user} {message}", _client)) return;

                string[] incomingString = _data.Split(' ');

                //wrong input
                if (ErrorCheck(incomingString.Length == 1, _data + " is not valid!", _client)) return;

                string receiver = incomingString[1];
                string message = "";

                List<ServerClient> recipients = new List<ServerClient>();
                recipients.Add(_client);

                //Get message
                for (int i = 2; i < incomingString.Length; i++)
                {
                    message += incomingString[i] + " ";
                }
                //Get receiver
                for (int i = 0; i < clientList.Count; i++)
                {
                    if (clientList[i].clientName == receiver)
                        recipients.Add(clientList[i]);
                }

                //No receiver
                if (ErrorCheck(recipients.Count < 2, $"{receiver} is not a valid user!", _client)) return;

                //No message content
                if (ErrorCheck(message == "", "Your message has no content", _client)) return;


                Broadcast($"(Whisper: {recipients[0].clientName} -> {recipients[1].clientName}) : {message}", recipients);
            }
            else if (_data.Split(' ')[0] == "/help")
            {
                BroadcastPrivate($"Server: Commands: /whisper, /help, /disconnect", _client);
            }
            else
                BroadcastPrivateErrorMessage(_data, _client);
        }
        else
        {
            Broadcast(_client.clientName + ": " + _data, clientList);
        }
    }

    private void SendClientNames()
    {
        string names = "";
        for (int i = 0; i < clientList.Count; i++)
        {
            names += "|" + clientList[i].clientName;
        }

        Broadcast("#USERNAME" + names, clientList);
    }

    #region Broadcasting to Clients
    private void BroadcastPrivateErrorMessage(string _falseInput, ServerClient _client)
    {
        BroadcastPrivate($"(ERROR) Server: {_falseInput}", _client);
    }
    private bool ErrorCheck(bool _condition, string _error, ServerClient _client)
    {
        if (_condition)
        {
            BroadcastPrivateErrorMessage(_error, _client);
            return true;
        }
        else
            return false;
    }
    private void BroadcastPrivate(string _data, ServerClient _receiver)
    {
        try
        {
            StreamWriter writer = new StreamWriter(_receiver.Tcp.GetStream());

            writer.WriteLine(_data);
            writer.Flush();
        }
        catch (Exception e)
        {
            Debug.LogWarning("WRITER ERROR: " + e);
        }
    }
    private void Broadcast(string _data, List<ServerClient> _receiver)
    {
        for (int i = 0; i < _receiver.Count; i++)
        {
            try
            {
                StreamWriter writer = new StreamWriter(_receiver[i].Tcp.GetStream());

                writer.WriteLine(_data);
                writer.Flush();
            }
            catch (Exception e)
            {
                Debug.LogWarning("WRITER ERROR: " + e);
            }
        }
    }
    #endregion

    private bool IsConnected(TcpClient _client)
    {
        try
        {
            if (_client == null)
                return false;
            if (_client.Client == null)
                return false;
            if (!_client.Client.Connected)
                return false;

            if (_client.Client.Poll(0, SelectMode.SelectRead))
            {
                return !(_client.Client.Receive(new byte[1], SocketFlags.Peek) == 0);
            }

            return true;
        }
        catch
        {
            return false;
        }
    }


    public void AcceptTcpClient(IAsyncResult _result)
    {
        TcpListener listener = (TcpListener)_result.AsyncState;

        clientList.Add(new ServerClient(listener.EndAcceptTcpClient(_result)));

        server.BeginAcceptTcpClient(AcceptTcpClient, server);

        Broadcast("%NAME", new List<ServerClient>() { clientList[clientList.Count - 1] });
    }
}

/// <summary>
/// Saves basic info about connected clients
/// </summary>
public class ServerClient
{
    private TcpClient tcp;
    public TcpClient Tcp { get { return tcp; } }

    public string clientName;

    public ServerClient(TcpClient _tcp)
    {
        tcp = _tcp;
    }
}