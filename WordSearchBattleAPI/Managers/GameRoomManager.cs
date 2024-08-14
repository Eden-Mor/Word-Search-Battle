using System.Net.Sockets;
using System.Text.Json;
using System.Text;
using WordSearchBattleAPI.Controllers;
using WordSearchBattleAPI.Database;
using WordSearchBattleAPI.Models;
using System.Net.WebSockets;
using Microsoft.AspNetCore.DataProtection;
using WordSearchBattleAPI.Helper;

namespace WordSearchBattleAPI.Managers
{
    public class GameRoomManager(PlayerInfo masterPlayerInfo, Action<string> removeRoom, IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        private readonly Dictionary<WebSocket, PlayerInfo> sockets = [];

        public async Task CleanupSocketsAsync()
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromSeconds(30), cancellationToken);

                foreach (var socket in sockets.Keys)
                    if (socket.State != WebSocketState.Open)
                        RemoveClient(socket);
            }
        }

        public async Task AddClient(WebSocket socket, PlayerInfo info)
        {
            FindAndReplacePlayerName(info);

            sockets.Add(socket, info);
            ConsoleLog.WriteLine($"Client added to game session {info.RoomCode}...");

            await SendPlayerJoinedData(info);
            await ReadStreamRecursively(socket, info);

            ConsoleLog.WriteLine($"Client stopped reading recursively.");
        }


        private async Task SendDataToClientsAsync(object dataSend)
        {
            byte[] data = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(dataSend));
            List<WebSocket> clientsToRemove = [];

            foreach (var socketItem in sockets)
            {
                var socket = socketItem.Key;
                try
                {
                    if (socket.State != WebSocketState.Open)
                    {
                        clientsToRemove.Add(socket);
                        continue;
                    }

                    await socket.SendAsync(new ArraySegment<byte>(data, 0, data.Length),
                                               WebSocketMessageType.Text,
                                               true,
                                               cancellationToken);
                }
                catch (Exception ex)
                {
                    await socket.CloseAsync(WebSocketCloseStatus.InternalServerError, ex.Message, CancellationToken.None);
                    clientsToRemove.Add(socket);
                }
            }

            foreach (var client in clientsToRemove)
                RemoveClient(client);
        }

        public async Task ReadStreamRecursively(WebSocket socket, PlayerInfo playerInfo)
        {
            try
            {
                WebSocketReceiveResult receiveResult = new(0, WebSocketMessageType.Text, true);

                while (!cancellationToken.IsCancellationRequested && !receiveResult.CloseStatus.HasValue)
                {
                    byte[] buffer = new byte[1024];

                    receiveResult = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);


                    if (socket.State != WebSocketState.Open || receiveResult.Count == 0)
                    {
                        await socket.CloseAsync(WebSocketCloseStatus.InternalServerError, "Closed while trying to send data.", CancellationToken.None);
                        ConsoleLog.WriteLine($"Client disconnected from room {playerInfo.RoomCode}, player {playerInfo.PlayerName}.");
                        break;
                    }

                    var message = Encoding.UTF8.GetString(buffer, 0, receiveResult.Count);

                    var result = JsonSerializer.Deserialize<SessionData>(message);
                    if (result != null)
                        _ = HandleServerReceivedMessageAsync(result, playerInfo);
                }
            }
            catch (Exception ex)
            {
                ConsoleLog.WriteLine($"Error reading from socket: {ex.Message}, room {playerInfo.RoomCode}, player {playerInfo.PlayerName}");
            }
            finally
            {
                await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closed", cancellationToken);
                RemoveClient(socket);
                ConsoleLog.WriteLine($"Closed socket finally, room {playerInfo.RoomCode}, player {playerInfo.PlayerName}.");
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
                    await WordComplete(result, playerInfo);
                    break;
            }
        }

        private void RemoveClient(WebSocket client)
        {
            sockets.Remove(client);

            if (sockets.Count == 0)
                removeRoom?.Invoke(masterPlayerInfo.RoomCode!);
        }


        private void FindAndReplacePlayerName(PlayerInfo info)
        {
            FindAndReplacePlayerName(info, 0);

            void FindAndReplacePlayerName(PlayerInfo info, int dupCount)
            {
                if (dupCount == 0)
                {
                    if (sockets.Any(x => x.Value.PlayerName == info.PlayerName))
                        FindAndReplacePlayerName(info, dupCount + 1);
                    else
                        return;
                }
                else
                {
                    if (sockets.Any(x => x.Value.PlayerName == info.PlayerName + " " + dupCount))
                        FindAndReplacePlayerName(info, dupCount + 1);
                    else
                        info.PlayerName += " " + (dupCount + 1);
                }
            }
        }


        private async Task WordComplete(SessionData result, PlayerInfo playerInfo)
        {
            var data = JsonSerializer.Deserialize<WordItem>(result.Data ?? string.Empty);

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

            ConsoleLog.WriteLine($"Word '{data.Word}' completed by user {data.PlayerName}.");

            playerInfo.WordsCorrect++;
            data.PlayerName = playerInfo.PlayerName;

            //This is where you would check if the word is actually on the grid in that specific location (or above this method).

            WordListItem completedWord = new() { Direction = data.Direction, GameSessionId = gameSession.GameSessionId, Word = data.Word, StartX = data.StartX, StartY = data.StartY };
            gameContext.WordList.Add(completedWord);
            _ = gameContext.SaveChangesAsync();


            await SendOutWordCompleted(data);
        }


        private GameSession? GetGameSession()
        {
            using var scope = serviceProvider.CreateScope();
            GameContext gameContext = scope.ServiceProvider.GetRequiredService<GameContext>();
            return gameContext.GameSessions.Where(x => x.RoomCode == masterPlayerInfo.RoomCode).FirstOrDefault();
        }

        private async Task SendOutWordCompleted(WordItem data)
        {
            SessionData sessionData = new()
            {
                DataType = SocketDataType.WordCompleted,
                Data = JsonSerializer.Serialize(data)
            };

            await SendDataToClientsAsync(sessionData);
        }

        private async Task StartRequestedAsync(PlayerInfo playerInfo)
        {
            if (masterPlayerInfo.PlayerName != playerInfo.PlayerName)
                return;

            ConsoleLog.WriteLine(string.Format("Game started {0} by player {1}.", playerInfo.RoomCode, playerInfo.PlayerName));

            await StartGameAsync();
        }

        private async Task SendPlayerJoinedData(PlayerInfo client)
        {
            PlayerJoinedInfo playerJoinedInfo = new()
            {
                IsJoined = true,
                PlayerCount = sockets.Count,
                PlayerName = client.PlayerName,
            };

            SessionData sessionData = new()
            {
                DataType = SocketDataType.PlayerJoined,
                Data = JsonSerializer.Serialize(playerJoinedInfo)
            };

            await SendDataToClientsAsync(sessionData);
        }

        public async Task StartGameAsync()
        {
            try
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

                await SendDataToClientsAsync(sessionData);

                ConsoleLog.WriteLine($"Game session {masterPlayerInfo.RoomCode} started for all players.");
            }
            catch (Exception ex)
            {
                ConsoleLog.WriteLine($"Start Game session Error: {ex.Message}.");
            }
        }
    }
}