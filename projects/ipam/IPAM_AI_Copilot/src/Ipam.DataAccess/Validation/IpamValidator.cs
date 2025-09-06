using Ipam.DataAccess.Models;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

namespace Ipam.DataAccess.Validation
{
    /// <summary>
    /// Validator for IPAM business rules
    /// </summary>
    public static class IpamValidator
    {
        public static void ValidateCidr(string cidr)
        {
            try
            {
                var parts = cidr.Split('/');
                if (parts.Length != 2) throw new ArgumentException("Invalid CIDR format");
                
                if (!IPAddress.TryParse(parts[0], out _))
                    throw new ArgumentException("Invalid IP address");

                if (!int.TryParse(parts[1], out int prefix) || prefix < 0 || prefix > 128)
                    throw new ArgumentException("Invalid prefix length");
            }
            catch (Exception ex)
            {
                throw new ValidationException($"Invalid CIDR format: {cidr}", ex);
            }
        }

        public static void ValidateTagInheritance(Dictionary<string, string> parentTags, Dictionary<string, string> childTags)
        {
            foreach (var tag in parentTags)
            {
                if (childTags.TryGetValue(tag.Key, out string childValue) && 
                    childValue != tag.Value)
                {
                    throw new ValidationException(
                        $"Tag inheritance conflict: Parent has {tag.Key}={tag.Value}, child has {tag.Key}={childValue}");
                }
            }
        }

        public static void ValidateTagImplications(Dictionary<string, Dictionary<string, string>> implies)
        {
            var visited = new HashSet<string>();
            foreach (var tagName in implies.Keys)
            {
                if (DetectCyclicDependency(tagName, implies, visited, new HashSet<string>()))
                {
                    throw new ValidationException("Cyclic dependency detected in tag implications");
                }
            }
        }

        private static bool DetectCyclicDependency(
            string tagName,
            Dictionary<string, Dictionary<string, string>> implies,
            HashSet<string> visited,
            HashSet<string> currentPath)
        {
            if (currentPath.Contains(tagName))
                return true;

            if (visited.Contains(tagName))
                return false;

            visited.Add(tagName);
            currentPath.Add(tagName);

            if (implies.TryGetValue(tagName, out var implications))
            {
                foreach (var impliedTag in implications.Keys)
                {
                    if (DetectCyclicDependency(impliedTag, implies, visited, currentPath))
                        return true;
                }
            }

            currentPath.Remove(tagName);
            return false;
        }
    }

    public class ValidationException : Exception
    {
        public ValidationException(string message) : base(message) { }
        public ValidationException(string message, Exception innerException) : base(message, innerException) { }
    }
}
