namespace TravelAd_Api.Hubs; // Your project name + .Hubs

using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

public class NotificationHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        Console.WriteLine($"Client Connected: {Context.ConnectionId}");
        await base.OnConnectedAsync();
    }
    public async Task SendTestMessage()
    {
        Console.WriteLine("Broadcasting WebSocket Message...");
        await Clients.All.SendAsync("ReceiveLiveCampaignUpdate", 9999);
    }
}