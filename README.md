# CloudFileHub - 云端文件管理系统

CloudFileHub 是一个基于 ASP.NET Core 8.0 开发的现代化文件管理系统，集成了阿里云OSS对象存储，提供安全、高效的文件存储和管理解决方案。

## 核心功能

### 🔐 用户认证与授权
- **安全登录**：基于 ASP.NET Core Identity 的用户认证系统
- **密码安全**：强密码策略，确保账户安全
- **会话管理**：安全的用户会话控制

### 📁 文件管理
- **文件上传**：支持多种文件格式上传到阿里云OSS
- **文件下载**：快速、安全的文件下载
- **文件预览**：支持常见文件格式的在线预览
- **批量操作**：支持批量下载、删除文件
- **文件搜索**：快速搜索文件名和描述

### 🗂️ 目录管理
- **目录结构**：创建、重命名、删除目录
- **目录导航**：面包屑导航，轻松浏览文件结构
- **批量移动**：支持文件和目录的批量移动操作

### ☁️ 云存储集成
- **阿里云OSS**：所有文件存储在阿里云OSS对象存储中
- **高可用性**：99.9%的数据持久性保证
- **全球访问**：通过CDN加速，全球快速访问
- **自动备份**：云端自动备份，数据安全可靠

### 📊 存储管理
- **存储配额**：用户存储空间管理
- **使用统计**：实时显示存储使用情况
- **文件统计**：文件数量和大小统计

### 🔗 文件分享
- **公开分享**：生成分享链接，方便文件共享
- **访问控制**：灵活的文件访问权限设置
- **分享管理**：管理已分享的文件链接

## 技术栈

### 后端技术
- **框架**：ASP.NET Core 8.0
- **数据库**：MySQL 8.0 with Entity Framework Core
- **认证**：ASP.NET Core Identity
- **对象存储**：阿里云OSS SDK
- **ORM**：Entity Framework Core with Pomelo MySQL Provider

### 前端技术
- **UI框架**：Bootstrap 5
- **JavaScript**：原生ES6+
- **图标**：Bootstrap Icons
- **响应式设计**：移动端友好的响应式布局

### 基础设施
- **容器化**：Docker & Docker Compose
- **数据库管理**：phpMyAdmin
- **云存储**：阿里云OSS对象存储
- **反向代理**：可配合Nginx使用

## 快速开始

### 前置要求
- .NET 8.0 SDK
- MySQL 8.0
- Docker & Docker Compose（可选）
- 阿里云OSS存储桶

### 本地开发
1. **克隆项目**
   ```bash
   git clone <repository-url>
   cd CloudFileHub
   ```

