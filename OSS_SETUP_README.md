# CloudFileHub - 阿里云OSS配置指南

本文档详细说明了如何为 CloudFileHub 云端文件管理系统配置阿里云OSS对象存储。

## 概述

CloudFileHub 已集成阿里云OSS SDK，支持将文件直接存储到阿里云对象存储中，提供高可用、高可靠的云端文件存储解决方案。

## 前置条件

### 1. 阿里云账号设置
- 已注册阿里云账号
- 已完成实名认证
- 账户余额充足（OSS按使用量计费）

### 2. 创建OSS存储桶
1. 登录 [阿里云OSS控制台](https://oss.console.aliyun.com/)
2. 点击"创建Bucket"
3. 配置Bucket信息：
   - **Bucket名称**：全局唯一，建议使用项目相关名称
   - **地域**：选择距离用户最近的地域
   - **存储类型**：标准存储（适合频繁访问）
   - **读写权限**：私有（推荐，通过应用控制访问）
   - **版本控制**：根据需要选择
   - **服务端加密**：建议启用AES256

### 3. 获取访问凭证
1. 在OSS控制台右上角，点击头像 → "访问控制"
2. 进入RAM访问控制台
3. 创建用户：
   - 用户名：`cloudfilehub-oss-user`
   - 访问方式：选择"编程访问"
   - 保存生成的AccessKey ID和AccessKey Secret
4. 为用户分配OSS权限：
   - 添加权限：`AliyunOSSFullAccess`（完整权限）
   - 或自定义权限策略（推荐生产环境）

## 配置步骤

### 1. 项目配置文件

编辑 `CloudFileHub/appsettings.json` 文件：

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Port=3306;Database=CloudFileHub;User=root;Password=123456;"
  },
  "AliyunOSS": {
    "AccessKeyId": "您的AccessKey ID",
    "AccessKeySecret": "您的AccessKey Secret", 
    "BucketName": "您的存储桶名称",
    "Endpoint": "oss-cn-hangzhou.aliyuncs.com",
    "Region": "cn-hangzhou"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

### 2. 开发环境配置

编辑 `CloudFileHub/appsettings.Development.json` 文件：

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Port=3306;Database=CloudFileHub;User=root;Password=123456;"
  },
  "AliyunOSS": {
    "AccessKeyId": "您的开发环境AccessKey ID",
    "AccessKeySecret": "您的开发环境AccessKey Secret",
    "BucketName": "您的开发环境存储桶名称",
    "Endpoint": "oss-cn-hangzhou.aliyuncs.com", 
    "Region": "cn-hangzhou"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore.Database.Command": "Information"
    }
  }
}
```

### 3. 配置参数说明

| 参数 | 说明 | 示例 |
|------|------|------|
| AccessKeyId | 阿里云访问密钥ID | LTAI5t... |
| AccessKeySecret | 阿里云访问密钥Secret | 保密信息 |
| BucketName | OSS存储桶名称 | cloudfilehub-storage |
| Endpoint | OSS服务端点 | oss-cn-hangzhou.aliyuncs.com |
| Region | OSS地域代码 | cn-hangzhou |

### 4. 常用地域端点

| 地域 | 地域代码 | 外网端点 |
|------|----------|----------|
| 华东1（杭州） | cn-hangzhou | oss-cn-hangzhou.aliyuncs.com |
| 华东2（上海） | cn-shanghai | oss-cn-shanghai.aliyuncs.com |
| 华北1（青岛） | cn-qingdao | oss-cn-qingdao.aliyuncs.com |
| 华北2（北京） | cn-beijing | oss-cn-beijing.aliyuncs.com |
| 华南1（深圳） | cn-shenzhen | oss-cn-shenzhen.aliyuncs.com |

## 环境变量配置（可选）

为了更好的安全性，可以通过环境变量设置敏感信息：

```bash
# Linux/Mac
export AliyunOSS__AccessKeyId="您的AccessKey ID"
export AliyunOSS__AccessKeySecret="您的AccessKey Secret"
export AliyunOSS__BucketName="您的存储桶名称"

# Windows
set AliyunOSS__AccessKeyId=您的AccessKey ID
set AliyunOSS__AccessKeySecret=您的AccessKey Secret  
set AliyunOSS__BucketName=您的存储桶名称
```

## Docker环境配置

在 `docker-compose.yml` 中配置环境变量：

```yaml
services:
  clouddisk:
    build:
      context: .
      dockerfile: CloudFileHub/Dockerfile
    environment:
      - AliyunOSS__AccessKeyId=您的AccessKey ID
      - AliyunOSS__AccessKeySecret=您的AccessKey Secret
      - AliyunOSS__BucketName=您的存储桶名称
      - AliyunOSS__Endpoint=oss-cn-hangzhou.aliyuncs.com
      - AliyunOSS__Region=cn-hangzhou
    # ... 其他配置
