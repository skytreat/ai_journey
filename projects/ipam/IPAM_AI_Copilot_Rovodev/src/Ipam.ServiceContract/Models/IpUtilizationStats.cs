namespace Ipam.ServiceContract.Models
{
    /// <summary>
    /// IP utilization statistics for a network
    /// </summary>
    public class IpUtilizationStats
    {
        public string NetworkCidr { get; set; } = null!;
        public long TotalAddresses { get; set; }
        public long AllocatedAddresses { get; set; }
        public long AvailableAddresses { get; set; }
        public double UtilizationPercentage { get; set; }
        public int SubnetCount { get; set; }
        public string LargestAvailableBlock { get; set; } = null!;
        public double FragmentationIndex { get; set; }
    }
}