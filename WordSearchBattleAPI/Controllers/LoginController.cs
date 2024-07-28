using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System;
using System.Text;
using WordSearchBattleAPI.Algorithm;
using WordSearchBattleAPI.Helper;
using WordSearchBattleAPI.Database;
using System.Data;
using System.IdentityModel.Tokens.Jwt;

namespace WordSearchBattleAPI.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class LoginController(IConfiguration config, PlayerContext context) : ControllerBase
    {
        private readonly IConfiguration _config = config;

        private readonly PlayerContext _context = context;


        [HttpPost(Name = "Login")]
        public IActionResult Login([FromBody] string Username)
        {
            Username = Username.ToLower();
            var user = _context.Users.SingleOrDefault(x => x.Username == Username);

            if (user == null)
                return NotFound();

            var token = GenerateToken(user.PlayerId);
            return Ok(token);
        }

        //[HttpPost("Register")]
        //public IActionResult Register([FromBody] LoginData loginRequest)
        //{
        //    if (_context.Users.Any(x => x.Username!.ToLower() == loginRequest.Username!.ToLower() || string.IsNullOrEmpty(loginRequest.PasswordHash) || loginRequest.PasswordHash.Length < 7))
        //        return BadRequest();

        //    var salt = Encryption.GenerateSalt(13);
        //    DatabaseLoginData userData = new()
        //    {
        //        Username = loginRequest.Username!.ToLower(),
        //        PasswordHash = Encryption.HashPassword(loginRequest.PasswordHash, salt, enhancedEntropy: true),
        //        Salt = salt
        //    };

        //    var user = _context.Users.Add(userData);
        //    _context.SaveChanges();

        //    var id = user.Entity.Id;

        //    if (!id.HasValue)
        //        return NotFound();

        //    var token = GenerateToken(id.Value);
        //    return Ok(token);
        //}

        protected string GenerateToken(int id)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(DockerSecretHelper.ReadSecret("jwt-key")));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            List<Claim> claims =
            [
                new(Microsoft.IdentityModel.JsonWebTokens.JwtRegisteredClaimNames.Sub, id.ToString()),
            ];

            var token = new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken(_config["Jwt:Issuer"], _config["Jwt:Audience"], claims, expires: DateTime.Now.AddMinutes(120), signingCredentials: credentials));
            //var data = Encoding.UTF8.GetBytes(DockerSecretHelper.ReadSecret("jwt-key"));
            //var securityKey = new SymmetricSecurityKey(data);

            //var claims = new Dictionary<string, object>
            //{
            //    [ClaimTypes.Name] = "Testuser",
            //    [JwtRegisteredClaimNames.Sub] = id.ToString()
            //};
            //var descriptor = new SecurityTokenDescriptor
            //{
            //    Issuer = _config["Jwt:Issuer"],
            //    Audience = _config["Jwt:Audience"],
            //    Claims = claims,
            //    IssuedAt = null,
            //    NotBefore = DateTime.UtcNow,
            //    Expires = DateTime.UtcNow.AddMinutes(120),
            //    SigningCredentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256Signature)
            //};

            //var handler = new JsonWebTokenHandler
            //{
            //    SetDefaultTimesOnTokenCreation = false
            //};

            //var tokenString = handler.CreateToken(descriptor);

            return token;
        }
    }
}
