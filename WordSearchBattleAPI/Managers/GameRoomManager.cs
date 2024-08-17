using System.Text.Json;
using System.Text;
using WordSearchBattleAPI.Controllers;
using WordSearchBattleAPI.Database;
using WordSearchBattleAPI.Models;
using System.Net.WebSockets;
using WordSearchBattleAPI.Helper;
using System.Collections.Concurrent;
using System.Drawing;
using WordSearchBattleShared.Enums;
using WordSearchBattleAPI.Algorithm;

namespace WordSearchBattleAPI.Managers
{
    public class GameRoomManager(JoinRequestInfo masterPlayerInfo, Func<string, Task> removeRoom, IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        private readonly ConcurrentDictionary<WebSocket, PlayerResultInfo> sockets = [];
        private SemaphoreSlim _pickColorSemaphor = new(1, 1);
        private SemaphoreSlim _wordCompleteSemaphor = new(1, 1);

        #region Startup


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


        private void RemoveClient(WebSocket client)
        {
            sockets.TryRemove(client, out _);

            if (sockets.Count == 0)
                _ = removeRoom?.Invoke(masterPlayerInfo.RoomCode!);
        }


        public async Task AddClient(WebSocket socket, PlayerResultInfo info)
        {
            FindAndReplacePlayerName(info);

            if (!sockets.TryAdd(socket, info))
            {
                ConsoleLog.WriteLine($"Socket was not added to game session {masterPlayerInfo.RoomCode}.");
                return;
            }

            ConsoleLog.WriteLine($"Socket added to game session {masterPlayerInfo.RoomCode}.");

            await SendPlayerJoinedData(info);
            await ReadStreamRecursively(socket, info);
        }


        private void FindAndReplacePlayerName(PlayerResultInfo info)
        {
            FindAndReplacePlayerName(info, 0);

            void FindAndReplacePlayerName(PlayerResultInfo info, int dupCount)
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
        #endregion

        #region Socket Read/Write
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


        public async Task ReadStreamRecursively(WebSocket socket, PlayerResultInfo playerInfo)
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
                        ConsoleLog.WriteLine($"Socket disconnected from room {masterPlayerInfo.RoomCode}, player {playerInfo.PlayerName}.");
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
                ConsoleLog.WriteLine($"Error reading from socket: {ex.Message}, room {masterPlayerInfo.RoomCode}, player {playerInfo.PlayerName}");
            }
            finally
            {
                await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closed", cancellationToken);
                RemoveClient(socket);
                ConsoleLog.WriteLine($"Closed socket. Room {masterPlayerInfo.RoomCode}, Player {playerInfo.PlayerName}.");
            }
        }
        #endregion


        private async Task HandleServerReceivedMessageAsync(SessionData? result, PlayerResultInfo playerInfo)
        {
            if (result == null)
                return;

            switch (result.DataType)
            {
                case SocketDataType.Start:
                    await StartRequestedAsync(result.Data, playerInfo);
                    break;
                case SocketDataType.WordCompleted:
                    await WordCompleteAsync(result.Data, playerInfo);
                    break;
                case SocketDataType.ColorChanged:
                    await PickedColorAsync(result.Data, playerInfo);
                    break;
            }
        }


        private async Task PickedColorAsync(string? data, PlayerResultInfo playerInfo)
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


