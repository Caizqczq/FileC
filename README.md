# FileC - 个人云存储系统

FileC是一个基于ASP.NET Core开发的个人云存储系统，提供文件上传、下载、分享和管理功能。系统采用MySQL作为后端数据库，支持用户认证、文件夹管理、批量操作以及文件分享功能。本项目展示了多种现代编程技术和设计模式的应用。

## 功能特点

- **用户管理**：
  - 用户注册与登录
  - 个人资料管理
  - 存储空间限制（默认1GB）

- **文件操作**：
  - 文件上传与下载
  - 文件重命名
  - 文件移动
  - 文件删除
  - 批量文件操作

- **目录管理**：
  - 创建文件夹
  - 文件夹重命名
  - 文件夹移动
  - 嵌套文件夹支持

- **文件分享**：
  - 生成分享链接
  - 公开/私有文件设置
  - 分享链接管理

- **搜索功能**：
  - 按文件名搜索
  - 按文件描述搜索

## 技术栈

- **后端**：
  - ASP.NET Core 8.0
  - Entity Framework Core
  - ASP.NET Core Identity（用户认证）
  - MySQL数据库

- **前端**：
  - Razor视图
  - Bootstrap 5
  - jQuery
  - 响应式设计

## 系统要求

- .NET 8.0 SDK 或更高版本
- MySQL 8.0 或更高版本
- 支持ASP.NET Core的服务器环境（IIS, Nginx, Apache等）

## 安装与配置

1. **克隆项目**：
   ```
   git clone https://your-repository/FileC.git
   cd FileC
   ```

2. **配置数据库连接**：
   打开`appsettings.json`文件，修改数据库连接字符串：
   ```json
   "ConnectionStrings": {
     "DefaultConnection": "Server=localhost;Port=3306;Database=FileC;User=your_username;Password=your_password;"
   }
   ```

3. **应用数据库迁移**：
   ```
   dotnet ef database update
   ```

4. **运行项目**：
   ```
   dotnet run
   ```

5. **访问应用**：
   打开浏览器，访问 `https://localhost:5001`

## 项目结构

