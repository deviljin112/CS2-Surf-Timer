using System.ComponentModel.DataAnnotations;
using SurfTimer.Validators;

namespace SurfTimer.Models;

public class MapZonesModel
{
    [Key]
    [IncrementValidator]
    public Int32 id { get; set; }
    
    [ReferenceValidator("Maps", "id")]
    public Int32 map_id { get; set; }
    
    [CommentValidator("0: start, 1: cp, 2: end, 10: stop timer, 11: speed limit, etc")]
    public Byte type { get; set; }
    
    [CommentValidator("Index of bonus, this < 1 is treated as no-bonus")]
    public Byte bonux { get; set; }
    
    [CommentValidator("Index of stage, this < 1 is treated as no-stage")]
    public Byte stage { get; set; }
    
    [CommentValidator("Trigger name to hook for this zone. Any position data for the zone is ignored if this is not blank.")]
    [StringLength(maximumLength: 64)]
    public string trigger_hook { get; set; }
    
    [CommentValidator("Max speed you can leave this zone with. (Only for type: 0)")]
    public Int16 max_zone_speed { get; set; }
}