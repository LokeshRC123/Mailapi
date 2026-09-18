using Microsoft.AspNetCore.Mvc;
using PortfolioContactApi.Models;
using System.Net;
using System.Net.Mail;
using System.Net.Mime;

namespace PortfolioContactApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ContactController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public ContactController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpPost]
        public async Task<IActionResult> SendContactEmail(
            [FromBody] ContactRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var host = _configuration["SmtpSettings:Host"];
                var username = _configuration["SmtpSettings:Username"];
                var password = _configuration["SmtpSettings:Password"];
                var fromEmail = _configuration["SmtpSettings:FromEmail"];
                var toEmail = _configuration["SmtpSettings:ToEmail"];

                var portString = _configuration["SmtpSettings:Port"];

                if (string.IsNullOrWhiteSpace(host) ||
                    string.IsNullOrWhiteSpace(username) ||
                    string.IsNullOrWhiteSpace(password) ||
                    string.IsNullOrWhiteSpace(fromEmail) ||
                    string.IsNullOrWhiteSpace(toEmail) ||
                    !int.TryParse(portString, out int port))
                {
                    return StatusCode(
                        500,
                        new
                        {
                            success = false,
                            message = "SMTP configuration is missing."
                        });
                }

                using var smtpClient = new SmtpClient(host, port)
                {
                    Credentials = new NetworkCredential(
                        username,
                        password
                    ),
                    EnableSsl = true
                };

                var safeName = WebUtility.HtmlEncode(request.Name);
                var safeEmail = WebUtility.HtmlEncode(request.Email);
                var safePhone = WebUtility.HtmlEncode(request.Phone);
                var safeSubject = WebUtility.HtmlEncode(request.Subject);
                var safeDescription = WebUtility.HtmlEncode(request.Description)
                    .Replace("\r\n", "<br>")
                    .Replace("\n", "<br>");

                var emailBody = $"""
                    <html>
                    <body style="font-family: Arial, sans-serif;">
                        <h2>New Contact Form Submission</h2>

                        <p>
                            <strong>Name:</strong>
                            {safeName}
                        </p>

                        <p>
                            <strong>Email:</strong>
                            {safeEmail}
                        </p>

                        <p>
                            <strong>Phone:</strong>
                            {safePhone}
                        </p>

                        <p>
                            <strong>Subject:</strong>
                            {safeSubject}
                        </p>

                        <p>
                            <strong>Description:</strong>
                        </p>

                        <p>{safeDescription}</p>
                    </body>
                    </html>
                    """;

                using var mailMessage = new MailMessage
                {
                    From = new MailAddress(fromEmail),
                    Subject = $"Portfolio Contact: {request.Subject}",
                    Body = emailBody,
                    IsBodyHtml = true
                };

                mailMessage.To.Add(toEmail);

                // When you click Reply in Gmail, it replies
                // directly to the person who submitted the form.
                mailMessage.ReplyToList.Add(
                    new MailAddress(request.Email, request.Name)
                );

                await smtpClient.SendMailAsync(mailMessage);

                return Ok(new
                {
                    success = true,
                    message = "Email sent successfully."
                });
            }
            catch (SmtpException ex)
            {
                return StatusCode(
                    500,
                    new
                    {
                        success = false,
                        message = "Unable to send email.",
                        error = ex.Message
                    });
            }
            catch (Exception ex)
            {
                return StatusCode(
                    500,
                    new
                    {
                        success = false,
                        message = "Something went wrong.",
                        error = ex.Message
                    });
            }
        }
    }
}