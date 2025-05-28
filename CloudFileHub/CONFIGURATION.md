# CloudFileHub 配置说明

## 环境配置

本项目需要配置数据库连接和阿里云OSS存储服务。为了安全起见，这些敏感信息不会提交到Git仓库中。

## 配置步骤

### 1. 创建配置文件

复制示例配置文件并重命名：

```bash
# 复制主配置文件
cp appsettings.example.json appsettings.json

# 复制开发环境配置文件
cp appsettings.Development.example.json appsettings.Development.json
```

### 2. 配置数据库连接

编辑 `appsettings.json` 文件，修改数据库连接字符串：

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Port=3306;Database=CloudFileHub;User=root;Password=您的MySQL密码;"
  }
}
```

### 3. 配置阿里云OSS

在 `appsettings.json` 和 `appsettings.Development.json` 中配置OSS信息：

```json
{
  "AliyunOSS": {
    "AccessKeyId": "您的AccessKeyId",
    "AccessKeySecret": "您的AccessKeySecret", 
    "BucketName": "您的存储桶名称",
    "Endpoint": "oss-cn-beijing.aliyuncs.com",
    "Region": "cn-beijing"
  }
}
```

### 4. 阿里云OSS配置指南

1. **登录阿里云控制台**
   - 访问 [阿里云控制台](https://oss.console.aliyun.com/)

2. **创建AccessKey**
   - 前往"访问控制 > 用户"
   - 创建新用户或使用现有用户
   - 为用户添加 `AliyunOSSFullAccess` 权限
   - 创建AccessKey，记录AccessKeyId和AccessKeySecret

3. **创建OSS存储桶**
   - 前往"对象存储OSS > 存储桶列表"
   - 创建新的存储桶
   - 选择合适的地域（建议与应用服务器同地域）
   - 设置访问权限为"私有"

4. **配置参数说明**
   - `AccessKeyId`: 访问密钥ID
   - `AccessKeySecret`: 访问密钥Secret（请妥善保管）
   - `BucketName`: 存储桶名称
   - `Endpoint`: OSS地域节点，格式：`oss-地域.aliyuncs.com`
   - `Region`: 地域代码，如：`cn-beijing`

### 5. Docker环境配置

如果使用Docker部署，可以通过环境变量设置：

```bash
# 设置环境变量
export MYSQL_PASSWORD="您的MySQL密码"
export OSS_ACCESS_KEY_ID="您的AccessKeyId"
export OSS_ACCESS_KEY_SECRET="您的AccessKeySecret"
export OSS_BUCKET_NAME="您的存储桶名称"
```

### 安全注意事项

⚠️ **重要提醒**：
- 切勿将包含真实密钥的配置文件提交到Git仓库
- 定期轮换AccessKey
- 为OSS用户设置最小权限原则
- 在生产环境中使用强密码

## 故障排除

### 常见问题

1. **OSS上传失败**
   - 检查AccessKey是否正确
   - 确认存储桶名称和地域设置
   - 验证网络连接

2. **数据库连接失败**
   - 确认MySQL服务正在运行
   - 检查连接字符串中的用户名和密码
   - 验证数据库是否存在

3. **权限问题**
   - 确认OSS用户有足够的权限
   - 检查存储桶的访问控制设置 