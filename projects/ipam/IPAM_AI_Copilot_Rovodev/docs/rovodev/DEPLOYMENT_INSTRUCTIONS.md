# IPAM System - Production Deployment Instructions

## 📋 **Overview**

This document provides comprehensive deployment instructions for the IPAM (IP Address Management) system, including all components, configurations, and operational procedures.

---

## 🏗️ **System Architecture**

```
┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐
│   API Gateway   │    │  Frontend API   │    │ DataAccess API  │
│  (Port 5000)    │───▶│  (Port 5001)    │───▶│  (Port 5002)    │
│                 │    │                 │    │                 │
└─────────────────┘    └─────────────────┘    └─────────────────┘
         │                       │                       │
         ▼                       ▼                       ▼
┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐
│   Load Balancer │    │   Health Checks │    │ Azure Table     │
│   (HTTPS/TLS)   │    │   Monitoring    │    │   Storage       │
└─────────────────┘    └─────────────────┘    └─────────────────┘
         │                                             │
         ▼                                             ▼
┌─────────────────┐                         ┌─────────────────┐
│   Web Portal    │                         │   Telemetry &   │
│  (Port 5003)    │                         │   Monitoring    │
└─────────────────┘                         └─────────────────┘
```

---

## 🔧 **Prerequisites**

### **Infrastructure Requirements:**
- **.NET 8 Runtime** or later
- **Azure Storage Account** (Standard_LRS or better)
- **Load Balancer** (Azure Load Balancer, NGINX, or similar)
- **SSL/TLS Certificates** for HTTPS
- **Monitoring Solution** (Azure Monitor, Prometheus, etc.)

### **Resource Requirements:**

#### **Minimum (Development/Testing):**
- **CPU**: 2 vCPUs
- **Memory**: 4 GB RAM
- **Storage**: 20 GB SSD
- **Network**: 100 Mbps

#### **Production (Small-Medium):**
- **CPU**: 4 vCPUs
- **Memory**: 8 GB RAM
- **Storage**: 100 GB SSD
- **Network**: 1 Gbps

#### **Production (Large/Enterprise):**
- **CPU**: 8+ vCPUs
- **Memory**: 16+ GB RAM
- **Storage**: 500+ GB SSD
- **Network**: 10+ Gbps

### **Azure Storage Requirements:**
- **Performance Tier**: Standard (Hot tier recommended)
- **Replication**: LRS (minimum), GRS (recommended for production)
- **Access Tier**: Hot (for active data)
- **Backup**: Point-in-time restore enabled

---

## 🚀 **Deployment Steps**

### **Phase 1: Infrastructure Setup**

#### **1.1 Azure Storage Account Setup**
```bash
# Create resource group
az group create --name ipam-rg --location eastus

# Create storage account
az storage account create \
    --name ipamstorageaccount \
    --resource-group ipam-rg \
    --location eastus \
    --sku Standard_GRS \
    --kind StorageV2 \
    --access-tier Hot

# Get connection string
az storage account show-connection-string \
    --name ipamstorageaccount \
    --resource-group ipam-rg \
    --query connectionString \
    --output tsv
```

#### **1.2 SSL Certificate Setup**
```bash
# Option 1: Let's Encrypt (Free)
certbot certonly --standalone -d your-ipam-domain.com

# Option 2: Azure Key Vault (Recommended for production)
az keyvault certificate create \
    --vault-name ipam-keyvault \
    --name ipam-ssl-cert \
    --policy "$(az keyvault certificate get-default-policy)"
```

### **Phase 2: Application Deployment**

#### **2.1 Build and Package Applications**
```bash
# Clone repository
git clone https://github.com/your-org/ipam-system.git
cd ipam-system

# Build all projects
dotnet build --configuration Release

# Publish Gateway
dotnet publish src/Ipam.Gateway/Ipam.Gateway.csproj \
    --configuration Release \
    --output ./publish/gateway \
    --self-contained false

# Note: The system uses Ipam.Gateway as the main API Gateway service

# Publish Frontend API
dotnet publish src/Ipam.Frontend/Ipam.Frontend.csproj \
    --configuration Release \
    --output ./publish/frontend \
    --self-contained false

# Publish Web Portal (if using)
dotnet publish src/Ipam.WebPortal/Ipam.WebPortal.csproj \
    --configuration Release \
    --output ./publish/webportal \
    --self-contained false
```

