namespace AttendanceManaagmentAPI.Models
{
    public class SendOtpRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; }
    }
}
