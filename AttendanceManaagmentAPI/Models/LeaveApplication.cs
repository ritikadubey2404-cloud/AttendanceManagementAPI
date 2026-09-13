using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AttendanceManaagmentAPI.Models
{
    public class LeaveApplication
    {
        [Key]
        public int LeaveId { get; set; }

        // ==========================================
        // STUDENT
        // ==========================================

        [Required]
        public int StudentId { get; set; }

        [ForeignKey(nameof(StudentId))]
        public Student? Student { get; set; }


        // ==========================================
        // TEACHER
        // ==========================================

        [Required]
        public int TeacherId { get; set; }

        [ForeignKey(nameof(TeacherId))]
        public Teacher? Teacher { get; set; }


        // ==========================================
        // LEAVE DETAILS
        // ==========================================

        [Required]
        public DateTime LeaveDate { get; set; }
        public string LeaveType { get; set; } = "Full Day";

        [Required]
        public string Reason { get; set; } = string.Empty;

        [Required]
        public string Status { get; set; } = "Pending";


        public string? TeacherRemark { get; set; }
        public bool IsDeletedByStudent { get; set; } = false;

        public DateTime AppliedOn { get; set; } = DateTime.Now;
    }
}