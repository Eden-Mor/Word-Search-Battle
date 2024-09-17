using System.Text;
using WordSearchBattleAPI.Models;
using System.Text.Json;
using WordSearchBattleAPI.Database;
using WordSearchBattleShared.Enums;
using System.Net.WebSockets;
using WordSearchBattleAPI.Helper;
using System.Collections.Concurrent;
using WordSearchBattleAPI.Services;

namespace WordSearchBattleAPI.Managers
{
    public class GameServerManager(IServiceProvider serviceProvider, IRoomCodeGenerator roomGen)
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

                if (info == null)
                {
                    ConsoleLog.WriteLine("Invalid room code.");
                    await webSocket.CloseAsync(WebSocketCloseStatus.Empty, "No room found.", CancellationToken.None);
                    return;
                }

                info.RoomCode = GetLettersString(info.RoomCode, string.Empty, true); 
                info.PlayerName = GetLettersString(info.PlayerName, "default", true);

                var cancelTokenSource = new CancellationTokenSource();

                //Create the room if no room code
                if (string.IsNullOrWhiteSpace(info.RoomCode))
                {
                    info.RoomCode = await roomGen.GenerateUniqueCodeAsync(cancelTokenSource.Token);
                    ConsoleLog.WriteLine("Room created: " + info.RoomCode);

                    using var scope = serviceProvider.CreateScope();
                    GameContext _gameContext = scope.ServiceProvider.GetRequiredService<GameContext>();

                    //Room exists in database but not in dictionary, old room was not removed properly, remove it here.
                    //if (_gameContext.GameSessions.Any(x => x.RoomCode == info.RoomCode))
                        //await _gameContext.RemoveGameSessionChildrenAsync(_gameContext.GameSessions.First(x => x.RoomCode == info.RoomCode), CancellationToken.None);

                    _gameContext.GameSessions.Add(new GameSession(GameSessionStatus.WaitingForPlayers, info.RoomCode));
                    await _gameContext.SaveChangesAsync();

                    gameSessions[info.RoomCode] = new(new GameRoomManager(info, RemoveRoomAsync, serviceProvider), cancelTokenSource);
                    gameSessions[info.RoomCode].Item1.Initialize(cancelTokenSource.Token);
                }

                if (!gameSessions.TryGetValue(info.RoomCode, out Tuple<GameRoomManager, CancellationTokenSource>? room))
                {
                    ConsoleLog.WriteLine(string.Format("Could not find room {0} for player {1}.", info?.PlayerName, info?.RoomCode));
                    await webSocket.CloseAsync(WebSocketCloseStatus.InternalServerError, "Could not find room.", CancellationToken.None);
                    return;
                }

                ConsoleLog.WriteLine(string.Format("Player {0} joined {1}.", info?.PlayerName, info?.RoomCode));
                await room.Item1.AddClientAsync(webSocket, new PlayerResultInfo() { PlayerName = info.PlayerName, RoomCode = info.RoomCode }, cancelTokenSource.Token);
            }
            catch (Exception ex)
            {
                ConsoleLog.WriteLine("Exception from Client. Error: " + ex.Message);
                await webSocket.CloseAsync(WebSocketCloseStatus.InternalServerError, ex.Message, CancellationToken.None);
            }
        }

        private string GetLettersString(string? roomCode, string defaultIfNull, bool toUppercase = false)
        {
            var value = new string(roomCode?.Where(char.IsLetter).ToArray()) ?? defaultIfNull;

            if (toUppercase)
                value = value.ToUpper();

            return value;
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

            await gameContext.RemoveGameSessionChildrenAsync(session, token);
        }
    }
}