        private async Task WordCompleteAsync(string? data, PlayerResultInfo playerInfo)
        {
            await _wordCompleteSemaphor.WaitAsync(cancellationToken);
            try
            {
                var wordItem = JsonSerializer.Deserialize<WordItem>(data ?? string.Empty);
                if (wordItem == null)
                    return;

                using var scope = serviceProvider.CreateScope();
                var gameContext = scope.ServiceProvider.GetRequiredService<GameContext>();

                var gameSession = gameContext.GameSessions.FirstOrDefault(x => x.RoomCode == masterPlayerInfo.RoomCode);
                if (gameSession == null || (!gameSession.WordList?.Any(x => x == wordItem.Word) ?? false))
                    return;

                var wordLocationWasFound = gameContext.WordList.Any(x => x.GameSessionId == gameSession.GameSessionId &&
                                                                         x.StartX == wordItem.StartX &&
                                                                         x.StartY == wordItem.StartY &&
                                                                         x.Direction == wordItem.Direction);
                if (wordLocationWasFound)
                    return;

                ConsoleLog.WriteLine($"Word '{wordItem.Word}' completed by user {wordItem.PlayerName}.");

                playerInfo.WordsCorrect++;
                wordItem.PlayerName = playerInfo.PlayerName;
                wordItem.Color = playerInfo.ColorEnum;

                //This is where you would check if the wordLocationWasFound is actually on the grid in that specific location (or above this method).

                WordListItem completedWord = new()
                {
                    Direction = wordItem.Direction,
                    GameSessionId = gameSession.GameSessionId,
                    Word = wordItem.Word,
                    StartX = wordItem.StartX,
                    StartY = wordItem.StartY
                };

                gameContext.WordList.Add(completedWord);
                await gameContext.SaveChangesAsync();

                await SendOutWordCompleted(wordItem);
            }
            finally
            {
                _wordCompleteSemaphor.Release();
            }
        }


        private async Task SendOutWordCompleted(WordItem data)
            => await SendDataToClientsAsync(SocketDataType.WordCompleted, data);


        private async Task StartRequestedAsync(string? data, PlayerResultInfo playerInfo)
        {
            if (masterPlayerInfo.PlayerName != playerInfo.PlayerName)
                return;

            var gameSettings = JsonSerializer.Deserialize<GameSettingsItem>(data ?? string.Empty);
            if (gameSettings == null)
                return;

            ConsoleLog.WriteLine($"Game started {masterPlayerInfo.RoomCode} by player {playerInfo.PlayerName} with settings {gameSettings}.");

            await StartGameAsync(gameSettings);
        }


        private async Task SendPlayerJoinedData(PlayerResultInfo client)
        {
            PlayerJoinedInfo playerJoinedInfo = new()
            {
                IsJoined = true,
                PlayerCount = sockets.Count,
                PlayerName = client.PlayerName,
            };

            await SendDataToClientsAsync(SocketDataType.PlayerJoined, playerJoinedInfo);
        }


        private void AssignRandomPlayerColors()
        {
            var playersWithNoColor = sockets.Where(x => x.Value.ColorEnum == KnownColor.Transparent).ToList();

            foreach (var player in playersWithNoColor)
            {
                while (true)
                {
                    var randomColor = Random.Shared.Next((int)KnownColor.AliceBlue, (int)KnownColor.RebeccaPurple);
                    if (sockets.Any(x => x.Value.ColorEnum != KnownColor.Transparent && (int)x.Value.ColorEnum == randomColor))
                        continue;

                    player.Value.ColorEnum = (KnownColor)randomColor;
                    break;
                }
            }
        }


        public async Task StartGameAsync(GameSettingsItem gameSettings)
        {
            try
            {
                using var scope = serviceProvider.CreateScope();
                var gameContext = scope.ServiceProvider.GetRequiredService<GameContext>();

                var gameSession = gameContext.GameSessions.FirstOrDefault(x => x.RoomCode == masterPlayerInfo.RoomCode);
                if (gameSession == null || gameSession.GameSessionStatusCode != GameSessionStatus.WaitingForPlayers)
                    return;

                AssignRandomPlayerColors();

                var themes = WordSearch.GetThemes();
                if (!themes.Any(x => x.Equals(gameSettings.Theme, StringComparison.InvariantCultureIgnoreCase)))
                    gameSettings.Theme = themes[Random.Shared.Next(themes.Count)];

                if (gameSettings.WordCount <= 0)
                    gameSettings.WordCount = 10;

                WordSearch wordSearch = new();
                wordSearch.HandleSetupWords(gameSettings.Theme, gameSettings.WordCount);
                wordSearch.HandleSetupGrid();

                gameSession.WordList = [.. wordSearch.Words];
                gameSession.LetterGrid = WordSearchController.ConvertCharArrayToStringGrid(wordSearch.Grid);


                //The item we send out to the players
                var gameData = new GameStartItem
                {
                    WordList = gameSession.WordList,
                    LetterGrid = gameSession.LetterGrid,
                    PlayerList = sockets.Select(x => (PlayerInfo)x.Value).ToList(),
                };

                gameSession.GameSessionStatusCode = GameSessionStatus.InProgress;

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