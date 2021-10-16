using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Matchmaking.Services;

namespace Matchmaking
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddOptions();
            services.AddSingleton<IMatchmakingService, MatchmakingService>();
            services.AddMvc().AddNewtonsoftJson();
            services.AddResponseCaching();
        }

        public static void Configure(IApplicationBuilder app)
        {
            app.UseResponseCaching();
            app.UseRouting();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