#### **2.2 Configuration Files**

**Create `appsettings.Production.json` for Gateway:**
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "Jwt": {
    "Issuer": "IpamSystem",
    "Audience": "IpamUsers",
    "Key": "${JWT_SECRET_KEY}",
    "ExpirationMinutes": 60
  },
  "Ocelot": {
    "Routes": [
      {
        "DownstreamPathTemplate": "/api/{everything}",
        "DownstreamScheme": "https",
        "DownstreamHostAndPorts": [
          {
            "Host": "ipam-frontend.internal",
            "Port": 5001
          }
        ],
        "UpstreamPathTemplate": "/api/{everything}",
        "UpstreamHttpMethod": [ "GET", "POST", "PUT", "DELETE" ]
      }
    ],
    "GlobalConfiguration": {
      "BaseUrl": "https://your-ipam-domain.com"
    }
  },
  "AllowedHosts": "*"
}
```

**Create `appsettings.Production.json` for Frontend:**
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "ConnectionStrings": {
    "AzureTableStorage": "${AZURE_STORAGE_CONNECTION_STRING}"
  },
  "DataAccess": {
    "EnableCaching": true,
    "CacheDurationMinutes": 5,
    "MaxRetryAttempts": 3,
    "RetryDelaySeconds": 1
  },
  "AllowedHosts": "*"
}
```

**Create `appsettings.Production.json` for Data Access API:**
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "ConnectionStrings": {
    "AzureTableStorage": "${AZURE_STORAGE_CONNECTION_STRING}"
  },
  "Jwt": {
    "Issuer": "IpamSystem",
    "Audience": "IpamUsers",
    "Key": "${JWT_SECRET_KEY}"
  },
  "DataAccess": {
    "EnableCaching": true,
    "CacheDurationMinutes": 5,
    "MaxRetryAttempts": 3,
    "RetryDelaySeconds": 1
  },
  "AllowedHosts": "*"
}
```

**Create `appsettings.Production.json` for Web Portal:**
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "ApiSettings": {
    "BaseUrl": "https://your-ipam-domain.com/api",
    "Timeout": 30
  },
  "AllowedHosts": "*"
}
```

#### **2.3 Environment Variables Setup**
```bash
# Create environment file
cat > .env << EOF
# Azure Storage
AZURE_STORAGE_CONNECTION_STRING="DefaultEndpointsProtocol=https;AccountName=ipamstorageaccount;AccountKey=YOUR_ACCOUNT_KEY;EndpointSuffix=core.windows.net"

# JWT Configuration
JWT_SECRET_KEY="your-super-secret-jwt-key-minimum-256-bits"

# Application Settings
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=https://+:5000;http://+:5001

# Monitoring
APPLICATIONINSIGHTS_CONNECTION_STRING="InstrumentationKey=your-app-insights-key"
EOF
```

### **Phase 3: Service Configuration**

#### **3.1 Systemd Service Files (Linux)**

**Gateway Service (`/etc/systemd/system/ipam-gateway.service`):**
```ini
[Unit]
Description=IPAM Gateway Service
After=network.target

[Service]
Type=notify
User=ipam
Group=ipam
WorkingDirectory=/opt/ipam/gateway
ExecStart=/usr/bin/dotnet Ipam.Gateway.dll
Restart=always
RestartSec=10
KillSignal=SIGINT
SyslogIdentifier=ipam-gateway
Environment=ASPNETCORE_ENVIRONMENT=Production
EnvironmentFile=/opt/ipam/config/.env

[Install]
WantedBy=multi-user.target
```

