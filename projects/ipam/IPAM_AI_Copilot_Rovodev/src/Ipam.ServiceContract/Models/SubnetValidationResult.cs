using System.Collections.Generic;

namespace Ipam.ServiceContract.Models
{
    /// <summary>
    /// Result of subnet allocation validation
    /// </summary>
    public class SubnetValidationResult
    {
        public bool IsValid { get; set; }
        public string ProposedCidr { get; set; } = null!;
        public List<string> ConflictingSubnets { get; set; } = new List<string>();
        public string ValidationMessage { get; set; } = null!;
    }
}