using FloodRescue.Services.Interface.Email;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace FloodRescue.Services.Implements.Gmail
{
    public class GmailService : IGmailService
    {
        private readonly ILogger<GmailService> _logger;
        private readonly string _fromEmail;
        private readonly string _appPassword;
        private readonly string _displayName;

        public GmailService(IConfiguration configuration, ILogger<GmailService> logger)
        {
            _logger = logger;   
            var gmailSection = configuration.GetSection("GmailSettings");   

            _fromEmail = gmailSection["FromEmail"] ?? throw new InvalidOperationException("GmailSettings:FromEmail is not configured.");

            _appPassword = gmailSection["AppPassword"] ?? throw new InvalidOperationException("GmailSettings:AppPassword is not configured.");

            _displayName = gmailSection["DisplayName"] ?? "Flood Rescue System";


            _logger.LogInformation("[GmailEmailService] Gmail client initialized. FromEmail: {FromEmail}", _fromEmail);

        }


        /// <summary>
        /// Gửi email qua Gmail SMTP Server
        /// SMTP Host: smtp.gmail.com  , Port: 587, TSL: enable
        /// Auth: Gmail account + App Password
        /// </summary>
        /// <param name="toEmail"></param>
        /// <param name="subject"></param>
        /// <param name="body"></param>
        /// <returns></returns>
        public async Task<bool> SendGmailAsync(string toEmail, string subject, string body)
        {
            _logger.LogInformation("[GmailEmailService] Sending email to {ToEmail}", toEmail);

            try
            {
                MailMessage mail = new MailMessage 
                { 
                    From = new MailAddress(_fromEmail, _displayName), //Người gửi + tên hiển thị
                    Subject = subject, //tiêu đề
                    Body = body, //nội dung
                    IsBodyHtml = true //cho phép gửi HTML nếu cần   
                };

                mail.To.Add(toEmail); //người nhận

                using var smtpClient = new SmtpClient("smtp.gmail.com")
                {
                    Port = 587,  // Port TLS của Gmail
                    Credentials = new NetworkCredential(_fromEmail, _appPassword), // Xác thực bằng email + app password
                    EnableSsl = true,   // Bật TLS encryption
                };

                await smtpClient.SendMailAsync(mail); //gửi email bất đồng bộ

                _logger.LogInformation("[GmailEmailService] Email sent successfully to {ToEmail}", toEmail);

                return true;

            }
            catch(SmtpException ex)
            {
                _logger.LogError(ex,
                  "[GmailEmailService - Error] SMTP error sending email to {ToEmail}. StatusCode: {StatusCode}",
                  toEmail, ex.StatusCode);
                return false;
            }
            catch (Exception ex)
            {
                // Lỗi khác: network, DNS, ...
                _logger.LogError(ex,
                    "[GmailEmailService - Error] Unexpected error sending email to {ToEmail}", toEmail);
                return false;
            }
        }
    }
}
