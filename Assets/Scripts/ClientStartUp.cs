using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayFab;
using System;
using PlayFab.ClientModels;
using PlayFab.MultiplayerModels;
using Mirror;
using TMPro;
using kcp2k;
//using PlayFab.Helpers;

public class ClientStartUp : MonoBehaviour
{
 	public Configuration configuration;
	public ServerStartUp serverStartUp;
	public NetworkManager networkManager;

    [SerializeField] private GameObject signInDisplay = default;
	[SerializeField] private GameObject searchGameDisplay = default;
	[SerializeField] private TMP_InputField usernameInputField = default;
    [SerializeField] private TMP_InputField emailInputField = default;
    [SerializeField] private TMP_InputField passwordInputField = default;

    public static string SessionTicket;

	public static string EntityId;


    public void OnLoginUserButtonClick()
	{
		if (configuration.buildType == BuildType.REMOTE_CLIENT)
		{
			if (configuration.buildId == "")
			{
				throw new Exception("A remote client build must have a buildId. Add it to the Configuration. Get this from your Multiplayer Game Manager in the PlayFab web console.");
			}
			else
			{
				LoginRemoteUser();
			}
		}
		else if (configuration.buildType == BuildType.LOCAL_CLIENT)
		{
			SetupTransport();

			networkManager.StartClient();
		}
	}

    public void OnCreateUserButtonClick()
    {
        if (configuration.buildType == BuildType.REMOTE_CLIENT)
        {
			CreateAccount();
        }
    }

    private static void SetupTransport()
	{
		var configuration = FindObjectOfType<Configuration>();

		var kcpTransport = FindObjectOfType<KcpTransport>();
		if (kcpTransport == null)
		{
			Debug.LogError($"[ClientStartUp.OnLoginUserButtonClick] Wrong IP:Port configuration!");
			return;
		}

		kcpTransport.Port = configuration.port;
		NetworkManager.singleton.networkAddress = configuration.ipAddress;
	}

    public void CreateAccount()
    {
		PlayFabClientAPI.RegisterPlayFabUser(new RegisterPlayFabUserRequest
		{
			TitleId = PlayFabSettings.TitleId,
			Username = usernameInputField.text,
			Email = emailInputField.text,
			Password = passwordInputField.text
		}, result =>
		{
			SessionTicket = result.SessionTicket;
			EntityId = result.EntityToken.Entity.Id;
			signInDisplay.SetActive(false);
			searchGameDisplay.SetActive(true);
		}, error =>
		{
			Debug.LogError(error.GenerateErrorReport());
		}); ;
    }

    public void LoginRemoteUser()
	{
		Debug.Log("[ClientStartUp].LoginRemoteUser");

        PlayFabClientAPI.LoginWithPlayFab(new LoginWithPlayFabRequest
        {
            Username = usernameInputField.text,
            Password = passwordInputField.text
        }, result =>
        {
            SessionTicket = result.SessionTicket;
			EntityId = result.EntityToken.Entity.Id;
			signInDisplay.SetActive(false);
			searchGameDisplay.SetActive(true);
            if (configuration.ipAddress == "")
            {
                //We need to grab an IP and Port from a server based on the buildId. Copy this and add it to your Configuration.
                RequestMultiplayerServer();
            }
            else
            {
                ConnectRemoteClient();
            }
        }, error =>
        {
            Debug.LogError(error.GenerateErrorReport());
        });
    }

	private void RequestMultiplayerServer()
	{
		Debug.Log("[ClientStartUp].RequestMultiplayerServer");
		RequestMultiplayerServerRequest requestData = new RequestMultiplayerServerRequest();
		requestData.BuildId = configuration.buildId;
		requestData.SessionId = System.Guid.NewGuid().ToString();
		requestData.PreferredRegions = new List<string>() {AzureRegion.NorthEurope.ToString()};
		PlayFabMultiplayerAPI.RequestMultiplayerServer(requestData, OnRequestMultiplayerServer, OnRequestMultiplayerServerError);
	}

	private void OnRequestMultiplayerServer(RequestMultiplayerServerResponse response)
	{
		Debug.Log(response.ToString());
		ConnectRemoteClient(response);
	}

	private void ConnectRemoteClient(RequestMultiplayerServerResponse response = null)
	{
		if (response == null)
		{
			networkManager.networkAddress = configuration.ipAddress;
			networkManager.GetComponent<KcpTransport>().Port = configuration.port;
		}
		else
		{
			Debug.Log("**** ADD THIS TO YOUR CONFIGURATION **** -- IP: " + response.IPV4Address + " Port: " + (ushort) response.Ports[0].Num);
			networkManager.networkAddress = response.IPV4Address;
			networkManager.GetComponent<KcpTransport>().Port = (ushort) response.Ports[0].Num;
		}

		/**
		 * This needs to be called when matchmaking is done
		 */
		networkManager.StartClient();
	}

	private void OnRequestMultiplayerServerError(PlayFabError error)
	{
		Debug.Log(error.ErrorDetails);
	}
}