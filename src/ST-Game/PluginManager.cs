using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.DependencyInjection;
using SurfTimer.ST_DB;

namespace SurfTimer.ST_Game;

public class PluginManager : IPluginServiceCollection<SurfTimer>
{
    public void ConfigureServices(IServiceCollection serviceCollection)
    {
        serviceCollection.AddScoped<GameManager>();
        serviceCollection.AddScoped<TimerDatabase>();
    }
}