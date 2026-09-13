using AttendanceManaagmentAPI.Data;
using AttendanceManaagmentAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AttendanceManaagmentAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StudentController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public StudentController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ==========================================
        // GET ALL STUDENTS
        // ==========================================

        [HttpGet]
        public async Task<IActionResult> GetStudents()
        {
            var students = await _context.Students
                .Where(s => s.IsActive != false)
                .OrderBy(s => s.FullName)
                .ToListAsync();

            return Ok(students);
        }

        // ==========================================
        // GET STUDENT
        // ==========================================

        [HttpGet("{id}")]
        public async Task<IActionResult> GetStudent(int id)
        {
            var student = await _context.Students
                .FirstOrDefaultAsync(s =>
                    s.StudentId == id);

            if (student == null)
            {
                return NotFound(new
                {
                    message = "Student not found."
                });
            }

            return Ok(student);
        }
        // ==========================================
        // GET STUDENT BY BARCODE
        // ==========================================

        [HttpGet("by-barcode/{barcode}")]
        public async Task<IActionResult> GetStudentByBarcode(string barcode)
        {
            if (string.IsNullOrWhiteSpace(barcode))
            {
                return BadRequest(new
                {
                    message = "Barcode is required."
                });
            }

            string code = barcode.Trim();

            var student = await _context.Students
                .FirstOrDefaultAsync(s =>
                    s.Barcode != null &&
                    s.Barcode.Trim() == code &&
                    s.IsActive != false);

            if (student == null)
            {
                return NotFound(new
                {
                    message = "No active student found for this barcode."
                });
            }

            return Ok(new
            {
                studentId = student.StudentId,
                fullName = student.FullName,
                barcode = student.Barcode,
                enrollmentNo = student.EnrollmentNo,
                semester = student.Semester
            });
        }

        // ==========================================
        // ADD STUDENT
        // ==========================================

        [HttpPost]
        public async Task<IActionResult> AddStudent(Student student)
        {
            if (string.IsNullOrWhiteSpace(student.FullName))
            {
                return BadRequest(new
                {
                    message = "Student name is required."
                });
            }

            if (string.IsNullOrWhiteSpace(student.Email))
            {
                return BadRequest(new
                {
                    message = "Student email is required."
                });
            }

            if (string.IsNullOrWhiteSpace(student.EnrollmentNo))
            {
                return BadRequest(new
                {
                    message = "Enrollment number is required."
                });
            }

            string email =
                student.Email.Trim().ToLower();

            string enrollment =
                student.EnrollmentNo.Trim().ToLower();

            // ==========================================
            // CHECK USERS
            // ==========================================

            var existingUser =
                await _context.Users
                    .FirstOrDefaultAsync(u =>
                        u.Email.ToLower() == email);

            if (existingUser != null)
            {
                return Conflict(new
                {
                    message =
                        $"This email is already registered as {existingUser.Role}."
                });
            }

            // ==========================================
            // CHECK STUDENTS
            // ==========================================

            var existingStudent =
                await _context.Students
                    .FirstOrDefaultAsync(s =>
                        s.Email != null &&
                        s.Email.ToLower() == email);

            if (existingStudent != null)
            {
                return Conflict(new
                {
                    message =
                        "This email is already registered for a student."
                });
            }

            // ==========================================
            // CHECK ENROLLMENT
            // ==========================================

            var existingEnrollment =
                await _context.Students
                    .FirstOrDefaultAsync(s =>
                        s.EnrollmentNo != null &&
                        s.EnrollmentNo.ToLower() == enrollment);

            if (existingEnrollment != null)
            {
                return Conflict(new
                {
                    message =
                        "This enrollment number already exists."
                });
            }

            // ==========================================
            // CREATE USER
            // ==========================================

            var newUser = new User
            {
                Name = student.FullName.Trim(),
                Email = student.Email.Trim(),
                Password = "",
                Role = "Student",
                isVerified = false
            };

            _context.Users.Add(newUser);

            await _context.SaveChangesAsync();

            // ==========================================
            // CREATE STUDENT
            // ==========================================

            student.StudentId = 0;
            student.UserId = newUser.UserId;

            student.FullName =
                student.FullName.Trim();

            student.Email =
                student.Email.Trim();

            student.Password = null;
            student.IsActive = true;
            student.CreatedAt = DateTime.Now;

            _context.Students.Add(student);

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Student added successfully.",
                studentId = student.StudentId,
                userId = newUser.UserId,
                role = "Student",
                student = student
            });
        }

        // ==========================================
        // UPDATE STUDENT
        // ==========================================

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateStudent(
            int id,
            Student updatedStudent)
        {
            var student =
                await _context.Students
                    .FirstOrDefaultAsync(s =>
                        s.StudentId == id);

            if (student == null)
            {
                return NotFound(new
                {
                    message = "Student not found."
                });
            }

            student.FullName =
                updatedStudent.FullName;

            student.Email =
                updatedStudent.Email;

            student.MobileNo =
                updatedStudent.MobileNo;

            student.EnrollmentNo =
                updatedStudent.EnrollmentNo;

            student.Barcode =
                updatedStudent.Barcode;

            student.Semester =
                updatedStudent.Semester;

            student.IsActive =
                updatedStudent.IsActive;

            // ==========================================
            // UPDATE USER
            // ==========================================

            if (student.UserId.HasValue)
            {
                var user =
                    await _context.Users
                        .FirstOrDefaultAsync(u =>
                            u.UserId ==
                            student.UserId.Value);

                if (user != null)
                {
                    user.Name =
                        updatedStudent.FullName;

                    user.Email =
                        updatedStudent.Email;

                    user.Role = "Student";
                }
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message =
                    "Student updated successfully.",

                student = student
            });
        }

        // ==========================================
        // DELETE STUDENT PERMANENTLY
        // ==========================================

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteStudent(int id)
        {
            using var transaction =
                await _context.Database.BeginTransactionAsync();

            try
            {
                var student = await _context.Students
                    .FirstOrDefaultAsync(s => s.StudentId == id);

                if (student == null)
                {
                    return NotFound(new
                    {
                        message = "Student not found."
                    });
                }

                // ==========================================
                // FIND LINKED USER
                // ==========================================

                User? user = null;

                if (student.UserId.HasValue)
                {
                    user = await _context.Users
                        .FirstOrDefaultAsync(u =>
                            u.UserId == student.UserId.Value);
                }

                // ==========================================
                // DELETE LEAVE APPLICATIONS
                // ==========================================

                var leaves = await _context.LeaveApplications
                    .Where(l => l.StudentId == id)
                    .ToListAsync();

                if (leaves.Count > 0)
                {
                    _context.LeaveApplications.RemoveRange(leaves);
                }

                // ==========================================
                // DELETE NOTIFICATIONS
                // ==========================================

                var notifications = await _context.Notifications
                    .Where(n => n.StudentId == id)
                    .ToListAsync();

                if (notifications.Count > 0)
                {
                    _context.Notifications.RemoveRange(notifications);
                }

                // ==========================================
                // DELETE ATTENDANCE RECORDS
                // ==========================================

                var attendance = await _context.Attendances
                    .Where(a => a.StudentId == id)
                    .ToListAsync();

                if (attendance.Count > 0)
                {
                    _context.Attendances.RemoveRange(attendance);
                }

                // ==========================================
                // DELETE STUDENT
                // ==========================================

                _context.Students.Remove(student);

                // ==========================================
                // DELETE LOGIN USER
                // ==========================================

                if (user != null)
                {
                    _context.Users.Remove(user);
                }

                // ==========================================
                // SAVE
                // ==========================================

                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                return Ok(new
                {
                    message =
                        "Student deleted successfully from database."
                });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();

                return StatusCode(500, new
                {
                    message = "Student delete failed.",
                    error = ex.InnerException?.InnerException?.Message
                            ?? ex.InnerException?.Message
                            ?? ex.Message
                });
            }
        }
        // ==========================================
        // APPLY LEAVE
        // ==========================================

        [HttpPost("apply-leave")]
        public async Task<IActionResult> ApplyLeave(
            [FromBody] LeaveApplication leave)
        {
            // ==========================================
            // VALIDATE STUDENT
            // ==========================================

            var student = await _context.Students
                .FirstOrDefaultAsync(s =>
                    s.StudentId == leave.StudentId &&
                    s.IsActive != false);

            if (student == null)
            {
                return NotFound(new
                {
                    message = "Student not found."
                });
            }

            // ==========================================
            // VALIDATE TEACHER
            // ==========================================

            var teacher = await _context.Teachers
                .FirstOrDefaultAsync(t =>
                    t.TeacherId == leave.TeacherId);

            if (teacher == null)
            {
                return NotFound(new
                {
                    message = "Teacher not found."
                });
            }

            // ==========================================
            // VALIDATE DATE
            // ==========================================

            if (leave.LeaveDate == default)
            {
                return BadRequest(new
                {
                    message = "Leave date is required."
                });
            }

            if (leave.LeaveDate.Date < DateTime.Today)
            {
                return BadRequest(new
                {
                    message =
                        "Leave date cannot be in the past."
                });
            }

            // ==========================================
            // VALIDATE REASON
            // ==========================================

            if (string.IsNullOrWhiteSpace(leave.Reason))
            {
                return BadRequest(new
                {
                    message =
                        "Leave reason is required."
                });
            }

            // ==========================================
            // CHECK DUPLICATE LEAVE
            // ==========================================

            bool alreadyApplied =
                await _context.LeaveApplications
                    .AnyAsync(l =>
                        l.StudentId == leave.StudentId &&
                        l.LeaveDate.Date ==
                        leave.LeaveDate.Date &&
                        l.Status != "Rejected");

            if (alreadyApplied)
            {
                return Conflict(new
                {
                    message =
                        "You have already applied for leave on this date."
                });
            }

            // ==========================================
            // CREATE LEAVE
            // ==========================================

            var newLeave = new LeaveApplication
            {
                StudentId = leave.StudentId,
                TeacherId = leave.TeacherId,
                LeaveDate = leave.LeaveDate.Date,
                Reason = leave.Reason.Trim(),
                Status = "Pending",
                TeacherRemark = null,
                AppliedOn = DateTime.Now
            };

            _context.LeaveApplications.Add(newLeave);

            await _context.SaveChangesAsync();

            // ==========================================
            // RESPONSE
            // ==========================================

            return Ok(new
            {
                message =
                    "Leave application submitted successfully.",

                leave = new
                {
                    newLeave.LeaveId,
                    newLeave.StudentId,
                    newLeave.TeacherId,
                    newLeave.LeaveDate,
                    newLeave.Reason,
                    newLeave.Status,
                    newLeave.TeacherRemark,
                    newLeave.AppliedOn
                }
            });
        }
    }
}