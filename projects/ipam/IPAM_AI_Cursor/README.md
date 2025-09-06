# IPAM System - Enterprise IP Address Management

A comprehensive, enterprise-grade IP Address Management (IPAM) system built with .NET 8, featuring microservices architecture, modern web interface, and robust API capabilities.

## 🚀 Features

### Core Functionality
- **Address Space Management**: Create, edit, delete, and organize network address spaces
- **Tag Management**: Hierarchical tags with inheritance rules (Inheritable/Non-Inheritable)
- **IP Address Management**: CIDR-based IP allocation and management (IPv4 & IPv6)
- **Role-Based Access Control**: SystemAdmin, AddressSpaceAdmin, AddressSpaceViewer roles

### Technical Features
- **Microservices Architecture**: API Gateway, Frontend Service, DataAccess Service
- **RESTful APIs**: Comprehensive CRUD operations with pagination
- **Modern Web Portal**: Bootstrap 5 + Bootstrap Icons, responsive design
- **Pagination**: Smart pagination with page size selection (10, 20, 50, 100)
- **Validation**: Client and server-side validation with meaningful error messages
- **Telemetry**: OpenTelemetry integration with Azure Monitor support
- **Containerization**: Docker support with Kubernetes manifests
- **Authentication**: JWT-based with development bypass for local testing

## 🏗️ Architecture

```
┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐
│   Web Portal    │    │  API Gateway    │    │  Frontend API   │
│  (ASP.NET Core) │◄──►│   (YARP Proxy)  │◄──►│  (ASP.NET Core) │
└─────────────────┘    └─────────────────┘    └─────────────────┘
                                │                       │
                                ▼                       ▼
                       ┌─────────────────┐    ┌─────────────────┐
                       │  DataAccess     │    │  Azure Table    │
                       │   Service       │◄──►│    Storage      │
                       └─────────────────┘    └─────────────────┘
```

## 🛠️ Technology Stack

- **Backend**: .NET 8, ASP.NET Core, C#
- **Frontend**: ASP.NET Core MVC, Bootstrap 5, jQuery
- **API Gateway**: YARP Reverse Proxy
- **Database**: Azure Table Storage
- **Authentication**: JWT Bearer Tokens
- **Telemetry**: OpenTelemetry + Azure Monitor
- **Containerization**: Docker, Kubernetes
- **Testing**: xUnit

## 📁 Project Structure

```
src/
├── Shared.Contracts/          # DTOs and shared interfaces
├── Shared.Domain/             # Domain models and business logic
├── Shared.Application/        # Application service interfaces
├── Shared.Infrastructure/     # Infrastructure abstractions
├── Services.Gateway/          # API Gateway (YARP)
├── Services.Frontend/         # REST API service
├── Services.DataAccess/       # Data access layer
├── Web.WebPortal/             # Web interface
├── Clients.SDK/               # C# client library
├── Clients.CLI/               # Command-line interface
└── Domain.Tests/              # Unit tests

deploy/                        # Kubernetes manifests
docs/                          # Documentation
```

## 🚀 Getting Started

### Prerequisites
- .NET 8 SDK
- Azure Storage Emulator (for local development)
- Docker (optional)

### Local Development

1. **Clone and Build**
   ```bash
   git clone <repository-url>
   cd IPAM_AI_Cursor
   dotnet build src/IPAM.sln
   ```

2. **Start Azure Storage Emulator**
   ```bash
   # Windows
   azurite --silent
   
   # Or use Docker
   docker run -d -p 10000:10000 -p 10001:10001 -p 10002:10002 mcr.microsoft.com/azure-storage/azurite
   ```

3. **Run Services** (in separate terminals)
   ```bash
   # Terminal 1: Frontend API
   cd src
   dotnet run --project Services.Frontend/Services.Frontend.csproj --urls http://localhost:5080
   
   # Terminal 2: API Gateway
   dotnet run --project Services.Gateway/Services.Gateway.csproj --urls http://localhost:5081
   
   # Terminal 3: Web Portal
   dotnet run --project Web.WebPortal/Web.WebPortal.csproj --urls http://localhost:5082
   ```

4. **Access the System**
   - **Dashboard**: http://localhost:5082/Dashboard
   - **Address Spaces**: http://localhost:5082/
   - **API Gateway**: http://localhost:5081
   - **Frontend API**: http://localhost:5080

### Configuration

The system uses `appsettings.Development.json` for local development:

```json
{
  "DevAuth": {
    "Enabled": true
  },
  "TableStorage": {
    "ConnectionString": "UseDevelopmentStorage=true"
  },
  "FrontendBaseUrl": "http://localhost:5080"
}
```

