using IPAM.Clients;
using System.Net.Http;

var baseAddress = args.Length > 0 ? args[0] : "http://localhost:5080/";
var http = new HttpClient { BaseAddress = new Uri(baseAddress) };
var client = new IpamClient(http);

var list = await client.GetAddressSpacesAsync();
Console.WriteLine($"AddressSpaces: {list.Count}");
