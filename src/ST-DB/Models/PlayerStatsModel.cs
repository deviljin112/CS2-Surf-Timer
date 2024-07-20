using System.ComponentModel.DataAnnotations;
using SurfTimer.ST_DB.Validators;

namespace SurfTimer.ST_DB.Models;

public class PlayerStatsModel
{
    [Key]
    [ReferenceValidator("Player", "id")]
    [IndexValidator]
    public Int32 player_id { get; set; }
    
    [IndexValidator]
    public Byte style { get; set; }
    
    public Int32 points { get; set; }
    
    [CommentValidator("Minutes played")]
    public Int32 playtime { get; set; }
}