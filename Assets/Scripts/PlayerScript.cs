using Mirror;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using static UnityEngine.InputSystem.InputAction;
using PlayFab;
using PlayFab.ServerModels;
using System.Collections.Generic;

namespace QuickStart
{
    public class PlayerScript : NetworkBehaviour
    {
        public TestControls Controls;

        public Vector2 moveValue;
        public Vector2 rotateValue;

        public TextMeshPro playerNameText;
        public GameObject floatingInfo;

        [SyncVar(hook = nameof(OnNameChanged))]
        public string playerName;

        [SyncVar(hook = nameof(OnColorChanged))]
        public Color playerColor = Color.white;

        private Material playerMaterialClone;

        private SceneScript sceneScript;

        private int selectedWeaponLocal = 1;
        public GameObject[] weaponArray;

        [SyncVar(hook = nameof(OnWeaponChanged))]
        public int activeWeaponSynced = 1;

        private Weapon activeWeapon;
        private float weaponCooldownTime;

        private string playFabId;

        public override void OnStartAuthority()
        {
            sceneScript.playerScript = this;

            Camera.main.transform.SetParent(transform);
            Camera.main.transform.localPosition = new Vector3(0, 0, 0);

            floatingInfo.transform.localPosition = new Vector3(0, -0.3f, 0.6f);
            floatingInfo.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);

            CmdSetSessionTicket(PlayFabLogin.SessionTicket);
        }

        [Command]
        private void CmdSetSessionTicket(string SessionTicket)
        {
            PlayFabServerAPI.AuthenticateSessionTicket(new AuthenticateSessionTicketRequest
            {
                SessionTicket = SessionTicket
            }, result =>
            {
                playFabId = result.UserInfo.PlayFabId;

                GetColor();
                CmdSetupPlayer(result.UserInfo.Username, playerColor);
            },error =>
            {
                Debug.LogError(error.GenerateErrorReport());
            });
        }

