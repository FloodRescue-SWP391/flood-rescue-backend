using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FloodRescue.Services.Interface.Email
{
    public interface IGmailService
    {
        /// <summary>
        /// Gửi thông báo cho citizen
        /// </summary>
        /// <param name="toEmail"></param>
        /// <param name="subject"></param>
        /// <param name="body"></param>
        /// <returns></returns>
        Task<bool> SendGmailAsync(string toEmail, string subject, string body);
    }
}
