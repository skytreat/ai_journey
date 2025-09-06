# IPAM PowerShell CLI

这是一个用于管理IP地址管理系统的命令行工具。它通过调用IPAM C#客户端与API网关进行交互。

## 先决条件

- .NET 6.0 SDK
- 运行中的IPAM API网关服务

## 构建

```bash
dotnet build
```

## 使用方法

### Address Space 命令

```bash
# 创建地址空间
ipam-cli create-address-space --name "MyAddressSpace" --description "My first address space"

# 获取地址空间
ipam-cli get-address-space "address-space-id"

# 获取所有地址空间
ipam-cli get-address-spaces

# 更新地址空间
ipam-cli update-address-space --id "address-space-id" --name "NewName" --description "Updated description"

# 删除地址空间
ipam-cli delete-address-space "address-space-id"
```

### Tag 命令

```bash
# 创建标签
ipam-cli create-tag --address-space-id "address-space-id" --name "Environment" --description "Environment tag"

# 获取标签
ipam-cli get-tag "address-space-id" "tag-name"

# 获取所有标签
ipam-cli get-tags "address-space-id"

# 更新标签
ipam-cli update-tag --address-space-id "address-space-id" --name "Environment" --description "Updated environment tag"

# 删除标签
ipam-cli delete-tag "address-space-id" "tag-name"
```

### IP Address 命令

```bash
# 创建IP地址
ipam-cli create-ip --address-space-id "address-space-id" --prefix "192.168.1.0/24"

# 获取IP地址
ipam-cli get-ip "address-space-id" "ip-id"

# 获取所有IP地址
ipam-cli get-ips "address-space-id"
```

## 运行

```bash
dotnet run -- [command] [options]
```

例如：

```bash
dotnet run -- get-address-spaces
```