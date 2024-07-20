using System.ComponentModel.DataAnnotations;
using SurfTimer.ST_DB.Validators;

namespace SurfTimer.ST_DB.Models;

public class PlayerModel
{
    [Key]
    [IncrementValidator]
    public Int32 id { get; set; }
    
    [UniqueValidator]
    [Required]
    [CommentValidator("Unique SteamID64")]
    public Int64 steam_id { get; set; }
    
    [StringLength(maximumLength: 32)]
    public string name { get; set; }
    
    [StringLength(maximumLength: 2)]
    [CommentValidator("ISO 3166-1 alpha-2")]
    public string country { get; set; }

    [CommentValidator("Unix timestamp")]
    public Int32 join_date { get; set; }

    [CommentValidator("Unix timestamp")]
    public Int32 last_seen { get; set; }
    
    public Int16 connections { get; set; }
}