using System.Diagnostics;
using OpenTelemetry;

namespace Ipam.DataAccess.Telemetry
{
    internal class AzureTableActivityProcessor : BaseProcessor<Activity>
    {
        public override void OnStart(Activity activity)
        {
            if (activity.Kind == ActivityKind.Client)
            {
                activity.SetTag("db.type", "azure_table");
                activity.SetTag("peer.service", "azure_storage");
            }
        }

        public override void OnEnd(Activity activity)
        {
            if (activity.Status == ActivityStatusCode.Error)
            {
                activity.SetTag("error", true);
                activity.SetTag("error.message", activity.StatusDescription);
            }
        }
    }
}
