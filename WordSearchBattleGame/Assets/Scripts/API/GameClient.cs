using WordSearchBattleShared.Models;
using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;
using WordSearchBattleShared.Global;
using System.Threading.Tasks;

namespace WordSearchBattleShared.API
{
    public class GameClient : MonoBehaviour
    {
        private TcpClient client;
        private NetworkStream stream;
        private const string serverIp = "127.0.0.1";
        private CancellationTokenSource cancellationTokenSource = new();

        public Action<string> OnGameStart;
        public Action<PlayerJoinedInfo> OnPlayerJoined;
        public Action<WordItem> OnWordComplete;
        public PlayerJoinInfo playerJoinInfo = new();

        public void ConnectToServer()
        {
            Disconnect();
            cancellationTokenSource = new CancellationTokenSource();

            client = new TcpClient();
            client.Connect(serverIp, Constants.Port);
            stream = client.GetStream();
            Debug.Log("Connected to server...");

            SendJoinRequest();
        }

        private void Disconnect()
        {
            cancellationTokenSource.Cancel();
            client?.Close();
        }

        private void SendJoinRequest()
        {
            var data = JsonUtility.ToJson(playerJoinInfo);
            _ = ReadSocketData();
            _ = SendData(data);
        }

        private async Task SendData(string dataString)
        {
            byte[] data = Encoding.UTF8.GetBytes(dataString);
            await stream.WriteAsync(data, 0, data.Length, cancellationTokenSource.Token);
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

        private async Task ReadSocketData()
        {
            int bytesRead;
            string message;
            byte[] data = new byte[4096];
            var token = cancellationTokenSource.Token;

            while (!token.IsCancellationRequested)
            {
                bytesRead = await stream.ReadAsync(data, 0, data.Length, token);
                
                if (bytesRead == 0)
                    Disconnect();

                if (token.IsCancellationRequested)
                    continue;

                message = Encoding.UTF8.GetString(data, 0, bytesRead);

                var result = JsonUtility.FromJson<SessionData>(message);

                HandleSocketReceivedMessage(result);
            }
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
                    PlayerJoined(message); 
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

        private void PlayerJoined(SessionData message)
        {
            var result = JsonUtility.FromJson<PlayerJoinedInfo>(message.Data);
            OnPlayerJoined?.Invoke(result);
        }
    }
}
