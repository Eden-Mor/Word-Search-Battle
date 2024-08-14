using WordSearchBattleShared.Models;
using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;
using System.Threading.Tasks;
using System.Net.WebSockets;

namespace WordSearchBattleShared.API
{
    public class GameClient : MonoBehaviour
    {
        private ClientWebSocket socket;
        //private readonly Uri _serverUri = new("ws://194.164.203.182:2943/ws");
        //private readonly Uri _serverUri = new("wss://localhost:7232/ws");
        private readonly Uri _serverUri = new("wss://wordsearchbattle.api.edenmor.com/ws");
        private CancellationTokenSource cancellationTokenSource = new();
        public Action<string> OnGameStart;
        public Action<PlayerJoinedInfo> OnPlayerJoined;
        public Action<WordItem> OnWordComplete;
        public PlayerJoinInfo playerJoinInfo = new();

        private async void OnDestroy()
        {
            try
            {
                if (socket != null)
                    await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closed game", CancellationToken.None);
            }
            catch (Exception)
            {

            }
        }

        public async Task ConnectToServerAsync()
        {
            await DisconnectAsync();
            cancellationTokenSource = new CancellationTokenSource();
            socket = new ClientWebSocket();

            try
            {
                await socket.ConnectAsync(_serverUri, cancellationTokenSource.Token);
                SendJoinRequest();
            }
            catch (Exception ex)
            {
                Debug.LogError($"Connection error: {ex.Message}");
            }
        }

        private async Task DisconnectAsync()
        {
            try
            {
                cancellationTokenSource.Cancel();

                if (socket != null && socket.State == WebSocketState.Open)
                    await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Left room.", CancellationToken.None);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        private async Task SendData(string dataString)
        {
            try
            {
                if (socket.State != WebSocketState.Open)
                {
                    await DisconnectAsync();
                    return;
                }

                byte[] data = Encoding.UTF8.GetBytes(dataString);
                await socket.SendAsync(data, WebSocketMessageType.Text, true, cancellationTokenSource.Token);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Send data error: {ex.Message}");
            }
        }

        private async Task ReadSocketData()
        {
            try
            {
                WebSocketReceiveResult receivedResult;
                string message;
                var token = cancellationTokenSource.Token;

                while (!token.IsCancellationRequested)
                {
                    byte[] data = new byte[1024];
                    receivedResult = await socket.ReceiveAsync(data, token);

                    if (receivedResult.Count == 0)
                    {
                        Debug.Log($"ReceivedResult was 0, disconnecting.");
                        return;
                    }

                    message = Encoding.UTF8.GetString(data, 0, receivedResult.Count);

                    var result = JsonUtility.FromJson<SessionData>(message);

                    await HandleSocketReceivedMessageAsync(result);
                }
            }
            catch (WebSocketException ex)
            {
                Debug.LogError($"WebSocket error: {ex.Message}");
                await DisconnectAsync();
            }
            catch (Exception ex)
            {
                Debug.LogError($"Unexpected error: {ex.Message}");
                await DisconnectAsync();
            }
        }

        private void SendJoinRequest()
        {
            var data = JsonUtility.ToJson(playerJoinInfo);
            _ = ReadSocketData();
            _ = SendData(data);
        }

        public void SendWordFound(WordItem wordItem)
        {
            SessionData sessionData = new()
            {
                DataType = SocketDataType.WordCompleted,
                Data = JsonUtility.ToJson(wordItem)
            };

            var data = JsonUtility.ToJson(sessionData);
            _ = SendData(data);
        }

        public void SendGameStart()
        {
            SessionData sessionData = new()
            {
                DataType = SocketDataType.Start
            };

            var data = JsonUtility.ToJson(sessionData);
            _ = SendData(data);
        }

        private async Task HandleSocketReceivedMessageAsync(SessionData message)
        {
            switch (message.DataType)
            {
                case SocketDataType.Start:
                    ReceiveGameStart(message);
                    break;

                case SocketDataType.End:
                case SocketDataType.Error:
                    Debug.Log("Game " + message.DataType.ToString() + "ed!");
                    await DisconnectAsync();
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
