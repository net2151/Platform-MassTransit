namespace MassTransit.Platform.Transports.AmazonSqs
{
    using System;
    using Amazon.SimpleNotificationService;
    using Amazon.SQS;
    using AmazonSqsTransport.Configuration;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Options;
    using Serilog;


    public class AmazonSqsStartupBusFactory :
        IStartupBusFactory
    {
        public IBusControl CreateBus(IServiceProvider provider, IStartupBusConfigurator configurator)
        {
            var options = provider.GetRequiredService<IOptions<AmazonSqsOptions>>().Value;

            return Bus.Factory.CreateUsingAmazonSqs(cfg =>
            {
                if (string.IsNullOrWhiteSpace(options.Region))
                {
                    cfg.Host(new UriBuilder("amazonsqs://docker.localhost:4576") {Path = options.Scope}.Uri, h =>
                    {
                        h.AccessKey("admin");
                        h.SecretKey("admin");
                        h.Config(new AmazonSimpleNotificationServiceConfig {ServiceURL = "http://docker.localhost:4575"});
                        h.Config(new AmazonSQSConfig {ServiceURL = "http://docker.localhost:4576"});
                    });
                }
                else
                {
                    cfg.Host(new UriBuilder("amazonsqs://host")
                    {
                        Host = options.Region,
                        Path = options.Scope
                    }.Uri, h =>
                    {
                        h.AccessKey(options.AccessKey);
                        h.SecretKey(options.SecretKey);
                    });
                }

                if (!configurator.TryConfigureQuartz(cfg))
                {
                    Log.Information("Configuring Amazon SQS Message Scheduler");
                    cfg.UseAmazonSqsMessageScheduler();
                }

                configurator.ConfigureBus(cfg, provider);
            });
        }

        public static void Configure(IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<AmazonSqsOptions>(configuration.GetSection("SQS"));
        }
    }
}
