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
*   **Telemetry:** OpenTelemetry, Azure Application Insights
*   **Caching:** Redis

### 2.3. High-Level Diagram

```
+-----------------+      +------------------+      +--------------------+       +-------------------+
|   Web Portal    |----->|                  |      |                    |       |                   |
+-----------------+      |                  |      |   Frontend Service |------>| DataAccess Service|
                         |                  |      | (Business Logic,   |       | (Repositories)    |
+-----------------+      |   API Gateway    |      |    REST API)       |       |                   |
| C#/.NET Client  |----->|    (Ocelot)      |----->+--------------------+       +-------------------+
+-----------------+      | (AuthN/AuthZ,    |               |                        |
                         |  Rate Limiting)  |               |                        v
+-----------------+      |                  |               |             +----------------------+
| PowerShell CLI  |----->|                  |               +------------>|   Redis Cache        |
+-----------------+      +------------------+                             +----------------------+
                                                                                     |
                                                                                     v
                                                                          +----------------------+
                                                                          |  Azure Table Storage |
                                                                          +----------------------+
```

### 2.4. Scalability

The system is designed to be horizontally scalable to meet the demands of 100,000 address spaces and millions of IPs.

*   **Stateless Services:** All microservices (API Gateway, Frontend, DataAccess) will be stateless. This allows us to add or remove instances of any service based on load without affecting user sessions.
*   **Database Partitioning:** Azure Table Storage is inherently scalable. By using `AddressSpaceId` as the `PartitionKey`, we ensure that data for different address spaces is distributed across multiple storage nodes. This allows for high throughput for concurrent requests across different address spaces.
*   **Container Orchestration:** Using Kubernetes or Azure Container Apps allows for automatic scaling (Horizontal Pod Autoscaler) based on CPU and memory usage.

### 2.5. High Availability

To achieve 99.9% uptime, we will implement redundancy at every layer of the architecture.

*   **Geo-Redundancy:** For disaster recovery, Azure Table Storage will be configured with Geo-Redundant Storage (GRS).
*   **Multi-Region Deployment:** The microservices will be deployed across multiple Azure regions. Azure Traffic Manager will be used to route traffic to the closest and healthiest region.
*   **Redundant Instances:** Within each region, we will run at least two instances of each microservice behind a load balancer to prevent a single point of failure.
*   **Health Checks:** Each microservice will expose a `/health` endpoint that the orchestrator (Kubernetes) and load balancer can use to determine if an instance is healthy and able to receive traffic.

### 2.6. Performance & Caching

*   **Response Time:** The goal is a median response time of < 200ms for 95% of API calls.
*   **Caching Strategy:** A distributed Redis cache will be introduced to reduce latency and load on the DataAccess service and the database.
    *   **Cached Data:** Frequently accessed and rarely changing data will be cached. This includes:
        *   Tag definitions (`TagDefinitions` table).
        *   Resolved inheritable tags for a given IP node.
        *   User roles and permissions.
    *   **Cache Invalidation:** A cache-aside pattern will be used. When data is updated (e.g., a tag is modified), the corresponding cache key will be explicitly invalidated.

## 3. Data Model (Azure Table Storage)

The data will be stored in Azure Table Storage, which provides a scalable and cost-effective NoSQL database solution. The schema is designed to support efficient querying and partitioning.

### 3.1. Partitioning Strategy

To ensure scalability and performance, data will be partitioned primarily by `AddressSpaceId`. All data for a given address space (its IPs and Tag definitions) will reside in the same Azure Storage partition. This allows for fast, single-partition queries for most operations.

*   **PartitionKey:** For address-space-specific data, the `PartitionKey` will be the `AddressSpaceId`. For system-global data (like the list of address spaces or users), a fixed sentinel value like `SYSTEM` will be used.
*   **RowKey:** The `RowKey` provides the unique identifier for an entity within a given `PartitionKey`.

---

### 3.2. Table Schemas

Below is the complete schema for each table. All complex objects (lists, dictionaries) will be serialized to a JSON string before being stored in a string property.

#### 3.2.1. `AddressSpaces`

Stores the top-level address space containers. This data is global.

