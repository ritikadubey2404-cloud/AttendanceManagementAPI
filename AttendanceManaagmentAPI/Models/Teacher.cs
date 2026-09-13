using System;
using System.ComponentModel.DataAnnotations;

namespace AttendanceManaagmentAPI.Models
{
    public class Teacher
    {
        [Key]
        public int TeacherId { get; set; }
        public int? UserId { get; set; }
        public string TeacherName { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? Password { get; set; }
        public string? MobileNo { get; set; }
        public string? Department { get; set; }
        public int? Semester { get; set; }
        public bool? IsActive { get; set; }
        public DateTime? CreatedAt { get; set; }
        }
}
