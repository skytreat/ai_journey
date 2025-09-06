
using System.Management.Automation;
using System.Threading.Tasks;
using Ipam.Client;

namespace Ipam.PowerShell
{
    [Cmdlet(VerbsCommon.Get, "IpamAddressSpace")]
    [OutputType(typeof(Dto.AddressSpaceDto))]
    public class GetIpamAddressSpace : PSCmdlet
    {
        [Parameter(Mandatory = false, ValueFromPipelineByPropertyName = true)]
        public string ApiBaseUrl { get; set; }

        public GetIpamAddressSpace()
        {
            ApiBaseUrl = string.Empty;
        }

        protected override void ProcessRecord()
        {
            if (string.IsNullOrEmpty(ApiBaseUrl))
            {
                // In a real scenario, get from configuration or environment variable
                ApiBaseUrl = "https://localhost:5001"; 
            }

            var client = new IpamClient(ApiBaseUrl);
            var task = client.GetAddressSpacesAsync();
            task.Wait();
            WriteObject(task.Result, true);
        }
    }
}
