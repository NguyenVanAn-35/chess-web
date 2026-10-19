using MajestyGambit.Web.Entities;

namespace MajestyGambit.Web.Models.Lobby;

public class LobbyViewModel
{
    public string Username { get; set; } = "";
    public int EloRating { get; set; }
    public string TimeControl { get; set; } = "blitz";
    public int TimeLimitSeconds { get; set; } = 300;
    public string ColorPreference { get; set; } = "random";
    public string JoinCode { get; set; } = "";
}

public class CreateRoomForm
{
    public string TimeControl { get; set; } = "blitz";
    public int TimeLimitSeconds { get; set; } = 300;
    public string ColorPreference { get; set; } = "random";
}
