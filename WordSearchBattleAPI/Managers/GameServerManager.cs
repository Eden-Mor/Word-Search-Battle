using System.Net.Sockets;
using System.Net;
using System.Text;
using WordSearchBattleAPI.Models;
using System.Net.Http;
using WordSearchBattleShared.Global;
using WordSearchBattleAPI.Controllers;
using System.Text.Json;

namespace WordSearchBattleAPI.Managers
{
    public class GameServerManager
    {
        private TcpListener listener;
        private Dictionary<int, GameSessionTCP> gameSessions = new();

        public GameServerManager()
        {
            listener = new TcpListener(IPAddress.Any, Constants.Port);
            listener.Start();
            Console.WriteLine("Server started...");

            _ = AcceptClientsAsync();
        }


        public void StartAllSessions()
        {
            foreach (var session in gameSessions.Values)
                session.StartGame();
        }

        private async Task AcceptClientsAsync()
        {
            while (true)
            {
                TcpClient client = await listener.AcceptTcpClientAsync();
                Console.WriteLine("Client connected...");

                // Handle the client in a separate task
                _ = Task.Run(() => HandleClient(client));
            }
        }

        private async Task HandleClient(TcpClient client)
        {
            NetworkStream stream = client.GetStream();
            byte[] buffer = new byte[256];
            int bytesRead = await stream.ReadAsync(buffer);
            string gameIdString = Encoding.UTF8.GetString(buffer, 0, bytesRead);

            if (int.TryParse(gameIdString, out int gameId))
            {
                if (!gameSessions.ContainsKey(gameId))
                    gameSessions[gameId] = new GameSessionTCP(gameId);

                gameSessions[gameId].AddClient(client);
            }
            else
            {
                Console.WriteLine("Invalid game ID.");
                client.Close();
            }
        }
    }


    public class GameSessionTCP
    {
        private int gameId;
        private readonly List<TcpClient> clients = [];
        private const int maxPlayers = 4;

        public GameSessionTCP(int gameId)
        {
            this.gameId = gameId;
        }

        public void AddClient(TcpClient client)
        {
            clients.Add(client);
            Console.WriteLine($"Client added to game session {gameId}...");
        }

        public void StartGame()
        {

            Tuple<string[], char[,]> tuple = WordSearchController.SetupGame();
            var gameData = new Tuple<string[], string>(tuple.Item1, WordSearchController.ConvertCharArrayToStringGrid(tuple.Item2));

            SessionData sessionData = new()
            {
                DataType = SocketDataType.Start,
                Data = JsonSerializer.Serialize(gameData)
            };

            byte[] data = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(sessionData));

            foreach (var client in clients)
            {
                NetworkStream stream = client.GetStream();
                stream.Write(data, 0, data.Length);
            }

            Console.WriteLine($"Game session {gameId} started for all players.");
        }
    }


    public class SessionData
    {
        public SocketDataType DataType { get; set; }
        public string? Data { get; set; }
    }

    public class WordItem
    {
        public string? Word { get; set; }
        public int StartX { get; set; }
        public int StartY { get; set; }
        public int EndX { get; set; }
        public int EndY { get; set; }
    }

    public enum SocketDataType
    {
        Error = 0,
        Start = 1,
        End = 2,
        WordCompleted = 3
    }
}