*   **PartitionKey:** `SYSTEM` (string)
*   **RowKey:** `AddressSpaceId` (string, GUID)
*   **Name:** (string) - The human-readable name of the address space (e.g., "Corporate-Network").
*   **Description:** (string) - A detailed description of the address space.
*   **CreatedOn:** (DateTimeOffset) - Timestamp of creation.
*   **ModifiedOn:** (DateTimeOffset) - Timestamp of the last modification.

#### 3.2.2. `Tags`

Stores the definition for each tag, including its rules, attributes, and implications. This table merges the concept of Tag Implications directly into the tag's definition.

*   **PartitionKey:** `AddressSpaceId` (string, GUID)
*   **RowKey:** `Name` (string) - The name of the tag (e.g., "Region", "Datacenter").
*   **Description:** (string) - A detailed description of the tag's purpose.
*   **Type:** (string) - The tag's inheritance behavior. Must be one of: `"Inheritable"` or `"NonInheritable"`.
*   **KnownValues:** (string, JSON) - A JSON-serialized array of allowed values for this tag. If null or empty, any value is allowed.
    *   *Example:* `["USEast", "USWest", "EuropeWest"]`
*   **Attributes:** (string, JSON) - A JSON-serialized dictionary defining meta-attributes for tag values.
    *   *Structure:* `Dictionary<string, Dictionary<string, string>>` which is `AttributeName -> [TagValue -> AttributeValue]`
    *   *Example:* For a "Datacenter" tag, this could store a "DisplayName" attribute: `{"DisplayName": {"AMS05": "Amsterdam-05", "DBL01": "Dublin-01"}}`
*   **Implies:** (string, JSON) - A JSON-serialized dictionary defining the tag's implication logic for inheritable tags.
    *   *Structure:* `Dictionary<string, Dictionary<string, string>>` which is `ImpliedTagName -> [TagValue -> ImpliedTagValue]`
    *   *Example:* For a "Datacenter" tag, this defines that `Datacenter=AMS05` implies `Region=EuropeWest`: `{"Region": {"AMS05": "EuropeWest"}}`
*   **CreatedOn:** (DateTimeOffset) - Timestamp of creation.
*   **ModifiedOn:** (DateTimeOffset) - Timestamp of the last modification.

#### 3.2.3. `IpAddresses`

Stores the core IP CIDR blocks and their directly assigned tags.

*   **PartitionKey:** `AddressSpaceId` (string, GUID)
*   **RowKey:** `IpId` (string, GUID) - A unique identifier for the IP node.
*   **Prefix:** (string) - The CIDR notation of the IP block (e.g., "10.1.2.0/24").
*   **ParentId:** (string, GUID) - The `IpId` of the parent node in the tree. The root of the address space will have a null or empty ParentId.
*   **Tags:** (string, JSON) - A JSON-serialized dictionary of key-value pairs for tags **directly applied** to this IP node. This includes both `Inheritable` and `NonInheritable` tags. The inheritance logic is applied at read-time by traversing the hierarchy.
    *   *Example:* `{"Region": "USWest", "Owner": "team-alpha"}` (Here, "Region" is inheritable, "Owner" is not).
*   **CreatedOn:** (DateTimeOffset) - Timestamp of creation.
*   **ModifiedOn:** (DateTimeOffset) - Timestamp of the last modification.

#### 3.2.4. `Users`

Stores user account information for authentication.

*   **PartitionKey:** `SYSTEM` (string)
*   **RowKey:** `Username` (string) - The unique username.
*   **PasswordHash:** (string) - The hashed password for the user.

#### 3.2.5. `UserRoles`

Maps users to roles for authorization (RBAC).

*   **PartitionKey:** `Username` (string)
*   **RowKey:** `AddressSpaceId` (string, GUID) - The address space the role applies to. For a `SystemAdmin`, this can be the `SYSTEM` sentinel value.
*   **Role:** (string) - The name of the role. Must be one of: `"SystemAdmin"`, `"AddressSpaceAdmin"`, `"AddressSpaceOperator"`, `"AddressSpaceViewer"`.

## 4. API Design (RESTful)

The API provides a versioned, RESTful interface for interacting with the IPAM system.

### 4.1. General Principles

*   **Versioning:** The API is versioned via the URL path (e.g., `/api/v1`).
*   **Authentication:** All requests must be authenticated. Clients must include a valid JWT in the `Authorization` header as a Bearer token.
    ```
    Authorization: Bearer <your_jwt_token>
    ```
