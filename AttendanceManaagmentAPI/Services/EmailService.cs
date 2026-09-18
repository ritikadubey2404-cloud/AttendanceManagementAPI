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
                Environment.GetEnvironmentVariable("BREVO_API_KEY");

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                throw new Exception(
                    "BREVO_API_KEY environment variable is not configured.");
            }

            string? senderEmail =
                Environment.GetEnvironmentVariable("BREVO_EMAIL");

            if (string.IsNullOrWhiteSpace(senderEmail))
            {
                throw new Exception(
                    "BREVO_EMAIL environment variable is not configured.");
            }

            var emailData = new
            {
                sender = new
                {
                    name = "Attendance Management",
                    email = senderEmail
                },

                to = new[]
                {
                new
                {
                    email = toEmail
                }
            },

                subject = subject,
                textContent = body
            };

            string json =
                JsonSerializer.Serialize(emailData);

            using var request =
                new HttpRequestMessage(
                    HttpMethod.Post,
                    "https://api.brevo.com/v3/smtp/email");

            request.Headers.Add("api-key", apiKey);

            request.Headers.Accept.Add(
                new MediaTypeWithQualityHeaderValue(
                    "application/json"));

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
                    $"Brevo email failed. " +
                    $"Status: {(int)response.StatusCode} " +
                    $"{response.StatusCode}. " +
                    $"Response: {responseBody}");
            }
        }
    }


}
