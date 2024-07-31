using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.JsonWebTokens;
using System.Text;
using WordSearchBattleAPI.Algorithm;
using WordSearchBattleAPI.Database;
using WordSearchBattleAPI.Managers;
using WordSearchBattleAPI.Models;
using WordSearchBattleAPI.Services;
using WordSearchBattleShared.Enums;

namespace WordSearchBattleAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [Authorize]
    public class WordSearchController(IConfiguration config, GameContext context, IClaimReader claims, IRoomCodeGenerator roomCodeGen, GameServerManager gameServerManager) : ControllerBase
    {

        //[HttpGet(nameof(GetNewGameRoom), Name = nameof(GetNewGameRoom))]
        //public async Task<ActionResult<string>> GetNewGameRoom(CancellationToken cancellationToken)
        //{
        //    if (!int.TryParse(claims.GetClaim(JwtRegisteredClaimNames.Sub), out int playerId))
        //        return NotFound("PlayerId not found in claims.");

        //    var currentOwnedGame = await context.GameSessions.FirstOrDefaultAsync(x => x.OwnerPlayerId == playerId, cancellationToken);

        //    if (currentOwnedGame?.GameSessionStatusCode is GameSessionStatus.Completed)
        //        return Conflict("You already own a game that is in progress.");

        //    if (currentOwnedGame?.GameSessionStatusCode is GameSessionStatus.WaitingForPlayers)
        //        return Ok(currentOwnedGame.RoomCode);

        //    var newRoomCode = await roomCodeGen.GenerateUniqueCodeAsync(cancellationToken);
        //    var gameSession = new GameSession(GameSessionStatus.WaitingForPlayers, playerId, newRoomCode);
        //    await context.GameSessions.AddAsync(gameSession, cancellationToken);
        //    await context.SaveChangesAsync(cancellationToken);

        //    return Ok(gameSession.RoomCode);
        //}


        //[HttpGet(nameof(StartGameRoom), Name = nameof(StartGameRoom))]
        //public async Task<ActionResult> StartGameRoom(CancellationToken cancellationToken)
        //{
        //    if (!int.TryParse(claims.GetClaim(JwtRegisteredClaimNames.Sub), out int playerId))
        //        return NotFound("PlayerId not found in claims.");

        //    var currentOwnedGame = await context.GameSessions.FirstOrDefaultAsync(x => x.OwnerPlayerId == playerId, cancellationToken);

        //    if (currentOwnedGame == null)
        //        return NotFound("Could not find game session.");

        //    if (currentOwnedGame.GameSessionStatusCode is GameSessionStatus.Completed)
        //    {
        //        await context.RemoveGameSessionChildren(currentOwnedGame, cancellationToken);
        //        return Problem("Found a game but it is completed. Removed from database.");
        //    }

        //    if (currentOwnedGame?.GameSessionStatusCode is not GameSessionStatus.WaitingForPlayers)
        //        return Forbid("Game is in progress.");

        //    //Game is Waiting for Players, allow us to start.

        //    var newRoomCode = await roomCodeGen.GenerateUniqueCodeAsync(cancellationToken);
        //    var gameSession = new GameSession(GameSessionStatus.WaitingForPlayers, playerId, newRoomCode);
        //    await context.GameSessions.AddAsync(gameSession, cancellationToken);

        //    return Ok(gameSession.RoomCode);
        //}



        [AllowAnonymous]
        [HttpGet(nameof(GetRandomWordSearch), Name = nameof(GetRandomWordSearch))]
        public ActionResult<Tuple<string[], string>> GetRandomWordSearch()
        {
            Tuple<string[], char[,]> tuple = SetupGame();
            return Ok(new Tuple<string[], string>(tuple.Item1, ConvertCharArrayToStringGrid(tuple.Item2)));
        }



        public static Tuple<string[], char[,]> SetupGame(int sizeList = 8, string nameList = "Instruments")
        {
            WordSearch wordSearch = new();
            wordSearch.HandleSetupWords(nameList, sizeList);
            wordSearch.HandleSetupGrid();

            return new(wordSearch.Words, wordSearch.Grid);
        }

        public static string ConvertCharArrayToStringGrid(char[,] array)
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
    }
}
