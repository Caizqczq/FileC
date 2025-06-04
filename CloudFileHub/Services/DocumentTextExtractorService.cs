using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using System.Text;

namespace CloudFileHub.Services;

/// <summary>
/// 文档文本提取服务
/// </summary>
public class DocumentTextExtractorService
{
    private readonly ILogger<DocumentTextExtractorService> _logger;

    public DocumentTextExtractorService(ILogger<DocumentTextExtractorService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// 根据文件类型提取文本内容
    /// </summary>
    public async Task<string> ExtractTextAsync(Stream fileStream, string fileName, string contentType)
    {
        try
        {
            return contentType.ToLower() switch
            {
                var ct when ct.Contains("pdf") => await ExtractFromPdfAsync(fileStream),
                var ct when ct.Contains("word") || ct.Contains("document") => await ExtractFromWordAsync(fileStream),
                var ct when ct.Contains("text") => await ExtractFromTextAsync(fileStream),
                _ => await ExtractFromTextAsync(fileStream) // 默认按文本处理
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "提取文档内容时出错: {FileName}", fileName);
            return string.Empty;
        }
    }

    /// <summary>
    /// 从PDF文件提取文本
    /// </summary>
    private async Task<string> ExtractFromPdfAsync(Stream pdfStream)
    {
        try
        {
            return await ExtractFromPdfWithITextAsync(pdfStream);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "PDF文本提取失败");
            return string.Empty;
        }
    }

    /// <summary>
    /// 使用iText7提取PDF文本（备用方案）
    /// </summary>
    private async Task<string> ExtractFromPdfWithITextAsync(Stream pdfStream)
    {
        try
        {
            // 如果流不支持Seek，则复制到内存流
            Stream processStream = pdfStream;
            if (!pdfStream.CanSeek)
            {
                var memoryStream = new MemoryStream();
                await pdfStream.CopyToAsync(memoryStream);
                memoryStream.Position = 0;
                processStream = memoryStream;
            }
            else
            {
                pdfStream.Position = 0;
            }

            var text = new StringBuilder();
            
            using var pdfReader = new PdfReader(processStream);
            using var pdfDocument = new PdfDocument(pdfReader);
            
            for (int i = 1; i <= pdfDocument.GetNumberOfPages(); i++)
            {
                var page = pdfDocument.GetPage(i);
                var pageText = PdfTextExtractor.GetTextFromPage(page);
                text.AppendLine(pageText);
            }

            // 如果创建了内存流，需要释放
            if (processStream != pdfStream)
            {
                processStream.Dispose();
            }

            return text.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "iText7 PDF文本提取失败");
            return string.Empty;
        }
    }

    /// <summary>
    /// 从Word文档提取文本
    /// </summary>
    private async Task<string> ExtractFromWordAsync(Stream wordStream)
    {
        try
        {
            // 如果流不支持Seek，则复制到内存流
            Stream processStream = wordStream;
            if (!wordStream.CanSeek)
            {
                var memoryStream = new MemoryStream();
                await wordStream.CopyToAsync(memoryStream);
                memoryStream.Position = 0;
                processStream = memoryStream;
            }
            else
            {
                wordStream.Position = 0;
            }

            var text = new StringBuilder();
            
            using var document = WordprocessingDocument.Open(processStream, false);
            var body = document?.MainDocumentPart?.Document?.Body;
            
            if (body != null)
            {
                foreach (var paragraph in body.Elements<Paragraph>())
                {
                    var paragraphText = paragraph.InnerText;
                    if (!string.IsNullOrWhiteSpace(paragraphText))
                    {
                        text.AppendLine(paragraphText);
                    }
                }
            }

            // 如果创建了内存流，需要释放
            if (processStream != wordStream)
            {
                processStream.Dispose();
            }

            return text.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Word文档文本提取失败");
            return string.Empty;
        }
    }

    /// <summary>
    /// 从文本文件提取内容
    /// </summary>
    private async Task<string> ExtractFromTextAsync(Stream textStream)
    {
        try
        {
            textStream.Position = 0;
            using var reader = new StreamReader(textStream, Encoding.UTF8);
            return await reader.ReadToEndAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "文本文件读取失败");
            return string.Empty;
        }
    }

    /// <summary>
    /// 清理和预处理提取的文本
    /// </summary>
    public string CleanExtractedText(string rawText)
    {
        if (string.IsNullOrWhiteSpace(rawText))
            return string.Empty;

        // 移除多余的空白字符
        var cleanText = System.Text.RegularExpressions.Regex.Replace(rawText, @"\s+", " ");
        
        // 移除特殊字符但保留基本标点
        cleanText = System.Text.RegularExpressions.Regex.Replace(cleanText, @"[^\w\s\u4e00-\u9fff\.,;:!?()""''""''—\-]", "");
        
        // 限制长度（AI处理的token限制）
        if (cleanText.Length > 8000)
        {
            cleanText = cleanText.Substring(0, 8000) + "...";
        }

        return cleanText.Trim();
    }

    /// <summary>
    /// 检查文件是否支持文本提取
    /// </summary>
    public bool IsSupportedFile(string contentType, string fileName)
    {
        var supportedTypes = new[]
        {
            "application/pdf",
            "application/msword",
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            "text/plain",
            "text/csv",
            "application/rtf"
        };

        var supportedExtensions = new[] { ".pdf", ".doc", ".docx", ".txt", ".csv", ".rtf" };

        return supportedTypes.Any(type => contentType.Contains(type, StringComparison.OrdinalIgnoreCase)) ||
               supportedExtensions.Any(ext => fileName.EndsWith(ext, StringComparison.OrdinalIgnoreCase));
    }
} 