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

## 部署指南

### 阿里云服务器部署

#### 1. 准备工作

- 购买并登录阿里云ECS服务器（建议选择Ubuntu或CentOS系统）
- 开放端口：80（HTTP）、443（HTTPS）、3306（MySQL，如需远程访问）

#### 2. 安装环境

```bash
# 添加Microsoft包仓库
wget https://packages.microsoft.com/config/ubuntu/20.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb

# 安装.NET SDK
sudo apt-get update
sudo apt-get install -y apt-transport-https
sudo apt-get install -y dotnet-sdk-8.0

# 安装MySQL
sudo apt-get install -y mysql-server

# 配置MySQL
sudo mysql_secure_installation
```

#### 3. 配置MySQL数据库

```bash
# 登录MySQL
sudo mysql -u root -p

# 在MySQL命令行中执行：
CREATE DATABASE FileC CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
CREATE USER 'filec_user'@'localhost' IDENTIFIED BY '设置一个安全密码';
GRANT ALL PRIVILEGES ON FileC.* TO 'filec_user'@'localhost';
FLUSH PRIVILEGES;
EXIT;
```

#### 4. 部署应用

```bash
# 创建应用目录
sudo mkdir -p /var/www/filec
cd /var/www/filec

# 上传项目
# 方法1：从本地上传（在本地执行）
scp -r ./publish/* username@your-server-ip:/var/www/filec/

# 方法2：直接从Git克隆（在服务器执行）
git clone https://your-repository/FileC.git .
dotnet publish -c Release -o ./publish
mv ./publish/* .
rm -rf ./publish

# 确保文件权限正确
sudo chown -R www-data:www-data /var/www/filec
sudo chmod -R 755 /var/www/filec
sudo chmod -R 775 /var/www/filec/wwwroot/uploads
```

#### 5. 配置应用

```bash
# 编辑appsettings.json
nano appsettings.json
```

修改数据库连接字符串：
```json
"ConnectionStrings": {
  "DefaultConnection": "Server=localhost;Port=3306;Database=FileC;User=filec_user;Password=你的密码;"
}
```

#### 6. 应用数据库迁移

```bash
cd /var/www/filec
dotnet ef database update
```

#### 7. 设置Nginx反向代理

```bash
# 安装Nginx
sudo apt-get install -y nginx

# 配置Nginx站点
sudo nano /etc/nginx/sites-available/filec
```

添加以下配置：
```nginx
server {
    listen 80;
    server_name your-domain-or-ip;
    
    location / {
        proxy_pass http://localhost:5000;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection keep-alive;
        proxy_set_header Host $host;
        proxy_cache_bypass $http_upgrade;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }
}
```

```bash
# 启用站点配置
sudo ln -s /etc/nginx/sites-available/filec /etc/nginx/sites-enabled/
sudo nginx -t  # 测试配置
sudo systemctl restart nginx
```

#### 8. 创建并启动应用服务

```bash
# 创建systemd服务
sudo nano /etc/systemd/system/filec.service
```

添加以下内容：
```ini
[Unit]
Description=FileC Web App
After=network.target

[Service]
WorkingDirectory=/var/www/filec
ExecStart=/usr/bin/dotnet /var/www/filec/FileC.dll
Restart=always
RestartSec=10
SyslogIdentifier=filec
User=www-data
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=ASPNETCORE_URLS=http://localhost:5000

[Install]
WantedBy=multi-user.target
```

```bash
# 启动服务
sudo systemctl enable filec
sudo systemctl start filec
sudo systemctl status filec  # 检查状态
```

#### 9. 访问应用

- 使用浏览器访问 `http://your-server-ip` 或者 `http://your-domain`
- 注册账户并测试功能是否正常

#### 10. 设置HTTPS（推荐）

```bash
# 安装Certbot
sudo apt-get install -y certbot python3-certbot-nginx

# 获取并设置SSL证书
sudo certbot --nginx -d your-domain.com
```

#### 11. 常见问题排查

1. **应用无法访问**
   - 检查防火墙：`sudo ufw status`
   - 检查服务状态：`sudo systemctl status filec`
   - 查看应用日志：`sudo journalctl -u filec`

2. **数据库连接错误**
   - 确认MySQL运行状态：`sudo systemctl status mysql`
   - 检查连接字符串是否正确
   - 验证数据库用户权限

3. **文件上传失败**
   - 检查上传目录权限：`ls -la /var/www/filec/wwwroot/uploads/`
   - 确保www-data用户有写入权限

#### 12. 维护建议

- **备份数据库**：
  ```bash
  # 设置定时备份（每天凌晨2点）
  echo "0 2 * * * mysqldump -u filec_user -p'密码' FileC > /backup/filec_\$(date +\%Y\%m\%d).sql" | sudo tee -a /var/spool/cron/crontabs/root
  ```

