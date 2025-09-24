namespace Common.Observability;

/// <summary>
/// Configuration settings for the application's observability features,
/// including OpenTelemetry and tracing integrations.
/// </summary>
public class ObservabilityOptions
{
    /// <summary>
    /// Enables or disables all observability features.
    /// Set to <c>true</c> to activate metrics, tracing, and exporters.
    /// </summary>
    public bool IsEnabled { get; set; } = false;

    /// <summary>
    /// gRPC endpoint of the OpenTelemetry Collector
    /// (e.g., "http://otel-collector:4317").
    /// </summary>
    public string OtelCollectorGrpcUri { get; set; } = string.Empty;

    /// <summary>
    /// Basic authentication header value used when sending data
    /// to the OpenTelemetry Collector, typically "Basic &lt;base64&gt;".
    /// </summary>
    public string OtelCollectorBasicAuth { get; set; } = string.Empty;

    /// <summary>
    /// Logical name of the service that emits telemetry data,
    /// used for tracing and metric identification.
    /// </summary>
    public string ServiceName { get; set; } = "Common.Observability";

    /// <summary>
    /// Service version string (e.g., "1.0.0") reported with telemetry data.
    /// </summary>
    public string ServiceVersion { get; set; } = "1.0.0";

    /// <summary>
    /// Deployment environment of the service (e.g., "Development",
    /// "Staging", or "Production").
    /// </summary>
    public string ServiceEnv { get; set; } = "Development";

    /// <summary>
    /// Indicates whether the current environment is Development.
    /// Used to toggle developer-friendly features such as verbose logging.
    /// </summary>
    public bool IsDevelopment { get; set; } = true;
}