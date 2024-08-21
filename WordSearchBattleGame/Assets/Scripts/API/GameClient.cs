using WordSearchBattleShared.Models;
using System;
using System.Text;
using UnityEngine;
using NativeWebSocket;
using System.Collections;
using System.Linq;

namespace WordSearchBattleShared.API
{
    public class GameClient : MonoBehaviour
    {
        private WebSocket socket;
        //private readonly Uri _serverUri = new("wss://wordsearchbattle.api.edenmor.com/ws");
        //private readonly Uri _serverUri = new("ws://194.164.203.182:2943/ws"); // Does not work on published game, only localhost
        private readonly Uri _serverUri = new("wss://localhost:7232/ws");
        public Action<string> OnGameStart;
        public Action<PlayerJoinedInfo> OnPlayerJoined;
        public Action<WordItem> OnWordComplete;
        public JoinRequestInfo playerJoinInfo = new();

        void Update()
        {
#if !UNITY_WEBGL || UNITY_EDITOR
            socket?.DispatchMessageQueue();
#endif
        }

        private void OnApplicationQuit()
        {
            Disconnect();
        }

        public void ConnectToServer()
        {
            try
            {
                Disconnect();
                SetUpSocket();
                SendJoinRequest();
            }
            catch (Exception ex)
            {
                Debug.LogError($"Connection error: {ex.Message}");
            }
        }

        private void SetUpSocket()
        {

            socket = new(_serverUri.ToString());

            socket.OnMessage += (e) =>
            {
                try
                {
                    var message = System.Text.Encoding.UTF8.GetString(e);
                    ReadSocketData(message);
                    //UnityMainThreadDispatcher.Instance().Enqueue(ReadSocketData(e.Data));
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                }
            };

            socket.OnOpen += () =>
            {
                Debug.Log("WebSocket connection opened.");
            };

            socket.OnClose += (e) =>
            {
                Debug.Log("WebSocket connection closed.");
            };

            socket.Connect();
        }

        private void Disconnect()
        {
            try
            {
                socket?.Close();
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        private IEnumerator SendData(string dataString)
        {
            if (socket.State == WebSocketState.Connecting)
                yield return new WaitUntil(() => socket.State != WebSocketState.Connecting);
            

            if (socket.State != WebSocketState.Open)
            {
                Disconnect();
                yield break;
            }

            try
            {
                byte[] data = Encoding.UTF8.GetBytes(dataString);
                socket.Send(data); //may use async to do an "On complete"
            }
            catch (Exception ex)
            {
                Debug.LogError($"Send data error: {ex.Message}");
            }
        }

        private void ReadSocketData(string data)
        {
            try
            {
                var result = JsonUtility.FromJson<SessionData>(data);

                HandleSocketReceivedMessage(result);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Unexpected error: {ex.Message}");
                Disconnect();
            }
        }

        private void SendJoinRequest()
        {
            var data = JsonUtility.ToJson(playerJoinInfo);
            StartCoroutine(SendData(data));
        }

        public void SendWordFound(WordItem wordItem)
        {
            SessionData sessionData = new()
            {
                DataType = SocketDataType.WordCompleted,
                Data = JsonUtility.ToJson(wordItem)
            };

            var data = JsonUtility.ToJson(sessionData);
            StartCoroutine(SendData(data));
        }

        public void SendGameStart(GameSettingsItem gameSettings)
        {
            SessionData sessionData = new()
            {
                DataType = SocketDataType.Start,
                Data = JsonUtility.ToJson(gameSettings)
            };

            var data = JsonUtility.ToJson(sessionData);
            StartCoroutine(SendData(data));
        }

        private void HandleSocketReceivedMessage(SessionData message)
        {
            switch (message.DataType)
            {
                case SocketDataType.Start:
                    ReceiveGameStart(message.Data);
                    break;

                case SocketDataType.End:
                    GameComplete(message.Data);
                    break;

                case SocketDataType.Error:
                    Debug.Log("Game Errored!");
                    Disconnect();
                    break;

                case SocketDataType.WordCompleted:
                    ReceivedWordCompleted(message.Data);
                    break;

                case SocketDataType.PlayerJoined:
                    ReceivedPlayerJoined(message.Data);
                    break;
            }
        }

        private void GameComplete(string data)
        {
            //https://discussions.unity.com/t/how-to-deserialize-json-data-into-list/185912/2

            var result = JsonUtility.FromJson<EndData>(data);
            var mostWordCount = result.PlayerResultList.Max(x => x.WordsCorrect);
            var players = result.PlayerResultList.Where(x => x.WordsCorrect == mostWordCount);

            var winText = "won!";
            if (players.Count() > 1)
                winText = "tied!";

            foreach (var player in players)
                Debug.Log(player.PlayerName + " got " + mostWordCount + " words correct and " + winText);
        }

        private void ReceiveGameStart(string data)
        {
            OnGameStart?.Invoke(data);
        }

        private void ReceivedWordCompleted(string data)
        {
            var result = JsonUtility.FromJson<WordItem>(data);
            OnWordComplete?.Invoke(result);
        }

        private void ReceivedPlayerJoined(string data)
        {
            var result = JsonUtility.FromJson<PlayerJoinedInfo>(data);
            OnPlayerJoined?.Invoke(result);
        }
    }

}
