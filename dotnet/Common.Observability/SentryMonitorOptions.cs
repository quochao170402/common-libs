using Microsoft.Extensions.Logging;

namespace Common.Observability;


/// <summary>
/// Configuration options for integrating Sentry with the application.
/// Bind this class from <c>appsettings.*.json</c> or environment variables
/// to control Sentry’s logging, tracing, and telemetry behavior.
/// Only the <see cref="Dsn"/> is required to enable Sentry;
/// other fields fine-tune error reporting, performance monitoring,
/// and privacy preferences.
/// </summary>
/// <remarks arks>
/// See the offic ial Sentry .NET SDK options documentation for full details:
/// https://docs.sentry.io/platforms/dotnet/configuration/options/
/// </remarks> 
public class SentryMonitorOptions
{
    #region Required / Core

    /// <summary>
    /// The Sentry Data Source Name (DSN) that uniquely identifies
    /// your Sentry project.
    /// If left empty or <c>null</c>, Sentry disables itself and
    /// no events are sent, but the application continues running normally.
    /// </summary>
    public string Dsn { get; set; } = string.Empty;

    /// <summary>
    /// Enables detailed diagnostic output from the Sentry SDK itself.
    /// Useful during development or troubleshooting but should be
    /// disabled in production to avoid verbose logs.
    /// </summary>
    public bool Debug { get; set; } = false;

    /// <summary>
    /// Determines how stack traces are collected and formatted.
    /// Use <see cref="StackTraceMode.Original"/> to send the
    /// original .NET stack trace, or <see cref="StackTraceMode.Enhanced"/>
    /// for additional Sentry processing.
    /// </summary>
    public StackTraceMode StackTraceMode { get; set; } = StackTraceMode.Original;

    #endregion

    #region Logging & Event Filtering

    /// <summary>
    /// Minimum log level of events that will be sent to Sentry.
    /// For example, <see cref="LogLevel.Error"/> sends only errors
    /// and above, while <see cref="LogLevel.Warning"/> includes warnings.
    /// </summary>
    public LogLevel MinimumEventLevel { get; set; } = LogLevel.Error;

    /// <summary>
    /// Minimum log level for messages to be added as Sentry
    /// breadcrumbs (contextual trail) even if they are not
    /// sent as full events.
    /// </summary>
    public LogLevel MinimumBreadcrumbLevel { get; set; } = LogLevel.Information;

    #endregion

    #region Event Sampling

    /// <summary>
    /// Configures the sample rate for error events, in the range of 0.0 to 1.0.
    /// The default is 1.0, which means that 100% of error events will be sent.
    /// If set to 0.1, only 10% of error events will be sent. Events are picked randomly.
    /// </summary>
    public double SampleRate { get; set; } = 1.0;

    #endregion

    #region Performance / Tracing

    /// <summary>
    /// When <c>true</c>, enables Sentry Performance Monitoring
    /// and distributed tracing for transactions and spans.
    /// </summary>
    public bool EnableTracing { get; set; } = false;

    /// <summary>
    /// Fraction of transactions to sample when tracing is enabled.
    /// Range is 0.0 (disable) to 1.0 (100% sampling).
    /// Ignored if <see cref="EnableTracing"/> is <c>false</c>.
    /// </summary>
    public double TracesSampleRate { get; set; } = 1.0;

    #endregion

    #region Context / Metadata

    /// <summary>
    /// Identifies the environment in which the application is running,
    /// such as "Development", "Staging", or "Production".
    /// Helps segment issues by deployment environment.
    /// </summary>
    public string? Environment { get; set; } = "Development";

    /// <summary>
    /// Identifies the version of the application that generated the event,
    /// such as a semantic version (e.g., "1.2.3") or a build/commit hash.
    /// Sentry uses this value to group issues by release, track deployments,
    /// detect regressions, and show when an error was first introduced.
    /// If omitted, Sentry will attempt to auto-detect the release or skip
    /// release-specific features.
    /// </summary>
    public string? Release { get; set; }

    #endregion

    #region Security / Privacy

    /// <summary>
    /// When <c>true</c>, Sentry will include personally identifiable
    /// information (PII) such as usernames or emails when available.
    /// Keep <c>false</c> in most cases to protect user privacy.
    /// </summary>
    public bool SendDefaultPii { get; set; } = false;

    #endregion

    #region Advanced / Error Handling

    /// <summary>
    /// If <c>true</c>, attaches the current stack trace to messages
    /// captured without an exception, which can aid in debugging.
    /// </summary>
    public bool AttachStacktrace { get; set; } = false;

    /// <summary>
    /// Optional timeout for flushing pending Sentry events
    /// when the application shuts down. If <c>null</c>,
    /// Sentry’s internal default is used.
    /// </summary>
    public TimeSpan? ShutdownTimeout { get; set; }

    /// <summary>
    /// When <c>true</c>, forwards OpenTelemetry spans to Sentry
    /// so distributed traces collected by an OpenTelemetry collector
    /// appear alongside Sentry events.
    /// </summary>
    public bool EnableOpenTelemetry { get; set; } = false;

    #endregion
}