2. **配置数据库连接**
   编辑 `CloudFileHub/appsettings.json`：
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Server=localhost;Port=3306;Database=CloudFileHub;User=root;Password=your_password;"
     }
   }
   ```

3. **配置阿里云OSS**
   在 `CloudFileHub/appsettings.json` 中配置OSS参数：
   ```json
   {
     "AliyunOSS": {
       "AccessKeyId": "您的AccessKey ID",
       "AccessKeySecret": "您的AccessKey Secret",
       "BucketName": "您的存储桶名称",
       "Endpoint": "oss-cn-hangzhou.aliyuncs.com",
       "Region": "cn-hangzhou"
     }
   }
   ```

4. **运行数据库迁移**
   ```bash
   cd CloudFileHub
   dotnet ef database update
   ```

5. **启动应用**
   ```bash
   dotnet run
   ```

### Docker 部署
1. **使用 Docker Compose 一键部署**
   ```bash
   docker-compose up -d
   ```

2. **访问应用**
   - 应用地址：http://localhost:5000
   - 数据库管理：http://localhost:8080 (phpMyAdmin)

## 项目结构
```
CloudFileHub/
├── CloudFileHub/                   # 主要应用程序
│   ├── Controllers/                # MVC控制器
│   ├── Models/                     # 数据模型和视图模型
│   ├── Views/                      # Razor视图
│   ├── Services/                   # 业务服务层
│   ├── Data/                       # 数据访问层
│   ├── Migrations/                 # 数据库迁移文件
│   ├── wwwroot/                    # 静态资源
│   ├── CloudFileHub.csproj        # 项目文件
│   └── Program.cs                  # 应用程序入口
├── docker-compose.yml              # Docker编排文件
├── CloudFileHub.sln               # 解决方案文件
├── OSS_SETUP_README.md           # OSS配置说明
└── README.md                      # 项目说明文档
```

## 配置说明

### 数据库配置
系统使用MySQL作为主数据库，支持：
- 用户信息存储
- 文件元数据管理
- 目录结构维护
- 分享链接管理

### OSS配置
详细的OSS配置说明请参考 [OSS_SETUP_README.md](OSS_SETUP_README.md)

### 环境变量
支持通过环境变量覆盖配置：
- `ConnectionStrings__DefaultConnection`：数据库连接字符串
- `AliyunOSS__AccessKeyId`：OSS访问密钥ID
- `AliyunOSS__AccessKeySecret`：OSS访问密钥Secret
- `AliyunOSS__BucketName`：OSS存储桶名称

## API文档

### 文件操作API
- `GET /File` - 文件列表页面
- `POST /File/Upload` - 文件上传
- `GET /File/Download/{id}` - 文件下载
- `DELETE /File/Delete/{id}` - 删除文件

### 目录操作API
- `GET /File/CreateDirectory` - 创建目录页面
- `POST /File/CreateDirectory` - 创建目录
- `DELETE /File/DeleteDirectory/{id}` - 删除目录

### 分享功能API
- `GET /Share` - 分享管理页面
- `POST /Share/Create` - 创建分享链接
- `GET /Share/Public/{shareId}` - 公开访问分享文件

## 安全特性

### 数据安全
- **加密传输**：全站HTTPS加密
- **访问控制**：基于用户的文件访问权限
- **SQL注入防护**：使用参数化查询防止SQL注入
- **XSS防护**：输入验证和输出编码

### 文件安全
- **病毒扫描**：可集成病毒扫描服务
- **文件类型限制**：支持配置允许的文件类型
- **大小限制**：文件大小和总存储空间限制
- **访问日志**：详细的文件访问日志记录

## 性能优化

### 文件处理
- **异步上传**：异步文件上传处理
- **断点续传**：支持大文件断点续传
- **缓存策略**：智能缓存减少重复下载
- **CDN加速**：结合阿里云CDN实现全球加速

### 数据库优化
- **连接池**：数据库连接池管理
- **索引优化**：关键字段索引优化
- **查询优化**：Entity Framework查询优化

## 监控与维护

### 日志记录
- **结构化日志**：使用Serilog记录结构化日志
- **错误追踪**：详细的错误堆栈追踪
- **性能监控**：关键操作性能监控

### 健康检查
- **应用健康检查**：内置健康检查端点
- **数据库连接检查**：数据库连接状态监控
- **OSS连接检查**：云存储连接状态监控

## 部署建议

### 生产环境
- **负载均衡**：使用Nginx进行负载均衡
- **数据库**：MySQL主从复制
- **缓存**：Redis缓存层
- **监控**：Prometheus + Grafana监控

### 备份策略
- **数据库备份**：定期数据库备份
- **OSS备份**：跨区域数据备份
- **配置备份**：配置文件版本控制

## 许可证
本项目采用 MIT 许可证。详情请参阅 [LICENSE](LICENSE) 文件。

## 贡献指南
欢迎提交Issue和Pull Request来改进这个项目。

## 联系我们
如有问题或建议，请通过以下方式联系：
- 提交Issue：[GitHub Issues](链接)
- 邮件：your-email@example.com

---

**CloudFileHub** - 让文件管理更简单，让云存储更安全！

