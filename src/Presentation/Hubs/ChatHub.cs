using Microsoft.AspNetCore.SignalR;

namespace ARABULUCU.Web.Hubs
{
    /// <summary>
    /// Basit SignalR hub: gerçek uygulamada yetkilendirme, odalar ve mesaj kaydı ekleyin.
    /// Bu hub örneği tüm bağlı kullanıcılara mesaj yayınlar.
    /// </summary>
    public class ChatHub : Hub
    {
        // Örnek: tüm bağlı kullanıcılara mesaj yayınla
        /// <summary>
        /// Bağlı tüm kullanıcılara gönderilen mesajı yayınlar.
        /// JavaScript tarafında ReceiveMessage isimli method dinlenmelidir.
        /// </summary>
        public async Task SendMessage(string user, string message)
        {
            await Clients.All.SendAsync("ReceiveMessage", user, message);
        }
    }
}
