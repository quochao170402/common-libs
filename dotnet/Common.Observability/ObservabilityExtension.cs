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
    /// Adds OpenTelemetry services—tracing, metrics, and logging—to the
    /// application's dependency injection container.
    /// </summary>
    /// <param name="services">
    /// The <see cref="IServiceCollection"/> to extend.
    /// </param>
    /// <param name="openTelemetryOptions">
    /// Settings that control exporters, service name/version, and environment flags.
    /// Bind this from configuration using <see cref="ObservabilityOptions"/>.
    /// </param>
    /// <returns>
    /// The same <see cref="IServiceCollection"/> instance for chaining calls.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method wires up:
    /// <list type="bullet">
    /// <item>Tracing with ASP.NET Core, HttpClient, Entity Framework Core, and MassTransit instrumentation.</item>
    /// <item>Metrics for runtime, process, and HTTP client.</item>
    /// <item>Logging with OTLP exporter support.</item>
    /// <item>Optional Sentry span/exporter integration if enabled in <paramref name="openTelemetryOptions"/>.</item>
    /// </list>
    /// Exporters are configured to send data to the specified OpenTelemetry Collector
    /// using gRPC and optional basic authentication.
    /// </para>
    /// </remarks>
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


    /// <summary>
    /// Registers Sentry as an <see cref="ILoggerProvider"/> for the application's logging pipeline.
    /// </summary>
    /// <param name="logging">
    /// The <see cref="ILoggingBuilder"/> to extend.
    /// </param>
    /// <param name="options">
    /// Strongly-typed <see cref="SentryMonitorOptions"/> containing the Sentry DSN,
    /// log-level filters, sampling rates, and other configuration settings.
    /// </param>
    /// <returns>
    /// The same <see cref="ILoggingBuilder"/> instance for chaining.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method maps values from <paramref name="options"/> to
    /// <see cref="Sentry.SentryOptions"/> to configure:
    /// <list type="bullet">
    /// <item>Sentry DSN and environment information.</item>
    /// <item>Minimum log levels for events and breadcrumbs.</item>
    /// <item>Optional performance tracing and sample rates.</item>
    /// <item>Stacktrace attachment, shutdown timeout, and PII preferences.</item>
    /// <item>Optional forwarding of OpenTelemetry spans to Sentry.</item>
    /// </list>
    /// </para>
    /// </remarks>
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