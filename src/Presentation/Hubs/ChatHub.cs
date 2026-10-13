using Microsoft.AspNetCore.SignalR;

namespace ARABULUCU.Web.Hubs
{
    // Basit SignalR hub: gerçek uygulamada yetkilendirme, odalar ve mesaj kaydı ekleyin.
    public class ChatHub : Hub
    {
        // Örnek: tüm bağlı kullanıcılara mesaj yayınla
        public async Task SendMessage(string user, string message)
        {
            await Clients.All.SendAsync("ReceiveMessage", user, message);
        }
    }
}
