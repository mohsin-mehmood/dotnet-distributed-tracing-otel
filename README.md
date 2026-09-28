# .NET Distributed Tracing with OpenTelemetry

This sample demonstrates a distributed tracing scenario using .NET, RabbitMQ, SQL Server, and the OpenTelemetry stack. It shows how a request flows from an ASP.NET Core API into a background worker, while trace context is propagated across messaging boundaries and exported to the Aspire dashboard and Azure Monitor.

## Overview

The solution contains two .NET services:

- PersonApi: an ASP.NET Core Web API that accepts a person payload and publishes it to RabbitMQ.
- PersonWorker: a background worker that consumes messages from RabbitMQ, saves the payload to SQL Server, and continues the distributed trace.

The data flow is:

1. A client sends a POST request to the API.
2. The API creates a trace span, adds correlation metadata, and publishes a message.
3. RabbitMQ carries the trace context in message headers.
4. The worker consumes the message, extracts the context, and starts a consumer span.
5. The worker persists the envelope in SQL Server.
6. OpenTelemetry exporters send traces, logs, and metrics to the Aspire dashboard and Azure Monitor.

## Solution structure

- src/PersonApi
  - ASP.NET Core API
  - OpenTelemetry configuration
  - RabbitMQ publisher
  - Person controller and models
- src/PersonWorker
  - Background worker service
  - RabbitMQ consumer
  - SQL persistence via Dapper and SQL Server
  - OpenTelemetry configuration
- docker-compose.yml
  - SQL Server
  - RabbitMQ
  - Aspire dashboard
- docker/
  - Docker utilities and initialization scripts

## Architecture

```text
Client
  |
  v
PersonApi (HTTP POST /Person)
  |  trace + correlationId + message headers
  v
RabbitMQ queue: queue-person
  |  trace context propagated
  v
PersonWorker (consumer)
  |  save Person + envelope details
  v
SQL Server
  |
  v
OpenTelemetry exporters --> Aspire Dashboard / Azure Monitor
```

## Features

- ASP.NET Core API with Swagger support
- RabbitMQ producer/consumer pattern
- W3C trace propagation over message headers
- SQL persistence with Dapper
- OpenTelemetry tracing, metrics, and logs
- Aspire dashboard for local observability
- Azure Monitor exporter configuration

## Prerequisites

Before running the sample, ensure the following are installed:

- .NET 10 SDK
- Docker Desktop or Docker Engine
- Optional: Azure Monitor / Application Insights connection if you want to use the Azure exporter

## Run the infrastructure

From the repository root, start the supporting services:

```bash
docker compose up -d
```

This starts:

- SQL Server on localhost:1433
- RabbitMQ on localhost:5672
- RabbitMQ management UI on localhost:15672
- Aspire dashboard on localhost:18888

## Configure the services

The application settings are defined in:

- src/PersonApi/appsettings.json
- src/PersonWorker/appsettings.json

Key values include:

- RabbitMQ connection: amqp://guest:guest@localhost:5672/
- Queue name: queue-person
- Aspire OTLP endpoint: http://localhost:4317
- SQL connection string for the worker

If you need to change the extra containers or ports, update the Docker Compose configuration before starting the stack.

## Run the API and worker

Open the solution in Visual Studio or run from the command line:

```bash
dotnet restore

dotnet build
```

Then run these projects:

- src/PersonApi
- src/PersonWorker

If using Visual Studio, set both projects as startup projects or use the solution launch configuration.

## Test the flow

Send a sample request to the API:

```http
POST http://localhost:6200/Person
Content-Type: application/json

{
  "name": "Jane Doe",
  "age": 34,
  "email": "jane.doe@example.com",
  "address": "123 Main Street"
}
```

This triggers:

- a producer span in PersonApi
- RabbitMQ message publication
- a consumer span in PersonWorker
- a row insert into SQL Server

## Expected observability

Open the Aspire dashboard at:

- http://localhost:18888

You should be able to inspect:

- traces
- logs
- metrics
- service dependencies
- correlation between API and worker spans

RabbitMQ management is available at:

- http://localhost:15672

Use the default credentials:

- username: guest
- password: guest

## Data model

The payload contains a Person model and a PersonEnvelope metadata wrapper. The worker stores both the person record and the traced envelope metadata in SQL Server.

This is useful for validating:

- correlation ID propagation
- trace continuity across queues
- producer and consumer metadata
- runtime environment and framework tags

## Notes

- The sample uses the OpenTelemetry .NET instrumentation model and OTLP exporters.
- Azure Monitor integration is configured via AppInsightsConnectionString when available.
- This project is meant to be a practical reference for distributed tracing in a message-driven .NET architecture.

## Useful references

- OpenTelemetry .NET: https://opentelemetry.io/docs/instrumentation/net/
- RabbitMQ: https://www.rabbitmq.com/documentation.html
- Aspire dashboard: https://learn.microsoft.com/dotnet/aspire/
- Azure Monitor OpenTelemetry: https://learn.microsoft.com/azure/azure-monitor/app/opentelemetry-overview
