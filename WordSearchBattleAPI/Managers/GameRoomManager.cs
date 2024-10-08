﻿using System.Text.Json;
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
    public class GameRoomManager(JoinRequestInfo masterPlayerInfo, Func<string, CancellationToken, Task> removeRoomAsync, IServiceProvider serviceProvider)
    {
        private readonly ConcurrentDictionary<WebSocket, PlayerResultInfo> usersDictionary = [];
        private SemaphoreSlim pickColorSemaphor = new(1, 1);
        private SemaphoreSlim wordCompleteSemaphor = new(1, 1);
        private CancellationTokenSource gameFinishedCancellationTokenSource = new();
        private int playerIdCounter = 0;

        #region Startup

        public void Initialize(CancellationToken token)
        {
            _ = CleanupSocketsAsync(token);
        }

        public async Task CleanupSocketsAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromSeconds(30), token);
                CheckAndRemoveClients(false, token);
            }

            //Do not pass in cancellation token, we want to force remove the clients, the original request has been canceled.
            CheckAndRemoveClients(true, CancellationToken.None);
        }

        private void CheckAndRemoveClients(bool forceClose, CancellationToken token)
        {
            foreach (var socket in usersDictionary.Keys)
            {
                if (token.IsCancellationRequested)
                    return;

                if (forceClose || (socket.State != WebSocketState.Open && socket.State != WebSocketState.Connecting))
                    RemoveClient(socket);
            }
        }


        private void RemoveClient(WebSocket client)
        {
            var userInfo = usersDictionary[client];
            usersDictionary.TryRemove(client, out _);

            if (usersDictionary.IsEmpty)
            {
                //We do not want this to get cancelled
                _ = removeRoomAsync?.Invoke(masterPlayerInfo.RoomCode!, CancellationToken.None);
                return;
            }

            if (userInfo.PlayerName == masterPlayerInfo.PlayerName)
                masterPlayerInfo.PlayerName = usersDictionary.FirstOrDefault().Value.PlayerName;

            _ = SendOutPlayerLeftAsync(userInfo, CancellationToken.None);
        }


        public async Task AddClientAsync(WebSocket socket, PlayerResultInfo info, CancellationToken token)
        {
            info.PlayerId = GetNextPlayerId();

            FindAndReplacePlayerName(info);

            //Wait until we are fully connected before sending data to prevent race conditions
            while (socket.State == WebSocketState.Connecting)
                await Task.Delay(100, token);

            if (!usersDictionary.TryAdd(socket, info))
            {
                ConsoleLog.WriteLine($"Socket was not added to game session {masterPlayerInfo.RoomCode}.");
                return;
            }

            ConsoleLog.WriteLine($"Socket added to game session {masterPlayerInfo.RoomCode}.");

            await SendPlayerJoinedDataAsync(info, token);
            await SendPlayerDetails(info, socket, token);
            await ReadStreamRecursivelyAsync(socket, info, token);
        }

        private void FindAndReplacePlayerName(PlayerResultInfo info)
        {
            FindAndReplacePlayerName(info, 0);

            void FindAndReplacePlayerName(PlayerResultInfo info, int dupCount)
            {
                if (dupCount == 0)
                {
                    if (usersDictionary.Any(x => x.Value.PlayerName == info.PlayerName))
                        FindAndReplacePlayerName(info, dupCount + 1);
                    else
                        return;
                }
                else
                {
                    if (usersDictionary.Any(x => x.Value.PlayerName == info.PlayerName + " " + dupCount))
                        FindAndReplacePlayerName(info, dupCount + 1);
                    else
                        info.PlayerName += " " + (dupCount + 1);
                }
            }
        }
        #endregion

        public int GetNextPlayerId()
        {
            playerIdCounter++;
            return playerIdCounter - 1;
        }

        #region Socket Read/Write
        private async Task SendDataToUserAsync(SocketDataType dataType, object dataToSend, WebSocket socket, CancellationToken token)
            => await SendDataToSocketsAsync(dataType, dataToSend, [socket], token);

        private async Task SendDataToUsersAsync(SocketDataType dataType, object dataToSend, CancellationToken token)
            => await SendDataToSocketsAsync(dataType, dataToSend, usersDictionary.Select(x => x.Key), token);

        private async Task SendDataToSocketsAsync(SocketDataType dataType, object dataToSend, IEnumerable<WebSocket> sockets, CancellationToken token)
        {
            SessionData sessionData = new()
            {
                DataType = dataType,
                Data = JsonSerializer.Serialize(dataToSend)
            };

            byte[] data = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(sessionData));
            List<WebSocket> clientsToRemove = [];

            foreach (var socket in sockets)
            {
                try
                {
                    //State is never 'Connecting' here, we previously await for it to be open.
                    if (socket.State != WebSocketState.Open)
                    {
                        clientsToRemove.Add(socket);
                        continue;
                    }

                    await socket.SendAsync(new ArraySegment<byte>(data, 0, data.Length),
                                               WebSocketMessageType.Text,
                                               true,
                                               token);
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


        public async Task ReadStreamRecursivelyAsync(WebSocket socket, PlayerResultInfo playerInfo, CancellationToken token)
        {
            try
            {
                WebSocketReceiveResult receiveResult = new(0, WebSocketMessageType.Text, true);

                while (!token.IsCancellationRequested && !receiveResult.CloseStatus.HasValue)
                {
                    byte[] buffer = new byte[1024];

                    receiveResult = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), token);


                    if (socket.State != WebSocketState.Open || receiveResult.Count == 0)
                    {
                        ConsoleLog.WriteLine($"Socket disconnected from room {masterPlayerInfo.RoomCode}, player {playerInfo.PlayerName}.");
                        break;
                    }

                    var message = Encoding.UTF8.GetString(buffer, 0, receiveResult.Count);

                    var result = JsonSerializer.Deserialize<SessionData>(message);
                    if (result != null)
                        _ = HandleServerReceivedMessageAsync(result, playerInfo, token);
                }
            }
            catch (Exception ex)
            {
                ConsoleLog.WriteLine($"Error reading from socket: {ex.Message}, room {masterPlayerInfo.RoomCode}, player {playerInfo.PlayerName}");
            }
            finally
            {
                await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closed", CancellationToken.None);
                RemoveClient(socket);
                ConsoleLog.WriteLine($"Closed socket. Room {masterPlayerInfo.RoomCode}, Player {playerInfo.PlayerName}.");
            }
        }
        #endregion


        private async Task HandleServerReceivedMessageAsync(SessionData? result, PlayerResultInfo playerInfo, CancellationToken token)
        {
            ConsoleLog.WriteLine($"Request made in room {masterPlayerInfo.RoomCode} by player {playerInfo.PlayerName}, {result}.");

            if (result == null)
                return;

            using var scope = serviceProvider.CreateScope();
            var gameContext = scope.ServiceProvider.GetRequiredService<GameContext>();
            var session = gameContext.GameSessions.FirstOrDefault(x => x.RoomCode == masterPlayerInfo.RoomCode) ?? throw new Exception("Session does not exist, how did we get here?.");


            //Block "word complete" request when the game is already over.
            if (session.GameSessionStatusCode == GameSessionStatus.Completed && result.DataType == SocketDataType.WordCompleted)
            {
                ConsoleLog.WriteLine($"Request was denied. Room {masterPlayerInfo.RoomCode}, Session Status {session.GameSessionStatusCode}, Request type {result.DataType}.");
                return;
            }


            switch (result.DataType)
            {
                case SocketDataType.Start:
                    await StartRequestedAsync(result.Data, playerInfo, token);
                    break;
                case SocketDataType.WordCompleted:
                    await WordCompleteAsync(result.Data, playerInfo, token);
                    break;
                case SocketDataType.ColorChanged:
                    await PickedColorAsync(result.Data, playerInfo, token);
                    break;
            }
        }



        //Send the player collection to show counts of word completed and winner.
        private async Task SendOutGameCompletedAsync(CancellationToken token)
            => await SendDataToUsersAsync(SocketDataType.End, new EndDataItem([.. usersDictionary.Values]), token);

        private async Task SendOutWordCompletedAsync(WordItem data, CancellationToken token)
            => await SendDataToUsersAsync(SocketDataType.WordCompleted, data, token);

        private async Task SendPlayerDetails(PlayerInfo info, WebSocket socket, CancellationToken token)
            => await SendDataToUserAsync(SocketDataType.PlayerDetails, info, socket, token);

        private async Task SendOutColorChangedAsync(ColorPickerItem data, CancellationToken token)
            => await SendDataToUsersAsync(SocketDataType.ColorChanged, data, token);

        private async Task SendOutPlayerLeftAsync(PlayerInfo data, CancellationToken token)
            => await SendDataToUsersAsync(SocketDataType.PlayerLeft, data, token);

        private async Task PickedColorAsync(string? data, PlayerResultInfo playerInfo, CancellationToken token)
        {
            await pickColorSemaphor.WaitAsync(token);
            try
            {
                if (data == null || !int.TryParse(data, out var knownColorInt))
                    return;

                var colorEnum = (KnownColor)knownColorInt;
                if (!Enum.IsDefined(typeof(KnownColor), colorEnum))
                    return;

                if (usersDictionary.Any(x => x.Value.ColorEnum == colorEnum))
                    return;

                var currentColor = playerInfo.ColorEnum;
                playerInfo.ColorEnum = colorEnum;

                await SendOutColorChangedAsync(new ColorPickerItem() { OldColor = currentColor, NewColor = colorEnum, PlayerId = playerInfo.PlayerId }, token);
            }
            finally
            {
                pickColorSemaphor.Release();
            }
        }


        private async Task WordCompleteAsync(string? data, PlayerResultInfo playerInfo, CancellationToken token)
        {
            await wordCompleteSemaphor.WaitAsync(token);
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
                                                                         x.Direction == wordItem.Direction &&
                                                                         x.Word == wordItem.Word);
                if (wordLocationWasFound)
                    return;

                ConsoleLog.WriteLine($"Word '{wordItem.Word}' completed by user {wordItem.PlayerId}.");

                playerInfo.WordsCorrect++;
                wordItem.PlayerId = playerInfo.PlayerId;
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
                await gameContext.SaveChangesAsync(token);

                await SendOutWordCompletedAsync(wordItem, token);

                var distinctWordCount = gameContext.WordList
                                            .Where(item => item.GameSessionId == gameSession.GameSessionId)
                                            .Select(item => item.Word)
                                            .Distinct()
                                            .Count();

                //Wait three seconds to allow players to select any dupes if any
                if (gameSession.WordList?.Count == distinctWordCount)
                    _ = WaitAndCallbackAsync(3000, GameFinishedAsync, gameFinishedCancellationTokenSource.Token);
            }
            finally
            {
                wordCompleteSemaphor.Release();
            }
        }

        public async Task WaitAndCallbackAsync(int milliseconds, Func<CancellationToken, Task> asyncMethod, CancellationToken token)
        {
            await Task.Delay(milliseconds, token);
            await asyncMethod(token);
        }

        private async Task GameFinishedAsync(CancellationToken token)
        {
            ConsoleLog.WriteLine($"Game completed. Room: '{masterPlayerInfo.RoomCode}'.");

            using var scope = serviceProvider.CreateScope();
            var gameContext = scope.ServiceProvider.GetRequiredService<GameContext>();

            var gameSession = gameContext.GameSessions.FirstOrDefault(x => x.RoomCode == masterPlayerInfo.RoomCode);
            if (gameSession == null)
                return;

            gameSession.GameSessionStatusCode = GameSessionStatus.Completed;
            await gameContext.SaveChangesAsync(token);

            //Incase they continue to replay in the room, just delete the word data.
            //We can use token here since we delete the word list when we start the room and or the room gets destroyed
            await gameContext.DeleteWordDataAsync(gameSession, token);

            await SendOutGameCompletedAsync(token);
        }


        private async Task StartRequestedAsync(string? data, PlayerResultInfo playerInfo, CancellationToken token)
        {
            //if the game was about to be finished, cancel sending out the finished request.
            gameFinishedCancellationTokenSource.Cancel();
            gameFinishedCancellationTokenSource = new();

            using var scope = serviceProvider.CreateScope();
            var gameContext = scope.ServiceProvider.GetRequiredService<GameContext>();

            var gameSession = gameContext.GameSessions.FirstOrDefault(x => x.RoomCode == masterPlayerInfo.RoomCode);
            if (gameSession == null)
                return;

            //Delete word data if game is a rerun
            await gameContext.DeleteWordDataAsync(gameSession, token);

            if (masterPlayerInfo.PlayerName != playerInfo.PlayerName)
                return;

            var gameSettings = JsonSerializer.Deserialize<GameSettingsItem>(data ?? string.Empty);
            if (gameSettings == null)
                return;

            ConsoleLog.WriteLine($"Game started {masterPlayerInfo.RoomCode} by player {playerInfo.PlayerName} with settings {gameSettings}.");

            await StartGameAsync(gameSettings, token);
        }


        private async Task SendPlayerJoinedDataAsync(PlayerResultInfo client, CancellationToken token)
        {
            PlayerJoinedInfo playerJoinedInfo = new()
            {
                PlayerCount = usersDictionary.Count,
                PlayerName = client.PlayerName,
                PlayerId = client.PlayerId,
            };

            await SendDataToUsersAsync(SocketDataType.PlayerJoined, playerJoinedInfo, token);
        }


        private void AssignRandomPlayerColors()
        {
            var playersWithNoColor = usersDictionary.Where(x => x.Value.ColorEnum == KnownColor.Transparent).ToList();

            foreach (var player in playersWithNoColor)
            {
                while (true)
                {
                    var randomColor = Random.Shared.Next((int)KnownColor.AliceBlue, (int)KnownColor.RebeccaPurple);
                    if (usersDictionary.Any(x => x.Value.ColorEnum != KnownColor.Transparent && (int)x.Value.ColorEnum == randomColor))
                        continue;

                    player.Value.ColorEnum = (KnownColor)randomColor;
                    break;
                }
            }
        }


        public async Task StartGameAsync(GameSettingsItem gameSettings, CancellationToken token)
        {
            try
            {
                ClearPlayerWordCounts();

                using var scope = serviceProvider.CreateScope();
                var gameContext = scope.ServiceProvider.GetRequiredService<GameContext>();

                var gameSession = gameContext.GameSessions.FirstOrDefault(x => x.RoomCode == masterPlayerInfo.RoomCode);
                if (gameSession == null) //|| gameSession.GameSessionStatusCode != GameSessionStatus.WaitingForPlayers)
                    return;

                AssignRandomPlayerColors();

                var themes = WordSearch.GetThemes();
                if (!themes.Any(x => x.Equals(gameSettings.Theme, StringComparison.InvariantCultureIgnoreCase)))
                    gameSettings.Theme = themes[Random.Shared.Next(themes.Count)];

                if (gameSettings.WordCount <= 0)
                    gameSettings.WordCount = 10;

#if DEBUG
                gameSettings.WordCount = 2;
#endif

                WordSearch wordSearch = new();
                wordSearch.HandleSetupWords(gameSettings.Theme, gameSettings.WordCount);
                wordSearch.HandleSetupGrid();

                gameSession.WordList = [.. wordSearch.Words];
                gameSession.LetterGrid = ConvertCharArrayToStringGrid(wordSearch.Grid);


                //The item we send out to the players
                var gameData = new GameStartItem
                {
                    WordList = gameSession.WordList,
                    LetterGrid = gameSession.LetterGrid,
                    PlayerList = usersDictionary.Select(x => (PlayerInfo)x.Value).ToList(),
                };

                gameSession.GameSessionStatusCode = GameSessionStatus.InProgress;

                await gameContext.SaveChangesAsync(token);

                await SendDataToUsersAsync(SocketDataType.Start, gameData, token);

                ConsoleLog.WriteLine($"Game session {masterPlayerInfo.RoomCode} started for all players.");
            }
            catch (Exception ex)
            {
                ConsoleLog.WriteLine($"Start Game session Error: {ex.Message}.");
            }
        }


        private static string ConvertCharArrayToStringGrid(char[,] array)
        {
            int rows = array.GetLength(0);
            int cols = array.GetLength(1);
            StringBuilder sb = new();

            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                    sb.Append(array[i, j]);

                if (i != rows - 1)
                    sb.Append('|');
            }

            return sb.ToString();
        }

        private void ClearPlayerWordCounts()
        {
            foreach (var player in usersDictionary.Values)
                player.WordsCorrect = 0;
        }

        public int GetPlayerCount()
            => usersDictionary.Count;

        public string GetRoomCode()
            => masterPlayerInfo.RoomCode ?? string.Empty;
    }
}