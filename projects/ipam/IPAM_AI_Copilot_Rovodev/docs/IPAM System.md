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
dotnet build# IPAM System

This is an IP Address Management (IPAM) system built with .NET 8. It provides a comprehensive solution for managing IP addresses, address spaces, and tags with inheritance support.

## Features

- IP address management with CRUD operations
- Address space management
- Tag management with inheritance
  - Inheritable tags that propagate to child IP addresses
  - Non-inheritable tags for specific IP addresses
  - Tag value validation with known values constraint
- RESTful API
- Authentication and authorization
  - JWT-based authentication
  - Role-based access control (RBAC) with Admin/User roles
- Rate limiting
- Unit tests
- Integration tests

## Architecture

The system consists of the following components:

- **ApiGateway**: Handles authentication, authorization, and rate limiting.
  - Implements JWT authentication with OAuth 2.0
  - Provides RBAC with different policies (AdminOnly, UserOnly)
  - Rate limiting with fixed window algorithm (100 requests per minute)
- **Frontend**: Exposes RESTful API endpoints.
  - Provides controllers for IP addresses, address spaces, and tags
  - Implements business logic for tag inheritance
- **DataAccess**: Manages data persistence in Azure Table Storage.
  - Interfaces for data access operations
  - Implementation using Azure Table Storage SDK
  - Data models for IP addresses, address spaces, and tags
- **WebPortal**: Provides a web-based user interface.
  - ASP.NET Core MVC implementation
  - Bootstrap-based UI
- **Client**: A C# client library for interacting with the API.
  - HttpClient-based implementation
  - Simplifies API interactions
- **PowershellCLI**: A PowerShell CLI for interacting with the API.
  - Wraps the C# client functionality
  - Provides command-line interface

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

## API Endpoints

### IP Addresses

- `POST /api/ipaddresses` - Create a new IP address
- `GET /api/ipaddresses/{addressSpaceId}/{ipId}` - Get a specific IP address
- `GET /api/ipaddresses/{addressSpaceId}?cidr={cidr}&tags={tags}` - Get IP addresses with optional filtering

## Data Models

### IP Address

- `Id`: Unique identifier
- `Prefix`: CIDR notation of the IP address
- `Tags`: Collection of tags associated with the IP address
- `CreatedOn`: Creation timestamp
- `ModifiedOn`: Last modification timestamp
- `ParentId`: Reference to parent IP address
- `AddressSpaceId`: Reference to the address space

### Tag

- `Name`: Tag name
- `Description`: Tag description
- `CreatedOn`: Creation timestamp
- `ModifiedOn`: Last modification timestamp
- `Type`: Tag type (Inheritable or NonInheritable)
- `KnownValues`: List of allowed values
- `Value`: Current value of the tag

### Address Space

- `Id`: Unique identifier
- `Name`: Address space name
- `Description`: Address space description
- `CreatedOn`: Creation timestamp
- `ModifiedOn`: Last modification timestamp
- `PartitionKey`: Azure Table Storage partition key

## Testing

- **Unit Tests**: Located in the `Ipam.UnitTests` project.
  - Tests for business logic (tag inheritance)
  - Tests for data access layer
- **Integration Tests**: Located in the `Ipam.IntegrationTests` project.
  - Tests for API endpoints
  - End-to-end testing of the system

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
