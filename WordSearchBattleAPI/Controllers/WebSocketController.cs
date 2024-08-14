using Microsoft.AspNetCore.Mvc;
using System.Net.WebSockets;
using WordSearchBattleAPI.Helper;
using WordSearchBattleAPI.Managers;

namespace WordSearchBattleAPI.Controllers
{
    public class WebSocketController(GameServerManager gameServerMaster) : ControllerBase
    {
        [HttpGet("/ws")]
        public async Task Get()
        {
            try
            {
                if (HttpContext.WebSockets.IsWebSocketRequest)
                {
                    using var webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();
                    await gameServerMaster.HandleNewUser(webSocket);
                    
                    if (webSocket.State == WebSocketState.Open)
                        await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Finished handling user.", CancellationToken.None);
                }
                else
                {
                    HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
                }
            }
            catch (Exception ex)
            {
                ConsoleLog.WriteLine(ex.Message);
            }
        }
    }
}
