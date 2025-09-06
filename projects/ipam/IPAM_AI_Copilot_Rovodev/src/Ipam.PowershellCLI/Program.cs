using System;
using System.CommandLine;
using System.CommandLine.NamingConventionBinder;
using System.Threading.Tasks;
using Ipam.Client;
using Ipam.DataAccess.Models;
using Newtonsoft.Json;

namespace IpamPowershellCLI
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // Create the root command
            var rootCommand = new RootCommand("IPAM PowerShell CLI");
            
            // Add subcommands
            rootCommand.AddCommand(CreateAddressSpaceCommand());
            rootCommand.AddCommand(GetAddressSpaceCommand());
            rootCommand.AddCommand(GetAddressSpacesCommand());
            rootCommand.AddCommand(UpdateAddressSpaceCommand());
            rootCommand.AddCommand(DeleteAddressSpaceCommand());
            
            rootCommand.AddCommand(CreateTagCommand());
            rootCommand.AddCommand(GetTagCommand());
            rootCommand.AddCommand(GetTagsCommand());
            rootCommand.AddCommand(UpdateTagCommand());
            rootCommand.AddCommand(DeleteTagCommand());
            
            rootCommand.AddCommand(CreateIPAddressCommand());
            rootCommand.AddCommand(GetIPAddressCommand());
            rootCommand.AddCommand(GetIPAddressesCommand());
            
            // Parse and invoke the command
            await rootCommand.InvokeAsync(args);
        }
        
        // Address Space Commands
        
        static Command CreateAddressSpaceCommand()
        {
            var command = new Command("create-address-space", "Create a new address space");
            var nameOption = new Option<string>("--name", "Name of the address space");
            var descriptionOption = new Option<string>("--description", "Description of the address space");
            
            command.AddOption(nameOption);
            command.AddOption(descriptionOption);
            
            command.Handler = CommandHandler.Create<string, string>(async (name, description) =>
            {
                var client = new IpamApiClient("http://localhost:5000");
                var addressSpace = new AddressSpace { Name = name, Description = description };
                var result = await client.CreateAddressSpaceAsync(addressSpace);
                Console.WriteLine(JsonConvert.SerializeObject(result, Formatting.Indented));
            });
            
            return command;
        }
        
        static Command GetAddressSpaceCommand()
        {
            var command = new Command("get-address-space", "Get an address space by ID");
            var idArgument = new Argument<string>("id", "ID of the address space");
            command.AddArgument(idArgument);
            
            command.Handler = CommandHandler.Create<string>(async (id) =>
            {
                var client = new IpamApiClient("http://localhost:5000");
                var result = await client.GetAddressSpaceAsync(id);
                Console.WriteLine(JsonConvert.SerializeObject(result, Formatting.Indented));
            });
            
            return command;
        }
        
        static Command GetAddressSpacesCommand()
        {
            var command = new Command("get-address-spaces", "Get all address spaces");
            command.Handler = CommandHandler.Create(async () =>
            {
                var client = new IpamApiClient("http://localhost:5000");
                var result = await client.GetAddressSpacesAsync();
                Console.WriteLine(JsonConvert.SerializeObject(result, Formatting.Indented));
            });
            
            return command;
        }
        
        static Command UpdateAddressSpaceCommand()
        {
            var command = new Command("update-address-space", "Update an address space");
            var idOption = new Option<string>("--id", "ID of the address space");
            var nameOption = new Option<string>("--name", "Name of the address space");
            var descriptionOption = new Option<string>("--description", "Description of the address space");
            
            command.AddOption(idOption);
            command.AddOption(nameOption);
            command.AddOption(descriptionOption);
            
            command.Handler = CommandHandler.Create<string, string, string>(async (id, name, description) =>
            {
                var client = new IpamApiClient("http://localhost:5000");
                var addressSpace = new AddressSpace { Id = id, Name = name, Description = description };
                var result = await client.UpdateAddressSpaceAsync(addressSpace);
                Console.WriteLine(JsonConvert.SerializeObject(result, Formatting.Indented));
            });
            
            return command;
        }
        
        static Command DeleteAddressSpaceCommand()
        {
            var command = new Command("delete-address-space", "Delete an address space");
            var idArgument = new Argument<string>("id", "ID of the address space");
            command.AddArgument(idArgument);
            
            command.Handler = CommandHandler.Create<string>(async (id) =>
            {
                var client = new IpamApiClient("http://localhost:5000");
                await client.DeleteAddressSpaceAsync(id);
                Console.WriteLine($"Address space {id} deleted successfully.");
            });
            
            return command;
        }
        
        // Tag Commands
        
        static Command CreateTagCommand()
        {
            var command = new Command("create-tag", "Create a new tag");
            var addressSpaceIdOption = new Option<string>("--address-space-id", "ID of the address space");
            var nameOption = new Option<string>("--name", "Name of the tag");
            var descriptionOption = new Option<string>("--description", "Description of the tag");
            
            command.AddOption(addressSpaceIdOption);
            command.AddOption(nameOption);
            command.AddOption(descriptionOption);
            
            command.Handler = CommandHandler.Create<string, string, string>(async (addressSpaceId, name, description) =>
            {
                var client = new IpamApiClient("http://localhost:5000");
                var tag = new Tag { Name = name, Description = description };
                var result = await client.CreateTagAsync(addressSpaceId, tag);
                Console.WriteLine(JsonConvert.SerializeObject(result, Formatting.Indented));
            });
            
            return command;
        }
        
        static Command GetTagCommand()
        {
            var command = new Command("get-tag", "Get a tag by name");
            var addressSpaceIdArgument = new Argument<string>("address-space-id", "ID of the address space");
            var nameArgument = new Argument<string>("name", "Name of the tag");
            command.AddArgument(addressSpaceIdArgument);
            command.AddArgument(nameArgument);
            
            command.Handler = CommandHandler.Create<string, string>(async (addressSpaceId, name) =>
            {
                var client = new IpamApiClient("http://localhost:5000");
                var result = await client.GetTagAsync(addressSpaceId, name);
                Console.WriteLine(JsonConvert.SerializeObject(result, Formatting.Indented));
            });
            
            return command;
        }
        
        static Command GetTagsCommand()
        {
            var command = new Command("get-tags", "Get all tags in an address space");
            var addressSpaceIdArgument = new Argument<string>("address-space-id", "ID of the address space");
            command.AddArgument(addressSpaceIdArgument);
            
            command.Handler = CommandHandler.Create<string>(async (addressSpaceId) =>
            {
                var client = new IpamApiClient("http://localhost:5000");
                var result = await client.GetTagsAsync(addressSpaceId);
                Console.WriteLine(JsonConvert.SerializeObject(result, Formatting.Indented));
            });
            
            return command;
        }
        
        static Command UpdateTagCommand()
        {
            var command = new Command("update-tag", "Update a tag");
            var addressSpaceIdOption = new Option<string>("--address-space-id", "ID of the address space");
            var nameOption = new Option<string>("--name", "Name of the tag");
            var descriptionOption = new Option<string>("--description", "Description of the tag");
            
            command.AddOption(addressSpaceIdOption);
            command.AddOption(nameOption);
            command.AddOption(descriptionOption);
            
            command.Handler = CommandHandler.Create<string, string, string>(async (addressSpaceId, name, description) =>
            {
                var client = new IpamApiClient("http://localhost:5000");
                var tag = new Tag { Name = name, Description = description };
                var result = await client.UpdateTagAsync(addressSpaceId, tag);
                Console.WriteLine(JsonConvert.SerializeObject(result, Formatting.Indented));
            });
            
            return command;
        }
        
        static Command DeleteTagCommand()
        {
            var command = new Command("delete-tag", "Delete a tag");
            var addressSpaceIdArgument = new Argument<string>("address-space-id", "ID of the address space");
            var nameArgument = new Argument<string>("name", "Name of the tag");
            command.AddArgument(addressSpaceIdArgument);
            command.AddArgument(nameArgument);
            
            command.Handler = CommandHandler.Create<string, string>(async (addressSpaceId, name) =>
            {
                var client = new IpamApiClient("http://localhost:5000");
                await client.DeleteTagAsync(addressSpaceId, name);
                Console.WriteLine($"Tag {name} deleted successfully from address space {addressSpaceId}.");
            });
            
            return command;
        }
        
        // IP Address Commands
        
        static Command CreateIPAddressCommand()
        {
            var command = new Command("create-ip", "Create a new IP address");
            var addressSpaceIdOption = new Option<string>("--address-space-id", "ID of the address space");
            var prefixOption = new Option<string>("--prefix", "IP prefix in CIDR format");
            
            command.AddOption(addressSpaceIdOption);
            command.AddOption(prefixOption);
            
            command.Handler = CommandHandler.Create<string, string>(async (addressSpaceId, prefix) =>
            {
                var client = new IpamApiClient("http://localhost:5000");
                var ipAddress = new IPAddress { AddressSpaceId = addressSpaceId, Prefix = prefix };
                var result = await client.CreateIPAddressAsync(ipAddress);
                Console.WriteLine(JsonConvert.SerializeObject(result, Formatting.Indented));
            });
            
            return command;
        }
        
        static Command GetIPAddressCommand()
        {
            var command = new Command("get-ip", "Get an IP address by ID");
            var addressSpaceIdArgument = new Argument<string>("address-space-id", "ID of the address space");
            var ipIdArgument = new Argument<string>("ip-id", "ID of the IP address");
            command.AddArgument(addressSpaceIdArgument);
            command.AddArgument(ipIdArgument);
            
            command.Handler = CommandHandler.Create<string, string>(async (addressSpaceId, ipId) =>
            {
                var client = new IpamApiClient("http://localhost:5000");
                var result = await client.GetIPAddressAsync(addressSpaceId, ipId);
                Console.WriteLine(JsonConvert.SerializeObject(result, Formatting.Indented));
            });
            
            return command;
        }
        
        static Command GetIPAddressesCommand()
        {
            var command = new Command("get-ips", "Get all IP addresses in an address space");
            var addressSpaceIdArgument = new Argument<string>("address-space-id", "ID of the address space");
            command.AddArgument(addressSpaceIdArgument);
            
            command.Handler = CommandHandler.Create<string>(async (addressSpaceId) =>
            {
                var client = new IpamApiClient("http://localhost:5000");
                var result = await client.GetIPAddressesAsync(addressSpaceId);
                Console.WriteLine(JsonConvert.SerializeObject(result, Formatting.Indented));
            });
            
            return command;
        }
    }
}