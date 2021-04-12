using Mirror;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace QuickStart
{
    public class SceneScript : NetworkBehaviour
    {
        public TextMeshProUGUI canvasStatusText;
        public PlayerScript playerScript;

        [SyncVar(hook = nameof(OnStatusTextChanged))]
        public string statusText;

        public SceneReference sceneReference;

        public TextMeshProUGUI canvasAmmoText;

        void OnStatusTextChanged(string _Old, string _New)
        {
            //called from sync var hook, to update info on screen for all players
            canvasStatusText.text = statusText;
        }

        public void ButtonSendMessage()
        {
            if (playerScript != null)
            {
                playerScript.CmdSendPlayerMessage();
            }
        }

        public void ButtonChangeScene()
        {
            if (isServer)
            {
                Scene scene = SceneManager.GetActiveScene();
                if (scene.name == "MyScene") { NetworkManager.singleton.ServerChangeScene("MyOtherScene"); }

                else { NetworkManager.singleton.ServerChangeScene("MyScene"); }
            }
            else { Debug.Log("You are not Host."); }
        }

        public void UIAmmo(int _value)
        {
            canvasAmmoText.text = "Ammo: " + _value;
        }
    }
}