*   **Error Handling:** Errors are returned with standard HTTP status codes and a consistent JSON error object.
    ```json
    // Example Error Response
    {
      "error": {
        "code": "NotFound",
        "message": "The requested resource was not found."
      }
    }
    ```

---

### 4.2. Resource: Address Spaces

Base Path: `/api/v1/addressspaces`

#### GET /addressspaces
*   **Description:** Retrieves a list of all address spaces, with optional filtering.
*   **Query Parameters:**
    *   `nameContains` (string): Filters spaces where the name contains this value.
    *   `createdBefore` (DateTime): Filters spaces created before this timestamp.
    *   `createdAfter` (DateTime): Filters spaces created after this timestamp.
*   **Success Response (200 OK):**
    ```json
    [
      {
        "id": "guid-1",
        "name": "Corporate-Network",
        "description": "Main corporate network.",
        "createdOn": "2023-10-27T10:00:00Z",
        "modifiedOn": "2023-10-27T10:00:00Z"
      }
    ]
    ```

#### POST /addressspaces
*   **Description:** Creates a new address space.
*   **Request Body:**
    ```json
    {
      "name": "New-Cloud-VPC",
      "description": "VPC for new cloud services."
    }
    ```
*   **Success Response (201 Created):** Returns the newly created address space object, including its generated ID.
    ```json
    {
      "id": "guid-2",
      "name": "New-Cloud-VPC",
      "description": "VPC for new cloud services.",
      "createdOn": "2023-10-28T11:00:00Z",
      "modifiedOn": "2023-10-28T11:00:00Z"
    }
    ```
*   **Error Responses:** `400 Bad Request` (if name is missing or invalid).

#### GET /addressspaces/{spaceId}
*   **Description:** Retrieves a specific address space by its ID.
*   **Success Response (200 OK):** Returns the address space object.
*   **Error Responses:** `404 Not Found`.

#### PUT /addressspaces/{spaceId}
*   **Description:** Updates an existing address space.
*   **Request Body:**
    ```json
    {
      "name": "New-Cloud-VPC",
      "description": "Updated description for the VPC."
    }
    ```
*   **Success Response (200 OK):** Returns the updated address space object.
*   **Error Responses:** `400 Bad Request`, `404 Not Found`.

#### DELETE /addressspaces/{spaceId}
*   **Description:** Deletes an address space and all its contents (tags, IPs). This is a permanent action.
*   **Success Response (204 No Content):**
*   **Error Responses:** `404 Not Found`.

---

### 4.3. Resource: Tags

Base Path: `/api/v1/addressspaces/{spaceId}/tags`

#### GET /tags
*   **Description:** Retrieves all tag definitions for a given address space.
*   **Query Parameters:**
    *   `nameContains` (string): Filters tags where the name contains this value.
*   **Success Response (200 OK):**
    ```json
    [
      {
        "name": "Region",
        "description": "Geographical region.",
        "type": "Inheritable",
        "knownValues": ["USEast", "USWest"],
        "attributes": {},
        "implies": {}
      }
    ]
    ```

#### POST /tags
*   **Description:** Creates a new tag definition.
*   **Request Body:**
    ```json
    {
      "name": "Datacenter",
      "description": "The specific datacenter.",
      "type": "Inheritable",
      "implies": {
        "Region": {
          "AMS05": "EuropeWest",
          "DBL01": "EuropeWest"
        }
      }
    }
    ```
*   **Success Response (201 Created):** Returns the new tag definition.

#### GET /tags/{tagName}
*   **Description:** Retrieves a specific tag definition.
*   **Success Response (200 OK):** Returns the tag definition object.
*   **Error Responses:** `404 Not Found`.

#### PUT /tags/{tagName}
*   **Description:** Updates a tag definition.
*   **Request Body:** The full tag definition object.
*   **Success Response (200 OK):** Returns the updated tag definition.
*   **Error Responses:** `400 Bad Request`, `404 Not Found`.

#### DELETE /tags/{tagName}
*   **Description:** Deletes a tag definition. Fails if the tag is currently in use by any IP.
*   **Success Response (204 No Content):**
*   **Error Responses:** `404 Not Found`, `400 Bad Request` (if tag is in use).

---

### 4.4. Resource: IP Addresses

Base Path: `/api/v1/addressspaces/{spaceId}/ips`