## 🔐 Authentication & Authorization

### Development Mode
- **DevAuth**: Enabled by default for local development
- **Bypass**: No JWT required, automatic role assignment
- **Roles**: SystemAdmin, AddressSpaceAdmin, AddressSpaceViewer

### Production Mode
- **JWT Bearer**: Standard JWT authentication
- **RBAC**: Role-based access control
- **Policies**: Configurable authorization policies

## 📊 API Endpoints

### Address Spaces
- `GET /api/v1/address-spaces` - List with pagination
- `POST /api/v1/address-spaces` - Create new
- `PUT /api/v1/address-spaces/{id}` - Update
- `DELETE /api/v1/address-spaces/{id}` - Delete

### Tags
- `GET /api/v1/address-spaces/{id}/tags` - List with pagination
- `PUT /api/v1/address-spaces/{id}/tags` - Create/Update
- `DELETE /api/v1/address-spaces/{id}/tags/{name}` - Delete

### IP Addresses
- `GET /api/v1/address-spaces/{id}/ips` - List with pagination
- `POST /api/v1/address-spaces/{id}/ips` - Create/Update
- `PUT /api/v1/address-spaces/{id}/ips/{ipId}` - Update
- `DELETE /api/v1/address-spaces/{id}/ips/{ipId}` - Delete

### Pagination
All list endpoints support pagination:
- `?pageNumber=1&pageSize=20`
- Page sizes: 10, 20, 50, 100
- Response includes total count and page information

## 🎨 Web Portal Features

### Dashboard
- System overview with statistics
- Quick action buttons
- Recent activity feed
- System status information

### Address Space Management
- List view with pagination
- Create/Edit forms with validation
- Delete with confirmation
- Navigation to tags and IPs

### Tag Management
- Per-address-space tag lists
- Create/Edit forms with type selection
- Inheritable vs Non-Inheritable types
- Helpful descriptions and tooltips

### IP Address Management
- CIDR-based IP allocation
- Validation for IPv4 and IPv6 formats
- Edit and delete operations
- Organized by address space

## 🧪 Testing

### Unit Tests
```bash
cd src
dotnet test Domain.Tests/Domain.Tests.csproj
```

### API Testing
Use the provided Postman collection in `docs/PostmanCollection.json`

### Smoke Testing
```bash
# Test Frontend API
curl http://localhost:5080/api/v1/address-spaces

# Test Gateway
curl http://localhost:5081/api/v1/address-spaces

# Test Web Portal
curl http://localhost:5082/
```

## 🐳 Containerization

### Docker
```bash
# Build images
docker build -t ipam-frontend src/Services.Frontend/
docker build -t ipam-gateway src/Services.Gateway/
docker build -t ipam-webportal src/Web.WebPortal/

# Run containers
docker run -p 5080:80 ipam-frontend
docker run -p 5081:80 ipam-gateway
docker run -p 5082:80 ipam-webportal
```

### Kubernetes
```bash
kubectl apply -f deploy/k8s.yaml
```

## 📈 Monitoring & Telemetry

### OpenTelemetry Integration
- **Tracing**: Distributed request tracing
- **Metrics**: Performance and business metrics
- **Logging**: Structured logging with correlation

### Azure Monitor
- **Application Insights**: Performance monitoring
- **Log Analytics**: Centralized logging
- **Metrics**: Custom business metrics

## 🔧 Development

### Adding New Features
1. **Domain Models**: Add to `Shared.Domain`
2. **Contracts**: Define DTOs in `Shared.Contracts`
3. **Repository**: Implement in `Services.DataAccess`
4. **API Controller**: Add to `Services.Frontend`
5. **Web Views**: Create in `Web.WebPortal`

### Code Quality
- **Validation**: Use DataAnnotations for input validation
- **Error Handling**: Return meaningful error messages
- **Pagination**: Implement for all list endpoints
- **Testing**: Maintain high test coverage

## 📚 Documentation

- **System Design**: `docs/SystemDesign.md`
- **Requirements**: `docs/Requirements.md`
- **API Reference**: Postman collection with examples

## 🤝 Contributing

1. Follow the existing code structure
2. Add validation and error handling
3. Include pagination for list endpoints
4. Write unit tests for new functionality
5. Update documentation

## 📄 License

This project is licensed under the MIT License.

## 🆘 Support

For issues and questions:
1. Check the documentation
2. Review the system design
3. Test with the provided examples
4. Create detailed issue reports

---

**Built with ❤️ using .NET 8 and modern web technologies**