**Frontend Service (`/etc/systemd/system/ipam-frontend.service`):**
```ini
[Unit]
Description=IPAM Frontend API Service
After=network.target

[Service]
Type=notify
User=ipam
Group=ipam
WorkingDirectory=/opt/ipam/frontend
ExecStart=/usr/bin/dotnet Ipam.Frontend.dll
Restart=always
RestartSec=10
KillSignal=SIGINT
SyslogIdentifier=ipam-frontend
Environment=ASPNETCORE_ENVIRONMENT=Production
EnvironmentFile=/opt/ipam/config/.env

[Install]
WantedBy=multi-user.target
```

#### **3.2 Windows Service Configuration**
```powershell
# Install as Windows Service using NSSM
nssm install "IPAM Gateway" "C:\Program Files\dotnet\dotnet.exe"
nssm set "IPAM Gateway" AppParameters "C:\inetpub\ipam\gateway\Ipam.Gateway.dll"
nssm set "IPAM Gateway" AppDirectory "C:\inetpub\ipam\gateway"
nssm set "IPAM Gateway" AppEnvironmentExtra "ASPNETCORE_ENVIRONMENT=Production"

nssm install "IPAM Frontend" "C:\Program Files\dotnet\dotnet.exe"
nssm set "IPAM Frontend" AppParameters "C:\inetpub\ipam\frontend\Ipam.Frontend.dll"
nssm set "IPAM Frontend" AppDirectory "C:\inetpub\ipam\frontend"
nssm set "IPAM Frontend" AppEnvironmentExtra "ASPNETCORE_ENVIRONMENT=Production"
```

### **Phase 4: Load Balancer Configuration**

#### **4.1 NGINX Configuration**
```nginx
upstream ipam_gateway {
    server 127.0.0.1:5000;
    # Add more servers for load balancing
    # server 127.0.0.1:5002;
}

server {
    listen 80;
    server_name your-ipam-domain.com;
    return 301 https://$server_name$request_uri;
}

server {
    listen 443 ssl http2;
    server_name your-ipam-domain.com;

    ssl_certificate /etc/letsencrypt/live/your-ipam-domain.com/fullchain.pem;
    ssl_certificate_key /etc/letsencrypt/live/your-ipam-domain.com/privkey.pem;
    ssl_protocols TLSv1.2 TLSv1.3;
    ssl_ciphers ECDHE-RSA-AES256-GCM-SHA512:DHE-RSA-AES256-GCM-SHA512:ECDHE-RSA-AES256-GCM-SHA384:DHE-RSA-AES256-GCM-SHA384;
    ssl_prefer_server_ciphers off;

    location / {
        proxy_pass http://ipam_gateway;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
        proxy_set_header X-Forwarded-Host $host;
        proxy_set_header X-Forwarded-Port $server_port;
    }

    location /health {
        proxy_pass http://ipam_gateway/api/health;
        access_log off;
    }
}
```

#### **4.2 Azure Load Balancer Configuration**
```bash
# Create public IP
az network public-ip create \
    --resource-group ipam-rg \
    --name ipam-public-ip \
    --sku Standard \
    --allocation-method Static

# Create load balancer
az network lb create \
    --resource-group ipam-rg \
    --name ipam-load-balancer \
    --sku Standard \
    --public-ip-address ipam-public-ip \
    --frontend-ip-name ipam-frontend-ip \
    --backend-pool-name ipam-backend-pool

# Create health probe
az network lb probe create \
    --resource-group ipam-rg \
    --lb-name ipam-load-balancer \
    --name ipam-health-probe \
    --protocol Http \
    --port 5000 \
    --path /api/health/live

# Create load balancing rule
az network lb rule create \
    --resource-group ipam-rg \
    --lb-name ipam-load-balancer \
    --name ipam-lb-rule \
    --protocol Tcp \
    --frontend-port 443 \
    --backend-port 5000 \
    --frontend-ip-name ipam-frontend-ip \
    --backend-pool-name ipam-backend-pool \
    --probe-name ipam-health-probe
```

---

## 🐳 **Docker Deployment**

