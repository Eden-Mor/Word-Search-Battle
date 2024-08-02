using System.Net.Sockets;
using System.Net;
using System.Text;
using WordSearchBattleAPI.Models;
using WordSearchBattleShared.Global;
using WordSearchBattleAPI.Controllers;
using System.Text.Json;
using WordSearchBattleAPI.Database;
using WordSearchBattleShared.Enums;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace WordSearchBattleAPI.Managers
{
    public class GameServerManager
    {
        private readonly TcpListener listener;
        private Dictionary<string, Tuple<GameSessionTCP, CancellationTokenSource>> gameSessions = [];
        private readonly IServiceProvider _serviceProvider;

        public GameServerManager(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;


            listener = new TcpListener(IPAddress.Any, Constants.Port);
            listener.Start();
            _ = AcceptClientsAsync();
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

            var info = JsonSerializer.Deserialize<PlayerInfo>(Encoding.UTF8.GetString(buffer, 0, bytesRead));

            if (info != null && !string.IsNullOrEmpty(info.RoomCode))
            {
                //Check if room exists, 
                if (!gameSessions.ContainsKey(info.RoomCode))
                {
                    using var scope = _serviceProvider.CreateScope();
                    GameContext _gameContext = scope.ServiceProvider.GetRequiredService<GameContext>();
                    _gameContext.GameSessions.Add(new GameSession(GameSessionStatus.WaitingForPlayers, info.RoomCode));
                    await _gameContext.SaveChangesAsync();


                    var cancelTokenSource = new CancellationTokenSource();
                    gameSessions[info.RoomCode] = new(new GameSessionTCP(info, RemoveRoom, cancelTokenSource.Token, _serviceProvider), cancelTokenSource);
                }

                gameSessions[info.RoomCode].Item1.AddClient(client, info);
            }
            else
            {
                Console.WriteLine("Invalid room code.");
                client.Close();
            }
        }


        private void RemoveRoom(string roomCode)
        {
            gameSessions[roomCode].Item2.Cancel();
            gameSessions.Remove(roomCode);
        }
    }


    //each instance is a new room, CLIENTS contains each instance of a player.
    public class GameSessionTCP(PlayerInfo masterPlayerInfo, Action<string> removeRoom, CancellationToken cancellationToken, IServiceProvider serviceProvider)
    {
        private readonly List<Tuple<TcpClient, PlayerInfo>> clients = [];
        
        public void AddClient(TcpClient client, PlayerInfo info)
        {
            FindAndReplacePlayerName(info);

            clients.Add(new(client, info));
            Console.WriteLine($"Client added to game session {info.RoomCode}...");

            SendPlayerJoinedData(info);

            _ = ReadStreamRecursively(client, info);
        }

        private void FindAndReplacePlayerName(PlayerInfo info)
        {
            FindAndReplacePlayerName(info, 0);

            void FindAndReplacePlayerName(PlayerInfo info, int dupCount)
            {
                if (dupCount == 0)
                {
                    if (clients.Any(x => x.Item2.PlayerName == info.PlayerName))
                        FindAndReplacePlayerName(info, dupCount + 1);
                    else
                        return;
                }
                else
                {
                    if (clients.Any(x => x.Item2.PlayerName == info.PlayerName + " " + dupCount))
                        FindAndReplacePlayerName(info, dupCount + 1);
                    else
                        info.PlayerName += " " + (dupCount + 1);
                }
            }
        }

        private async Task ReadStreamRecursively(TcpClient client, PlayerInfo playerInfo)
        {
            byte[] buffer = new byte[1024];
            var clientStream = client.GetStream();

            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    if (!clientStream.DataAvailable)
                    {
                        await Task.Delay(100); // Adjust delay as necessary
                        continue;
                    }

                    int bytesRead = await clientStream.ReadAsync(buffer, cancellationToken);

                    if (bytesRead == 0)
                    {
                        Console.WriteLine("Client disconnected.");
                        break;
                    }

                    string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    var result = JsonSerializer.Deserialize<SessionData>(message);

                    if (result != null)
                        await HandleServerReceivedMessageAsync(result, playerInfo);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading from clientStream: {ex.Message}");
            }
            finally
            {
                if (clientStream != null)
                {
                    clientStream.Close();
                    clientStream.Dispose();
                }
            }
        }

        private async Task HandleServerReceivedMessageAsync(SessionData? result, PlayerInfo playerInfo)
        {
            if (result == null)
                return;

            switch (result.DataType)
            {
                case SocketDataType.Start:
                    await StartRequestedAsync(playerInfo);
                    break;
                case SocketDataType.WordCompleted:
                    WordComplete(result, playerInfo);
                    break;
            }
        }

        private void WordComplete(SessionData result, PlayerInfo playerInfo)
        {
            var data = JsonSerializer.Deserialize<WordItem>(result.Data);

            if (data == null)
                return;

            var gameSession = GetGameSession();

            if (gameSession == null)
                return;

            if (!gameSession.WordList?.Any(x => x == data.Word) ?? false)
                return;

            using var scope = serviceProvider.CreateScope();

            GameContext gameContext = scope.ServiceProvider.GetRequiredService<GameContext>();

            var word = gameContext.WordList.Where(x => x.GameSessionId == gameSession.GameSessionId).Where(x => x.StartX == data.StartX && x.StartY == data.StartY && x.Direction == data.Direction).FirstOrDefault();
            if (word != null)
                return;

            playerInfo.WordsCorrect++;
            data.PlayerName = playerInfo.PlayerName;

            //This is where you would check if the word is actually on the grid in that specific location (or above this method).

            WordListItem completedWord = new() { Direction = data.Direction, GameSessionId = gameSession.GameSessionId, Word = data.Word, StartX = data.StartX, StartY = data.StartY };
            gameContext.WordList.Add(completedWord);
            _ = gameContext.SaveChangesAsync();


            SendOutWordCompleted(data);
        }


        private GameSession? GetGameSession()
        {
            using var scope = serviceProvider.CreateScope();
            GameContext gameContext = scope.ServiceProvider.GetRequiredService<GameContext>();
            return gameContext.GameSessions.Where(x => x.RoomCode == masterPlayerInfo.RoomCode).FirstOrDefault();
        }

        private void SendOutWordCompleted(WordItem data)
        {
            SessionData sessionData = new()
            {
                DataType = SocketDataType.WordCompleted,
                Data = JsonSerializer.Serialize(data)
            };

            SendDataToClients(sessionData);
        }

        private async Task StartRequestedAsync(PlayerInfo playerInfo)
        {
            if (masterPlayerInfo.PlayerName != playerInfo.PlayerName)
                return;

            await StartGameAsync();
        }

        private void SendPlayerJoinedData(PlayerInfo client)
        {
            PlayerJoinedInfo playerJoinedInfo = new()
            {
                IsJoined = true,
                PlayerCount = clients.Count,
                PlayerName = client.PlayerName,
            };

            SessionData sessionData = new()
            {
                DataType = SocketDataType.PlayerJoined,
                Data = JsonSerializer.Serialize(playerJoinedInfo)
            };

            SendDataToClients(sessionData);
        }

        public async Task StartGameAsync()
        {
            var gameSession = GetGameSession();
            if (gameSession == null)
                return;


            Tuple<string[], char[,]> tuple = WordSearchController.SetupGame();
            var gameData = new Tuple<string[], string>(tuple.Item1, WordSearchController.ConvertCharArrayToStringGrid(tuple.Item2));

            gameSession.WordList = gameData.Item1;
            gameSession.LetterGrid = gameData.Item2;

            using var scope = serviceProvider.CreateScope();
            GameContext gameContext = scope.ServiceProvider.GetRequiredService<GameContext>();

            await gameContext.SaveChangesAsync();

            SessionData sessionData = new()
            {
                DataType = SocketDataType.Start,
                Data = JsonSerializer.Serialize(gameData)
            };

            SendDataToClients(sessionData);

            Console.WriteLine($"Game session {masterPlayerInfo.RoomCode} started for all players.");
        }


        private void SendDataToClients(object dataSend)
        {
            byte[] data = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(dataSend));

            List<Tuple<TcpClient, PlayerInfo>> clientsToRemove = [];

            foreach (var client in clients)
            {
                try
                {
                    NetworkStream stream = client.Item1.GetStream();
                    stream.Write(data, 0, data.Length);
                }
                catch (Exception)
                {
                    client.Item1.Close();
                    client.Item1.Dispose();
                    clientsToRemove.Add(client);
                }
            }

            foreach (var client in clientsToRemove)
                clients.Remove(client);
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
        public DirectionEnum Direction { get; set; }
        public string? PlayerName { get; set; }
    }

    public class PlayerJoinedInfo
    {
        public bool IsJoined { get; set; }
        public int PlayerCount { get; set; }
        public string? PlayerName { get; set; }
    }

    public class PlayerInfo
    {
        public int WordsCorrect { get; set; } = 0;
        public string? PlayerName { get; set; }
        public string? RoomCode { get; set; }
    }

    public enum SocketDataType
    {
        Error = 0,
        Start = 1,
        End = 2,
        WordCompleted = 3,
        PlayerJoined = 4
    }
}
