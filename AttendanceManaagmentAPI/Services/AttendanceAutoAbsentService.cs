using AttendanceManaagmentAPI.Data;
using AttendanceManaagmentAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace AttendanceManaagmentAPI.Services
{
    public class AttendanceAutoAbsentService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;


    // 1:00 PM
    private static readonly TimeSpan AbsentTime =
        new TimeSpan(13, 0, 0);

        public AttendanceAutoAbsentService(
            IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        protected override async Task ExecuteAsync(
            CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    DateTime now = DateTime.Now;

                    // Sirf 1:00 PM ke baad automatic absent lagana hai
                    if (now.TimeOfDay >= AbsentTime)
                    {
                        await MarkAbsentStudentsAsync(stoppingToken);

                        // Aaj ka kaam ho gaya.
                        // Kal 1:00 PM ke baad dobara check karenge.
                        DateTime tomorrow =
                            DateTime.Today.AddDays(1).AddMinutes(1);

                        TimeSpan waitTime =
                            tomorrow - DateTime.Now;

                        if (waitTime.TotalMilliseconds > 0)
                        {
                            await Task.Delay(
                                waitTime,
                                stoppingToken);
                        }
                    }
                    else
                    {
                        // 1 minute baad dobara time check
                        await Task.Delay(
                            TimeSpan.FromMinutes(1),
                            stoppingToken);
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(
                        $"Automatic Absent Error: {ex}");

                    // Error ke baad 1 minute baad retry
                    await Task.Delay(
                        TimeSpan.FromMinutes(1),
                        stoppingToken);
                }
            }
        }

        private async Task MarkAbsentStudentsAsync(
            CancellationToken cancellationToken)
        {
            using IServiceScope scope =
                _scopeFactory.CreateScope();

            var context =
                scope.ServiceProvider
                    .GetRequiredService<ApplicationDbContext>();

            DateTime today = DateTime.Today;

            // Active students
            var students = await context.Students
                .Where(s => s.IsActive == true)
                .ToListAsync(cancellationToken);

            // Aaj approved leave par students
            var approvedLeaves = await context.LeaveApplications
                .Where(l =>
                    l.LeaveDate.Date == today &&
                    l.Status == "Approved")
                .Select(l => l.StudentId)
                .Distinct()
                .ToListAsync(cancellationToken);

            foreach (var student in students)
            {
                // Approved leave wale student ko Absent mat karo
                if (approvedLeaves.Contains(student.StudentId))
                {
                    continue;
                }

                // Aaj ki attendance check karo
                var todayAttendance =
                    await context.Attendances
                        .FirstOrDefaultAsync(
                            a =>
                                a.StudentId == student.StudentId &&
                                a.AttendanceDate.HasValue &&
                                a.AttendanceDate.Value.Date == today,
                            cancellationToken);

                // Agar aaj koi attendance nahi hai,
                // to automatic Absent create karo.
                if (todayAttendance == null)
                {
                    context.Attendances.Add(
                        new Attendance
                        {
                            StudentId = student.StudentId,
                            TeacherId = null,
                            AttendanceDate = today,
                            ScanTime = null,
                            Status = "Absent",
                            Remarks =
                                "Automatically marked Absent after 01:00 PM"
                        });
                }
            }

            await context.SaveChangesAsync(
                cancellationToken);

            Console.WriteLine(
                $"Automatic Absent check completed for {today:yyyy-MM-dd}");
        }
    }


}
