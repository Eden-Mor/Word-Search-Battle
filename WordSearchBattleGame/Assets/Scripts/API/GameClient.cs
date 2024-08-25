﻿using WordSearchBattleShared.Models;
using System;
using System.Text;
using UnityEngine;
using NativeWebSocket;
using System.Collections;
using System.Linq;
using System.Drawing;
using Assets.Helpers;

namespace WordSearchBattleShared.API
{
    public class GameClient : MonoBehaviour
    {
        private WebSocket socket;
        private readonly Uri _serverUri = new("wss://wordsearchbattle.api.edenmor.com/ws");
        private readonly Uri _localServerUri = new("wss://localhost:7232/ws");
        public Action<GameStartItem> OnGameStart;
        public Action<PlayerJoinedInfo> OnPlayerJoined;
        public Action<WordItem> OnWordComplete;
        public Action<ColorPickerItem> OnColorPicked; 
        public Action OnGameComplete;
        public JoinRequestInfo playerJoinInfo = new();
        public PlayerInfo PlayerDetails = new();

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

        public void ConnectToServer(bool local = false)
        {
            try
            {
                Disconnect();
                SetUpSocket(local);
                SendJoinRequest();
            }
            catch (Exception ex)
            {
                Debug.LogError($"Connection error: {ex.Message}");
            }
        }

        private void SetUpSocket(bool local)
        {
            socket = new((local ? _localServerUri : _serverUri).ToString());

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
            };

            socket.OnClose += (e) =>
            {
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

        public void SendColorPickRequest(KnownColor color)
        {
            SessionData sessionData = new()
            {
                DataType = SocketDataType.ColorChanged,
                Data = color.ToIntString()
            };

            var data = JsonUtility.ToJson(sessionData);
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
                    ReceivedGameComplete(message.Data);
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

                case SocketDataType.ColorChanged:
                    ReceivedColorChanged(message.Data);
                    break;

                case SocketDataType.PlayerDetails:
                    ReceivedPlayerDetails(message.Data);
                    break;
            }
        }

        private void ReceivedGameComplete(string data)
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

            OnGameComplete?.Invoke();
        }

        private void ReceivedPlayerDetails(string data)
            => this.PlayerDetails = JsonUtility.FromJson<PlayerInfo>(data);

        private void ReceivedColorChanged(string data)
            => OnColorPicked?.Invoke(JsonUtility.FromJson<ColorPickerItem>(data));

        private void ReceiveGameStart(string data)
            => OnGameStart?.Invoke(JsonUtility.FromJson<GameStartItem>(data));

        private void ReceivedWordCompleted(string data)
            => OnWordComplete?.Invoke(JsonUtility.FromJson<WordItem>(data));

        private void ReceivedPlayerJoined(string data)
            => OnPlayerJoined?.Invoke(JsonUtility.FromJson<PlayerJoinedInfo>(data));
    }

}
