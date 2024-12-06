using Trackings.Infrastructure.Interface.Mongo;
using Trackings.Infrastructure.Mongo;
using Trackings.Services.Interfaces;
using Trackings.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

namespace Trackings.IntegrationTest
{
    public class DIServices
    {
        public ServiceProvider GenerateDependencyInjection()
        {
            var services = new ServiceCollection();
            RegisterServices(services);

            return services
                .AddLogging()
                .BuildServiceProvider();
        }

        private void RegisterServices(IServiceCollection services)
        {
            services.AddScoped<IDummyService, DummyService>();
            services.AddScoped<IMongoHealthCheckService, MongoHealthCheckService>();
            services.AddScoped(typeof(IPaginationService<>), typeof(PaginationService<>));
            RegisterRepositories(services);
        }

        private void RegisterRepositories(IServiceCollection services)
        {
            var cfg = GetConfiguraiton();
            var connString = cfg.MongoConnectionString;
            var dbName = cfg.MongoDBName;
            services.AddSingleton<IMongoDBSettings>(_ =>
                new MongoDBSettings()
                {
                    ConnectionString = connString,
                    DatabaseName = dbName
                });
            services.AddSingleton<IMongoClient>(_ => new MongoClient(connString));
            services.AddScoped<IConnectionThrottlingPipeline, ConnectionThrottlingPipeline>();
            services.AddScoped<IMongoTestConnection, MongoTestConnection>();
            services.AddScoped(typeof(IMongoRepository<>), typeof(MongoRepository<>));
        }

        private Config GetConfiguraiton()
        {
            var configObj = new Config();
            var config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.test.json")
                .Build();
            config.GetSection("Config").Bind(configObj);
            return configObj;
        }
    }
}