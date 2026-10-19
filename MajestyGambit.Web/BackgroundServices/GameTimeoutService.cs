using MajestyGambit.Web.Services;

namespace MajestyGambit.Web.BackgroundServices;

public class GameTimeoutService(IServiceScopeFactory scopeFactory, ILogger<GameTimeoutService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var games = scope.ServiceProvider.GetRequiredService<GameService>();
                await games.CheckTimeoutsAsync();
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Timeout sweep failed");
            }

            await Task.Delay(1000, stoppingToken);
        }
    }
}
