using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Sentry.OpenTelemetry;

namespace Common.Observability;

/// <summary>
/// Provides extension methods to register and configure application-wide
/// observability features such as OpenTelemetry tracing, metrics,
/// logging, and Sentry integration.
/// </summary>
public static class ObservabilityExtension
{
    /// <summary>
    /// Adds OpenTelemetry services (tracing, metrics, and logging) to the DI container.
    /// Reads configuration from <see cref="ObservabilityOptions"/> and wires up exporters
    /// to the specified OpenTelemetry Collector, including optional Sentry integration.
    /// </summary>
    /// <param name="services">The service collection to extend.</param>
    /// <param name="openTelemetryOptions">Settings that control exporters,
    /// service name/version, and environment flags.</param>
    /// <returns>The same <see cref="IServiceCollection"/> instance for chaining.</returns>
    public static IServiceCollection AddObservability(
        this IServiceCollection services,
        ObservabilityOptions openTelemetryOptions)
    {
        if (!openTelemetryOptions.IsEnabled)
            return services;

        services.AddOpenTelemetry()
            .ConfigureResource(resourceBuilder =>
            {
                resourceBuilder.AddService(
                    serviceName: openTelemetryOptions.ServiceName,
                    serviceVersion: openTelemetryOptions.ServiceVersion);

                resourceBuilder.AddAttributes(new List<KeyValuePair<string, object>>
                {
                    new("deployment.environment", openTelemetryOptions.ServiceEnv)
                });
            })
            .WithTracing(opts =>
            {
                if (openTelemetryOptions.IsDevelopment)
                    opts.SetSampler(new AlwaysOnSampler());

                opts
                    .AddSource(openTelemetryOptions.ServiceName)
                    .SetErrorStatusOnException()
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddEntityFrameworkCoreInstrumentation()
                    .AddMassTransitInstrumentation()
                    .AddSentry()
                    .AddOtlpExporter(options =>
                    {
                        options.Endpoint =
                            new Uri($"{openTelemetryOptions.OtelCollectorGrpcUri}/v1/traces");
                        options.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.Grpc;
                        options.Headers =
                            $"Authorization=Basic {Convert.ToBase64String(Encoding.ASCII.GetBytes(openTelemetryOptions.OtelCollectorBasicAuth))}";
                    });
            })
            .WithMetrics(opts =>
            {
                opts
                    .AddMeter(openTelemetryOptions.ServiceName)
                    .AddAspNetCoreInstrumentation()
                    .AddRuntimeInstrumentation()
                    .AddProcessInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddOtlpExporter(options =>
                    {
                        options.Endpoint =
                            new Uri($"{openTelemetryOptions.OtelCollectorGrpcUri}/v1/metrics");
                        options.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.Grpc;
                        options.Headers =
                            $"Authorization=Basic {Convert.ToBase64String(Encoding.ASCII.GetBytes(openTelemetryOptions.OtelCollectorBasicAuth))}";
                    });
            })
            .WithLogging(opts =>
            {
                if (openTelemetryOptions.IsDevelopment)
                    opts.AddConsoleExporter();

                opts
                    .AddOtlpExporter(options =>
                    {
                        options.Endpoint =
                            new Uri($"{openTelemetryOptions.OtelCollectorGrpcUri}/v1/logs");
                        options.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.Grpc;
                        options.Headers =
                            $"Authorization=Basic {Convert.ToBase64String(Encoding.ASCII.GetBytes(openTelemetryOptions.OtelCollectorBasicAuth))}";
                    });
            });

        return services;
    }


    public static ILoggingBuilder AddSentryMonitor(
        this ILoggingBuilder logging,
        SentryMonitorOptions options)
    {
        // Add Sentry, mapping from options
        logging.AddSentry(sentryOpts =>
        {
            // Only set DSN if provided
            if (!string.IsNullOrWhiteSpace(options.Dsn))
            {
                sentryOpts.Dsn = options.Dsn;
            }
            
            sentryOpts.Debug = options.Debug;

            sentryOpts.MinimumEventLevel = options.MinimumEventLevel;
            sentryOpts.MinimumBreadcrumbLevel = options.MinimumBreadcrumbLevel;

            if (options.EnableTracing)
            {
                sentryOpts.TracesSampleRate = options.TracesSampleRate;
            }
            else
            {
                sentryOpts.TracesSampleRate = 0;
            }

            if (!string.IsNullOrEmpty(options.Environment))
                sentryOpts.Environment = options.Environment;

            if (!string.IsNullOrEmpty(options.Release))
                sentryOpts.Release = options.Release;

            sentryOpts.SendDefaultPii = options.SendDefaultPii;
            sentryOpts.AttachStacktrace = options.AttachStacktrace;

            if (options.ShutdownTimeout.HasValue)
            {
                sentryOpts.ShutdownTimeout = options.ShutdownTimeout.Value;
            }


            if (options.EnableOpenTelemetry)
            {
                sentryOpts.UseOpenTelemetry();
            }
            
            
        });

        return logging;
    }
}