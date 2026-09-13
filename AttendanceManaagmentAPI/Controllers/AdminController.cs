using AttendanceManaagmentAPI.Data;
using AttendanceManaagmentAPI.Models;
using AttendanceManaagmentAPI.Services;
using AttendanceManagementAPI.Models;
using Microsoft.AspNetCore.Mvc;

namespace AttendanceManaagmentAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AdminController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly EmailService _emailService;

        private static Dictionary<string, string> adminOtpStore = new();

        public AdminController(
            ApplicationDbContext context,
            EmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        // CHECK WHETHER ADMIN EMAIL ALREADY EXISTS
        [HttpPost("check-email")]
        public IActionResult CheckEmail([FromBody] AdminEmailRequest request)
        {
            var admin = _context.Admin
                .FirstOrDefault(a => a.Email == request.Email);

            if (admin == null)
            {
                return Ok(new
                {
                    Exists = false
                });
            }

            return Ok(new
            {
                Exists = true
            });
        }

        // SEND OTP
        [HttpPost("send-otp")]
        public async Task<IActionResult> SendOtp([FromBody] AdminEmailRequest request)
        {
            try
            {
                var otp = new Random().Next(100000, 999999).ToString();

                adminOtpStore[request.Email] = otp;

                await _emailService.SendEmailAsync(
                    request.Email,
                    "Admin OTP",
                    $"Your Admin OTP is: {otp}");

                return Ok(new
                {
                    Message = "OTP sent successfully"
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    Message = ex.Message
                });
            }
        }

        // VERIFY OTP
        [HttpPost("verify-otp")]
        public IActionResult VerifyOtp([FromBody] AdminOtpRequest request)
        {
            if (adminOtpStore.ContainsKey(request.Email) &&
                adminOtpStore[request.Email] == request.Otp)
            {
                adminOtpStore.Remove(request.Email);

                return Ok(new
                {
                    Message = "OTP verified successfully"
                });
            }

            return BadRequest(new
            {
                Message = "Invalid OTP"
            });
        }

        // SET ADMIN PASSWORD
        [HttpPost("set-password")]
        public IActionResult SetPassword([FromBody] AdminPasswordRequest request)
        {
            var admin = _context.Admin
                .FirstOrDefault(a => a.Email == request.Email);

            if (admin == null)
            {
                admin = new Admin
                {
                    Name = request.Name,
                    Email = request.Email,
                    Password = request.Password,
                    MobileNo = "",
                    IsActive = true,
                    CreatedDate = DateTime.Now
                };

                _context.Admin.Add(admin);
            }
            else
            {
                admin.Password = request.Password;
                admin.IsActive = true;
            }

            _context.SaveChanges();

            return Ok(new
            {
                Message = "Admin password saved successfully"
            });
        }

        // ADMIN LOGIN
        [HttpPost("login")]
        public IActionResult Login([FromBody] AdminLoginRequest request)
        {
            var admin = _context.Admin
                .FirstOrDefault(a =>
                    a.Email == request.Email &&
                    a.Password == request.Password &&
                    a.IsActive == true);

            if (admin == null)
            {
                return BadRequest(new
                {
                    Message = "Invalid Admin Email or Password"
                });
            }

            return Ok(new
            {
                Message = "Admin Login Successfully",
                Role = "Admin",
                Name = admin.Name,
                Email = admin.Email
            });
        }
    }

    // REQUEST MODELS

    public class AdminEmailRequest
    {
        public string Email { get; set; } = "";
    }

    public class AdminOtpRequest
    {
        public string Email { get; set; } = "";
        public string Otp { get; set; } = "";
    }

    public class AdminPasswordRequest
    {
        public string Name { get; set; } = "";
        public string Email { get; set; } = "";
        public string Password { get; set; } = "";
    }

    public class AdminLoginRequest
    {
        public string Email { get; set; } = "";
        public string Password { get; set; } = "";
    }
}