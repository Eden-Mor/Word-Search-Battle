using Assets.Scripts.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor.VersionControl;
using UnityEngine;

namespace Assets.Scripts.API
{
    public class GameClient : MonoBehaviour
    {
        private TcpClient client;
        private NetworkStream stream;
        private const string serverIp = "127.0.0.1";
        public int gameId = 1;
        private CancellationTokenSource cancellationTokenSource = new();

        public Action<string> OnGameStart;

        public void ConnectToServer()
        {
            Disconnect();
            cancellationTokenSource = new CancellationTokenSource();

            client = new TcpClient();
            client.Connect(serverIp, 12345);
            stream = client.GetStream();
            Debug.Log("Connected to server...");

            SendGameId();
        }

        private void Disconnect()
        {
            cancellationTokenSource.Cancel();
            client?.Close();
        }

        private async void SendGameId()
        {
            byte[] data = Encoding.UTF8.GetBytes(gameId.ToString());
            await stream.WriteAsync(data, 0, data.Length, cancellationTokenSource.Token);

            ReadSocketData();
        }

        private async void ReadSocketData()
        {
            byte[] data = new byte[4096];
            int bytesRead;
            var token = cancellationTokenSource.Token;
            string message = string.Empty;

            while (!token.IsCancellationRequested)
            {
                bytesRead = await stream.ReadAsync(data, 0, data.Length, token);

                if (token.IsCancellationRequested)
                    return;

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
                    GameStart(message);
                    break;

                case SocketDataType.End:
                case SocketDataType.Error:
                    Debug.Log("Game " + message.DataType.ToString() + "ed!");
                    Disconnect();
                    break;

                case SocketDataType.WordCompleted:
                    WordCompleted(message);
                    break;
            }
        }

        private void GameStart(SessionData message)
        {
            Debug.Log("Game started!");
            OnGameStart?.Invoke(message.Data);
        }

        private void WordCompleted(SessionData message)
        {
            var result = JsonUtility.FromJson<WordItem>(message.Data);

            Debug.Log("Word completed: " + result.Word + ".");
        }
    }
}
