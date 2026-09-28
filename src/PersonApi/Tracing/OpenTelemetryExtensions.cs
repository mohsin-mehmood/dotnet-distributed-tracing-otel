using Azure.Monitor.OpenTelemetry.AspNetCore;
using OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using System.Diagnostics;
namespace PersonApi.Tracing
{
	public static class OpenTelemetryExtensions
	{
		public static string ServiceName { get; } //cloud_RoleName
		public static string ServiceInstanceId { get; } //cloud_RoleInstance
		public static string ServiceVersion { get; }

		static OpenTelemetryExtensions()
		{
			ServiceName = typeof(OpenTelemetryExtensions).Assembly.GetName().Name!;
			ServiceInstanceId = typeof(OpenTelemetryExtensions).Assembly.GetName().GetHashCode().ToString();
			ServiceVersion = typeof(OpenTelemetryExtensions).Assembly.GetName().Version!.ToString();
		}

		public static ActivitySource CreateActivitySource() =>
			new ActivitySource(ServiceName, ServiceVersion);

		public static IServiceCollection AddOpenTelemetryPilars(this IServiceCollection serviceCollection, IConfiguration configuration, ILoggingBuilder loggingBuilder)
		{
			var aspireAgentHost = configuration["Aspire:AgentHost"];
			var aspireAgentPort = configuration["Aspire:AgentPort"];
			var appInsightsConnString = configuration["AppInsightsConnectionString"];
			Uri aspireOtlpExporterUrl = new($"http://{aspireAgentHost}:{aspireAgentPort}");

			var openTelemetryResourceBuilder = ResourceBuilder.CreateDefault()
					.AddService(serviceName: OpenTelemetryExtensions.ServiceName,
								serviceInstanceId: OpenTelemetryExtensions.ServiceInstanceId,
								serviceVersion: OpenTelemetryExtensions.ServiceVersion);

			serviceCollection.AddOpenTelemetry()
				.ConfigureResource(resource => resource.AddService(serviceName: OpenTelemetryExtensions.ServiceName,
								serviceInstanceId: OpenTelemetryExtensions.ServiceInstanceId,
								serviceVersion: OpenTelemetryExtensions.ServiceVersion))
				.WithTracing(tracing =>
				{
					tracing.AddSource(OpenTelemetryExtensions.ServiceName)
						.AddOtlpExporter(exporter => exporter.Endpoint = aspireOtlpExporterUrl)
						.AddConsoleExporter();
				})
				.WithMetrics(metrics =>
				{
					metrics
						.AddAspNetCoreInstrumentation()
						.AddHttpClientInstrumentation()
						.AddOtlpExporter(exporter => exporter.Endpoint = aspireOtlpExporterUrl);
				})
				.WithLogging(logging =>
				{
					logging.AddOtlpExporter(exporter => exporter.Endpoint = aspireOtlpExporterUrl);
				})				
				.UseAzureMonitor(endpoint => endpoint.ConnectionString = appInsightsConnString);


			return serviceCollection;
		}
	}
}