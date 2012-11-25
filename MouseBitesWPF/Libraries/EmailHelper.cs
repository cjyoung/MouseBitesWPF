using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Mail;

namespace LaVie.Libraries
{
    static class EmailHelper
    {
        static public void SendEmail(string login, string password, string body)
        {
            string loginName = login.Split('@')[0];
            var fromAddress = new MailAddress(string.Format("{0}@gmail.com",loginName), "From Name");
            var toAddress = new MailAddress(string.Format("{0}@gmail.com", loginName), "To Name");
            string fromPassword = password;
            string subject = "MouseBites Availability";

            var smtp = new SmtpClient
                       {
                           Host = "smtp.gmail.com",
                           Port = 587,
                           EnableSsl = true,
                           DeliveryMethod = SmtpDeliveryMethod.Network,
                           UseDefaultCredentials = false,
                           Credentials = new NetworkCredential(fromAddress.Address, fromPassword)
                       };
            using (var message = new MailMessage(fromAddress, toAddress)
                    {
                        Subject = subject,
                        Body = body
                    })
            {
                smtp.Send(message);
            }
        }
    }
}
