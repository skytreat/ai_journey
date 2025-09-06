# IPAM System Design Document

## 1. Introduction

This document outlines the system design for the Enterprise IP Address Management (IPAM) system. The system is designed to be a scalable, highly available, and performant platform for managing IP address spaces, CIDR blocks, and associated metadata. It will be built using a microservices architecture with C# and .NET Core, leveraging Azure Table Storage for data persistence.

## 2. Architecture

### 2.1. Microservices Architecture

The system will be composed of three core microservices:

*   **API Gateway:** The single entry point for all clients. It handles request routing, authentication, authorization, rate limiting, and IP black/white listing. We will use [Ocelot](https://github.com/ThreeMammals/Ocelot) as the API Gateway.
*   **Frontend Service:** This service exposes the core business logic and RESTful API for managing address spaces, tags, and IP addresses. It will handle all CRUD operations and the complex logic related to tag inheritance and implication.
*   **DataAccess Service:** This service is responsible for all interactions with the Azure Table Storage. It abstracts the data persistence layer from the rest of the system, providing a clean repository-based interface.

### 2.2. Technology Stack

*   **Backend:** C# with .NET 8
*   **Web Framework:** ASP.NET Core
*   **Database:** Azure Table Storage
*   **Web UI:** ASP.NET Core MVC/Razor Pages with Bootstrap 5 and jQuery
*   **API Gateway:** Ocelot
*   **Containerization:** Docker
*   **Deployment:** Kubernetes, Azure Container Apps
*   **Testing:** xUnit, Moq
*   **Telemetry:** Azure Application Insights

### 2.3. High-Level Diagram

```
+-----------------+      +------------------+      +--------------------+       +-------------------+
|   Web Portal    |----->|                  |      |                    |       |                   |
+-----------------+      |                  |      |   Frontend Service |------>| DataAccess Service|
                         |                  |      |   (Business Logic, |       | (Repositories)    |
+-----------------+      |   API Gateway    |      |    REST API)       |       |                   |
| C#/.NET Client  |----->|    (Ocelot)      |----->+--------------------+       +-------------------+
+-----------------+      | (AuthN/AuthZ,    |                                          |
                         |  Rate Limiting)  |                                          |
+-----------------+      |                  |                                          v
| PowerShell CLI  |----->|                  |                               +----------------------+
+-----------------+      +------------------+                               |  Azure Table Storage |
                                                                            +----------------------+
```

## 3. Data Model (Azure Table Storage)

### 3.1. Partitioning Strategy

To ensure scalability, data will be partitioned by `AddressSpaceId`. All data related to a single address space (IPs, Tags) will reside in the same Azure Storage partition. This allows for efficient querying and scaling. A designated `system` partition will be used for global data like the list of address spaces themselves.

*   **PartitionKey:** For most tables, this will be the `AddressSpaceId`. For global tables, it will be a fixed value like "SYSTEM".
*   **RowKey:** This will be the unique identifier for the entity within the partition (e.g., `IpId`, `TagName`).

### 3.2. Tables

#### 3.2.1. `AddressSpaces`

Stores information about each address space.

*   **PartitionKey:** "SYSTEM"
*   **RowKey:** `AddressSpaceId` (GUID)
*   **Name:** `string`
*   **Description:** `string`
*   **CreatedOn:** `DateTimeOffset`
*   **ModifiedOn:** `DateTimeOffset`

#### 3.2.2. `TagDefinitions`

Stores the definition of each tag within an address space.

*   **PartitionKey:** `AddressSpaceId`
*   **RowKey:** `Name` (Tag Name)
*   **Description:** `string`
*   **Type:** `string` ("Inheritable" or "NonInheritable")
*   **KnownValues:** `string` (JSON serialized list of strings)
*   **Attributes:** `string` (JSON serialized dictionary)
*   **CreatedOn:** `DateTimeOffset`
*   **ModifiedOn:** `DateTimeOffset`

#### 3.2.3. `TagImplications`

Stores tag implication rules.

*   **PartitionKey:** `AddressSpaceId`
*   **RowKey:** `IfTagValue` (e.g., "Datacenter:AMS05")
*   **ThenTagValue:** `string` (e.g., "Region:EuropeWest")

#### 3.2.4. `IpAddresses`

Stores IP CIDR blocks and their associated tags.

*   **PartitionKey:** `AddressSpaceId`
*   **RowKey:** `IpId` (GUID)
*   **Prefix:** `string` (e.g., "192.168.1.0/24")
*   **ParentId:** `string` (GUID of the parent IP)
*   **Tags:** `string` (JSON serialized dictionary of `TagName:TagValue` pairs for **NonInheritable** tags only)
*   **CreatedOn:** `DateTimeOffset`
*   **ModifiedOn:** `DateTimeOffset`

*Note on Tag Inheritance:* Inheritable tags are not stored directly on the `IpAddress` entity. They are resolved at runtime by traversing the tree upwards to the root. When a parent is deleted, its inheritable tags will be computed and pushed down to its children.

#### 3.2.5. `Users` and `Roles`

For RBAC, we will have simple tables to manage users and their roles.

*   **`Users` Table**
    *   **PartitionKey:** "SYSTEM"
    *   **RowKey:** `Username`
    *   **PasswordHash:** `string`
*   **`UserRoles` Table**
    *   **PartitionKey:** `Username`
    *   **RowKey:** `AddressSpaceId` (or "SYSTEM" for SystemAdmin)
    *   **Role:** `string` (`SystemAdmin`, `AddressSpaceAdmin`, `AddressSpaceViewer`)

## 4. API Design (RESTful)

The API will be versioned (e.g., `/api/v1/...`). Authentication will be handled via JWT Bearer tokens in the `Authorization` header.

### 4.1. Address Spaces

*   `GET /api/v1/addressspaces`: List all address spaces (with filtering).
*   `POST /api/v1/addressspaces`: Create a new address space.
*   `GET /api/v1/addressspaces/{id}`: Get a specific address space.
*   `PUT /api/v1/addressspaces/{id}`: Update an address space.
*   `DELETE /api/v1/addressspaces/{id}`: Delete an address space.

### 4.2. Tags

*   `GET /api/v1/addressspaces/{spaceId}/tags`: List all tags in an address space.
*   `POST /api/v1/addressspaces/{spaceId}/tags`: Create a new tag.
*   `GET /api/v1/addressspaces/{spaceId}/tags/{tagName}`: Get a specific tag.
*   `PUT /api/v1/addressspaces/{spaceId}/tags/{tagName}`: Update a tag.
*   `DELETE /api/v1/addressspaces/{spaceId}/tags/{tagName}`: Delete a tag.

### 4.3. IP Addresses

*   `GET /api/v1/addressspaces/{spaceId}/ips`: List/search IPs in an address space (by CIDR, tags).
*   `POST /api/v1/addressspaces/{spaceId}/ips`: Create a new IP.
*   `GET /api/v1/addressspaces/{spaceId}/ips/{ipId}`: Get a specific IP.
*   `PUT /api/v1/addressspaces/{spaceId}/ips/{ipId}`: Update an IP (including adding/removing tags).
*   `DELETE /api/v1/addressspaces/{spaceId}/ips/{ipId}`: Delete an IP.
*   `GET /api/v1/addressspaces/{spaceId}/ips/{ipId}/children`: Get child IPs.

## 5. Security

*   **Authentication:** Implemented in the API Gateway. It will validate JWTs and pass user identity information to upstream services.
*   **Authorization:** Implemented in the Frontend Service. A custom middleware will check the user's role (`SystemAdmin`, `AddressSpaceAdmin`, `AddressSpaceViewer`) against the required permissions for the requested resource.
*   **Rate Limiting & IP Filtering:** Handled by the API Gateway.

## 6. Clients

*   **C# .NET Core Client:** A class library distributed as a NuGet package. It will contain typed clients for the REST API, handling serialization and HTTP communication.
*   **PowerShell CLI:** A PowerShell module built on top of the C# client library, providing cmdlets for easy scripting and administration.

## 7. Deployment

Each microservice will have its own `Dockerfile`. A `docker-compose.yml` file will be provided for local development and testing. For production, Kubernetes deployment manifests (`deployment.yaml`, `service.yaml`, etc.) will be created for each service.

## 8. Testing

*   **Unit Tests (xUnit):** Each service will have a corresponding test project. Business logic, controllers, and repositories will be thoroughly tested. Moq will be used for mocking dependencies. Target code coverage is >= 90%.
*   **Integration Tests:** Tests will be written to verify the interaction between services, especially between the Frontend and DataAccess services.
*   **Functional Tests:** End-to-end tests will be created to simulate user workflows through the API.

## 9. Telemetry

Azure Application Insights will be used for a unified telemetry solution.
*   **Logging:** Structured logging will be implemented in all services.
*   **Metrics:** Key performance indicators (e.g., request latency, error rates) will be tracked.
*   **Alerting:** Alerts will be configured in Azure Monitor for critical conditions (e.g., high error rates, service unavailability).
