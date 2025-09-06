using System.Diagnostics;
using OpenTelemetry.Trace;

namespace Ipam.DataAccess.Telemetry
{
    internal static class AzureTableInstrumentation
    {
        private static readonly string ActivitySourceName = "Ipam.DataAccess.AzureTable";
        private static readonly ActivitySource ActivitySource = new(ActivitySourceName);

        public static TracerProviderBuilder AddAzureTableClientInstrumentation(
            this TracerProviderBuilder builder)
        {
            return builder.AddSource(ActivitySourceName)
                         .AddProcessor(new AzureTableActivityProcessor());
        }

        public static Activity StartTableOperation(string operation, string table)
        {
            var activity = ActivitySource.StartActivity(
                $"Azure.Table.{operation}",
                ActivityKind.Client);

            if (activity != null)
            {
                activity.SetTag("db.system", "azure_table");
                activity.SetTag("db.name", table);
                activity.SetTag("db.operation", operation);
            }

            return activity;
        }
    }
}
