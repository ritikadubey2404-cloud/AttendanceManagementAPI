using MailKit.Net.Smtp;
using MimeKit;
using MailKit.Security;
using System.Threading.Tasks;

namespace AttendanceManaagmentAPI.Services
{
    public class EmailService
    {
        public async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("Attendance Management", "ritikadubey2404@gmail.com"));
            message.To.Add(MailboxAddress.Parse(toEmail));
            message.Subject = subject;
            message.Body = new TextPart("plain")
            {
                Text = body
            };
            using var client = new SmtpClient();
            client.Timeout = 10000;

            try
            {
                await client.ConnectAsync("smtp.gmail.com", 587, SecureSocketOptions.StartTls);
                await client.AuthenticateAsync("ritikadubey2404@gmail.com", "bkomcsjupgfhmskt");
                await client.SendAsync(message);
                await client.DisconnectAsync(true);
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
                throw;
            }

        }

    }
}
