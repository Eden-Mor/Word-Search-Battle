using System.Net.Sockets;
using System.Text.Json;
using System.Text;
using WordSearchBattleAPI.Controllers;
using WordSearchBattleAPI.Database;
using WordSearchBattleAPI.Models;
using System.Net.WebSockets;
using WordSearchBattleAPI.Helper;
using System.Collections.Concurrent;
using System.Drawing;

namespace WordSearchBattleAPI.Managers
{
    public class GameRoomManager(PlayerInfo masterPlayerInfo, Func<string, Task> removeRoom, IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        private readonly ConcurrentDictionary<WebSocket, PlayerInfo> sockets = [];

        public async Task CleanupSocketsAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromSeconds(30), token);

                if (!token.IsCancellationRequested)
                    foreach (var socket in sockets.Keys)
                        if (socket.State != WebSocketState.Open)
                            RemoveClient(socket);
            }
        }

        public async Task AddClient(WebSocket socket, PlayerInfo info)
        {
            FindAndReplacePlayerName(info);

            if (!sockets.TryAdd(socket, info))
            {
                ConsoleLog.WriteLine($"Socket was not added to game session {info.RoomCode}.");
                return;
            }

            ConsoleLog.WriteLine($"Socket added to game session {info.RoomCode}.");

            await SendPlayerJoinedData(info);
            await ReadStreamRecursively(socket, info);
        }


        private async Task SendDataToClientsAsync(SocketDataType dataType, object dataToSend)
        {
            SessionData sessionData = new()
            {
                DataType = dataType,
                Data = JsonSerializer.Serialize(dataToSend)
            };

            byte[] data = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(sessionData));
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
                        ConsoleLog.WriteLine($"Socket disconnected from room {playerInfo.RoomCode}, player {playerInfo.PlayerName}.");
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
                ConsoleLog.WriteLine($"Closed socket. Room {playerInfo.RoomCode}, Player {playerInfo.PlayerName}.");
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
                    await WordCompleteAsync(result.Data, playerInfo);
                    break;
                case SocketDataType.ColorChanged:
                    await PickedColorAsync(result.Data, playerInfo);
                    break;
            }
        }


        private SemaphoreSlim _pickColorSemaphor = new(1, 1);

        private async Task PickedColorAsync(string? data, PlayerInfo playerInfo)
        {
            await _pickColorSemaphor.WaitAsync(cancellationToken);
            try
            {
                if (data == null || !int.TryParse(data, out var knownColorInt)) 
                    return;

                var colorEnum = (KnownColor)knownColorInt;
                if (!Enum.IsDefined(typeof(KnownColor), colorEnum))
                    return;

                if (sockets.Any(x => x.Value.ColorEnum == colorEnum))
                    return;

                var currentColor = playerInfo.ColorEnum;
                playerInfo.ColorEnum = colorEnum;

                await SendOutColorChanged(new ColorPickerItem() { OldColor = currentColor, NewColor = colorEnum });
            }
            finally
            {
                _pickColorSemaphor.Release();
            }
        }


        private async Task SendOutColorChanged(ColorPickerItem data)
            => await SendDataToClientsAsync(SocketDataType.ColorChanged, data);


        private void RemoveClient(WebSocket client)
        {
            sockets.TryRemove(client, out _);

            if (sockets.Count == 0)
                _ = removeRoom?.Invoke(masterPlayerInfo.RoomCode!);
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


        private SemaphoreSlim _wordCompleteSemaphor = new(1, 1);
        private async Task WordCompleteAsync(string? data, PlayerInfo playerInfo)
        {
            await _wordCompleteSemaphor.WaitAsync(cancellationToken);
            try
            {
                var wordItem = JsonSerializer.Deserialize<WordItem>(data ?? string.Empty);

                if (wordItem == null)
                    return;

                var gameSession = GetGameSession();

                if (gameSession == null)
                    return;

                if (!gameSession.WordList?.Any(x => x == wordItem.Word) ?? false)
                    return;

                using var scope = serviceProvider.CreateScope();

                GameContext gameContext = scope.ServiceProvider.GetRequiredService<GameContext>();

                var word = gameContext.WordList.Where(x => x.GameSessionId == gameSession.GameSessionId).Where(x => x.StartX == wordItem.StartX && x.StartY == wordItem.StartY && x.Direction == wordItem.Direction).FirstOrDefault();
                if (word != null)
                    return;

                ConsoleLog.WriteLine($"Word '{wordItem.Word}' completed by user {wordItem.PlayerName}.");

                playerInfo.WordsCorrect++;
                wordItem.PlayerName = playerInfo.PlayerName;
                wordItem.Color = playerInfo.ColorEnum;

                //This is where you would check if the word is actually on the grid in that specific location (or above this method).

                WordListItem completedWord = new() { Direction = wordItem.Direction, GameSessionId = gameSession.GameSessionId, Word = wordItem.Word, StartX = wordItem.StartX, StartY = wordItem.StartY };
                gameContext.WordList.Add(completedWord);
                await gameContext.SaveChangesAsync();

                await SendOutWordCompleted(wordItem);
            }
            finally
            {
                _wordCompleteSemaphor.Release();
            }
        }


        private GameSession? GetGameSession()
        {
            using var scope = serviceProvider.CreateScope();
            GameContext gameContext = scope.ServiceProvider.GetRequiredService<GameContext>();
            return gameContext.GameSessions.Where(x => x.RoomCode == masterPlayerInfo.RoomCode).FirstOrDefault();
        }

        private async Task SendOutWordCompleted(WordItem data)
            => await SendDataToClientsAsync(SocketDataType.WordCompleted, data);
        

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

            await SendDataToClientsAsync(SocketDataType.PlayerJoined, playerJoinedInfo);
        }

        public async Task StartGameAsync()
        {
            try
            {
                var gameSession = GetGameSession();
                if (gameSession == null)
                    return;

                var playersWithNoColor = sockets.Where(x => x.Value.ColorEnum == null).ToList();
                
                foreach (var player in playersWithNoColor)
                {
                    while (true)
                    {
                        var randomColor = Random.Shared.Next((int)KnownColor.AliceBlue, (int)KnownColor.RebeccaPurple);

                        if (sockets.Any(x => x.Value.ColorEnum != null && (int)x.Value.ColorEnum == randomColor))
                            continue;

                        player.Value.ColorEnum = (KnownColor)randomColor;
                        break;
                    }
                }


                Tuple<string[], char[,]> tuple = WordSearchController.SetupGame();
                var gameData = new Tuple<string[], string>(tuple.Item1, WordSearchController.ConvertCharArrayToStringGrid(tuple.Item2));

                gameSession.WordList = gameData.Item1;
                gameSession.LetterGrid = gameData.Item2;

                using var scope = serviceProvider.CreateScope();
                GameContext gameContext = scope.ServiceProvider.GetRequiredService<GameContext>();

                await gameContext.SaveChangesAsync();

                await SendDataToClientsAsync(SocketDataType.Start, gameData);

                ConsoleLog.WriteLine($"Game session {masterPlayerInfo.RoomCode} started for all players.");
            }
            catch (Exception ex)
            {
                ConsoleLog.WriteLine($"Start Game session Error: {ex.Message}.");
            }
        }
    }
}