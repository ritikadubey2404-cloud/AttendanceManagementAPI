using AttendanceManaagmentAPI.Data;
using AttendanceManaagmentAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AttendanceManaagmentAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LeaveController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public LeaveController(ApplicationDbContext context)
        {
            _context = context;
        }

        // =========================================================
        // APPLY LEAVE
        // =========================================================

        [HttpPost("apply")]
        public async Task<IActionResult> ApplyLeave(
            [FromBody] ApplyLeaveRequest request)
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
                        message = "Student information is missing."
                    });
                }

                if (request.TeacherId <= 0)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Teacher information is missing."
                    });
                }

                if (request.LeaveDate == default)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Leave date is required."
                    });
                }

                if (string.IsNullOrWhiteSpace(request.Reason))
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Leave reason is required."
                    });
                }

                // =================================================
                // CHECK STUDENT
                // =================================================

                var student = await _context.Students
                    .FirstOrDefaultAsync(s =>
                        s.StudentId == request.StudentId);

                if (student == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "Student not found."
                    });
                }

                if (student.IsActive != true)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Student account is inactive."
                    });
                }

                // =================================================
                // CHECK TEACHER
                // =================================================

                var teacher = await _context.Teachers
                    .FirstOrDefaultAsync(t =>
                        t.TeacherId == request.TeacherId);

                if (teacher == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "Teacher not found."
                    });
                }

                if (teacher.IsActive != true)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Teacher account is inactive."
                    });
                }

                // =================================================
                // CREATE LEAVE APPLICATION
                // =================================================

                var leave = new LeaveApplication
                {
                    StudentId = student.StudentId,
                    TeacherId = teacher.TeacherId,
                    LeaveDate = request.LeaveDate.Date,
                    LeaveType = request.LeaveType,
                    Reason = request.Reason.Trim(),
                    Status = "Pending",
                    TeacherRemark = null,
                    AppliedOn = DateTime.Now,

                    // New leave student ke My Leave me visible rahegi.
                    IsDeletedByStudent = false
                };

                _context.LeaveApplications.Add(leave);

                await _context.SaveChangesAsync();

                // =================================================
                // CREATE TEACHER NOTIFICATION
                // =================================================

                var notification = new Notification
                {
                    StudentId = student.StudentId,
                    TeacherId = teacher.TeacherId,
                    Title = "New Leave Request",
                    Message =
                        $"{student.FullName} has submitted a leave request for {leave.LeaveDate:dd MMM yyyy}.",
                    IsRead = false,
                    SentOn = DateTime.Now
                };

                _context.Notifications.Add(notification);

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    message =
                        "Leave application submitted successfully.",
                    leaveId = leave.LeaveId,
                    notificationId = notification.NotificationId,
                    status = leave.Status,
                    teacherId = teacher.TeacherId
                });
            }
            catch (DbUpdateException ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Unable to save leave application.",
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
                    message = "Unable to apply leave.",
                    error =
                        ex.InnerException?.Message ??
                        ex.Message
                });
            }
        }

        // =========================================================
        // GET STUDENT LEAVES
        // =========================================================

        [HttpGet("student/{studentId:int}")]
        public async Task<IActionResult> GetStudentLeaves(
            int studentId)
        {
            try
            {
                if (studentId <= 0)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Invalid student ID."
                    });
                }

                var student = await _context.Students
                    .FirstOrDefaultAsync(s =>
                        s.StudentId == studentId);

                if (student == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "Student not found."
                    });
                }

                // IMPORTANT:
                // Sirf wahi leaves student ko dikhenge
                // jo student ne apni My Leave se hide nahi ki hain.
                //
                // Database record delete nahi hota.

                var leaves = await _context.LeaveApplications
                    .Where(l =>
                        l.StudentId == studentId &&
                        !l.IsDeletedByStudent)
                    .OrderByDescending(l =>
                        l.AppliedOn)
                    .Select(l => new
                    {
                        l.LeaveId,
                        l.StudentId,
                        l.TeacherId,
                        l.LeaveDate,
                        l.LeaveType,
                        l.Reason,
                        l.Status,
                        l.TeacherRemark,
                        l.AppliedOn
                    })
                    .ToListAsync();

                return Ok(leaves);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message =
                        "Unable to load student leaves.",
                    error =
                        ex.InnerException?.Message ??
                        ex.Message
                });
            }
        }

        // =========================================================
        // GET TEACHER LEAVES
        // =========================================================

        [HttpGet("teacher/{teacherId:int}")]
        public async Task<IActionResult> GetTeacherLeaves(
            int teacherId)
        {
            try
            {
                if (teacherId <= 0)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Invalid teacher ID."
                    });
                }

                bool teacherExists =
                    await _context.Teachers
                        .AnyAsync(t =>
                            t.TeacherId == teacherId &&
                            t.IsActive == true);

                if (!teacherExists)
                {
                    return NotFound(new
                    {
                        success = false,
                        message =
                            "Teacher not found or inactive."
                    });
                }

                // IMPORTANT:
                // IsDeletedByStudent ka filter YAHAN nahi hai.
                //
                // Student apni My Leave se hide kare tab bhi
                // teacher ko leave dikhti rahegi.

                var leaves =
                    await _context.LeaveApplications
                    .Where(l =>
                        l.TeacherId == teacherId)
                    .Join(
                        _context.Students,
                        leave =>
                            leave.StudentId,
                        student =>
                            student.StudentId,
                        (leave, student) => new
                        {
                            leave.LeaveId,
                            leave.StudentId,

                            StudentName =
                                student.FullName,

                            student.EnrollmentNo,

                            leave.TeacherId,
                            leave.LeaveDate,
                            leave.LeaveType,
                            leave.Reason,
                            leave.Status,
                            leave.TeacherRemark,
                            leave.AppliedOn
                        })
                    .OrderByDescending(l =>
                        l.AppliedOn)
                    .ToListAsync();

                return Ok(leaves);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message =
                        "Unable to load teacher leaves.",
                    error =
                        ex.InnerException?.Message ??
                        ex.Message
                });
            }
        }

        // =========================================================
        // APPROVE LEAVE
        // =========================================================

        [HttpPut("{leaveId:int}/approve")]
        public async Task<IActionResult> ApproveLeave(
            int leaveId,
            [FromBody] LeaveDecisionRequest? request)
        {
            try
            {
                var leave =
                    await _context.LeaveApplications
                        .FirstOrDefaultAsync(l =>
                            l.LeaveId == leaveId);

                if (leave == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message =
                            "Leave request not found."
                    });
                }

                if (leave.Status != "Pending")
                {
                    return BadRequest(new
                    {
                        success = false,
                        message =
                            "This leave request has already been processed."
                    });
                }

                // =================================================
                // FIND TEACHER
                // =================================================

                var teacher =
                    await _context.Teachers
                        .FirstOrDefaultAsync(t =>
                            t.TeacherId == leave.TeacherId);

                if (teacher == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message =
                            "Teacher not found."
                    });
                }

                // =================================================
                // APPROVE
                // =================================================

                leave.Status = "Approved";

                leave.TeacherRemark =
                    string.IsNullOrWhiteSpace(
                        request?.Remark)
                    ? "Approved by teacher."
                    : request!.Remark.Trim();

                // =================================================
                // GET STUDENT NAME
                // =================================================

                string studentName =
                    await GetStudentName(
                        leave.StudentId);

                // =================================================
                // MARK ORIGINAL TEACHER NOTIFICATION AS READ
                // =================================================

                string expectedMessage =
                    $"{studentName} has submitted a leave request for {leave.LeaveDate:dd MMM yyyy}.";

                var oldNotification =
                    await _context.Notifications
                        .Where(n =>
                            n.TeacherId == leave.TeacherId &&
                            n.StudentId == leave.StudentId &&
                            n.Title == "New Leave Request" &&
                            n.Message == expectedMessage &&
                            n.IsRead == false)
                        .OrderByDescending(n =>
                            n.SentOn)
                        .FirstOrDefaultAsync();

                if (oldNotification != null)
                {
                    oldNotification.IsRead = true;
                }

                // =================================================
                // CREATE STUDENT APPROVAL MESSAGE
                // =================================================

                var studentNotification = new Notification
                {
                    StudentId = leave.StudentId,
                    TeacherId = teacher.TeacherId,
                    Title = "Leave Approved",
                    Message =
                        $"{teacher.TeacherName} approved your leave for {leave.LeaveDate:dd MMM yyyy}.",
                    IsRead = false,
                    SentOn = DateTime.Now
                };

                _context.Notifications.Add(
                    studentNotification);

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    message =
                        "Leave approved successfully.",
                    leaveId = leave.LeaveId,
                    status = leave.Status,
                    notificationId =
                        studentNotification.NotificationId,
                    teacherId = leave.TeacherId
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message =
                        "Unable to approve leave.",
                    error =
                        ex.InnerException?.Message ??
                        ex.Message
                });
            }
        }

        // =========================================================
        // REJECT LEAVE
        // =========================================================

        [HttpPut("{leaveId:int}/reject")]
        public async Task<IActionResult> RejectLeave(
            int leaveId,
            [FromBody] LeaveDecisionRequest? request)
        {
            try
            {
                var leave =
                    await _context.LeaveApplications
                        .FirstOrDefaultAsync(l =>
                            l.LeaveId == leaveId);

                if (leave == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message =
                            "Leave request not found."
                    });
                }

                if (leave.Status != "Pending")
                {
                    return BadRequest(new
                    {
                        success = false,
                        message =
                            "This leave request has already been processed."
                    });
                }

                // =================================================
                // FIND TEACHER
                // =================================================

                var teacher =
                    await _context.Teachers
                        .FirstOrDefaultAsync(t =>
                            t.TeacherId == leave.TeacherId);

                if (teacher == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message =
                            "Teacher not found."
                    });
                }

                // =================================================
                // REJECT
                // =================================================

                leave.Status = "Rejected";

                leave.TeacherRemark =
                    string.IsNullOrWhiteSpace(
                        request?.Remark)
                    ? "Rejected by teacher."
                    : request!.Remark.Trim();

                // =================================================
                // GET STUDENT NAME
                // =================================================

                string studentName =
                    await GetStudentName(
                        leave.StudentId);

                // =================================================
                // MARK ORIGINAL TEACHER NOTIFICATION AS READ
                // =================================================

                string expectedMessage =
                    $"{studentName} has submitted a leave request for {leave.LeaveDate:dd MMM yyyy}.";

                var oldNotification =
                    await _context.Notifications
                        .Where(n =>
                            n.TeacherId == leave.TeacherId &&
                            n.StudentId == leave.StudentId &&
                            n.Title == "New Leave Request" &&
                            n.Message == expectedMessage &&
                            n.IsRead == false)
                        .OrderByDescending(n =>
                            n.SentOn)
                        .FirstOrDefaultAsync();

                if (oldNotification != null)
                {
                    oldNotification.IsRead = true;
                }

                // =================================================
                // CREATE STUDENT REJECTION MESSAGE
                // =================================================

                var studentNotification = new Notification
                {
                    StudentId = leave.StudentId,
                    TeacherId = teacher.TeacherId,
                    Title = "Leave Rejected",
                    Message =
                        $"{teacher.TeacherName} rejected your leave for {leave.LeaveDate:dd MMM yyyy}.",
                    IsRead = false,
                    SentOn = DateTime.Now
                };

                _context.Notifications.Add(
                    studentNotification);

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    message =
                        "Leave rejected successfully.",
                    leaveId = leave.LeaveId,
                    status = leave.Status,
                    notificationId =
                        studentNotification.NotificationId,
                    teacherId = leave.TeacherId
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message =
                        "Unable to reject leave.",
                    error =
                        ex.InnerException?.Message ??
                        ex.Message
                });
            }
        }

        // =========================================================
        // STUDENT DELETE / HIDE LEAVE
        // =========================================================
        //
        // Student ki My Leave se remove hogi.
        // Database se actual record delete nahi hoga.
        // Teacher ki Leave Requests me leave rahegi.
        //
        // Pending / Approved / Rejected:
        // teeno status ki leave student hide kar sakta hai.
        // =========================================================

        [HttpDelete("student/{leaveId:int}/{studentId:int}")]
        public async Task<IActionResult> DeleteStudentLeave(
            int leaveId,
            int studentId)
        {
            try
            {
                if (leaveId <= 0)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Invalid leave ID."
                    });
                }

                if (studentId <= 0)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Invalid student ID."
                    });
                }

                var leave =
                    await _context.LeaveApplications
                        .FirstOrDefaultAsync(l =>
                            l.LeaveId == leaveId &&
                            l.StudentId == studentId);

                if (leave == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message =
                            "Leave request not found."
                    });
                }

                // IMPORTANT:
                // Actual database row delete nahi hogi.
                // Sirf My Leave se hide hogi.

                leave.IsDeletedByStudent = true;

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    message =
                        "Leave removed from My Leave successfully."
                });
            }
            catch (DbUpdateException ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message =
                        "Unable to remove leave from My Leave.",
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
                        "Unable to remove leave from My Leave.",
                    error =
                        ex.InnerException?.Message ??
                        ex.Message
                });
            }
        }

        // =========================================================
        // OLD DELETE LEAVE
        // =========================================================
        //
        // Existing endpoint preserved.
        // Student app is NOT using this endpoint.
        //
        // Student ke My Leave delete ke liye upar wala
        // student/{leaveId}/{studentId} endpoint use hoga.
        // =========================================================

        [HttpDelete("{leaveId:int}")]
        public async Task<IActionResult> DeleteLeave(
            int leaveId)
        {
            try
            {
                var leave =
                    await _context.LeaveApplications
                        .FirstOrDefaultAsync(l =>
                            l.LeaveId == leaveId);

                if (leave == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message =
                            "Leave request not found."
                    });
                }

                // =================================================
                // MARK TEACHER NOTIFICATION AS READ
                // BEFORE DELETE
                // =================================================

                if (leave.Status == "Pending")
                {
                    string studentName =
                        await GetStudentName(
                            leave.StudentId);

                    string expectedMessage =
                        $"{studentName} has submitted a leave request for {leave.LeaveDate:dd MMM yyyy}.";

                    var notification =
                        await _context.Notifications
                            .Where(n =>
                                n.TeacherId == leave.TeacherId &&
                                n.StudentId == leave.StudentId &&
                                n.Title == "New Leave Request" &&
                                n.Message == expectedMessage &&
                                n.IsRead == false)
                            .OrderByDescending(n =>
                                n.SentOn)
                            .FirstOrDefaultAsync();

                    if (notification != null)
                    {
                        notification.IsRead = true;
                    }
                }

                // =================================================
                // ACTUAL DELETE
                // =================================================

                _context.LeaveApplications.Remove(
                    leave);

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    message =
                        "Leave deleted successfully."
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message =
                        "Unable to delete leave.",
                    error =
                        ex.InnerException?.Message ??
                        ex.Message
                });
            }
        }

        // =========================================================
        // GET STUDENT NAME
        // =========================================================

        private async Task<string> GetStudentName(
            int studentId)
        {
            return await _context.Students
                .Where(s =>
                    s.StudentId == studentId)
                .Select(s =>
                    s.FullName)
                .FirstOrDefaultAsync()
                ?? "Student";
        }

        // =========================================================
        // GET TEACHER NAME
        // =========================================================

        private string GetTeacherName(
            object teacher)
        {
            try
            {
                var teacherType =
                    teacher.GetType();

                var fullNameProperty =
                    teacherType.GetProperty("FullName");

                if (fullNameProperty != null)
                {
                    var fullName =
                        fullNameProperty.GetValue(teacher)
                        ?.ToString();

                    if (!string.IsNullOrWhiteSpace(fullName))
                    {
                        return fullName.Trim();
                    }
                }

                var nameProperty =
                    teacherType.GetProperty("Name");

                if (nameProperty != null)
                {
                    var name =
                        nameProperty.GetValue(teacher)
                        ?.ToString();

                    if (!string.IsNullOrWhiteSpace(name))
                    {
                        return name.Trim();
                    }
                }

                var teacherNameProperty =
                    teacherType.GetProperty("TeacherName");

                if (teacherNameProperty != null)
                {
                    var name =
                        teacherNameProperty.GetValue(teacher)
                        ?.ToString();

                    if (!string.IsNullOrWhiteSpace(name))
                    {
                        return name.Trim();
                    }
                }

                return "Teacher";
            }
            catch
            {
                return "Teacher";
            }
        }
    }

    // =============================================================
    // APPLY LEAVE REQUEST
    // =============================================================

    public class ApplyLeaveRequest
    {
        public int StudentId { get; set; }

        public int TeacherId { get; set; }

        public DateTime LeaveDate { get; set; }

        public string LeaveType { get; set; } =
            "Full Day";

        public string Reason { get; set; } =
            string.Empty;
    }

    // =============================================================
    // TEACHER DECISION REQUEST
    // =============================================================

    public class LeaveDecisionRequest
    {
        public string? Remark { get; set; }
    }
}