#### GET /ips
*   **Description:** Finds IPs within an address space.
*   **Query Parameters:**
    *   `prefix` (string): Finds the IP with this exact CIDR prefix.
    *   `tags` (string): Finds IPs with these tags. Format: `tag1=value1,tag2=value2`.
    *   `parentId` (string): Finds direct children of this IP ID.
*   **Success Response (200 OK):**
    ```json
    [
      {
        "id": "guid-ip-1",
        "prefix": "10.0.0.0/8",
        "parentId": null,
        "tags": { "Region": "USWest" },
        "resolvedTags": { "Region": "USWest" }
      }
    ]
    ```

#### POST /ips
*   **Description:** Creates a new IP address/subnet.
*   **Request Body:**
    ```json
    {
      "prefix": "10.1.0.0/16",
      "parentId": "guid-ip-1",
      "tags": {
        "Owner": "team-networking"
      }
    }
    ```
*   **Success Response (201 Created):** Returns the new IP object.

#### GET /ips/{ipId}
*   **Description:** Retrieves a specific IP by its ID. The response includes `resolvedTags`, which computes the full set of inherited tags.
*   **Success Response (200 OK):**
    ```json
    {
      "id": "guid-ip-2",
      "prefix": "10.1.0.0/16",
      "parentId": "guid-ip-1",
      "tags": { "Owner": "team-networking" },
      "resolvedTags": {
        "Region": "USWest",
        "Owner": "team-networking"
      }
    }
    ```
*   **Error Responses:** `404 Not Found`.

#### PUT /ips/{ipId}
*   **Description:** Updates an IP. This is primarily used to add, modify, or remove tags on the IP.
*   **Request Body:**
    ```json
    {
      "tags": {
        "Owner": "team-alpha",
        "Service": "new-service"
      }
    }
    ```
*   **Success Response (200 OK):** Returns the updated IP object.

#### DELETE /ips/{ipId}
*   **Description:** Deletes an IP. When a parent is deleted, its inheritable tags are pushed down to its children.
*   **Success Response (204 No Content):**
*   **Error Responses:** `404 Not Found`.

## 5. Security

*   **Authentication:** JWT Bearer tokens validated by the API Gateway.
*   **Authorization:** RBAC checks performed in the Frontend Service via a custom middleware.
*   **Data Encryption:** Data at rest in Azure Storage is encrypted by default. Data in transit will be encrypted using TLS.
*   **Audit Logs:** All mutating operations (create, update, delete) will be logged for auditing purposes, including the user who performed the action and the timestamp.

## 6. Deployment

Each microservice will have a `Dockerfile`. A `docker-compose.yml` will be provided for local development. For production, Kubernetes manifests will be created.

## 7. Testing

*   **Unit Tests (xUnit, Moq):** Target >= 90% code coverage.
*   **Integration Tests:** Verify interactions between services.
*   **Functional Tests:** End-to-end API tests.
*   **Stress Tests:** Use a tool like k6 or JMeter to load test the API and determine the system's breaking point and bottlenecks.

## 8. Monitoring & Telemetry

We will use a unified OpenTelemetry-based solution feeding into Azure Application Insights.

*   **Distributed Tracing:** All incoming requests to the API Gateway will be assigned a correlation ID, which will be propagated to all downstream microservices. This allows for end-to-end tracing of a single request.
*   **Metrics:** Key performance indicators (KPIs) will be collected for each service:
    *   Request Rate, Error Rate, Latency (The "Golden Signals").
    *   CPU and Memory utilization.
    *   Cache hit/miss ratio.
*   **Alerting:** Alerts will be configured in Azure Monitor for critical conditions such as:
    *   Service availability drops below 99.9%.
    *   API response latency exceeds 500ms for a sustained period.
    *   Error rate exceeds 1%.

## 9. Cost Estimation

A detailed cost analysis will be maintained, but the primary Azure costs will be:

*   **Azure Container Apps / Kubernetes Service:** Cost depends on the number and size of container instances. Can be optimized with auto-scaling.
*   **Azure Table Storage:** Billed based on the amount of data stored and the number of transactions. The partitioning strategy is designed to be cost-effective.
*   **Azure Redis Cache:** Billed based on the cache size and tier.
*   **Azure Application Insights:** Billed based on data ingestion volume.
*   **Azure Traffic Manager:** Billed based on the number of DNS queries.

Initial estimates should be based on a small-scale deployment, with costs expected to grow linearly with usage.