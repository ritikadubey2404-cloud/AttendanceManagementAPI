using AttendanceManaagmentAPI.Data;
using AttendanceManaagmentAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AttendanceManaagmentAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AttendanceController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private static DateTime GetIndiaTime()
        {
            TimeZoneInfo indiaTimeZone =
            TimeZoneInfo.FindSystemTimeZoneById("Asia/Kolkata");


            return TimeZoneInfo.ConvertTimeFromUtc(
                DateTime.UtcNow,
                indiaTimeZone);


}



        public AttendanceController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ============================================================
        // AUTOMATIC ABSENT AFTER 01:00 PM
        // ============================================================
        private async Task MarkAutomaticAbsentsAsync()
        {
            DateTime now = GetIndiaTime();
            DateTime today = now.Date;


            // Before 01:00 PM -> do nothing
            if (now.TimeOfDay < new TimeSpan(13, 0, 0))
            {
                return;
            }

            var students = await _context.Students
                .Where(s => s.IsActive == true)
                .ToListAsync();

            if (students.Count == 0)
            {
                return;
            }

            var todayAttendance = await _context.Attendances
                .Where(a =>
                    a.AttendanceDate.HasValue &&
                    a.AttendanceDate.Value.Date == today)
                .ToListAsync();

            var approvedLeaves = await _context.LeaveApplications
                .Where(l =>
                    l.LeaveDate.Date == today &&
                    l.Status == "Approved")
                .Select(l => l.StudentId)
                .Distinct()
                .ToListAsync();

            foreach (var student in students)
            {
                bool alreadyPresent = todayAttendance.Any(a =>
                    a.StudentId == student.StudentId &&
                    string.Equals(
                        a.Status,
                        "Present",
                        StringComparison.OrdinalIgnoreCase));

                bool onApprovedLeave =
                    approvedLeaves.Contains(student.StudentId);

                if (alreadyPresent || onApprovedLeave)
                {
                    continue;
                }

                bool alreadyAbsent = todayAttendance.Any(a =>
                    a.StudentId == student.StudentId &&
                    string.Equals(
                        a.Status,
                        "Absent",
                        StringComparison.OrdinalIgnoreCase));

                if (alreadyAbsent)
                {
                    continue;
                }

                var absentRecord = new Attendance
                {
                    StudentId = student.StudentId,
                    TeacherId = null,
                    AttendanceDate = today,
                    ScanTime = null,
                    Status = "Absent",
                    Remarks = "Automatically marked absent after 01:00 PM"
                };

                _context.Attendances.Add(absentRecord);
            }

            await _context.SaveChangesAsync();
        }

        // ============================================================
        // MONTHLY ATTENDANCE
        // ============================================================
        [HttpGet("monthly/{studentId}")]
        public async Task<IActionResult> GetMonthlyAttendance(
            int studentId,
            [FromQuery] int? month = null,
            [FromQuery] int? year = null)
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

                DateTime today = DateTime.Today;

                int selectedMonth = month ?? today.Month;
                int selectedYear = year ?? today.Year;

                if (selectedMonth < 1 || selectedMonth > 12)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Invalid month."
                    });
                }

                if (selectedYear < 2000 || selectedYear > 2100)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Invalid year."
                    });
                }

                bool studentExists = await _context.Students
                    .AnyAsync(s => s.StudentId == studentId);

                if (!studentExists)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "Student not found."
                    });
                }

                DateTime monthStart = new DateTime(
                    selectedYear,
                    selectedMonth,
                    1);

                DateTime nextMonth = monthStart.AddMonths(1);

                var records = await _context.Attendances
                    .Where(a =>
                        a.StudentId == studentId &&
                        a.AttendanceDate.HasValue &&
                        a.AttendanceDate.Value >= monthStart &&
                        a.AttendanceDate.Value < nextMonth)
                    .ToListAsync();

                int presentDays = records
                    .Where(a =>
                        string.Equals(
                            a.Status,
                            "Present",
                            StringComparison.OrdinalIgnoreCase))
                    .Where(a => a.AttendanceDate.HasValue)
                    .Select(a => a.AttendanceDate!.Value.Date)
                    .Distinct()
                    .Count();

                int absentDays = records
                    .Where(a =>
                        string.Equals(
                            a.Status,
                            "Absent",
                            StringComparison.OrdinalIgnoreCase))
                    .Where(a => a.AttendanceDate.HasValue)
                    .Select(a => a.AttendanceDate!.Value.Date)
                    .Distinct()
                    .Count();

                int totalDays = DateTime.DaysInMonth(
                    selectedYear,
                    selectedMonth);

                double percentage = totalDays == 0
                    ? 0
                    : (double)presentDays / totalDays * 100;

                return Ok(new
                {
                    success = true,
                    studentId = studentId,
                    month = selectedMonth,
                    year = selectedYear,
                    monthName = monthStart.ToString("MMMM yyyy"),
                    presentDays = presentDays,
                    absentDays = absentDays,
                    totalDays = totalDays,
                    attendancePercentage = Math.Round(
                        percentage,
                        2)
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Unable to load monthly attendance.",
                    error = ex.Message,
                    innerError = ex.InnerException?.Message
                });
            }
        }

        // ============================================================
        // STUDENT DASHBOARD ATTENDANCE
        // ============================================================
        [HttpGet("student-dashboard/{studentId}")]
        public async Task<IActionResult> GetStudentDashboardAttendance(
            int studentId)
        {
            try
            {
                await MarkAutomaticAbsentsAsync();

                if (studentId <= 0)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Invalid student ID."
                    });
                }

                DateTime today = DateTime.Today;

                DateTime monthStart = new DateTime(
                    today.Year,
                    today.Month,
                    1);

                DateTime nextDay = today.AddDays(1);

                var records = await _context.Attendances
                    .Where(a =>
                        a.StudentId == studentId &&
                        a.AttendanceDate.HasValue &&
                        a.AttendanceDate.Value >= monthStart &&
                        a.AttendanceDate.Value < nextDay)
                    .OrderByDescending(a => a.AttendanceDate)
                    .ThenByDescending(a => a.ScanTime)
                    .ToListAsync();

                int presentDays = records
                    .Where(a =>
                        string.Equals(
                            a.Status,
                            "Present",
                            StringComparison.OrdinalIgnoreCase))
                    .Select(a => a.AttendanceDate!.Value.Date)
                    .Distinct()
                    .Count();

                int absentDays = records
                    .Where(a =>
                        string.Equals(
                            a.Status,
                            "Absent",
                            StringComparison.OrdinalIgnoreCase))
                    .Select(a => a.AttendanceDate!.Value.Date)
                    .Distinct()
                    .Count();

                int daysInCurrentMonth = DateTime.DaysInMonth(
                    today.Year,
                    today.Month);

                int totalDays = daysInCurrentMonth;

                double percentage = daysInCurrentMonth == 0
                    ? 0
                    : (double)presentDays /
                      daysInCurrentMonth * 100;

                var todayRecord = records
                    .Where(a =>
                        a.AttendanceDate.HasValue &&
                        a.AttendanceDate.Value.Date == today)
                    .OrderByDescending(a => a.ScanTime)
                    .FirstOrDefault();

                return Ok(new
                {
                    success = true,
                    presentDays = presentDays,
                    absentDays = absentDays,
                    totalDays = totalDays,
                    attendancePercentage = Math.Round(
                        percentage,
                        2),
                    todayStatus = todayRecord?.Status,
                    todayScanTime = todayRecord?.ScanTime,
                    todayAttendanceDate = todayRecord?.AttendanceDate
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Unable to load student attendance.",
                    error = ex.Message,
                    innerError = ex.InnerException?.Message
                });
            }
        }

        // ============================================================
        // ALL ATTENDANCE LOGS
        // ============================================================
        [HttpGet("logs")]
        public async Task<IActionResult> GetAttendanceLogs()
        {
            try
            {
                var records = await _context.Attendances
                    .Join(
                        _context.Students,
                        attendance => attendance.StudentId,
                        student => student.StudentId,
                        (attendance, student) => new
                        {
                            attendance.AttendanceId,
                            student.StudentId,
                            StudentName = student.FullName,
                            student.EnrollmentNo,
                            attendance.AttendanceDate,
                            attendance.ScanTime,
                            attendance.Status,
                            attendance.TeacherId,
                            attendance.Remarks
                        })
                    .OrderByDescending(a => a.AttendanceDate)
                    .ThenByDescending(a => a.ScanTime)
                    .ToListAsync();

                return Ok(records);
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    message = "Unable to load attendance records.",
                    error = ex.Message,
                    innerError = ex.InnerException?.Message
                });
            }
        }

        // ============================================================
        // TEACHER ATTENDANCE LOGS
        // ============================================================
        [HttpGet("logs/teacher/{teacherId}")]
        public async Task<IActionResult> GetTeacherAttendanceLogs(
 int teacherId)
        {
            try
            {
                await MarkAutomaticAbsentsAsync();


    if (teacherId <= 0)
                {
                    return BadRequest(new
                    {
                        message = "Invalid teacher ID."
                    });
                }

                var teacherExists = await _context.Teachers
                    .AnyAsync(t => t.TeacherId == teacherId);

                if (!teacherExists)
                {
                    return NotFound(new
                    {
                        message = "Teacher not found."
                    });
                }

                // ============================================================
                // ALL STUDENTS ADDED BY ADMIN
                // ============================================================

                var students = await _context.Students
                    .AsNoTracking()
                    .Where(s => s.IsActive == true)
                    .Select(s => new
                    {
                        s.StudentId,
                        StudentName = s.FullName,
                        s.EnrollmentNo
                    })
                    .OrderBy(s => s.StudentName)
                    .ToListAsync();

                // ============================================================
                // TODAY'S ATTENDANCE
                // ============================================================

                DateTime today = GetIndiaTime().Date;
                DateTime tomorrow = today.AddDays(1);

                var attendanceRecords = await _context.Attendances
                    .AsNoTracking()
                    .Where(a =>
                        a.AttendanceDate.HasValue &&
                        a.AttendanceDate.Value >= today &&
                        a.AttendanceDate.Value < tomorrow)
                    .Select(a => new
                    {
                        a.AttendanceId,
                        a.StudentId,
                        a.AttendanceDate,
                        a.ScanTime,
                        a.Status,
                        a.TeacherId,
                        a.Remarks
                    })
                    .ToListAsync();

                // ============================================================
                // EVERY ADMIN STUDENT WILL BE SHOWN
                // ============================================================

                var records = students
                    .Select(student =>
                    {
                        var attendance = attendanceRecords
                            .Where(a => a.StudentId == student.StudentId)
                            .OrderByDescending(a => a.ScanTime)
                            .FirstOrDefault();

                        return new
                        {
                            AttendanceId =
                                attendance?.AttendanceId ?? 0,

                            StudentId =
                                student.StudentId,

                            StudentName =
                                student.StudentName,

                            EnrollmentNo =
                                student.EnrollmentNo,

                            AttendanceDate =
                                attendance?.AttendanceDate,

                            ScanTime =
                                attendance?.ScanTime,

                            Status =
                                attendance?.Status ?? "Not Marked",

                            TeacherId =
                                attendance?.TeacherId,

                            Remarks =
                                attendance?.Remarks
                        };
                    })
                    .ToList();

                return Ok(records);
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    message =
                        "Unable to load teacher attendance records.",

                    error = ex.Message,

                    innerError =
                        ex.InnerException?.Message
                });
            }


}

        // ============================================================
        // MARK ATTENDANCE BY BARCODE
        // ============================================================
        [HttpPost("mark-by-barcode")]
        public async Task<IActionResult> MarkAttendanceByBarcode(
            [FromBody] BarcodeAttendanceRequest request)
        {
            try
            {
                if (request == null ||
                    string.IsNullOrWhiteSpace(request.Barcode))
                {
                    return BadRequest(new
                    {
                        message = "Barcode is required."
                    });
                }

                if (request.TeacherId <= 0)
                {
                    return BadRequest(new
                    {
                        message = "Invalid teacher ID."
                    });
                }

                string barcode = request.Barcode.Trim();

                var student = await _context.Students
                    .AsNoTracking()
                    .FirstOrDefaultAsync(s => s.Barcode == barcode);

                if (student == null)
                {
                    return NotFound(new
                    {
                        message = "Student not found for this ID card."
                    });
                }

                var teacherExists = await _context.Teachers
                    .AsNoTracking()
                    .AnyAsync(t => t.TeacherId == request.TeacherId);

                if (!teacherExists)
                {
                    return NotFound(new
                    {
                        message = "Teacher not found."
                    });
                }

                DateTime indiaNow = GetIndiaTime();

                DateTime attendanceDate = indiaNow.Date;
                DateTime scanTime = indiaNow;


                // ====================================================
                // AFTER 01:00 PM -> NEW ATTENDANCE CANNOT BE MARKED
                // ====================================================
                if (scanTime.TimeOfDay >= new TimeSpan(13, 0, 0))
                {
                    return Ok(new
                    {
                        success = false,
                        attendanceTimeOver = true,
                        message = "Attendance marking time is over.",
                        studentId = student.StudentId,
                        studentName = student.FullName,
                        barcode = student.Barcode,
                        status = "Not Marked",
                        attendanceDate = attendanceDate
                    });
                }

                DateTime nextDay = attendanceDate.AddDays(1);

                // ====================================================
                // CHECK IF STUDENT IS ALREADY PRESENT TODAY
                // ====================================================
                var alreadyPresent = await _context.Attendances
                    .AnyAsync(a =>
                        a.StudentId == student.StudentId &&
                        a.AttendanceDate.HasValue &&
                        a.AttendanceDate.Value >= attendanceDate &&
                        a.AttendanceDate.Value < nextDay &&
                        a.Status == "Present");

                if (alreadyPresent)
                {
                    return Ok(new
                    {
                        success = true,
                        alreadyPresent = true,
                        message =
                            $"{student.FullName} is already Present today.",
                        studentId = student.StudentId,
                        studentName = student.FullName,
                        barcode = student.Barcode,
                        status = "Present",
                        attendanceDate = attendanceDate
                    });
                }

                // ====================================================
                // BEFORE 01:00 PM -> MARK PRESENT
                // ====================================================
                var attendance = new Attendance
                {
                    StudentId = student.StudentId,
                    TeacherId = request.TeacherId,
                    AttendanceDate = attendanceDate,
                    ScanTime = scanTime,
                    Status = "Present",
                    Remarks = "Attendance marked by ID card scan"
                };

                _context.Attendances.Add(attendance);

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    message = $"{student.FullName} Present",
                    studentId = student.StudentId,
                    studentName = student.FullName,
                    barcode = student.Barcode,
                    status = "Present",
                    attendanceDate = attendance.AttendanceDate,
                    scanTime = attendance.ScanTime
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Unable to mark attendance.",
                    error = ex.Message,
                    innerError = ex.InnerException?.Message
                });
            }
        }
        // ============================================================
        // MARK ATTENDANCE
        // ============================================================
        [HttpPost("mark")]
        public async Task<IActionResult> MarkAttendance(
            [FromBody] MarkAttendanceRequest request)
        {
            try
            {
                if (request == null)
                {
                    return BadRequest(new
                    {
                        message =
                            "Attendance request is required."
                    });
                }

                if (request.StudentId <= 0)
                {
                    return BadRequest(new
                    {
                        message = "Invalid student ID."
                    });
                }

                if (request.TeacherId <= 0)
                {
                    return BadRequest(new
                    {
                        message = "Invalid teacher ID."
                    });
                }

                var student = await _context.Students
                    .FirstOrDefaultAsync(
                        s => s.StudentId == request.StudentId);

                if (student == null)
                {
                    return NotFound(new
                    {
                        message = "Student not found."
                    });
                }

                var teacher = await _context.Teachers
                    .FirstOrDefaultAsync(
                        t => t.TeacherId == request.TeacherId);

                if (teacher == null)
                {
                    return NotFound(new
                    {
                        message = "Teacher not found."
                    });
                }

                DateTime indiaNow = GetIndiaTime();

                DateTime attendanceDate =
                request.AttendanceDate == default
                ? indiaNow.Date
                : request.AttendanceDate.Date;

                DateTime scanTime =
                request.ScanTime == default
                ? indiaNow
                : request.ScanTime;


                // Prevent manual attendance after 01:00 PM
                if (scanTime.TimeOfDay >= new TimeSpan(13, 0, 0))
                {
                    return Ok(new
                    {
                        success = false,
                        attendanceTimeOver = true,
                        message =
                            "Attendance marking time is over.",
                        studentId = student.StudentId,
                        studentName = student.FullName,
                        status = "Not Marked",
                        attendanceDate = attendanceDate
                    });
                }

                string status =
                    string.IsNullOrWhiteSpace(request.Status)
                        ? "Present"
                        : request.Status;

                var attendance = new Attendance
                {
                    StudentId = request.StudentId,
                    TeacherId = request.TeacherId,
                    AttendanceDate = attendanceDate,
                    ScanTime = scanTime,
                    Status = status,
                    Remarks = request.Remarks
                };

                _context.Attendances.Add(attendance);

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    message =
                        $"{student.FullName} Present",
                    studentId = student.StudentId,
                    studentName = student.FullName,
                    barcode = student.Barcode,
                    status = status,
                    attendanceDate =
                        attendance.AttendanceDate,
                    scanTime =
                        attendance.ScanTime
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Unable to mark attendance.",
                    error = ex.Message,
                    innerError = ex.InnerException?.Message
                });
            }
        }

        // ============================================================
        // TEACHER MONTHLY STUDENT-WISE ATTENDANCE
        // ============================================================
        [HttpGet("monthly/teacher/{teacherId}")]
        public async Task<IActionResult> GetTeacherMonthlyAttendance(
            int teacherId,
            [FromQuery] int? month = null,
            [FromQuery] int? year = null)
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

                DateTime today = DateTime.Today;

                int selectedMonth = month ?? today.Month;
                int selectedYear = year ?? today.Year;

                if (selectedMonth < 1 || selectedMonth > 12)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Invalid month."
                    });
                }

                if (selectedYear < 2000 || selectedYear > 2100)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Invalid year."
                    });
                }

                bool teacherExists = await _context.Teachers
                    .AnyAsync(t => t.TeacherId == teacherId);

                if (!teacherExists)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "Teacher not found."
                    });
                }

                DateTime monthStart = new DateTime(
                    selectedYear,
                    selectedMonth,
                    1);

                DateTime nextMonth = monthStart.AddMonths(1);

                var students = await _context.Students
                    .AsNoTracking()
                    .Select(s => new
                    {
                        s.StudentId,
                        s.FullName,
                        s.EnrollmentNo
                    })
                    .OrderBy(s => s.FullName)
                    .ToListAsync();

                var attendanceRecords = await _context.Attendances
                    .AsNoTracking()
                    .Where(a =>
                        a.TeacherId == teacherId &&
                        a.AttendanceDate.HasValue &&
                        a.AttendanceDate.Value >= monthStart &&
                        a.AttendanceDate.Value < nextMonth)
                    .Select(a => new
                    {
                        a.StudentId,
                        a.AttendanceDate,
                        a.Status
                    })
                    .ToListAsync();

                var studentAttendance = students
                    .Select(student =>
                    {
                        var studentRecords = attendanceRecords
                            .Where(a =>
                                a.StudentId == student.StudentId &&
                                a.AttendanceDate.HasValue)
                            .ToList();

                        int presentDays = studentRecords
                            .Where(a =>
                                string.Equals(
                                    a.Status,
                                    "Present",
                                    StringComparison.OrdinalIgnoreCase))
                            .Select(a =>
                                a.AttendanceDate!.Value.Date)
                            .Distinct()
                            .Count();

                        int absentDays = studentRecords
                            .Where(a =>
                                string.Equals(
                                    a.Status,
                                    "Absent",
                                    StringComparison.OrdinalIgnoreCase))
                            .Select(a =>
                                a.AttendanceDate!.Value.Date)
                            .Distinct()
                            .Count();

                        return new
                        {
                            studentId = student.StudentId,
                            fullName = student.FullName,
                            enrollmentNo = student.EnrollmentNo,

                            presentDays = presentDays,
                            absentDays = absentDays,
                            presentToday = studentRecords.Any(a =>
                                a.AttendanceDate.HasValue &&
                                a.AttendanceDate.Value.Date == today &&
                                string.Equals(
                                    a.Status,
                                    "Present",
                                    StringComparison.OrdinalIgnoreCase))
                        };
                    })
                    .ToList();

                int totalStudents = studentAttendance.Count;

                int presentStudents = studentAttendance
                    .Count(s => s.presentDays > 0);

                int totalPresentDays = studentAttendance
                    .Sum(s => s.presentDays);

                return Ok(new
                {
                    success = true,

                    teacherId = teacherId,

                    month = selectedMonth,
                    year = selectedYear,

                    monthName = monthStart.ToString("MMMM yyyy"),

                    totalStudents = totalStudents,

                    presentStudents = presentStudents,

                    totalPresentDays = totalPresentDays,

                    students = studentAttendance
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message =
                        "Unable to load teacher monthly attendance.",
                    error = ex.Message,
                    innerError = ex.InnerException?.Message
                });
            }
        }

        // ============================================================
        // REQUEST MODELS
        // ============================================================
        public class BarcodeAttendanceRequest
        {
            public string Barcode { get; set; } = string.Empty;

            public int TeacherId { get; set; }
        }

        public class MarkAttendanceRequest
        {
            public int StudentId { get; set; }

            public int TeacherId { get; set; }

            public DateTime AttendanceDate { get; set; }

            public DateTime ScanTime { get; set; }

            public string Status { get; set; } = "Present";

            public string? Remarks { get; set; }
        }
    }


}



