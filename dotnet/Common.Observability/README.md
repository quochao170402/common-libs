# Herond.Common.Observability

`Herond.Common.Observability` is a .NET library providing common observability utilities for logging, metrics, and distributed tracing. It is designed to help developers instrument their applications for better monitoring and diagnostics.

## Features

- Unified logging abstractions
- Metrics collection and reporting
- Distributed tracing support
- Extensible interfaces for custom observability providers

## Installation

Install via NuGet:

```shell
dotnet add package Herond.Common.Observability
```

## Usage for Error logging (Sentry)

```csharp
builder.Logging.AddSentryMonitor(dsn: "dsn_of_Sentry_project");
```

## Usage for Observability

```csharp
builder.Services.AddObservability(options)
```

## License

This project is licensed under the MIT License.