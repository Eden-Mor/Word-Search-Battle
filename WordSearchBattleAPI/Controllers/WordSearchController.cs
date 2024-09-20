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
    public class WordSearchController(IConfiguration config, GameContext context, IClaimReader claims, GameServerManager gameServerManager) : ControllerBase
    {
        [AllowAnonymous]
        [HttpGet(nameof(GetPublicGames), Name = nameof(GetPublicGames))]
        public ActionResult<Dictionary<string, int>> GetPublicGames()
        {
            //IN THE FUTURE, MAKE SURE TO PUT PLAYER DATA IN DATABASE, AND GRAB THIS INFO FROM THE DATABASE.
            var list = gameServerManager.GameSessions.ToDictionary(x => x.Value.Item1.GetRoomCode(), x => x.Value.Item1.GetPlayerCount());
            return Ok(list);
        }
    }
}
