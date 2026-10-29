namespace ARABULUCU.Web.Services;

public interface IAccountEmailSender
{
    Task SendPasswordResetLinkAsync(string email, string resetUrl);
}
