using Trackings.Services;
using Trackings.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace Trackings.UnitTest
{
    public class DIServices
    {
        public ServiceProvider GenerateDependencyInjection()
        {
            var services = new ServiceCollection();
            services.AddScoped(typeof(IPaginationService<>), typeof(PaginationService<>));

            return services
                .AddLogging()
                .BuildServiceProvider();
        }
    }
}