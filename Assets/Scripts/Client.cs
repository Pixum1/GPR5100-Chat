using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net.Sockets;
using System.Net;
using System;
using System.IO;
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.Events;

public class Client : MonoBehaviour
{
    private bool isConnectedFlag = false;

    private TcpClient client;

    private StreamWriter writer;
    private StreamReader reader;

    private NetworkStream networkStream;

    private string ip;
    private int port;
    private string username;

    [SerializeField]
    private TMP_InputField ipInput;
    [SerializeField]
    private TMP_InputField portInput;
    [SerializeField]
    private TMP_InputField userNameInput;
    [SerializeField]
    private TMP_Text chatText;
    [SerializeField]
    private TMP_InputField messageText;
    [SerializeField]
    private TMP_Text userListText;
    [SerializeField]
    private GameObject userListPanel;
    [SerializeField]
    private Button connectionButton;
    [SerializeField]
    private EventSystem ev;
    [SerializeField]
    private int chatLength = 20;
    string[] chatMessages;

    private void Start()
    {
        chatMessages = new string[chatLength];

        for (int i = 0; i < chatLength; i++)
        {
            chatMessages[i] = "";
        }

        ipInput.text = "127.0.0.1";
        portInput.text = "8888";
    }

    public void ConnectToServer()
    {
        if (isConnectedFlag)
        {
            return;
        }

        //UI Adjustments
        userListPanel.SetActive(true);
        connectionButton.GetComponentInChildren<TMP_Text>().text = "Disconnect";
        connectionButton.onClick.RemoveAllListeners();
        connectionButton.onClick.AddListener(DisconnectedFromServer);

        //Get Server Info
        username = "Guest";
        if (!string.IsNullOrEmpty(userNameInput.text))
            username = userNameInput.text;
        ip = "127.0.0.1";
        if (!string.IsNullOrEmpty(ipInput.text))
            ip = ipInput.text;
        port = Int32.Parse(portInput.text);
        if (port <= 0)
            port = 8888;

        try
        {
            client = new TcpClient(ip, port);

            networkStream = client.GetStream();
            writer = new StreamWriter(networkStream);
            reader = new StreamReader(networkStream);

            chatText.text = "";
            Debug.Log($"CLIENT INFO: Connected to server on port {port}");

            isConnectedFlag = true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"CLIENT ERROR: Couldn't connect to server. Error info: {ex.Message}");
        }
    }

    private void Update()
    {
        if (isConnectedFlag)
        {
            if (networkStream.DataAvailable)
            {
                string data = reader.ReadLine();
                if (data != null)
                {
                    OnInComingData(data);
                }
            }
            if (Input.GetKeyDown(KeyCode.Return))
                OnSendMessage();
        }
    }
    private void OnInComingData(string _data)
    {
        if (_data == "%NAME")
        {
            Debug.Log("I have entered the chat.");

            SendMessageToServer("&NAME" + "|" + username);

            return;
        }
        if (_data.Contains("#USERNAME"))
        {
            string[] names = _data.Split('|');

            userListText.text = "";
            for (int i = 1; i < names.Length; i++)
            {
                userListText.text += names[i];
                userListText.text += "\n";
            }
            return;
        }

        AddNewChatMessage(_data);
    }

    public void OnSendMessage()
    {
        string data = messageText.text;
        messageText.text = "";

        if (data.Contains("/disconnect"))
        {
            DisconnectedFromServer();
        }
        else
            SendMessageToServer(data);

        ev.SetSelectedGameObject(messageText.gameObject);
    }

    private void SendMessageToServer(string _data)
    {
        if (!isConnectedFlag || string.IsNullOrEmpty(_data)) return;
        Debug.Log("Send Message to Server");

        writer.WriteLine(_data);
        writer.Flush();
    }

    private void AddNewChatMessage(string _message)
    {
        for (int i = 0; i < chatLength - 1; i++)
        {
            chatMessages[i] = chatMessages[i + 1];
        }

        chatMessages[chatLength - 1] = _message;

        chatText.text = "";
        for (int i = 0; i < chatMessages.Length; i++)
        {
            if (string.IsNullOrEmpty(chatMessages[i])) continue;

            chatText.text += chatMessages[i] + "\n";
        }
    }

    public void DisconnectedFromServer()
    {
        writer.Close();
        reader.Close();
        client.Close();
        isConnectedFlag = false;

        //UI Adjustments
        connectionButton.GetComponentInChildren<TMP_Text>().text = "Connect";
        connectionButton.onClick.RemoveAllListeners();
        connectionButton.onClick.AddListener(ConnectToServer);
        userListPanel.SetActive(false);

        return;
    }

    private void OnApplicationQuit()
    {
        DisconnectedFromServer();
    }
    private void OnDestroy()
    {
        DisconnectedFromServer();
    }
    private void OnDisable()
    {
        DisconnectedFromServer();
    }
}
