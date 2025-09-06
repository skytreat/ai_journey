using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace IpamClient
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("IPAM C# Client started.");
            using var client = new HttpClient();
            // Example call to API Gateway health endpoint
            var response = await client.GetAsync("http://localhost:5000/health");
            if(response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"API Gateway Response: {content}");
            }
            else
            {
                Console.WriteLine($"API call failed with status code: {response.StatusCode}");
            }
            
            // ...existing code...

            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }
    }
}