- **Controllers/**：包含应用的控制器
  - `AccountController.cs`：用户账户管理
  - `FileController.cs`：文件操作控制
  - `HomeController.cs`：首页控制
  - `ShareController.cs`：文件分享功能

- **Models/**：数据模型
  - `ApplicationUser.cs`：用户模型
  - `DirectoryModel.cs`：文件夹模型
  - `FileModel.cs`：文件模型
  - `FileShareModel.cs`：文件分享模型

- **Services/**：业务逻辑服务
  - `FileService.cs`：文件处理核心服务
  - `ShareService.cs`：文件分享服务

- **Views/**：用户界面视图
  - 按控制器组织的各类视图文件

- **wwwroot/**：静态资源
  - `css/`：样式文件
  - `js/`：JavaScript文件
  - `uploads/`：用户上传的文件存储目录

## 数据库设计

项目采用关系型数据库MySQL，主要包含以下实体：

1. **ApplicationUser**：用户信息表
   - 继承自IdentityUser
   - 添加了存储空间管理等扩展字段

2. **FileModel**：文件信息表
   - 记录文件名、路径、大小、类型等属性
   - 与用户和目录建立外键关系

3. **DirectoryModel**：目录信息表
   - 实现目录层级结构
   - 支持嵌套文件夹管理

4. **FileShareModel**：文件分享表
   - 记录分享链接、访问权限等信息
   - 与文件建立外键关系

## 安全考虑

1. **身份验证与授权**：
   - 基于ASP.NET Core Identity实现用户认证
   - 基于角色的访问控制
   - 防止未授权访问

2. **数据保护**：
   - 密码哈希存储
   - 敏感数据加密
   - SQL注入防护

3. **输入验证**：
   - 服务器端数据验证
   - 防止XSS攻击
   - 防止CSRF攻击

4. **文件安全**：
   - 文件类型验证
   - 病毒扫描集成
   - 文件访问控制

## Docker部署

1. **将项目文件上传到服务器**
   
   ```bash
   git clone https://github.com/Caizqczq/FileC.git
   cd ~/filec
   ```
   
2. **修改配置**
   
   编辑`docker-compose.yml`文件，设置安全的密码:
   ```bash
   vim docker-compose.yml
   ```
   
   修改以下部分:
   ```yaml
   environment:
     - MYSQL_ROOT_PASSWORD=设置安全的root密码
     - MYSQL_PASSWORD=设置安全的用户密码
     
   # 确保ConnectionStrings__DefaultConnection中的密码与MYSQL_PASSWORD一致
   ```

3. **启动容器**
   
   ```bash
   docker compose up -d
   ```
   
4. **应用数据库迁移**
   ```bash
   # 等待几秒钟，确保MySQL已启动
   sleep 30
   
   # 查看应用容器ID
   docker ps
   
   # 执行迁移
   docker exec -it 容器ID dotnet ef database update
   ```

5. **访问应用**
   - 使用浏览器访问 `http://your-server-ip`
   - 应用应该正常运行，可以注册账户并开始使用

### 维护操作

1. **查看日志**
   ```bash
   # 应用日志
   docker logs filec-app
   
   # MySQL日志
   docker logs mysql
   
   # 实时日志
   docker logs -f filec-app
   ```

2. **备份数据**
   ```bash
   # 备份MySQL数据
   docker exec mysql mysqldump -u root -p"root_password" FileC > backup.sql
   
   # 备份上传的文件
   docker cp filec-app:/app/wwwroot/uploads ./uploads-backup
   ```

3. **更新应用**
   ```bash
   # 拉取最新代码
   git pull
   
   # 重新构建并启动
   docker-compose down
   docker-compose build
   docker-compose up -d
   ```

4. **性能监控**
   ```bash
   # 查看容器资源使用情况
   docker stats
   ```

### 常见问题排查

1. **应用无法启动**
   ```bash
   # 查看容器状态
   docker ps -a
   
   # 检查应用日志
   docker logs filec-app
   ```

2. **数据库连接错误**
   
   ```bash
   # 检查MySQL容器是否正常运行
   docker ps | grep mysql
   
   # 检查MySQL日志
   docker logs mysql
   
   # 验证网络连接
   docker exec filec-app ping mysql
   ```
   
3. **文件上传问题**
   ```bash
   # 检查卷权限
   docker exec filec-app ls -la /app/wwwroot/uploads
   ```

4. **容器崩溃自动重启**
   
   由于我们已在`docker-compose.yml`中设置了`restart: unless-stopped`，容器会在崩溃后自动重启。但如果频繁崩溃，应检查日志找出根本原因。



## 其他部分

- **Controllers/**：包含应用的控制器
  - `AccountController.cs`：用户账户管理
  - `FileController.cs`：文件操作控制
  - `HomeController.cs`：首页控制
  - `ShareController.cs`：文件分享功能

- **Models/**：数据模型
  - `ApplicationUser.cs`：用户模型
  - `DirectoryModel.cs`：文件夹模型
  - `FileModel.cs`：文件模型
  - `FileShareModel.cs`：文件分享模型

- **Services/**：业务逻辑服务
  - `FileService.cs`：文件处理核心服务
  - `ShareService.cs`：文件分享服务

- **Views/**：用户界面视图
  - 按控制器组织的各类视图文件

- **wwwroot/**：静态资源
  - `css/`：样式文件
  - `js/`：JavaScript文件
  - `uploads/`：用户上传的文件存储目录

## 数据库设计

项目采用关系型数据库MySQL，主要包含以下实体：

1. **ApplicationUser**：用户信息表
   - 继承自IdentityUser
   - 添加了存储空间管理等扩展字段

2. **FileModel**：文件信息表
   - 记录文件名、路径、大小、类型等属性
   - 与用户和目录建立外键关系

3. **DirectoryModel**：目录信息表
   - 实现目录层级结构
   - 支持嵌套文件夹管理

4. **FileShareModel**：文件分享表
   - 记录分享链接、访问权限等信息
   - 与文件建立外键关系

## 安全考虑

1. **身份验证与授权**：
   - 基于ASP.NET Core Identity实现用户认证
   - 基于角色的访问控制
   - 防止未授权访问

2. **数据保护**：
   - 密码哈希存储
   - 敏感数据加密
   - SQL注入防护

3. **输入验证**：
   - 服务器端数据验证
   - 防止XSS攻击
   - 防止CSRF攻击

4. **文件安全**：
   - 文件类型验证
   - 病毒扫描集成
   - 文件访问控制

## 许可证

[指定适合您项目的许可证]

## 开发者信息

[您的联系信息，如邮箱、网站等]
