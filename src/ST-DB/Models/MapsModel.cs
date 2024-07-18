using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using SurfTimer.Validators;

namespace SurfTimer.Models;

public class MapsModel
{
    [Key]
    [IncrementValidator]
    public Int32 id { get; set; }
    
    [DefaultValue(0)]
    public Byte tier { get; set; }
    
    [StringLength(maximumLength: 64)]
    public string author { get; set; }
    
    [CommentValidator("0 = linear, 1+ = count of stages")]
    public Byte stages { get; set; }
    
    [CommentValidator("0 = linear, 1+ = count of bonuses")]
    public Byte bonuses { get; set; }
    
    public bool ranked { get; set; }
    
    [CommentValidator("Unix timestamp")]
    public Int32 date_added { get; set; }

    [CommentValidator("Unix timestamp")]
    public Int32 last_played { get; set; }
}