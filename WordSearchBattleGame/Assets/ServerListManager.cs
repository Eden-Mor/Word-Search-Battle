using Assets.Scripts.API;
using Assets.Scripts.GameData;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;

public class ServerListManager : MonoBehaviour
{
    public GameDataObject gameData;
    public GameApiService apiService;

    private Transform m_container;
    private Transform container => m_container ??= this.transform.GetChild(0);


    void OnEnable()
        => apiService.PopulatePublicServerList();

    public void SetServerList(bool resultOk)
    {
        foreach (Transform child in container)
            Destroy(child.gameObject);


        if (gameData.ServerList.Count == 0)
        {
            GameObject newTextObject = new("ServerText");
            newTextObject.transform.SetParent(container.transform, false);
            TextMeshProUGUI textComponent = newTextObject.AddComponent<TextMeshProUGUI>();
            textComponent.text = resultOk ? "No servers available." : "Connection could not be made.";
            textComponent.alignment = TextAlignmentOptions.Center;
            return;
        }
     
        foreach (var keyPair in gameData.ServerList)
        {
            GameObject newTextObject = new("ServerText");
            newTextObject.transform.SetParent(container.transform, false);
            TextMeshProUGUI textComponent = newTextObject.AddComponent<TextMeshProUGUI>();
            textComponent.text = keyPair.Key;
        }
    }
}
