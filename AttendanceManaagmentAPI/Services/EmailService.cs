using System.Net;
using System.Net.Mail;

namespace AttendanceManaagmentAPI.Services
{
    public class EmailService
    {
        public async Task SendEmailAsync(
        string toEmail,
        string subject,
        string body)
        {
            string? gmailEmail =
            Environment.GetEnvironmentVariable("ritikadubey2404@gmail.com");

            string? gmailAppPassword =
                Environment.GetEnvironmentVariable("qbqgreikxjchuwzf");

            if (string.IsNullOrWhiteSpace(gmailEmail))
            {
                throw new Exception(
                    "GMAIL_EMAIL environment variable is not configured.");
            }

            if (string.IsNullOrWhiteSpace(gmailAppPassword))
            {
                throw new Exception(
                    "GMAIL_APP_PASSWORD environment variable is not configured.");
            }

            using var mailMessage = new MailMessage();

            mailMessage.From = new MailAddress(
                gmailEmail,
                "Attendance Management");

            mailMessage.To.Add(toEmail);
            mailMessage.Subject = subject;
            mailMessage.Body = body;
            mailMessage.IsBodyHtml = false;

            using var smtpClient = new SmtpClient(
                "smtp.gmail.com",
                587);

            smtpClient.EnableSsl = true;
            smtpClient.Credentials = new NetworkCredential(
                gmailEmail,
                gmailAppPassword);

            await smtpClient.SendMailAsync(mailMessage);
        }
    }

}