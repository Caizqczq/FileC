using Aliyun.OSS;
using CloudFileHub.Models;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;

namespace CloudFileHub.Services;

public class AliyunOSSService
{
    private readonly OssClient _ossClient;
    private readonly AliyunOSSOptions _options;
    private readonly ILogger<AliyunOSSService> _logger;

    public AliyunOSSService(IOptions<AliyunOSSOptions> options, ILogger<AliyunOSSService> logger)
    {
        _options = options.Value;
        _logger = logger;
        _ossClient = new OssClient(_options.Endpoint, _options.AccessKeyId, _options.AccessKeySecret);
    }

    /// <summary>
    /// 上传文件到OSS
    /// </summary>
    /// <param name="key">文件在OSS中的键名</param>
    /// <param name="stream">文件流</param>
    /// <returns>是否上传成功</returns>
    public Task<bool> UploadFileAsync(string key, Stream stream)
    {
        try
        {
            _logger.LogInformation("开始上传文件到OSS: {Key}", key);
            var result = _ossClient.PutObject(_options.BucketName, key, stream);
            var success = result.HttpStatusCode == System.Net.HttpStatusCode.OK;
            
            if (success)
            {
                _logger.LogInformation("文件上传成功: {Key}", key);
            }
            else
            {
                _logger.LogWarning("文件上传失败，HTTP状态码: {StatusCode}, 文件: {Key}", result.HttpStatusCode, key);
            }
            
            return Task.FromResult(success);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OSS上传失败: {Key}", key);
            Console.WriteLine($"OSS上传失败: {ex.Message}");
            return Task.FromResult(false);
        }
    }

    /// <summary>
    /// 从OSS下载文件
    /// </summary>
    /// <param name="key">文件在OSS中的键名</param>
    /// <returns>文件流</returns>
    public Task<Stream?> DownloadFileAsync(string key)
    {
        try
        {
            _logger.LogInformation("开始从OSS下载文件: {Key}", key);
            
            // 首先检查文件是否存在
            var exists = _ossClient.DoesObjectExist(_options.BucketName, key);
            if (!exists)
            {
                _logger.LogWarning("文件在OSS中不存在: {Key}", key);
                return Task.FromResult<Stream?>(null);
            }
            
            var result = _ossClient.GetObject(_options.BucketName, key);
            _logger.LogInformation("文件下载成功: {Key}", key);
            return Task.FromResult<Stream?>(result.Content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OSS下载失败: {Key}", key);
            Console.WriteLine($"OSS下载失败: {ex.Message}");
            return Task.FromResult<Stream?>(null);
        }
    }

    /// <summary>
    /// 从OSS删除文件
    /// </summary>
    /// <param name="key">文件在OSS中的键名</param>
    /// <returns>是否删除成功</returns>
    public Task<bool> DeleteFileAsync(string key)
    {
        try
        {
            _logger.LogInformation("开始从OSS删除文件: {Key}", key);
            _ossClient.DeleteObject(_options.BucketName, key);
            _logger.LogInformation("文件删除成功: {Key}", key);
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OSS删除失败: {Key}", key);
            Console.WriteLine($"OSS删除失败: {ex.Message}");
            return Task.FromResult(false);
        }
    }

    /// <summary>
    /// 检查文件是否存在
    /// </summary>
    /// <param name="key">文件在OSS中的键名</param>
    /// <returns>文件是否存在</returns>
    public Task<bool> FileExistsAsync(string key)
    {
        try
        {
            var exists = _ossClient.DoesObjectExist(_options.BucketName, key);
            _logger.LogInformation("文件存在检查: {Key} = {Exists}", key, exists);
            return Task.FromResult(exists);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OSS检查文件存在失败: {Key}", key);
            Console.WriteLine($"OSS检查文件存在失败: {ex.Message}");
            return Task.FromResult(false);
        }
    }

    /// <summary>
    /// 生成文件的预签名URL（用于直接访问）
    /// </summary>
    /// <param name="key">文件在OSS中的键名</param>
    /// <param name="expiration">过期时间（默认1小时）</param>
    /// <returns>预签名URL</returns>
    public string GeneratePresignedUrl(string key, TimeSpan? expiration = null)
    {
        try
        {
            var exp = expiration ?? TimeSpan.FromHours(1);
            var expiry = DateTime.UtcNow.Add(exp);
            var uri = _ossClient.GeneratePresignedUri(_options.BucketName, key, expiry);
            _logger.LogInformation("生成预签名URL成功: {Key}", key);
            return uri.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OSS生成预签名URL失败: {Key}", key);
            Console.WriteLine($"OSS生成预签名URL失败: {ex.Message}");
            return string.Empty;
        }
    }
} 