### **5.1 Dockerfile for Gateway**
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 5000
EXPOSE 5001

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/Ipam.Gateway/Ipam.Gateway.csproj", "src/Ipam.Gateway/"]
COPY ["src/Ipam.DataAccess/Ipam.DataAccess.csproj", "src/Ipam.DataAccess/"]
RUN dotnet restore "src/Ipam.Gateway/Ipam.Gateway.csproj"
COPY . .
WORKDIR "/src/src/Ipam.Gateway"
RUN dotnet build "Ipam.Gateway.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Ipam.Gateway.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Ipam.Gateway.dll"]
```

### **5.2 Docker Compose Configuration**
```yaml
version: '3.8'

services:
  ipam-gateway:
    build:
      context: .
      dockerfile: src/Ipam.Gateway/Dockerfile
    ports:
      - "5000:5000"
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ASPNETCORE_URLS=http://+:5000
      - JWT_SECRET_KEY=${JWT_SECRET_KEY}
    depends_on:
      - ipam-frontend
    restart: unless-stopped

  ipam-frontend:
    build:
      context: .
      dockerfile: src/Ipam.Frontend/Dockerfile
    ports:
      - "5001:5001"
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ASPNETCORE_URLS=http://+:5001
      - DataAccessApi__BaseUrl=http://ipam-frontend:5001
      - DataAccessApi__ApiKey=${JWT_SECRET_KEY}
    restart: unless-stopped

  nginx:
    image: nginx:alpine
    ports:
      - "80:80"
      - "443:443"
    volumes:
      - ./nginx.conf:/etc/nginx/nginx.conf:ro
      - ./ssl:/etc/ssl/certs:ro
    depends_on:
      - ipam-gateway
    restart: unless-stopped
```

### **5.3 Kubernetes Deployment**
```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: ipam-gateway
  namespace: ipam
spec:
  replicas: 3
  selector:
    matchLabels:
      app: ipam-gateway
  template:
    metadata:
      labels:
        app: ipam-gateway
    spec:
      containers:
      - name: ipam-gateway
        image: your-registry/ipam-gateway:latest
        ports:
        - containerPort: 5000
        env:
        - name: ASPNETCORE_ENVIRONMENT
          value: "Production"
        - name: JWT_SECRET_KEY
          valueFrom:
            secretKeyRef:
              name: ipam-secrets
              key: jwt-secret-key
        livenessProbe:
          httpGet:
            path: /api/health/live
            port: 5000
          initialDelaySeconds: 30
          periodSeconds: 10
        readinessProbe:
          httpGet:
            path: /api/health/ready
            port: 5000
          initialDelaySeconds: 5
          periodSeconds: 5
        resources:
          requests:
            memory: "256Mi"
            cpu: "250m"
          limits:
            memory: "512Mi"
            cpu: "500m"

---
apiVersion: apps/v1
kind: Deployment
metadata:
  name: ipam-dataaccess-api
  namespace: ipam
spec:
  replicas: 2
  selector:
    matchLabels:
      app: ipam-dataaccess-api
  template:
    metadata:
      labels:
        app: ipam-dataaccess-api
    spec:
      containers:
      - name: ipam-dataaccess-api
        image: your-registry/ipam-dataaccess-api:latest
        ports:
        - containerPort: 5002
        env:
        - name: ASPNETCORE_ENVIRONMENT
          value: "Production"
        - name: ConnectionStrings__AzureTableStorage
          valueFrom:
            secretKeyRef:
              name: ipam-secrets
              key: azure-storage-connection-string
        - name: JWT_SECRET_KEY
          valueFrom:
            secretKeyRef:
              name: ipam-secrets
              key: jwt-secret-key
        livenessProbe:
          httpGet:
            path: /api/health/live
            port: 5002
          initialDelaySeconds: 30
          periodSeconds: 10
        readinessProbe:
          httpGet:
            path: /api/health/ready
            port: 5002
          initialDelaySeconds: 5
          periodSeconds: 5
        resources:
          requests:
            memory: "256Mi"
            cpu: "250m"
          limits:
            memory: "512Mi"
            cpu: "500m"

