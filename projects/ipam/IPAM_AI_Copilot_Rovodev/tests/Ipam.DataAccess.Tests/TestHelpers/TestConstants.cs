namespace Ipam.DataAccess.Tests.TestHelpers
{
    /// <summary>
    /// Centralized test constants to eliminate duplication across test files
    /// </summary>
    public static class TestConstants
    {
        // Address Space Constants
        public const string DefaultAddressSpaceId = "test-space";
        public const string DefaultAddressSpaceId1 = "test-space-1";
        public const string DefaultAddressSpaceId2 = "test-space-2";
        public const string PerformanceTestAddressSpaceId = "perf-test";
        public const string LoadTestAddressSpaceId = "load-test-space";
        
        // IP and Network Constants
        public const string DefaultIpId = "test-ip";
        public const string DefaultCidr = "192.168.1.0/24";
        public const string DefaultCidrIpv6 = "2001:db8::/32";
        
        // Common Network Ranges
        public static class Networks
        {
            public const string ParentNetwork = "10.0.0.0/16";
            public const string ChildNetwork1 = "10.0.1.0/24";
            public const string ChildNetwork2 = "10.0.2.0/24";
            public const string ConflictingNetwork = "10.0.1.128/25";
            
            // IPv6 Networks
            public const string Ipv6Parent = "2001:db8::/32";
            public const string Ipv6Child1 = "2001:db8:1::/48";
            public const string Ipv6Child2 = "2001:db8:2::/48";
        }
        
        // Tag Constants
        public static class Tags
        {
            public const string EnvironmentTagName = "Environment";
            public const string ApplicationTagName = "Application";
            public const string RegionTagName = "Region";
            
            public static readonly Dictionary<string, string> DefaultTags = new()
            {
                { "Environment", "Development" },
                { "Application", "TestApp" }
            };
            
            public static readonly Dictionary<string, string> ProductionTags = new()
            {
                { "Environment", "Production" },
                { "Application", "CriticalApp" }
            };
        }
        
        // Connection Strings
        public static class ConnectionStrings
        {
            public const string DefaultAzureStorage = "UseDevelopmentStorage=true";
            public const string TestTablePrefix = "IpamTest";
        }
        
        // Performance Test Constants
        public static class Performance
        {
            public const int DefaultConcurrencyLevel = 10;
            public const int DefaultIterations = 100;
            public const int LoadTestIterations = 1000;
        }
    }
}