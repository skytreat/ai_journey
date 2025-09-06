# AI Agent Instructions for IPAM Project

## Project Overview
This is an Enterprise IP Address Management (IPAM) system built with .NET 8 using a microservices architecture. The system manages IP address spaces, CIDR blocks, and their associated metadata with a focus on tag inheritance and implications.

## Key Architecture Components

### Service Boundaries
- **API Gateway** (`src/Gateways/ApiGateway`): Entry point using Ocelot for routing, auth, and rate limiting
- **Frontend Service** (`src/Services/Ipam.Frontend`): Core business logic and REST API
- **DataAccess Service** (`src/Services/Ipam.DataAccess`): Azure Table Storage persistence layer
- **Client Libraries** (`src/Clients/*`): .NET and PowerShell clients for API consumption

### Critical Data Flows
1. All requests flow through ApiGateway for auth/routing
2. Frontend Service handles business logic, especially tag inheritance
3. DataAccess Service abstracts Azure Table Storage operations
4. Tag inheritance is computed at runtime by traversing the IP tree

## Data Model Patterns

### Azure Table Storage Conventions
- Partition Strategy: Data partitioned by `AddressSpaceId`
- Global data (users, address spaces) uses "SYSTEM" partition key
- See `src/Services/Ipam.DataAccess/Entities` for entity structures

### Tag System
- Two types: Inheritable and NonInheritable (defined in `src/Shared/Ipam.Core/Tag.cs`)
- Tag implications stored separately for dynamic rule evaluation
- Inheritable tags resolved at runtime through parent traversal

## Development Workflow

### Project Structure
```
src/
  Clients/    # API clients (.NET, PowerShell)
  Gateways/   # API Gateway (Ocelot)
  Services/   # Core microservices
  Shared/     # Shared models and DTOs
```

### Key Development Commands
1. Use Docker Compose for local development:
   ```
   docker-compose up
   ```
2. Run tests:
   ```
   dotnet test IPAM.sln
   ```

## Testing Patterns
- Integration tests use `CustomWebApplicationFactory` (`tests/Ipam.Frontend.Tests`)
- Address space operations tested in `AddressSpaceApiTests`
- Authentication flows tested in `AuthApiTests`

## Common Patterns

### API Endpoints
- RESTful endpoints under `/api/v1/`
- Follow pattern: `/api/v1/addressspaces/{spaceId}/[resource]`
- Resources: `ips`, `tags`

### Authorization
- JWT-based authentication in API Gateway
- Role-based access: SystemAdmin, AddressSpaceAdmin, AddressSpaceViewer
- Roles checked in Frontend Service middleware

## Integration Points
- Azure Table Storage for persistence
- Azure Application Insights for telemetry
- JWT provider for authentication

## Common Tasks
1. Adding new API endpoint:
   - Add controller action in Frontend Service
   - Add route in `ocelot.json`
   - Add DTO in `src/Shared/Ipam.Dto`
   
2. Modifying data model:
   - Update entity in `DataAccess/Entities`
   - Update corresponding DTO in `Shared/Ipam.Dto`
   - Update Core model in `Shared/Ipam.Core`

3. Adding new tag rules:
   - Modify `TagImplication` logic in Core
   - Update repository methods in DataAccess