---
apiVersion: v1
kind: Service
metadata:
  name: ipam-dataaccess-api-service
  namespace: ipam
spec:
  selector:
    app: ipam-dataaccess-api
  ports:
  - protocol: TCP
    port: 80
    targetPort: 5002
  type: ClusterIP

---
apiVersion: v1
kind: Service
metadata:
  name: ipam-gateway-service
  namespace: ipam
spec:
  selector:
    app: ipam-gateway
  ports:
  - protocol: TCP
    port: 80
    targetPort: 5000
  type: LoadBalancer
```

---

## 🔐 **Security Configuration**

### **6.1 JWT Configuration**
```bash
# Generate secure JWT key (256-bit minimum)
openssl rand -base64 32

# Store in Azure Key Vault
az keyvault secret set \
    --vault-name ipam-keyvault \
    --name jwt-secret-key \
    --value "your-generated-key"
```

### **6.2 Azure Storage Security**
```bash
# Enable firewall rules
az storage account update \
    --name ipamstorageaccount \
    --resource-group ipam-rg \
    --default-action Deny

# Add allowed IP ranges
az storage account network-rule add \
    --account-name ipamstorageaccount \
    --resource-group ipam-rg \
    --ip-address "your-server-ip"

# Enable encryption at rest
az storage account update \
    --name ipamstorageaccount \
    --resource-group ipam-rg \
    --encryption-services blob table
```

### **6.3 Application Security Headers**
```csharp
// Add to Program.cs or Startup.cs
app.Use(async (context, next) =>
{
    context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Add("X-Frame-Options", "DENY");
    context.Response.Headers.Add("X-XSS-Protection", "1; mode=block");
    context.Response.Headers.Add("Strict-Transport-Security", "max-age=31536000; includeSubDomains");
    context.Response.Headers.Add("Content-Security-Policy", "default-src 'self'");
    await next();
});
```

---

## 📊 **Monitoring & Observability**

### **7.1 Application Insights Configuration**
```json
{
  "ApplicationInsights": {
    "ConnectionString": "InstrumentationKey=your-key;IngestionEndpoint=https://eastus-8.in.applicationinsights.azure.com/"
  },
  "Logging": {
    "ApplicationInsights": {
      "LogLevel": {
        "Default": "Information"
      }
    }
  }
}
```

### **7.2 Health Check Endpoints**
```bash
# Basic health check
curl https://your-ipam-domain.com/api/health

# Detailed health check
curl https://your-ipam-domain.com/api/health/detailed

# Kubernetes liveness probe
curl https://your-ipam-domain.com/api/health/live

# Kubernetes readiness probe
curl https://your-ipam-domain.com/api/health/ready

# Performance metrics
curl https://your-ipam-domain.com/api/health/metrics
```

### **7.3 Prometheus Metrics Configuration**
```yaml
# prometheus.yml
global:
  scrape_interval: 15s

scrape_configs:
  - job_name: 'ipam-gateway'
    static_configs:
      - targets: ['ipam-gateway:5000']
    metrics_path: '/metrics'
    scrape_interval: 10s

  - job_name: 'ipam-frontend'
    static_configs:
      - targets: ['ipam-frontend:5001']
    metrics_path: '/metrics'
    scrape_interval: 10s
```

---

## 🔄 **Backup & Recovery**

### **8.1 Azure Table Storage Backup**
```bash
# Enable point-in-time restore
az storage account blob-service-properties update \
    --account-name ipamstorageaccount \
    --resource-group ipam-rg \
    --enable-restore-policy true \
    --restore-days 30

# Create backup script
cat > backup-tables.sh << 'EOF'
#!/bin/bash
DATE=$(date +%Y%m%d_%H%M%S)
BACKUP_DIR="/backups/ipam/$DATE"
mkdir -p "$BACKUP_DIR"

# Export all tables
az storage entity export \
    --account-name ipamstorageaccount \
    --table-name AddressSpaces \
    --destination "$BACKUP_DIR/AddressSpaces.json"

az storage entity export \
    --account-name ipamstorageaccount \
    --table-name IpNodes \
    --destination "$BACKUP_DIR/IpNodes.json"

