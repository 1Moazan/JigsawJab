using System.Collections.Generic;
using Networking;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MainMenu
{
    public class BuildStartup : MonoBehaviour
    {
        [SerializeField] private SharedConnectionData connectionData;
        [SerializeField] private List<GameObject>  clientObjects;

        private void Start()
        {
            SwitchServerScene();
        }
        
        private void SwitchServerScene()
        {
            if (connectionData.CurrentClientType == ClientType.Server)
            {
                SceneManager.LoadScene("Gameplay");
            }
            else
            {
                foreach (GameObject clientObject in clientObjects)
                {
                    clientObject.SetActive(true);
                }
            }
        }
    }
}
