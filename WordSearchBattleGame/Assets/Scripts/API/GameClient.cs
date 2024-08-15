using WordSearchBattleShared.Models;
using System;
using System.Text;
using UnityEngine;
using WebSocketSharp;
using System.Collections;
using PimDeWitte.UnityMainThreadDispatcher;

namespace WordSearchBattleShared.API
{
    public class GameClient : MonoBehaviour
    {
        private WebSocket socket;
        private readonly Uri _serverUri = new("wss://wordsearchbattle.api.edenmor.com/ws");
        //private readonly Uri _serverUri = new("ws://194.164.203.182:2943/ws");
        //private readonly Uri _serverUri = new("wss://localhost:7232/ws");
        public Action<string> OnGameStart;
        public Action<PlayerJoinedInfo> OnPlayerJoined;
        public Action<WordItem> OnWordComplete;
        public PlayerJoinInfo playerJoinInfo = new();

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

            socket.OnMessage += (sender, e) =>
            {
                try
                {
                    UnityMainThreadDispatcher.Instance().Enqueue(ReadSocketData(e.Data));
                }
                catch (Exception ex)
                {

                }
            };

            socket.OnOpen += (sender, e) =>
            {
                Debug.Log("WebSocket connection opened.");
            };

            socket.OnClose += (sender, e) =>
            {
                Debug.Log("WebSocket connection closed.");
            };

            socket.Connect();
        }

        private void Disconnect()
        {
            try
            {
                if (socket != null && socket.ReadyState == WebSocketState.Open)
                    socket.CloseAsync(CloseStatusCode.Normal, "Left room.");
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        private IEnumerator SendData(string dataString)
        {
            try
            {
                if (socket.ReadyState != WebSocketState.Open)
                {
                    Disconnect();
                    yield break;
                }

                byte[] data = Encoding.UTF8.GetBytes(dataString);
                socket.Send(data); //may use async to do an "On complete"
            }
            catch (Exception ex)
            {
                Debug.LogError($"Send data error: {ex.Message}");
            }
        }

        private IEnumerator ReadSocketData(string data)
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
            yield return null;
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

        public void SendGameStart()
        {
            SessionData sessionData = new()
            {
                DataType = SocketDataType.Start
            };

            var data = JsonUtility.ToJson(sessionData);
            StartCoroutine(SendData(data));
        }

        private void HandleSocketReceivedMessage(SessionData message)
        {
            switch (message.DataType)
            {
                case SocketDataType.Start:
                    ReceiveGameStart(message);
                    break;

                case SocketDataType.End:
                case SocketDataType.Error:
                    Debug.Log("Game " + message.DataType.ToString() + "ed!");
                    Disconnect();
                    break;

                case SocketDataType.WordCompleted:
                    ReceivedWordCompleted(message);
                    break;

                case SocketDataType.PlayerJoined:
                    ReceivedPlayerJoined(message);
                    break;
            }
        }

        private void ReceiveGameStart(SessionData message)
        {
            OnGameStart?.Invoke(message.Data);
        }

        private void ReceivedWordCompleted(SessionData message)
        {
            var result = JsonUtility.FromJson<WordItem>(message.Data);
            OnWordComplete?.Invoke(result);
        }

        private void ReceivedPlayerJoined(SessionData message)
        {
            var result = JsonUtility.FromJson<PlayerJoinedInfo>(message.Data);
            OnPlayerJoined?.Invoke(result);
        }
    }

}
