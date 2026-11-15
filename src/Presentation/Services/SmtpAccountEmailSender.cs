using System.Net;
using System.Net.Mail;

namespace ARABULUCU.Web.Services;

public sealed class SmtpAccountEmailSender(IConfiguration configuration) : IAccountEmailSender
{
    public async Task SendPasswordResetLinkAsync(string email, string resetUrl)
    {
        var host = configuration["Email:Smtp:Host"];
        var username = configuration["Email:Smtp:UserName"];
        var password = configuration["Email:Smtp:Password"];
        var fromAddress = configuration["Email:Smtp:FromAddress"];
        var fromName = configuration["Email:Smtp:FromName"] ?? "ARABULUCU";

        if (string.IsNullOrWhiteSpace(host)
            || string.IsNullOrWhiteSpace(username)
            || string.IsNullOrWhiteSpace(password)
            || string.IsNullOrWhiteSpace(fromAddress))
        {
            throw new InvalidOperationException(
                "SMTP is not configured. Set Email:Smtp:Host, UserName, Password, and FromAddress.");
        }

        if (!int.TryParse(configuration["Email:Smtp:Port"], out var port) || port is < 1 or > 65535)
        {
            throw new InvalidOperationException("Email:Smtp:Port must be a valid TCP port.");
        }

        using var message = new MailMessage
        {
            From = new MailAddress(fromAddress, fromName),
            Subject = "ARABULUCU şifre sıfırlama",
            Body = $"""
                <p>Şifrenizi yenilemek için aşağıdaki bağlantıya tıklayın:</p>
                <p><a href="{WebUtility.HtmlEncode(resetUrl)}">Şifremi yenile</a></p>
                <p>Bu isteği siz yapmadıysanız bu e-postayı yok sayabilirsiniz.</p>
                """,
            IsBodyHtml = true
        };
        message.To.Add(new MailAddress(email));

        using var client = new SmtpClient(host, port)
        {
            EnableSsl = true,
            Credentials = new NetworkCredential(username, password)
        };

        await client.SendMailAsync(message);
    }
}
