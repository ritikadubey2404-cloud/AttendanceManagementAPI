using AttendanceManaagmentAPI.Data;
using AttendanceManaagmentAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AttendanceManaagmentAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class NotificationController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public NotificationController(ApplicationDbContext context)
        {
            _context = context;
        }

        // =========================================================
        // GET ACTIVE STUDENTS
        // =========================================================

        [HttpGet("students")]
        public async Task<IActionResult> GetStudents()
        {
            try
            {
                var students = await _context.Students
                    .Where(s => s.IsActive == true)
                    .OrderBy(s => s.FullName)
                    .Select(s => new
                    {
                        StudentId = s.StudentId,
                        FullName = s.FullName,
                        EnrollmentNo = s.EnrollmentNo
                    })
                    .ToListAsync();

                return Ok(students);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Unable to load students.",
                    error = ex.Message
                });
            }
        }

        // =========================================================
        // STUDENT UNREAD MESSAGE COUNT
        //
        // IMPORTANT:
        // "New Leave Request" ko count nahi karega.
        // Leave notification teacher ke liye hai.
        // =========================================================

        [HttpGet("unread-count/{studentId:int}")]
        public async Task<IActionResult> GetStudentUnreadCount(
            int studentId)
        {
            try
            {
                if (studentId <= 0)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Invalid Student ID."
                    });
                }

                bool studentExists =
                    await _context.Students.AnyAsync(s =>
                        s.StudentId == studentId &&
                        s.IsActive == true);

                if (!studentExists)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "Student not found."
                    });
                }

                int unreadCount =
                    await _context.Notifications
                        .Where(n =>
                            n.StudentId == studentId &&
                            n.Title != "New Leave Request" &&
                            n.IsRead == false)
                        .CountAsync();

                return Ok(new
                {
                    success = true,
                    unreadCount = unreadCount
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message =
                        "Unable to load unread message count.",
                    error = ex.Message
                });
            }
        }

        // =========================================================
        // TEACHER UNREAD LEAVE COUNT
        //
        // ONLY leave applications count here.
        // =========================================================

        [HttpGet("teacher-unread-count/{teacherId:int}")]
        public async Task<IActionResult> GetTeacherUnreadCount(
            int teacherId)
        {
            try
            {
                if (teacherId <= 0)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Invalid Teacher ID."
                    });
                }

                bool teacherExists =
                    await _context.Teachers.AnyAsync(t =>
                        t.TeacherId == teacherId &&
                        t.IsActive == true);

                if (!teacherExists)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "Teacher not found."
                    });
                }

                int unreadCount =
                    await _context.Notifications
                        .Where(n =>
                            n.TeacherId == teacherId &&
                            n.Title == "New Leave Request" &&
                            n.StudentId > 0 &&
                            n.IsRead == false)
                        .CountAsync();

                return Ok(new
                {
                    success = true,
                    unreadCount = unreadCount
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message =
                        "Unable to load teacher leave notification count.",
                    error = ex.Message
                });
            }
        }

        // =========================================================
        // SEND MESSAGE FROM TEACHER TO STUDENT
        // =========================================================

        [HttpPost("send")]
        public async Task<IActionResult> SendMessage(
            [FromBody] SendNotificationRequest request)
        {
            try
            {
                if (request == null)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Invalid request."
                    });
                }

                if (request.StudentId <= 0)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Please select a student."
                    });
                }

                if (request.TeacherId <= 0)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Teacher ID is required."
                    });
                }

                if (string.IsNullOrWhiteSpace(request.Title))
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Message title is required."
                    });
                }

                if (string.IsNullOrWhiteSpace(request.Message))
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Message is required."
                    });
                }

                // =================================================
                // CHECK STUDENT
                // =================================================

                var student =
                    await _context.Students
                        .FirstOrDefaultAsync(s =>
                            s.StudentId == request.StudentId &&
                            s.IsActive == true);

                if (student == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message =
                            "Student not found or inactive."
                    });
                }

                // =================================================
                // CHECK TEACHER
                // =================================================

                var teacher =
                    await _context.Teachers
                        .FirstOrDefaultAsync(t =>
                            t.TeacherId == request.TeacherId &&
                            t.IsActive == true);

                if (teacher == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message =
                            "Teacher not found or inactive."
                    });
                }

                // =================================================
                // CREATE NORMAL MESSAGE
                // =================================================

                var notification = new Notification
                {
                    StudentId = student.StudentId,

                    TeacherId = teacher.TeacherId,

                    Title = request.Title.Trim(),

                    Message = request.Message.Trim(),

                    IsRead = false,

                    SentOn = DateTime.Now
                };

                _context.Notifications.Add(notification);

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    message = "Message sent successfully.",
                    notificationId =
                        notification.NotificationId,
                    studentId =
                        student.StudentId,
                    studentName =
                        student.FullName,
                    teacherId =
                        teacher.TeacherId
                });
            }
            catch (DbUpdateException ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message =
                        "Message could not be saved in database.",
                    error =
                        ex.InnerException?.Message ??
                        ex.Message
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message =
                        "Message could not be sent.",
                    error = ex.Message
                });
            }
        }

        // =========================================================
        // GET STUDENT MESSAGES
        //
        // IMPORTANT:
        // Leave notification is NOT returned to student.
        // =========================================================

        [HttpGet("student/{studentId:int}")]
        public async Task<IActionResult> GetStudentMessages(
            int studentId)
        {
            try
            {
                if (studentId <= 0)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Invalid Student ID."
                    });
                }

                bool studentExists =
                    await _context.Students
                        .AnyAsync(s =>
                            s.StudentId == studentId);

                if (!studentExists)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "Student not found."
                    });
                }

                var messages =
                    await _context.Notifications

                        .Where(n =>
                            n.StudentId == studentId &&

                            // VERY IMPORTANT
                            // Leave notification student
                            // message section me nahi jayegi.
                            n.Title != "New Leave Request")

                        .OrderByDescending(n =>
                            n.SentOn)

                        .Select(n => new
                        {
                            NotificationId =
                                n.NotificationId,

                            StudentId =
                                n.StudentId,

                            TeacherId =
                                n.TeacherId,

                            Title =
                                n.Title,

                            Message =
                                n.Message,

                            IsRead =
                                n.IsRead,

                            SentOn =
                                n.SentOn
                        })

                        .ToListAsync();

                return Ok(messages);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message =
                        "Unable to load messages.",
                    error = ex.Message
                });
            }
        }

        // =========================================================
        // MARK NOTIFICATION AS READ
        // =========================================================

        [HttpPut("read/{notificationId:int}")]
        public async Task<IActionResult> MarkAsRead(
            int notificationId)
        {
            try
            {
                if (notificationId <= 0)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message =
                            "Invalid notification ID."
                    });
                }

                var notification =
                    await _context.Notifications
                        .FirstOrDefaultAsync(n =>
                            n.NotificationId ==
                            notificationId);

                if (notification == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message =
                            "Notification not found."
                    });
                }

                notification.IsRead = true;

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    message =
                        "Notification marked as read."
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message =
                        "Unable to mark notification as read.",
                    error = ex.Message
                });
            }
        }

        // =========================================================
        // DELETE STUDENT MESSAGE
        // =========================================================

        [HttpDelete("delete/{notificationId:int}")]
        public async Task<IActionResult> DeleteMessage(
            int notificationId)
        {
            try
            {
                if (notificationId <= 0)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message =
                            "Invalid notification ID."
                    });
                }

                var notification =
                    await _context.Notifications
                        .FirstOrDefaultAsync(n =>
                            n.NotificationId ==
                            notificationId);

                if (notification == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message =
                            "Message not found."
                    });
                }

                // =================================================
                // SAFETY:
                // Leave notification ko is API se delete
                // nahi karna.
                // =================================================

                if (notification.Title ==
                    "New Leave Request")
                {
                    return BadRequest(new
                    {
                        success = false,
                        message =
                            "Leave notifications cannot be deleted from student messages."
                    });
                }

                _context.Notifications.Remove(
                    notification);

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    message =
                        "Message deleted successfully."
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message =
                        "Unable to delete message.",
                    error =
                        ex.Message
                });
            }
        }
    }

    // =============================================================
    // REQUEST MODEL
    // =============================================================

    public class SendNotificationRequest
    {
        public int StudentId { get; set; }

        public int TeacherId { get; set; }

        public string? Title { get; set; }

        public string? Message { get; set; }
    }
}