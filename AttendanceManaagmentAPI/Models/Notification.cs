namespace AttendanceManaagmentAPI.Models
{
    public class Notification
    {
        public int NotificationId { get; set; }

        public int StudentId { get; set; }

        public string Title { get; set; } = string.Empty;

        public string Message { get; set; } = string.Empty;

        public bool IsRead { get; set; } = false;

        public DateTime? SentOn { get; set; }

        public int TeacherId { get; set; }
    }
}