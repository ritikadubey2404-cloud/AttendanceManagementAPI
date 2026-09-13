using System;
using System.ComponentModel.DataAnnotations;

namespace AttendanceManaagmentAPI.Models
{
    public class Student
    {
        [Key]
        public int StudentId { get; set; }
        public int? UserId { get; set; }
        public string? EnrollmentNo { get; set; }
        public string? Barcode { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? Password { get; set; }
        public string? MobileNo { get; set; }
        public int? Semester { get; set; }
        public bool? IsActive { get; set; }
        public DateTime? CreatedAt { get; set; }

    }
}
