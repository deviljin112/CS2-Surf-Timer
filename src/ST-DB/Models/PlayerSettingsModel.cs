using System.ComponentModel.DataAnnotations;
using SurfTimer.ST_DB.Validators;

namespace SurfTimer.ST_DB.Models;

public class PlayerSettingsModel
{
    [Key]
    [ReferenceValidator("Player", "id")]
    [IndexValidator]
    public Int32 player_id { get; set; }
    
    [Required]
    [IndexValidator]
    [StringLength(maximumLength: 32)]
    public string setting { get; set; }
    
    [Required]
    [StringLength(maximumLength: 64)]
    public string value { get; set; }

    [CommentValidator("Unix timestamp")]
    public Int32 created_date { get; set; }
    
    [CommentValidator("Unix timestamp")]
    public Int32 last_modified { get; set; }
}