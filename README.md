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

## 项目技术亮点

### 必选项实现

#### 1. 面向对象编程
项目充分利用了面向对象编程范式，通过类的封装、继承和多态实现系统功能：
- **继承关系**：`ApplicationUser`类继承自`IdentityUser`，扩展了用户属性
- **封装**：所有模型类（`FileModel`、`DirectoryModel`等）均封装了相关属性和行为
- **依赖注入**：通过构造函数注入服务，降低了组件间耦合度
- **接口分离**：服务层与控制器层明确分离，实现了关注点分离

```csharp
// 对象继承示例
public class ApplicationUser : IdentityUser
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public long StorageUsed { get; set; } = 0;
    public long StorageLimit { get; set; } = 1073741824; // 1GB default
}
```

#### 2. 用户界面设计
项目采用了现代化的用户界面设计原则：
- **响应式设计**：使用Bootstrap框架实现响应式布局，适配不同设备屏幕
- **用户友好**：直观的文件管理界面，支持拖放操作
- **交互反馈**：操作结果实时反馈，表单验证即时提示
- **视图组件复用**：通过Razor视图组件复用UI元素，保持一致性

#### 3. 数据库交互
项目使用Entity Framework Core实现数据访问层：
- **ORM映射**：对象关系映射简化了数据库操作
- **Code First**：使用代码优先方法定义数据模型和关系
- **迁移管理**：支持数据库架构版本控制和迁移
- **关系定义**：明确定义了实体间的一对多、多对多关系

```csharp
// 数据库关系映射示例
protected override void OnModelCreating(ModelBuilder builder)
{
    base.OnModelCreating(builder);
    
    builder.Entity<DirectoryModel>()
        .HasMany(d => d.Subdirectories)
        .WithOne(d => d.Parent)
        .HasForeignKey(d => d.ParentId)
        .OnDelete(DeleteBehavior.Restrict);
}
```

#### 4. 异常处理
项目实现了健壮的异常处理机制：
- **全局异常处理**：配置了全局异常过滤器
- **特定异常处理**：针对不同类型的异常提供适当的用户反馈
- **日志记录**：异常发生时记录详细日志，便于问题诊断
- **用户友好错误**：将技术错误转换为用户友好的消息

```csharp
// 异常处理示例
try
{
    await _fileService.UploadFileAsync(file, userId, directoryId, description);
    return RedirectToAction(nameof(Index), new { directoryId });
}
catch (Exception ex)
{
    _logger.LogError(ex, "上传文件时出错");
    ModelState.AddModelError("", $"上传文件时出错: {ex.Message}");
    return View();
}
```

### 可选项实现

#### 1. LINQ技术
项目广泛使用LINQ（语言集成查询）进行数据操作：
- **查询语法**：使用LINQ表达式查询数据库
- **延迟执行**：利用LINQ的延迟执行特性优化性能
- **投影**：使用Select进行数据投影，仅获取所需字段
- **复杂查询**：通过组合LINQ操作实现复杂的数据筛选和排序

```csharp
// LINQ查询示例
public async Task<List<FileModel>> SearchFilesAsync(string searchTerm, string userId)
{
    return await _context.Files
        .Where(f => f.UserId == userId &&
                (f.FileName.Contains(searchTerm) ||
                (f.Description != null && f.Description.Contains(searchTerm))))
        .OrderByDescending(f => f.UploadDate)
        .ToListAsync();
}
```

#### 2. 文件操作
项目实现了全面的文件系统操作：
- **文件读写**：安全地读写文件内容
- **目录管理**：创建、删除和管理目录结构
- **文件元数据**：处理文件名、大小、类型等元数据
- **流处理**：使用流处理大文件上传和下载，提高性能

```csharp
// 文件操作示例
public async Task<FileModel> UploadFileAsync(IFormFile file, string userId, int? directoryId = null)
{
    // 创建用户上传目录
    var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", userId);
    if (!Directory.Exists(uploadsFolder))
    {
        Directory.CreateDirectory(uploadsFolder);
    }

    // 生成唯一文件名
    var uniqueFileName = $"{Guid.NewGuid()}_{file.FileName}";
    var filePath = Path.Combine(uploadsFolder, uniqueFileName);

    // 保存文件
    using (var stream = new FileStream(filePath, FileMode.Create))
    {
        await file.CopyToAsync(stream);
    }
    
    // 数据库记录创建
    // ...
}
```

#### 3. 异步编程
项目全面采用异步编程模式：
- **异步控制器**：所有控制器操作均实现为异步方法
- **任务并行**：并行处理批量操作，提高性能
- **异步流**：处理数据流的异步传输
- **取消令牌**：支持操作取消，提高资源利用率

```csharp
// 异步编程示例
public async Task<IActionResult> Index(int? directoryId = null)
{
    var userId = _userManager.GetUserId(User);
    if (userId == null)
    {
        return Challenge();
    }

    var filesTask = _fileService.GetUserFilesAsync(userId, directoryId);
    var directoriesTask = _fileService.GetUserDirectoriesAsync(userId, directoryId);
    
    await Task.WhenAll(filesTask, directoriesTask);
    
    var files = await filesTask;
    var directories = await directoriesTask;
    
    // ...
}
```

#### 4. 网络通信
项目实现了多种网络通信功能：
- **HTTP请求处理**：处理用户浏览器请求
- **文件下载流**：高效率的文件流传输
- **共享链接**：生成和验证公共访问链接
- **安全通信**：支持HTTPS协议确保数据传输安全

```csharp
// 网络通信示例
[HttpGet]
public async Task<IActionResult> Download(string shareCode)
{
    var share = await _shareService.GetShareByCodeAsync(shareCode);
    if (share == null || !share.IsActive)
    {
        return NotFound();
    }

    var file = share.File;
    var filePath = Path.Combine(_environment.WebRootPath, file.FilePath);
    
    // 增加下载计数
    await _shareService.IncrementDownloadCountAsync(share.Id);
    
    // 流式返回文件，避免大文件加载到内存
    var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
    return File(stream, file.ContentType, file.FileName);
}
```

#### 5. 正则表达式
项目使用正则表达式进行数据验证和处理：
- **表单验证**：验证邮箱、密码等格式
- **文件名处理**：过滤非法字符，确保文件名合法
- **搜索优化**：增强搜索功能，支持模式匹配
- **内容解析**：解析文件内容中的特定模式

```csharp
// 正则表达式使用示例
[RegularExpression(@"^[\w\-. ]+$", ErrorMessage = "文件名只能包含字母、数字、下划线、连字符、点和空格")]
[Required(ErrorMessage = "文件名是必须的")]
public string NewName { get; set; }
```

#### 6. 多线程编程
项目利用多线程提升处理能力：
- **并行处理**：批量文件处理采用并行任务
- **背景作业**：使用后台任务处理耗时操作
- **资源同步**：确保多用户同时访问时的资源同步
- **线程池**：优化线程资源分配

```csharp
// 多线程处理示例
public async Task<(int SuccessCount, int FailCount)> BatchDeleteFilesAsync(List<int> fileIds, string userId)
{
    var results = await Task.WhenAll(
        fileIds.Select(fileId => DeleteFileAsync(fileId, userId))
    );
    
    return (results.Count(r => r), results.Count(r => !r));
}
```

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

## 许可证

[指定适合您项目的许可证]

## 开发者信息

[您的联系信息，如邮箱、网站等]