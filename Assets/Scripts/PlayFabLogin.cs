using UnityEngine;
using PlayFab;
using PlayFab.ClientModels;
using TMPro;

public class PlayFabLogin : MonoBehaviour
{
    [SerializeField] private GameObject signInDisplay = default;
    [SerializeField] private TMP_InputField usernameInputField = default;
    [SerializeField] private TMP_InputField emailInputField = default;
    [SerializeField] private TMP_InputField passwordInputField = default;

    public static string SessionTicket;

    /*private void Start()
    {
        Color color = new Color(Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f));
        Debug.Log(color.ToString());
    }*/

    public void CreateAccount()
    {
        PlayFabClientAPI.RegisterPlayFabUser(new RegisterPlayFabUserRequest
        {
            Username = usernameInputField.text,
            Email = emailInputField.text,
            Password = passwordInputField.text
        }, result =>
        {
            SessionTicket = result.SessionTicket;
            signInDisplay.SetActive(false);
        }, error =>
        {
            Debug.LogError(error.GenerateErrorReport());
        });
    }

    public void SingIn()
    {
        PlayFabClientAPI.LoginWithPlayFab(new LoginWithPlayFabRequest
        {
            Username = usernameInputField.text,
            Password = passwordInputField.text
        }, result =>
        {
            SessionTicket = result.SessionTicket;
            signInDisplay.SetActive(false);
        }, error =>
        {
            Debug.LogError(error.GenerateErrorReport());
        });
    }
}
