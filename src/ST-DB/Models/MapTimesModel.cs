using System.ComponentModel.DataAnnotations;
using SurfTimer.Validators;

namespace SurfTimer.Models;

public class MapTimesModel
{
    [Key] 
    [IncrementValidator] 
    public Int32 id { get; set; }

    [IndexValidator]
    [ReferenceValidator("Player", "id")] 
    public Int32 player_id { get; set; }

    [IndexValidator]
    [ReferenceValidator("Maps", "id")] 
    public Int32 map_id { get; set; }

    [IndexValidator]
    public Byte style { get; set; }

    [IndexValidator]
    [CommentValidator("0 = map time, 1+ = bonus no. THIS MUST BE 0 IF stages > 0, WE DO NOT HAVE STAGES IN BONUSES.")]
    public Byte type { get; set; }

    [IndexValidator]
    [CommentValidator(
        "if type = 0: 0 = not staged, 1+ = stage no. THIS MUST BE 0 IF type > 0, WE DO NOT HAVE STAGES IN BONUSES.")]
    public Byte stage { get; set; }

    public Int32 run_time { get; set; }

    [DecimalValidator(8, 3)]
    public decimal start_vel_x { get; set; }

    [DecimalValidator(8, 3)]
    public decimal start_vel_y { get; set; }

    [DecimalValidator(8, 3)]
    public decimal start_vel_z { get; set; }

    [DecimalValidator(8, 3)]
    public decimal end_vel_x { get; set; }

    [DecimalValidator(8, 3)]
    public decimal end_vel_y { get; set; }

    [DecimalValidator(8, 3)]
    public decimal end_vel_z { get; set; }
    
    [CommentValidator("Unix timestamp")]
    public Int32 run_date { get; set; }
    
    [CustomTypeValidator("longblob")]
    [CommentValidator("Replay for the run")]
    public byte[] replay_frames { get; set; }
}