using System.ComponentModel.DataAnnotations;

namespace WordSearchBattleAPI.Models
{
    public class User
    {
        [Key]
        public int PlayerId { get; set; }
        public string? Username { get; set; }
    }
}
