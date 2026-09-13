using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AttendanceManagementAPI.Models
{
    [Table("Admin")]
    public class Admin
    {
        [Key]
        [Column("admin_id")]
        public int Admin_Id { get; set; }

        public string Name { get; set; } = "";

        public string Email { get; set; } = "";

        public string Password { get; set; } = "";

        public string MobileNo { get; set; } = "";

        public bool IsActive { get; set; }

        [Column("Created_Date")]
        public DateTime CreatedDate { get; set; }
    }
}