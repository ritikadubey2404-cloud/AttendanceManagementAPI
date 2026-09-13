using System;
using System.ComponentModel.DataAnnotations;

namespace AttendanceManaagmentAPI.Models
{
    public class Attendance
    {
        [Key]
        public int AttendanceId { get; set; }
        public int? StudentId { get; set; }
        public DateTime? AttendanceDate { get; set; }
        public DateTime? ScanTime { get; set; }
        public string? Status { get; set; }
        public int? TeacherId { get; set; }
        public string? Remarks { get; set; }
    }
}
