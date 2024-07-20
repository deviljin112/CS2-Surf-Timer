using System.ComponentModel.DataAnnotations;
using SurfTimer.ST_DB.Validators;

namespace SurfTimer.ST_DB.Models;

public class CheckpointsModel
{
    [Key]
    [IndexValidator]
    [ReferenceValidator("MapTimes", "id")]
    public Int32 maptime_id { get; set; }
    
    [IndexValidator]
    public Byte cp { get; set; }
    
    [CommentValidator("start_touch")]
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
    
    [DecimalValidator(12,6)]
    public decimal end_touch { get; set; }
    
    public Int32 attempts { get; set; }
}