az storage entity export \
    --account-name ipamstorageaccount \
    --table-name Tags \
    --destination "$BACKUP_DIR/Tags.json"

echo "Backup completed: $BACKUP_DIR"
EOF

chmod +x backup-tables.sh
```

### **8.2 Automated Backup Schedule**
```bash
# Add to crontab for daily backups at 2 AM
crontab -e
0 2 * * * /opt/ipam/scripts/backup-tables.sh
```

---

## 🚀 **Deployment Checklist**

### **Pre-Deployment:**
- [ ] Azure Storage Account created and configured
- [ ] SSL certificates obtained and installed
- [ ] Environment variables configured
- [ ] Security settings applied
- [ ] Monitoring tools configured
- [ ] Backup procedures tested

### **Deployment:**
- [ ] Applications built and published
- [ ] Services installed and configured
- [ ] Load balancer configured
- [ ] Health checks passing
- [ ] SSL/TLS working correctly
- [ ] Authentication/authorization tested

### **Post-Deployment:**
- [ ] Performance monitoring active
- [ ] Backup schedule configured
- [ ] Security scan completed
- [ ] Load testing performed
- [ ] Documentation updated
- [ ] Team training completed

---

## 🔧 **Troubleshooting**

### **Common Issues:**

#### **Service Won't Start:**
```bash
# Check service status
systemctl status ipam-gateway
systemctl status ipam-frontend

# Check logs
journalctl -u ipam-gateway -f
journalctl -u ipam-frontend -f

# Check configuration
dotnet Ipam.Gateway.dll --environment Production --dry-run
```

#### **Azure Storage Connection Issues:**
```bash
# Test connection string
az storage account show-connection-string \
    --name ipamstorageaccount \
    --resource-group ipam-rg

# Test table access
az storage table list \
    --connection-string "your-connection-string"
```

#### **SSL Certificate Issues:**
```bash
# Check certificate validity
openssl x509 -in /path/to/certificate.crt -text -noout

# Test SSL connection
openssl s_client -connect your-ipam-domain.com:443
```

#### **Performance Issues:**
```bash
# Check resource usage
top
htop
iotop

# Check application metrics
curl https://your-ipam-domain.com/api/health/metrics

# Check Azure Storage metrics
az monitor metrics list \
    --resource ipamstorageaccount \
    --metric-names Transactions
```

---

## 📞 **Support & Maintenance**

### **Log Locations:**
- **Linux**: `/var/log/ipam/` or `journalctl -u ipam-*`
- **Windows**: Windows Event Log or `C:\Logs\IPAM\`
- **Docker**: `docker logs container-name`
- **Kubernetes**: `kubectl logs deployment/ipam-gateway -n ipam`

### **Configuration Files:**
- **Application Settings**: `appsettings.Production.json`
- **Environment Variables**: `.env` or system environment
- **Service Configuration**: `/etc/systemd/system/ipam-*.service`
- **Load Balancer**: `/etc/nginx/sites-available/ipam`

### **Monitoring Dashboards:**
- **Application Insights**: Azure Portal → Application Insights
- **Prometheus/Grafana**: `http://monitoring-server:3000`
- **Azure Monitor**: Azure Portal → Monitor

### **Emergency Contacts:**
- **Development Team**: dev-team@yourcompany.com
- **Operations Team**: ops-team@yourcompany.com
- **On-Call Engineer**: +1-555-ON-CALL

---

## 📚 **Additional Resources**

- **API Documentation**: `https://your-ipam-domain.com/swagger`
- **System Architecture**: `docs/IPAM_System_Design.md`
- **Performance Analysis**: `PERFORMANCE_ANALYSIS_REPORT.md`
- **Concurrency Analysis**: `CONCURRENCY_ANALYSIS_REPORT.md`
- **Test Coverage**: `TEST_COVERAGE_ANALYSIS.md`

---

*This deployment guide is maintained by the IPAM development team. For questions or updates, please contact the development team or create an issue in the project repository.*