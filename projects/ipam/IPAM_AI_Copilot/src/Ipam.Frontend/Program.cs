using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Azure.Data.Tables;
using Ipam.DataAccess;

var builder = WebApplication.CreateBuilder(args);

// Register Azure Table Storage client
builder.Services.AddSingleton<TableServiceClient>(sp =>
{
    var connectionString = builder.Configuration.GetConnectionString("AzureTableStorage");
    return new TableServiceClient(connectionString);
});

// Register DataAccess service
builder.Services.AddSingleton<IDataAccessService, DataAccessService>();

builder.Services.AddControllers();

var app = builder.Build();

app.MapControllers();

app.Run();