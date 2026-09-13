using AttendanceManaagmentAPI.Data;
using AttendanceManaagmentAPI.Models;
using AttendanceManaagmentAPI.Services;
using AttendanceManagementAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AttendanceManaagmentAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly EmailService _emailService;
        private readonly ApplicationDbContext _context;

        private static readonly Dictionary<string, string> otpStore = new();

        public AuthController(
            ApplicationDbContext context,
            EmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        // ============================================
        // CHECK EMAIL
        // ============================================

        [HttpPost("check-email")]
        public async Task<IActionResult> CheckEmail(
            [FromBody] CheckEmailRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Email))
            {
                return BadRequest(new
                {
                    Exists = false,
                    HasPassword = false,
                    Message = "Email is required."
                });
            }

            string email =
                request.Email.Trim().ToLower();

            var user = await _context.Users
                .FirstOrDefaultAsync(x =>
                    x.Email.ToLower() == email);

            if (user == null)
            {
                return Ok(new
                {
                    Exists = false,
                    HasPassword = false,
                    Message =
                        "This email is not registered by Admin."
                });
            }

            if (user.Role == "Student")
            {
                var student =
                    await _context.Students
                        .FirstOrDefaultAsync(s =>
                            s.UserId == user.UserId);

                if (student == null)
                {
                    return Ok(new
                    {
                        Exists = false,
                        HasPassword = false,
                        Message =
                            "This student account is not active."
                    });
                }

                if (student.IsActive == false)
                {
                    return Ok(new
                    {
                        Exists = false,
                        HasPassword = false,
                        Message =
                            "This student account is inactive."
                    });
                }
            }

            if (user.Role == "Teacher")
            {
                var teacher =
                    await _context.Teachers
                        .FirstOrDefaultAsync(t =>
                            t.UserId == user.UserId);

                if (teacher == null)
                {
                    return Ok(new
                    {
                        Exists = false,
                        HasPassword = false,
                        Message =
                            "This teacher account is not active."
                    });
                }

                if (teacher.IsActive == false)
                {
                    return Ok(new
                    {
                        Exists = false,
                        HasPassword = false,
                        Message =
                            "This teacher account is inactive."
                    });
                }
            }

            bool hasPassword =
                !string.IsNullOrWhiteSpace(user.Password);

            return Ok(new
            {
                Exists = true,
                HasPassword = hasPassword,
                Role = user.Role,
                IsVerified = user.isVerified,
                Message = hasPassword
                    ? "Account already exists. Please login using your email and password."
                    : "New account. OTP verification is required."
            });
        }


        // ============================================
        // SEND OTP
        // ============================================

        [HttpPost("send-otp")]
        public async Task<IActionResult> SendOtp(
            [FromBody] SendOtpRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Email))
                {
                    return BadRequest(new
                    {
                        Message = "Email is required."
                    });
                }

                string email =
                    request.Email.Trim().ToLower();

                var user = await _context.Users
                    .FirstOrDefaultAsync(x =>
                        x.Email.ToLower() == email);

                if (user == null)
                {
                    return BadRequest(new
                    {
                        Message =
                            "This email is not registered by Admin."
                    });
                }

                if (user.isVerified &&
                    !string.IsNullOrWhiteSpace(user.Password))
                {
                    return BadRequest(new
                    {
                        Message =
                            "This account is already verified. Please login using your email and password."
                    });
                }

                if (user.Role == "Student")
                {
                    var student =
                        await _context.Students
                            .FirstOrDefaultAsync(s =>
                                s.UserId == user.UserId);

                    if (student == null)
                    {
                        return BadRequest(new
                        {
                            Message =
                                "Student record was not added by Admin."
                        });
                    }

                    if (student.IsActive == false)
                    {
                        return BadRequest(new
                        {
                            Message =
                                "This student account is inactive."
                        });
                    }
                }

                if (user.Role == "Teacher")
                {
                    var teacher =
                        await _context.Teachers
                            .FirstOrDefaultAsync(t =>
                                t.UserId == user.UserId);

                    if (teacher == null)
                    {
                        return BadRequest(new
                        {
                            Message =
                                "Teacher record was not added by Admin."
                        });
                    }

                    if (teacher.IsActive == false)
                    {
                        return BadRequest(new
                        {
                            Message =
                                "This teacher account is inactive."
                        });
                    }
                }

                string otp =
                    new Random()
                        .Next(100000, 999999)
                        .ToString();

                otpStore[email] = otp;

                await _emailService.SendEmailAsync(
                    email,
                    "Attendance Management OTP",
                    $"Your OTP is: {otp}");

                return Ok(new
                {
                    Message =
                        "OTP sent successfully",

                    Role =
                        user.Role
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    Message =
                        "Unable to send OTP.",

                    Error =
                        ex.Message
                });
            }
        }


        // ============================================
        // VERIFY OTP
        // ============================================

        [HttpPost("verify-otp")]
        public async Task<IActionResult> VerifyOtp(
            [FromBody] VerifyOtpRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Email) ||
                string.IsNullOrWhiteSpace(request.Otp))
            {
                return BadRequest(new
                {
                    Message =
                        "Email and OTP are required."
                });
            }

            string email =
                request.Email.Trim().ToLower();

            string otp =
                request.Otp.Trim();

            if (!otpStore.ContainsKey(email) ||
                otpStore[email] != otp)
            {
                return BadRequest(new
                {
                    Message =
                        "Please enter valid OTP"
                });
            }

            otpStore.Remove(email);

            var user = await _context.Users
                .FirstOrDefaultAsync(u =>
                    u.Email.ToLower() == email);

            if (user == null)
            {
                return BadRequest(new
                {
                    Message =
                        "This email is not registered by Admin."
                });
            }

            if (user.Role == "Student")
            {
                var student =
                    await _context.Students
                        .FirstOrDefaultAsync(s =>
                            s.UserId == user.UserId);

                if (student == null)
                {
                    return BadRequest(new
                    {
                        Message =
                            "Student record was not added by Admin."
                    });
                }

                if (student.IsActive == false)
                {
                    return BadRequest(new
                    {
                        Message =
                            "This student account is inactive."
                    });
                }
            }

            if (user.Role == "Teacher")
            {
                var teacher =
                    await _context.Teachers
                        .FirstOrDefaultAsync(t =>
                            t.UserId == user.UserId);

                if (teacher == null)
                {
                    return BadRequest(new
                    {
                        Message =
                            "Teacher record was not added by Admin."
                    });
                }

                if (teacher.IsActive == false)
                {
                    return BadRequest(new
                    {
                        Message =
                            "This teacher account is inactive."
                    });
                }
            }

            user.isVerified = true;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                Message =
                    "OTP Verified Successfully",

                Role =
                    user.Role
            });
        }


        // ============================================
        // SET PASSWORD
        // ============================================

        [HttpPost("set-password")]
        public async Task<IActionResult> SetPassword(
            [FromBody] SetPasswordRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Email))
            {
                return BadRequest(new
                {
                    Message =
                        "Email is required."
                });
            }

            if (string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest(new
                {
                    Message =
                        "Password is required."
                });
            }

            string email =
                request.Email.Trim().ToLower();

            var user = await _context.Users
                .FirstOrDefaultAsync(x =>
                    x.Email.ToLower() == email);

            if (user == null)
            {
                return BadRequest(new
                {
                    Message =
                        "This email is not registered by Admin."
                });
            }

            if (user.Role == "Student")
            {
                var student =
                    await _context.Students
                        .FirstOrDefaultAsync(s =>
                            s.UserId == user.UserId);

                if (student == null)
                {
                    return BadRequest(new
                    {
                        Message =
                            "Student record was not found."
                    });
                }

                if (student.IsActive == false)
                {
                    return BadRequest(new
                    {
                        Message =
                            "This student account is inactive."
                    });
                }

                student.Password =
                    request.Password;
            }
            else if (user.Role == "Teacher")
            {
                var teacher =
                    await _context.Teachers
                        .FirstOrDefaultAsync(t =>
                            t.UserId == user.UserId);

                if (teacher == null)
                {
                    return BadRequest(new
                    {
                        Message =
                            "Teacher record was not found."
                    });
                }

                if (teacher.IsActive == false)
                {
                    return BadRequest(new
                    {
                        Message =
                            "This teacher account is inactive."
                    });
                }

                teacher.Password =
                    request.Password;
            }
            else
            {
                return BadRequest(new
                {
                    Message =
                        "Invalid account role."
                });
            }

            user.Password =
                request.Password;

            user.isVerified = true;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                Message =
                    "Password set successfully",

                Role =
                    user.Role
            });
        }


        // ============================================
        // LOGIN
        // ============================================

        [HttpPost("login")]
        public async Task<IActionResult> Login(
            [FromBody] LoginRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Email) ||
                string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest(new
                {
                    Message =
                        "Email and Password are required."
                });
            }

            string email =
                request.Email.Trim().ToLower();

            var user = await _context.Users
                .FirstOrDefaultAsync(x =>
                    x.Email.ToLower() == email);

            if (user == null)
            {
                return BadRequest(new
                {
                    Message =
                        "This email is not registered by Admin."
                });
            }

            // ========================================
            // STUDENT
            // ========================================

            Student? studentRecord = null;

            if (user.Role == "Student")
            {
                studentRecord =
                    await _context.Students
                        .FirstOrDefaultAsync(s =>
                            s.UserId == user.UserId);

                if (studentRecord == null)
                {
                    return BadRequest(new
                    {
                        Message =
                            "Student record was not added by Admin."
                    });
                }

                if (studentRecord.IsActive == false)
                {
                    return BadRequest(new
                    {
                        Message =
                            "This student account has been deleted by Admin."
                    });
                }
            }

            // ========================================
            // TEACHER
            // ========================================

            Teacher? teacherRecord = null;

            if (user.Role == "Teacher")
            {
                teacherRecord =
                    await _context.Teachers
                        .FirstOrDefaultAsync(t =>
                            t.UserId == user.UserId);

                if (teacherRecord == null)
                {
                    return BadRequest(new
                    {
                        Message =
                            "Teacher record was not added by Admin."
                    });
                }

                if (teacherRecord.IsActive == false)
                {
                    return BadRequest(new
                    {
                        Message =
                            "This teacher account has been deleted by Admin."
                    });
                }
            }

            else if (user.Role != "Student")
            {
                return BadRequest(new
                {
                    Message =
                        "Invalid account role."
                });
            }

            // ========================================
            // PASSWORD CHECK
            // ========================================

            if (string.IsNullOrWhiteSpace(user.Password))
            {
                return BadRequest(new
                {
                    Message =
                        "Password has not been set. OTP verification is required."
                });
            }

            if (user.Password != request.Password)
            {
                return BadRequest(new
                {
                    Message =
                        "Invalid Email or Password"
                });
            }

            // ========================================
            // DISPLAY NAME
            // ========================================

            string displayName =
                user.Name ?? "";

            if (string.IsNullOrWhiteSpace(displayName))
            {
                string emailName =
                    user.Email.Split('@')[0];

                emailName =
                    new string(
                        emailName
                            .Where(c =>
                                !char.IsDigit(c))
                            .ToArray());

                displayName =
                    System.Globalization
                        .CultureInfo
                        .CurrentCulture
                        .TextInfo
                        .ToTitleCase(emailName);
            }

            // ========================================
            // LOGIN SUCCESS
            // ========================================

            return Ok(new
            {
                Message =
                    "Login Successfully",

                Role =
                    user.Role,

                Name =
                    displayName,

                UserId =
                    user.UserId,

                // ====================================
                // ADDED FOR STUDENT FEATURES
                // ====================================

                StudentId =
                    studentRecord?.StudentId ?? 0,

                TeacherId =
                    teacherRecord?.TeacherId ?? 0
            });
        }
    }


    // ============================================
    // CHECK EMAIL REQUEST
    // ============================================

    public class CheckEmailRequest
    {
        public string? Email { get; set; }
    }
}