        [Server]
        private void GetColor()
        {
            PlayFabServerAPI.GetUserData(new GetUserDataRequest
            {
                PlayFabId = playFabId
            }, result =>
            {
                if (result.Data != null)
                {
                    if (!result.Data.ContainsKey("Color"))
                    {
                        Color newCol = new Color(Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f));

                        PlayFabServerAPI.UpdateUserData(new UpdateUserDataRequest
                        {
                            PlayFabId = playFabId,
                            Data = new Dictionary<string, string>
                            {
                                { "Color", newCol.ToString() }
                            }
                        }, result =>
                        {
                            playerColor = newCol;
                        }, error =>
                        {
                            Debug.LogError(error.GenerateErrorReport());
                        });
                    }
                    else
                    {
                        playerColor = ColorExtensions.ParseColor(result.Data["Color"].Value);
                    }
                }
            }, error =>
            {
                Debug.LogError(error.GenerateErrorReport());
            });
        }

        void OnNameChanged(string _Old, string _New)
        {
            playerNameText.text = playerName;
        }

        void OnColorChanged(Color _Old, Color _New)
        {
            playerNameText.color = _New;
            playerMaterialClone = new Material(GetComponent<Renderer>().material);
            playerMaterialClone.color = _New;
            GetComponent<Renderer>().material = playerMaterialClone;
        }

        void OnWeaponChanged(int _Old, int _New)
        {
            // disable old weapon
            // in range and not null
            if (0 < _Old && _Old < weaponArray.Length && weaponArray[_Old] != null)
            {
                weaponArray[_Old].SetActive(false);
            }

            // enable new weapon
            // in range and not null
            if (0 < _New && _New < weaponArray.Length && weaponArray[_New] != null)
            {
                weaponArray[_New].SetActive(true);
                activeWeapon = weaponArray[activeWeaponSynced].GetComponent<Weapon>();
                if (isLocalPlayer) { sceneScript.UIAmmo(activeWeapon.weaponAmmo); }
            }
        }

        /*public override void OnStartLocalPlayer()
        {
            sceneScript.playerScript = this;

            Camera.main.transform.SetParent(transform);
            Camera.main.transform.localPosition = new Vector3(0, 0, 0);

            floatingInfo.transform.localPosition = new Vector3(0, -0.3f, 0.6f);
            floatingInfo.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);

            string name = "Player" + Random.Range(100, 999);
            Color color = new Color(Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f));
            CmdSetupPlayer(name, color);
        }*/

        [Command]
        public void CmdSetupPlayer(string _name, Color _col)
        {
            // player info sent to server, then server updates sync vars which handles it on all clients
            playerName = _name;
            playerColor = _col;

            sceneScript.statusText = $"{playerName} joined.";
        }

        [Command]
        public void CmdSendPlayerMessage()
        {
            if (sceneScript)
            {
                sceneScript.statusText = $"{playerName} says hello {Random.Range(10, 99)}";
            }
        }

        [Command]
        public void CmdChangeActiveWeapon(int newIndex)
        {
            activeWeaponSynced = newIndex;
        }

        [Command]
        void CmdShootRay()
        {
            RpcFireWeapon();
        }

        [ClientRpc]
        void RpcFireWeapon()
        {
            //bulletAudio.Play(); muzzleflash  etc
            var bullet = (GameObject)Instantiate(activeWeapon.weaponBullet, activeWeapon.weaponFirePosition.position, activeWeapon.weaponFirePosition.rotation);
            bullet.GetComponent<Rigidbody>().velocity = bullet.transform.forward * activeWeapon.weaponSpeed;
            if (bullet) { Destroy(bullet, activeWeapon.weaponLife); }
        }

        public void OnInputAction(CallbackContext context)
        {
            if (Application.isFocused && isLocalPlayer)
            {
                if (context.action.name == Controls.Player.Move.name)
                {
                    Move(context);
                }
                if (context.action.name == Controls.Player.Rotate.name)
                {
                    Rotate(context);
                }
                if (context.action.name == Controls.Player.SwitchWeapon.name && context.performed)
                {
                    SwitchWeapon();
                }
                if (context.action.name == Controls.Player.Shoot.name && context.started)
                {
                    Shoot();
                }
            }
        }

        public void Move(CallbackContext context)
        {
            moveValue = context.ReadValue<Vector2>();
        }
        public void Rotate(CallbackContext context)
        {
            rotateValue = context.ReadValue<Vector2>();
        }

        public void SwitchWeapon()
        {
            selectedWeaponLocal += 1;

            if (selectedWeaponLocal > weaponArray.Length)
            {
                selectedWeaponLocal = 1;
            }

            CmdChangeActiveWeapon(selectedWeaponLocal);

        }

        public void Shoot()
        {
            if (activeWeapon && Time.time > weaponCooldownTime && activeWeapon.weaponAmmo > 0)
            {
                weaponCooldownTime = Time.time + activeWeapon.weaponCooldown;
                activeWeapon.weaponAmmo -= 1;
                sceneScript.UIAmmo(activeWeapon.weaponAmmo);
                CmdShootRay();
            }
        }

        private void Awake()
        {
            Controls = new TestControls();

            GetComponent<PlayerInput>().onActionTriggered += OnInputAction;

            sceneScript = GameObject.Find("SceneReference").GetComponent<SceneReference>().sceneScript;

            // disable all weapons
            foreach (var item in weaponArray)
            {
                if (item != null)
                {
                    item.SetActive(false);
                }
            }

            if (selectedWeaponLocal < weaponArray.Length && weaponArray[selectedWeaponLocal] != null)
            { activeWeapon = weaponArray[selectedWeaponLocal].GetComponent<Weapon>(); sceneScript.UIAmmo(activeWeapon.weaponAmmo); }
        }

        void Update()
        {
            if (!isLocalPlayer)
            {
                // make non-local players run this
                floatingInfo.transform.LookAt(Camera.main.transform);
                return;
            }

            Vector2 move = moveValue * Time.deltaTime * 4f;
            Vector2 rotate = rotateValue * Time.deltaTime * 110.0f;

            transform.Translate(move.x, 0, move.y);
            transform.Rotate(0, rotate.x, 0);
        }
    }
}