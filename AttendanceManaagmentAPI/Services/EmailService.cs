using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace AttendanceManaagmentAPI.Services
{
    public class EmailService
    {
        private readonly HttpClient _httpClient;


    public EmailService()
        {
            _httpClient = new HttpClient();
        }

        public async Task SendEmailAsync(
            string toEmail,
            string subject,
            string body)
        {
            string? apiKey =
                Environment.GetEnvironmentVariable("RESEND_API_KEY");

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                throw new Exception(
                    "RESEND_API_KEY environment variable is not configured.");
            }

            var request = new HttpRequestMessage(
                HttpMethod.Post,
                "https://api.resend.com/emails");

            request.Headers.Authorization =
                new AuthenticationHeaderValue(
                    "Bearer",
                    apiKey);

            var emailData = new
            {
                from = "Attendance Management <onboarding@resend.dev>",
                to = new[] { toEmail },
                subject = subject,
                text = body
            };

            string json =
                JsonSerializer.Serialize(emailData);

            request.Content =
                new StringContent(
                    json,
                    Encoding.UTF8,
                    "application/json");

            HttpResponseMessage response =
                await _httpClient.SendAsync(request);

            string responseBody =
                await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception(
                    $"Resend email failed. " +
                    $"Status: {(int)response.StatusCode} " +
                    $"{response.StatusCode}. " +
                    $"Response: {responseBody}");
            }
        }
    }


}
