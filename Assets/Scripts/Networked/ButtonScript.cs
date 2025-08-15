using System.Collections.Generic;
using FishNet.Connection;
using FishNet.Managing;
using FishNet.Object;
using FishNet.Transporting;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace FishNet.Example
{
    public class ButtonScript : NetworkBehaviour
    {

        #region Private.
        private NetworkManager _networkManager;

        [SerializeField]
        private GameObject currentPlayerInstance; // Track the current player instance

#if !ENABLE_INPUT_SYSTEM
        private EventSystem _eventSystem;
#endif
        #endregion

        void OnGUI()
        {
            if (base.IsOwner)
            {
#if ENABLE_INPUT_SYSTEM
                GUILayout.BeginArea(new Rect(4, 110, 256, 9000));
                Vector2 defaultResolution = new Vector2(1920f, 1080f);
                GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(Screen.width / defaultResolution.x, Screen.height / defaultResolution.y, 1));

                GUIStyle style = GUI.skin.GetStyle("button");
                int originalFontSize = style.fontSize;

                Vector2 buttonSize = new Vector2(165f, 42f);
                style.fontSize = 26;

                // Client button.
                if (GUILayout.Button($"Client", GUILayout.Width(buttonSize.x), GUILayout.Height(buttonSize.y)))
                    SpawnAndroPC();

                style.fontSize = originalFontSize;
                GUILayout.EndArea();
#endif
            }
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            if (base.IsOwner)
            {
#if !ENABLE_INPUT_SYSTEM
            SetEventSystem();
            BaseInputModule inputModule = FindObjectOfType<BaseInputModule>();
            if (inputModule == null)
                gameObject.AddComponent<StandaloneInputModule>();

#endif

                _networkManager = FindObjectOfType<NetworkManager>();
                if (_networkManager == null)
                {
                    Debug.LogError("NetworkManager not found, HUD will not function.");
                    return;
                }
            }
        }





        // Called by the client to spawn a specific character
        public void SpawnAndroPC()
        {
            if (base.IsOwner)
            {
                Debug.Log("Button clicked, starting character spawn...");

                GameObject serverObject = GameObject.FindWithTag("Server"); // Ensure the GameObject has the "Server" tag
                if (serverObject != null)
                {
                    ServerLogics logics = serverObject.GetComponent<ServerLogics>();
                    if (logics != null)
                    {
                        // Despawn the current player instance if it exists
                        if (currentPlayerInstance != null)
                        {
                            logics.SpawnRequestServerRpc(0, LocalConnection, currentPlayerInstance);
                        }
                    }
                    else
                    {
                        Debug.LogError("ServerLogics component not found on the GameObject with 'Server' tag!");
                    }
                }
                else
                {
                    Debug.LogError("No GameObject found with the 'Server' tag!");
                }
            }
        }







        private void SetEventSystem()
        {
#if !ENABLE_INPUT_SYSTEM
            if (_eventSystem != null)
                return;

            _eventSystem = FindObjectOfType<EventSystem>();
            if (_eventSystem == null)
                _eventSystem = gameObject.AddComponent<EventSystem>();
#endif
        }

        private void DeselectButtons()
        {
#if !ENABLE_INPUT_SYSTEM
            SetEventSystem();
            _eventSystem?.SetSelectedGameObject(null);
#endif
        }
    }
}
