using AttendanceManaagmentAPI.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AttendanceManaagmentAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AdminDashboardController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public AdminDashboardController(ApplicationDbContext context)
        {
            _context = context;
        }

        // =====================================================
        // ADMIN DASHBOARD SUMMARY
        // =====================================================

        [HttpGet("summary")]
        public async Task<IActionResult> GetSummary()
        {
            try
            {
                int totalStudents = await _context.Students
                    .CountAsync(s => s.IsActive != false);

                int totalTeachers = await _context.Teachers
                    .CountAsync(t => t.IsActive != false);

                DateTime today = DateTime.Today;

                var todayAttendance = await _context.Attendances
                    .Where(a =>
                        a.AttendanceDate.HasValue &&
                        a.AttendanceDate.Value.Date == today)
                    .ToListAsync();

                int presentToday = todayAttendance
                    .Count(a => a.Status == "Present");

                int absentToday = todayAttendance
                    .Count(a => a.Status == "Absent");

                int totalAttendanceToday =
                    presentToday + absentToday;

                double attendancePercentage =
                    totalAttendanceToday == 0
                        ? 0
                        : (double)presentToday /
                          totalAttendanceToday * 100;

                return Ok(new
                {
                    totalStudents,
                    totalTeachers,
                    presentToday,
                    absentToday,
                    attendancePercentage =
                        Math.Round(attendancePercentage, 2)
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    message = ex.Message
                });
            }
        }

        // =====================================================
        // TODAY'S ATTENDANCE
        // =====================================================

        [HttpGet("today-attendance")]
        public async Task<IActionResult> GetTodayAttendance()
        {
            try
            {
                DateTime today = DateTime.Today;

                var records = await _context.Attendances
                    .Where(a =>
                        a.AttendanceDate.HasValue &&
                        a.AttendanceDate.Value.Date == today)
                    .Join(
                        _context.Students,
                        attendance => attendance.StudentId,
                        student => student.StudentId,
                        (attendance, student) => new
                        {
                            student.FullName,
                            student.EnrollmentNo,
                            attendance.Status,
                            attendance.ScanTime,
                            attendance.Remarks
                        })
                    .OrderBy(x => x.FullName)
                    .ToListAsync();

                return Ok(records);
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    message = ex.Message
                });
            }
        }
    }
}