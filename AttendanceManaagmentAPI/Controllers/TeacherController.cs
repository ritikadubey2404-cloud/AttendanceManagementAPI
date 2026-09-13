using AttendanceManaagmentAPI.Data;
using AttendanceManaagmentAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AttendanceManaagmentAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TeacherController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public TeacherController(ApplicationDbContext context)
        {
            _context = context;
        }

        // =====================================================
        // GET ALL TEACHERS
        // =====================================================

        [HttpGet]
        public async Task<IActionResult> GetTeachers()
        {
            var teachers = await _context.Teachers
                .Where(t => t.IsActive != false)
                .OrderBy(t => t.TeacherName)
                .ToListAsync();

            return Ok(teachers);
        }

        // =====================================================
        // GET TEACHER BY ID
        // =====================================================

        [HttpGet("{id}")]
        public async Task<IActionResult> GetTeacher(int id)
        {
            var teacher = await _context.Teachers
                .FirstOrDefaultAsync(t => t.TeacherId == id);

            if (teacher == null)
            {
                return NotFound(new
                {
                    message = "Teacher not found."
                });
            }

            return Ok(teacher);
        }

        // =====================================================
        // ADD TEACHER
        // =====================================================

        [HttpPost]
        public async Task<IActionResult> AddTeacher(Teacher teacher)
        {
            if (string.IsNullOrWhiteSpace(teacher.TeacherName))
            {
                return BadRequest(new
                {
                    message = "Teacher name is required."
                });
            }

            if (string.IsNullOrWhiteSpace(teacher.Email))
            {
                return BadRequest(new
                {
                    message = "Teacher email is required."
                });
            }

            // Check email in Users
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u =>
                    u.Email.ToLower() ==
                    teacher.Email.Trim().ToLower());

            if (existingUser != null)
            {
                return Conflict(new
                {
                    message =
                        "This email is already registered as " +
                        existingUser.Role +
                        ". Please use a different email."
                });
            }

            // Check email in Teachers
            var existingTeacher = await _context.Teachers
                .FirstOrDefaultAsync(t =>
                    t.Email != null &&
                    t.Email.ToLower() ==
                    teacher.Email.Trim().ToLower());

            if (existingTeacher != null)
            {
                return Conflict(new
                {
                    message =
                        "This email is already registered for a teacher."
                });
            }

            // =================================================
            // CREATE USER ACCOUNT
            // =================================================

            var newUser = new User
            {
                Name = teacher.TeacherName.Trim(),
                Email = teacher.Email.Trim(),
                Password = "",
                Role = "Teacher",
                isVerified = false
            };

            _context.Users.Add(newUser);

            await _context.SaveChangesAsync();

            // =================================================
            // CREATE TEACHER RECORD
            // =================================================

            teacher.TeacherId = 0;
            teacher.TeacherName = teacher.TeacherName.Trim();
            teacher.Email = teacher.Email.Trim();
            teacher.CreatedAt = DateTime.Now;
            teacher.IsActive = true;
            teacher.UserId = newUser.UserId;

            _context.Teachers.Add(teacher);

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Teacher added successfully.",
                teacherId = teacher.TeacherId,
                userId = newUser.UserId,
                role = "Teacher",
                teacher = teacher
            });
        }

        // =====================================================
        // UPDATE TEACHER
        // =====================================================

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateTeacher(
            int id,
            Teacher updatedTeacher)
        {
            var teacher = await _context.Teachers
                .FirstOrDefaultAsync(t =>
                    t.TeacherId == id);

            if (teacher == null)
            {
                return NotFound(new
                {
                    message = "Teacher not found."
                });
            }

            // =================================================
            // CHECK EMAIL USED BY ANOTHER USER
            // =================================================

            var emailExists = await _context.Users
                .FirstOrDefaultAsync(u =>
                    u.Email.ToLower() ==
                    updatedTeacher.Email.Trim().ToLower() &&
                    u.UserId != teacher.UserId);

            if (emailExists != null)
            {
                return Conflict(new
                {
                    message =
                        "This email is already registered as " +
                        emailExists.Role +
                        "."
                });
            }

            // =================================================
            // UPDATE TEACHER
            // =================================================

            teacher.TeacherName =
                updatedTeacher.TeacherName.Trim();

            teacher.Email =
                updatedTeacher.Email.Trim();

            teacher.MobileNo =
                updatedTeacher.MobileNo;

            teacher.Department =
                updatedTeacher.Department;

            teacher.Semester =
                updatedTeacher.Semester;

            teacher.IsActive =
                updatedTeacher.IsActive;

            // =================================================
            // UPDATE USER
            // =================================================

            if (teacher.UserId.HasValue)
            {
                var user = await _context.Users
                    .FirstOrDefaultAsync(u =>
                        u.UserId == teacher.UserId.Value);

                if (user != null)
                {
                    user.Name = teacher.TeacherName;
                    user.Email = teacher.Email;

                    // Teacher page cannot change role
                    user.Role = "Teacher";
                }
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Teacher updated successfully.",
                teacher = teacher
            });
        }

        // =====================================================
        // DELETE TEACHER
        // =====================================================

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTeacher(int id)
        {
            using var transaction =
                await _context.Database.BeginTransactionAsync();

            try
            {
                var teacher = await _context.Teachers
                    .FirstOrDefaultAsync(t =>
                        t.TeacherId == id);

                if (teacher == null)
                {
                    return NotFound(new
                    {
                        message = "Teacher not found."
                    });
                }

                // =================================================
                // FIND LINKED USER
                // =================================================

                User? user = null;

                if (teacher.UserId.HasValue)
                {
                    user = await _context.Users
                        .FirstOrDefaultAsync(u =>
                            u.UserId == teacher.UserId.Value);
                }

                // =================================================
                // DELETE TEACHER'S LEAVE APPLICATIONS
                // =================================================

                var leaves = await _context.LeaveApplications
                    .Where(l => l.TeacherId == id)
                    .ToListAsync();

                if (leaves.Count > 0)
                {
                    _context.LeaveApplications.RemoveRange(leaves);
                }

                // =================================================
                // DELETE TEACHER
                // =================================================

                _context.Teachers.Remove(teacher);

                // =================================================
                // DELETE LOGIN USER
                // =================================================

                if (user != null)
                {
                    _context.Users.Remove(user);
                }

                // =================================================
                // SAVE
                // =================================================

                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                return Ok(new
                {
                    message =
                        "Teacher deleted successfully from database."
                });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();

                return StatusCode(500, new
                {
                    message = "Teacher delete failed.",
                    error = ex.InnerException?.InnerException?.Message
                            ?? ex.InnerException?.Message
                            ?? ex.Message
                });
            }
        }
    }
}