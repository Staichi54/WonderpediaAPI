using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace WonderpediaAPI.Services
{
    public class EmailService
    {
        private readonly IConfiguration _configuration;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task EnviarCorreoAsync(string destinatario, string asunto, string cuerpo)
        {
            string host = _configuration["Smtp:Host"]!;
            int port = int.Parse(_configuration["Smtp:Port"]!);
            string user = _configuration["Smtp:User"]!;
            string password = _configuration["Smtp:Password"]!;
            string from = _configuration["Smtp:From"]!;
            string fromName = _configuration["Smtp:FromName"]!;

            MimeMessage mensaje = new MimeMessage();

            mensaje.From.Add(new MailboxAddress(fromName, from));
            mensaje.To.Add(MailboxAddress.Parse(destinatario));
            mensaje.Subject = asunto;

            mensaje.Body = new TextPart("plain")
            {
                Text = cuerpo
            };

            using SmtpClient smtp = new SmtpClient();

            await smtp.ConnectAsync(host, port, SecureSocketOptions.StartTls);
            await smtp.AuthenticateAsync(user, password);
            await smtp.SendAsync(mensaje);
            await smtp.DisconnectAsync(true);
        }
    }
}