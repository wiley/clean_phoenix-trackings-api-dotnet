using Trackings.Infrastructure.Mongo;
using Trackings.Infrastructure.Interface.Mongo;
using Trackings.Services;
using Trackings.Services.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.Core.Events.Diagnostics;
using MongoDB.Driver;
using MongoDB.Bson;
using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Net.Security;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting.Elasticsearch;
using Serilog.Sinks.SystemConsole.Themes;
using Serilog;
using Newtonsoft.Json;
using WLS.Log.LoggerTransactionPattern;
using DarwinAuthorization;
using WLS.Log.LoggerTransactionPattern;
using Confluent.Kafka;
using System.Collections.Generic;
using WLS.KafkaMessenger.Infrastructure.Interface;
using WLS.KafkaMessenger.Infrastructure;
using WLS.KafkaMessenger.Services.Interfaces;
using WLS.KafkaMessenger.Services;
using Trackings.Domain.Utils;

namespace Trackings.API
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddLogging();
            services.AddCors(o => o.AddPolicy("AllowAll", builder =>
            {
                builder.AllowAnyOrigin()
                       .AllowAnyMethod()
                       .AllowAnyHeader();
            }));
            services.AddControllers().AddNewtonsoftJson(options => {
                options.SerializerSettings.DateTimeZoneHandling = DateTimeZoneHandling.Utc;
                options.SerializerSettings.DateFormatString = "yyyy-MM-ddTHH:mm:sszzz";
            });
            services.AddApiVersioning();
            // Add Authentication Schemes for Users API v4 Jwt and ApiKey
            // Add Authorization Policy using OPA
            services.AddDarwinAuthzConfiguration();

            ConfigureOpenAPI(services);
            ConfigureVersioning(services);
            ConfigureLogging(services);
            ConfigureApiBehavior(services);
            ConfigureJsonAttributesFormat(services);

            RegisterServices(services);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseCors("AllowAll");

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(settings =>
                {
                    settings.SwaggerEndpoint("/swagger/v4/swagger.json", "v4");
                    settings.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.List);
                });
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            // Injects AuthenticationContextMiddleware: This middleware will have the information about authentication context, such as hasJwt, hasApiKey, userId
            app.UseDarwinAuthenticationContext();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }

        private void ConfigureOpenAPI(IServiceCollection services)
        {
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v4", new OpenApiInfo
                {
                    Version = "v4",
                    Title = "Trackings API",
                    Description = "Trackings API",
                });
            });
        }

        private void ConfigureVersioning(IServiceCollection services)
        {
            services.AddVersionedApiExplorer(o =>
            {
                o.GroupNameFormat = "'v'VVV";
                o.SubstituteApiVersionInUrl = true;
            });
            services.AddApiVersioning(options =>
            {
                options.DefaultApiVersion = new ApiVersion(4, 0);
                options.AssumeDefaultVersionWhenUnspecified = true;
                options.ReportApiVersions = true;
            });
        }

        private static void RegisterServices(IServiceCollection services)
        {
            services.AddScoped<ITrackingsService, TrackingsService>();
            services.AddScoped<IMongoHealthCheckService, MongoHealthCheckService>();
            services.AddScoped<ILoggerStateFactory, LoggerStateFactory>();
            services.AddScoped(typeof(IPaginationService<>), typeof(PaginationService<>));

            services.AddSingleton<IKafkaMessengerService, KafkaMessengerService>();
            string host = Environment.GetEnvironmentVariable("KAFKA_HOST");
            var senders = new List<KafkaSender>
            {
                new KafkaSender
                {
                    Topic = Environment.GetEnvironmentVariable("KAFKA_TOPIC")
                }
            };
            services.AddSingleton<IKafkaConfig>(kc =>
                new KafkaConfig() { Host = host, Sender = senders, Source = "trackings-api" }
            );
            services.AddSingleton(p => new ProducerBuilder<string, string>(new ProducerConfig
            {
                BootstrapServers = host
            }).Build());
            
            services.AddSingleton<KafkaMessageChannel>();
            services.AddSingleton<GenerateKafkaEventsMutex>();
            services.AddScoped<IKafkaService, KafkaService>();
            services.AddHostedService<SendKafkaMessageService>();
            services.AddHostedService<GenerateKafkaEventsService>();

            RegisterRepositories(services);
        }

        private static void RegisterRepositories(IServiceCollection services)
        {
            var connString = Environment.GetEnvironmentVariable("MONGO_CONNECTION_STRING");
            var dbName = Environment.GetEnvironmentVariable("MONGO_DB_NAME");
            var tlsCAFilePath = Environment.GetEnvironmentVariable("MONGO_TLS_CA_FILE_PATH");

            if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("MONGO_TLS_CA_FILE_PATH"))) {
                // ADD CA certificate to local trust store
                // DO this once - Maybe when your service starts
                X509Store localTrustStore = new X509Store(StoreName.Root);
                X509Certificate2Collection certificateCollection = new X509Certificate2Collection();
                certificateCollection.Import(tlsCAFilePath);

                try
                {
                    localTrustStore.Open(OpenFlags.ReadWrite);
                    localTrustStore.AddRange(certificateCollection);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Root certificate import failed: " + ex.Message);
                    throw;
                }
                finally
                {
                    Console.WriteLine("Mongo Root certificate imported sucessfully");
                    localTrustStore.Close();
                }
            }

            var settings = MongoClientSettings.FromConnectionString(connString);

            services.AddSingleton<IMongoDBSettings>(_ =>
                new MongoDBSettings()
                {
                    ConnectionString = connString,
                    DatabaseName = dbName
                });
            services.AddSingleton<IMongoClient>(_ => new MongoClient(settings));
            services.AddScoped<IConnectionThrottlingPipeline, ConnectionThrottlingPipeline>();
            services.AddScoped<IMongoTestConnection, MongoTestConnection>();
            services.AddScoped(typeof(IMongoRepository<>), typeof(MongoRepository<>));
        }

        private void ConfigureLogging(IServiceCollection services)
        {
            /* Switching to using "Serilog" log provider for everything
                NOTE: Call to ClearProviders() is what turns off the default Console Logging
                Output to the Console is now controlled by the WriteTo format below
                DevOps can control the Log output with environment variables
                    LOG_MINIMUMLEVEL - values like INFORMATION, WARNING, ERROR
                    LOG_JSON - true means to output log to console in JSON format
            */
            LogLevel level = LogLevel.None;
            var serilogLevel = new LoggingLevelSwitch
            {
                MinimumLevel = LogEventLevel.Information
            };

            if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("LOG_MINIMUMLEVEL")))
            {
                Enum.TryParse(Environment.GetEnvironmentVariable("LOG_MINIMUMLEVEL"), out level);
                LogEventLevel eventLevel = LogEventLevel.Information;
                Enum.TryParse(Environment.GetEnvironmentVariable("LOG_MINIMUMLEVEL"), out eventLevel);
                serilogLevel.MinimumLevel = eventLevel;
            }

            bool useJson = Environment.GetEnvironmentVariable("LOG_JSON") == "true";

            var config = new LoggerConfiguration()
                .Enrich.FromLogContext()
                .ReadFrom.Configuration(Configuration);

            if (useJson)
                config.WriteTo.Console(new ElasticsearchJsonFormatter());
            else
                config.WriteTo.Console(outputTemplate: "[{Timestamp:MM-dd-yyyy HH:mm:ss.SSS} {Level:u3}] {Message:lj} {TransactionID}{NewLine}{Exception}", theme: SystemConsoleTheme.Literate);

            if (level != LogLevel.None)
                config.MinimumLevel.ControlledBy(serilogLevel);

            Log.Logger = config.CreateLogger();

            services.AddLogging(lb =>
            {
                lb.ClearProviders();
                lb.AddSerilog();
                lb.AddDebug(); //Write to VS Output window (controlled by appsettings "Logging" section)
            });
        }

        private void ConfigureApiBehavior(IServiceCollection services)
        {
            services.Configure<ApiBehaviorOptions>(options =>
            {
                options.SuppressModelStateInvalidFilter = true;
            });
        }

        private void ConfigureJsonAttributesFormat(IServiceCollection services)
        {
            services.AddControllers().AddNewtonsoftJson(options =>
                {
                    options.SerializerSettings.DateTimeZoneHandling = DateTimeZoneHandling.Utc;
                    options.SerializerSettings.DateFormatString = "yyyy-MM-ddTHH:mm:sszzz";

                }
            );
        }
    }
}
