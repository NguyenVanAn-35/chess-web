using System.Security.Claims;
using Microsoft.AspNetCore.SignalR;

namespace MajestyGambit.Web.Hubs;

public class NameUserIdProvider : IUserIdProvider
{
    public string? GetUserId(HubConnectionContext connection)
        => connection.User?.FindFirstValue(ClaimTypes.NameIdentifier);
}
