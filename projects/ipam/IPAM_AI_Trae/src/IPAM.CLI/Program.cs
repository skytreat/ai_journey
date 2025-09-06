﻿﻿﻿using System.CommandLine;
using System.CommandLine.Invocation;
using System.Net.Http;
using System.Net.Http.Json;
using IPAM.Core;

var httpClient = new HttpClient
{
    BaseAddress = new Uri("https://localhost:5001")
};

var rootCommand = new RootCommand("IP Address Management CLI");

// Address Space Commands
var addressSpaceCommand = new Command("address-space", "Manage address spaces");
var addressSpaceListCommand = new Command("list", "List address spaces");
addressSpaceCommand.AddCommand(addressSpaceListCommand);
addressSpaceListCommand.Handler = CommandHandler.Create(async ()
=>
{
    Console.WriteLine("Listing address spaces...");
    // TODO: Implement API call to list address spaces
});

// IP Commands
var ipCommand = new Command("ip", "Manage IP addresses");
var ipListCommand = new Command("list", "List IP addresses");
ipCommand.AddCommand(ipListCommand);
ipListCommand.Handler = CommandHandler.Create(async ()
=>
{
    Console.WriteLine("Listing IP addresses...");
    // TODO: Implement API call to list IP addresses
});

// Tag Commands
var tagCommand = new Command("tag", "Manage tags");
var tagListCommand = new Command("list", "List tags");
tagCommand.AddCommand(tagListCommand);
tagListCommand.Handler = CommandHandler.Create(async () =>
{
    try
    {
        var tags = await httpClient.GetFromJsonAsync<List<Tag>>("api/tag");
        if (tags == null || !tags.Any())
        {
            Console.WriteLine("No tags found.");
            return;
        }

        Console.WriteLine("Tags:");
        Console.WriteLine("Name\t\tType\t\tDescription");
        foreach (var tag in tags)
        {
            Console.WriteLine($"{tag.Name}\t\t{tag.Type}\t\t{tag.Description}");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error: {ex.Message}");
    }
});

rootCommand.AddCommand(addressSpaceCommand);
rootCommand.AddCommand(ipCommand);
rootCommand.AddCommand(tagCommand);

return await rootCommand.InvokeAsync(args);