# IPAM System

This is an IP Address Management (IPAM) system built with .NET 8. It provides a comprehensive solution for managing IP addresses, address spaces, and tags with inheritance support.

## Features

- IP address management with CRUD operations
- Address space management
- Tag management with inheritance
- RESTful API
- Authentication and authorization
- Rate limiting
- Unit tests
- Integration tests

## Getting Started

### Prerequisites

- .NET 8 SDK
- Azure Storage Account (for Table Storage)

### Configuration

Set the following environment variables:

- `AzureTableStorage`: Connection string to Azure Table Storage
- `Jwt:Issuer`: JWT issuer
- `Jwt:Audience`: JWT audience
- `Jwt:Key`: JWT key
- `FrontendServiceUrl`: URL of the frontend service

### Building and Running

```bash
dotnet build
```

```bash
dotnet run --project src/Ipam.ApiGateway
```

### Running Tests

```bash
dotnet test
```

## Architecture

The system consists of the following components:

- **ApiGateway**: Handles authentication, authorization, and rate limiting.
- **Frontend**: Exposes RESTful API endpoints.
- **DataAccess**: Manages data persistence in Azure Table Storage.
- **WebPortal**: Provides a web-based user interface.
- **Client**: A C# client library for interacting with the API.
- **PowershellCLI**: A PowerShell CLI for interacting with the API.

## Testing

- **Unit Tests**: Located in the `Ipam.UnitTests` project.
- **Integration Tests**: Located in the `Ipam.IntegrationTests` project.