- **更新应用**：
  ```bash
  cd /var/www/filec
  # 从Git更新代码（如果使用Git）
  git pull
  # 编译应用
  dotnet publish -c Release -o ./temp_publish
  # 停止服务
  sudo systemctl stop filec
  # 备份当前应用
  sudo cp -r /var/www/filec /var/www/filec_backup_$(date +%Y%m%d)
  # 替换应用
  sudo cp -r ./temp_publish/* .
  # 更新权限
  sudo chown -R www-data:www-data /var/www/filec
  # 启动服务
  sudo systemctl start filec
  ```

这套部署方案更加简洁直观，适合在阿里云服务器上快速部署您的FileC项目。您只需按照步骤操作，不需要Docker或其他复杂的容器技术。

## Docker部署指南（阿里云）

### 前期准备

1. **购买阿里云ECS服务器**
   - 推荐选择Ubuntu 20.04或更高版本
   - 至少2核4GB内存配置
   - 开放端口：80, 443, 3306(可选)

2. **安装Docker和Docker Compose**
   ```bash
   # 更新包索引
   sudo apt-get update

   # 安装必要的包
   sudo apt-get install -y apt-transport-https ca-certificates curl software-properties-common

   # 添加Docker官方GPG密钥
   curl -fsSL https://download.docker.com/linux/ubuntu/gpg | sudo apt-key add -

   # 设置Docker仓库
   sudo add-apt-repository "deb [arch=amd64] https://download.docker.com/linux/ubuntu $(lsb_release -cs) stable"

   # 更新包索引
   sudo apt-get update

   # 安装Docker CE
   sudo apt-get install -y docker-ce

   # 安装Docker Compose
   sudo curl -L "https://github.com/docker/compose/releases/download/v2.18.1/docker-compose-$(uname -s)-$(uname -m)" -o /usr/local/bin/docker-compose
   sudo chmod +x /usr/local/bin/docker-compose

   # 将当前用户添加到docker组（免sudo运行docker）
   sudo usermod -aG docker $USER

   # 应用组更改（需要重新登录终端）
   newgrp docker
   ```

### 部署应用

1. **将项目文件上传到服务器**
   ```bash
   # 在本地打包项目（排除bin/obj等不需要的文件）
   # Windows PowerShell:
   Compress-Archive -Path .\* -DestinationPath filec.zip -Force

   # 上传到服务器
   scp filec.zip username@your-server-ip:~/
   
   # 在服务器上解压
   ssh username@your-server-ip
   mkdir -p ~/filec
   unzip filec.zip -d ~/filec
   cd ~/filec
   ```

   或者使用Git:
   ```bash
   git clone https://your-repository-url.git ~/filec
   cd ~/filec
   ```

2. **修改配置**
   
   编辑`docker-compose.yml`文件，设置安全的密码:
   ```bash
   nano docker-compose.yml
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
   docker-compose up -d
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

### 配置HTTPS（可选但推荐）

1. **安装Certbot**
   ```bash
   sudo apt-get update
   sudo apt-get install -y certbot
   ```

2. **获取证书**
   ```bash
   sudo certbot certonly --standalone -d your-domain.com
   ```

3. **创建证书目录**
   ```bash
   mkdir -p ~/filec/certs
   sudo cp /etc/letsencrypt/live/your-domain.com/fullchain.pem ~/filec/certs/
   sudo cp /etc/letsencrypt/live/your-domain.com/privkey.pem ~/filec/certs/
   sudo chown -R $USER:$USER ~/filec/certs
   ```

4. **修改docker-compose.yml**
   ```bash
   nano docker-compose.yml
   ```
   
   添加证书卷和环境变量:
   ```yaml
   filec-app:
     # ...existing config...
     volumes:
       - filec-uploads:/app/wwwroot/uploads
       - ./certs:/app/certs
     environment:
       # ...existing environment...
       - ASPNETCORE_URLS=https://+:8081;http://+:8080
       - ASPNETCORE_HTTPS_PORT=443
       - ASPNETCORE_Kestrel__Certificates__Default__Path=/app/certs/fullchain.pem
       - ASPNETCORE_Kestrel__Certificates__Default__KeyPath=/app/certs/privkey.pem
   ```

5. **重启容器**
   ```bash
   docker-compose down
   docker-compose up -d
   ```

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

### 安全建议

1. **限制端口访问**
   - 只开放必要的端口（80和443）
   - 考虑禁用3306端口的外部访问

2. **定期更新**
   ```bash
   # 更新容器镜像
   docker-compose pull
   docker-compose up -d
   
   # 更新系统
   sudo apt-get update && sudo apt-get upgrade
   ```

3. **定期备份**
   - 设置定时任务自动备份数据库和文件
   - 将备份存储在外部位置

这种Docker部署方式比传统部署简单得多，您只需几个命令就能完成整个部署过程，并且能够轻松管理和维护应用。

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
