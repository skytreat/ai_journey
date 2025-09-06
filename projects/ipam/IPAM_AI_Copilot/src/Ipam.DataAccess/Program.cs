using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Azure.Data.Tables;
using Ipam.DataAccess.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

var builder = WebApplication.CreateBuilder(args);

// Register Azure Table Storage client
builder.Services.AddSingleton<TableServiceClient>(sp =>
{
    var connectionString = builder.Configuration.GetConnectionString("AzureTableStorage");
    return new TableServiceClient(connectionString);
});

// Register DataAccess service dependencies
builder.Services.AddSingleton<IDataAccessService, DataAccessService>();

builder.Services.AddControllers();
var app = builder.Build();

// Health Check Endpoint for DataAccess Service
app.MapGet("/data/health", () => "DataAccess service healthy");
app.MapControllers();

app.Run();