```

## 功能特性

### 已实现的OSS功能

1. **文件上传**
   - 支持单文件和多文件上传
   - 自动生成唯一文件名（UUID + 原文件名）
   - 按用户ID组织文件夹结构：`{userId}/{uuid}_{filename}`

2. **文件下载**
   - 直接从OSS下载文件流
   - 支持预签名URL（7天有效期）
   - 批量下载（ZIP压缩）

3. **文件删除**
   - 删除数据库记录的同时删除OSS文件
   - 批量删除支持

4. **文件管理**
   - 检查文件是否存在
   - 获取文件元数据
   - 生成临时访问URL

### 文件存储结构

```
存储桶根目录/
├── user1/
│   ├── uuid1_document.pdf
│   ├── uuid2_image.jpg
│   └── uuid3_video.mp4
├── user2/
│   ├── uuid4_presentation.pptx
│   └── uuid5_spreadsheet.xlsx
└── user3/
    └── uuid6_archive.zip
```

## 安全建议

### 1. 访问控制
- 使用RAM用户而非主账号AccessKey
- 遵循最小权限原则，仅授予必要的OSS权限
- 定期轮换AccessKey

### 2. 网络安全
- 生产环境建议使用内网端点（VPC环境）
- 启用OSS防盗链功能
- 配置跨域资源共享（CORS）规则

### 3. 数据保护
- 启用服务端加密（SSE-OSS或SSE-KMS）
- 配置生命周期规则，自动清理过期文件
- 启用版本控制和备份

### 4. 监控告警
- 开启OSS访问日志
- 配置费用告警
- 监控异常访问模式

## 故障排除

### 常见错误

1. **AccessDenied 错误**
   - 检查AccessKey权限
   - 确认Bucket读写权限
   - 验证RAM用户权限策略

2. **NoSuchBucket 错误**
   - 确认Bucket名称正确
   - 检查Endpoint是否匹配Bucket所在地域

3. **InvalidAccessKeyId 错误**
   - 验证AccessKey ID格式
   - 确认AccessKey状态为启用

4. **SignatureDoesNotMatch 错误**
   - 检查AccessKey Secret是否正确
   - 确认时间同步

### 调试步骤

1. **检查配置**
   ```bash
   # 验证配置文件
   cat CloudFileHub/appsettings.json | grep -A 6 "AliyunOSS"
   ```

2. **测试连接**
   ```bash
   # 使用阿里云CLI测试
   ossutil ls oss://your-bucket-name/
   ```

3. **查看日志**
   ```bash
   # 查看应用日志
   docker logs cloudfilehub-app
   ```

## 成本优化

### 1. 存储类型选择
- **标准存储**：频繁访问的文件
- **低频访问**：不常访问但需要快速获取的文件
- **归档存储**：长期保存的文件

### 2. 生命周期管理
```json
{
  "Rules": [
    {
      "ID": "DeleteOldFiles",
      "Status": "Enabled",
      "Expiration": {
        "Days": 365
      }
    }
  ]
}
```

### 3. 数据传输优化
- 启用OSS传输加速
- 使用CDN减少直接访问OSS的请求
- 合理配置缓存策略

## 扩展功能

### 1. 图片处理
利用OSS图片处理服务：
```csharp
// 生成缩略图URL
string thumbnailUrl = $"{baseUrl}?x-oss-process=image/resize,w_200,h_200";
```

### 2. 视频处理
集成阿里云媒体处理服务：
```csharp
// 视频转码和截图
string videoProcessUrl = $"{baseUrl}?x-oss-process=video/snapshot,t_1000";
```

### 3. 内容分发
结合阿里云CDN加速：
```csharp
// CDN加速域名
string cdnUrl = baseUrl.Replace("oss-cn-hangzhou.aliyuncs.com", "your-cdn-domain.com");
```

## 最佳实践

1. **性能优化**
   - 使用OSS内网端点（VPC环境）
   - 启用OSS传输加速
   - 合理设置并发连接数

2. **安全加固**
   - 启用MFA（多因素认证）
   - 配置IP白名单
   - 使用STS临时凭证

3. **监控运维**
   - 配置CloudMonitor监控
   - 设置费用预算告警
   - 定期备份重要数据

4. **合规要求**
   - 了解数据存储地域要求
   - 配置数据保留策略
   - 实施访问审计

---

通过以上配置，CloudFileHub 将能够充分利用阿里云OSS的强大功能，为用户提供稳定、安全、高效的云端文件存储服务。 