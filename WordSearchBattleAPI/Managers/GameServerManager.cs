﻿using System.Text;
using WordSearchBattleAPI.Models;
using System.Text.Json;
using WordSearchBattleAPI.Database;
using WordSearchBattleShared.Enums;
using System.Net.WebSockets;
using WordSearchBattleAPI.Helper;
using System.Collections.Concurrent;

namespace WordSearchBattleAPI.Managers
{
    public class GameServerManager(IServiceProvider serviceProvider)
    {
        private ConcurrentDictionary<string, Tuple<GameRoomManager, CancellationTokenSource>> gameSessions = [];


        public async Task HandleNewUser(WebSocket webSocket)
        {
            try
            {
                var buffer = new byte[256];
                var receiveResult = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                var stringval = Encoding.UTF8.GetString(buffer, 0, receiveResult.Count);

                var info = JsonSerializer.Deserialize<JoinRequestInfo>(stringval);
                ConsoleLog.WriteLine("Player joined: " + info?.PlayerName);

                if (info == null || string.IsNullOrEmpty(info.RoomCode))
                {
                    ConsoleLog.WriteLine("Invalid room code.");
                    await webSocket.CloseAsync(WebSocketCloseStatus.Empty, "No room found.", CancellationToken.None);
                    return;
                }

                info.PlayerName ??= "default";
                var cancelTokenSource = new CancellationTokenSource();

                //Check if room exists, 
                if (!gameSessions.ContainsKey(info.RoomCode))
                {
                    ConsoleLog.WriteLine("Room created: " + info.RoomCode);
                    
                    using var scope = serviceProvider.CreateScope();
                    GameContext _gameContext = scope.ServiceProvider.GetRequiredService<GameContext>();

                    if (_gameContext.GameSessions.Any(x => x.RoomCode == info.RoomCode))
                        throw new Exception($"Room '{info.RoomCode}' already exists.");

                    _gameContext.GameSessions.Add(new GameSession(GameSessionStatus.WaitingForPlayers, info.RoomCode));
                    await _gameContext.SaveChangesAsync();


                    gameSessions[info.RoomCode] = new(new GameRoomManager(info, RemoveRoomAsync, serviceProvider), cancelTokenSource);
                    _ = gameSessions[info.RoomCode].Item1.CleanupSocketsAsync(cancelTokenSource.Token);
                }

                ConsoleLog.WriteLine(string.Format("Player {0} joined {1}.", info?.PlayerName, info?.RoomCode));
                await gameSessions[info!.RoomCode].Item1.AddClientAsync(webSocket, new PlayerResultInfo() { PlayerName = info.PlayerName }, cancelTokenSource.Token);
            }
            catch (Exception ex)
            {
                ConsoleLog.WriteLine("Handle Client Error: " + ex.Message);
                await webSocket.CloseAsync(WebSocketCloseStatus.InternalServerError, ex.Message, CancellationToken.None);
            }
        }


        private async Task RemoveRoomAsync(string roomCode, CancellationToken token)
        {
            gameSessions[roomCode].Item2.Cancel();
            gameSessions.Remove(roomCode, out _);
            ConsoleLog.WriteLine(string.Format("Room {0} removed.", roomCode));

            using var scope = serviceProvider.CreateScope();
            GameContext gameContext = scope.ServiceProvider.GetRequiredService<GameContext>();

            var session = gameContext.GameSessions.FirstOrDefault(x => x.RoomCode == roomCode);
            //Log this data, and handle it in the future? see why it wont exist for any reason.
            if (session == null)
                return;
            
            await gameContext.RemoveGameSessionChildren(session, token);
        }
    }
}
