using Assets.Scripts.API;
using Assets.Scripts.GameData;
using TMPro;
using UnityEngine;
using WordSearchBattleShared.API;

public class ServerListManager : MonoBehaviour
{
    public GameClient gameClient;
    public GameDataObject gameData;
    public GameApiService apiService;
    public RoomSelectorController roomSelectPrefab;

    private Transform m_container;
    private Transform container => m_container ??= this.transform.GetChild(0);

    void OnEnable()
    {
        apiService.PopulatePublicServerList();
    }

    public void OnJoinClicked(string roomCode)
    {
        gameData.SetRoomCode(roomCode);
        gameClient.JoinRoomCode();
    }


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
            RoomSelectorController controller = Instantiate(roomSelectPrefab, container.transform);
            controller.Initialize(keyPair.Key, keyPair.Value, 6, OnJoinClicked);
        }
    }
}
