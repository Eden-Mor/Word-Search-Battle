using System.Security.Claims;

namespace WordSearchBattleAPI.Services
{
    public class ClaimReaderService(IHttpContextAccessor http) : IClaimReader
    {
        public string? GetClaim(string claimName)
        {
            return http?.HttpContext?.User?.Claims?.FirstOrDefault(x => x.Properties.FirstOrDefault().Value == claimName)?.Value;
        }
    }

    public interface IClaimReader
    {
        public string? GetClaim(string claimName);
    }
}
