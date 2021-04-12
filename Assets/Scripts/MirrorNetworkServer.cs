
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using UnityEngine.Events;
using System;
using PlayFab;

public class MirrorNetworkServer : NetworkBehaviour
{
    public Configuration configuration;

    public PlayerEvent OnPlayerAdded = new PlayerEvent();
    public PlayerEvent OnPlayerRemoved = new PlayerEvent();

    public int MaxConnections = 100;
    public int Port = 7777;

    public NetworkManager _networkManager;

    public List<UnityNetworkConnection> Connections
    {
        get { return _connections; }
        private set { _connections = value; }
    }

    private List<UnityNetworkConnection> _connections = new List<UnityNetworkConnection>();

    public class PlayerEvent : UnityEvent<string>
    {
    }


    void Awake()
    {
        if (configuration.buildType == BuildType.REMOTE_SERVER)
        {
            AddRemoteServerListeners();
        }
    }


    private void AddRemoteServerListeners()
    {
        Debug.Log("[UnityNetworkServer].AddRemoteServerListeners");
        NetworkServer.RegisterHandler<OnServerConnectMessage>(OnServerConnect);
        NetworkServer.RegisterHandler<OnServerDisconnectMessage>(OnServerDisconnect);
        NetworkServer.RegisterHandler<OnServerErrorMessage>(OnServerError);
        NetworkServer.RegisterHandler<ReceiveAuthenticateMessage>(OnReceiveAuthenticate);
    }


    private void OnServerError(NetworkConnection arg1, OnServerErrorMessage arg2)
    {
        try
        {
            // todo
            Debug.Log("Unity Network Connection Status: code ");
        }
        catch (Exception)
        {
            Debug.Log("Unity Network Connection Status, but we could not get the reason, check the Unity Logs for more info.");
        }
    }


    public struct OnServerErrorMessage : NetworkMessage
    {
    }


    private void OnServerDisconnect(NetworkConnection arg1, OnServerDisconnectMessage arg2)
    {
        var conn = _connections.Find(c => c.ConnectionId == arg1.connectionId);
        if (conn != null)
        {
            if (!string.IsNullOrEmpty(conn.PlayFabId))
            {
                OnPlayerRemoved.Invoke(conn.PlayFabId);
            }

            _connections.Remove(conn);
        }
    }


    public struct OnServerDisconnectMessage : NetworkMessage
    {
    }


    private void OnServerConnect(NetworkConnection arg1, OnServerConnectMessage arg2)
    {
        Debug.LogWarning("Client Connected");
        var conn = _connections.Find(c => c.ConnectionId == arg1.connectionId);
        if (conn == null)
        {
            _connections.Add(new UnityNetworkConnection()
            {
                Connection = arg1,
                ConnectionId = arg1.connectionId,
                LobbyId = PlayFabMultiplayerAgentAPI.SessionConfig.SessionId
            });
        }
    }


    public struct OnServerConnectMessage : NetworkMessage
    {
    }


    public void StartServer()
    {
        NetworkServer.Listen(Port);
    }


    private void OnApplicationQuit()
    {
        NetworkServer.Shutdown();
    }


    private void OnReceiveAuthenticate(NetworkConnection arg1, ReceiveAuthenticateMessage arg2)
    {
        var conn = _connections.Find(c => c.ConnectionId == arg1.connectionId);
        if (conn != null)
        {
            conn.PlayFabId = arg2.PlayFabId;
            conn.IsAuthenticated = true;
            OnPlayerAdded.Invoke(arg2.PlayFabId);
        }
    }
}

[Serializable]
public class UnityNetworkConnection
{
    public bool IsAuthenticated;
    public string PlayFabId;
    public string LobbyId;
    public int ConnectionId;
    public NetworkConnection Connection;
}

public class CustomGameServerMessageTypes
{
    public const short ReceiveAuthenticate = 900;
    public const short ShutdownMessage = 901;
    public const short MaintenanceMessage = 902;
}

public struct ReceiveAuthenticateMessage : NetworkMessage
{
    public string PlayFabId;
}

public struct ShutdownMessage : NetworkMessage
{
}

[Serializable]
public struct MaintenanceMessage : NetworkMessage
{
    public DateTime ScheduledMaintenanceUTC;
}