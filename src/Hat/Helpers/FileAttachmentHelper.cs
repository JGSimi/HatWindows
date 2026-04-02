using System.Text;
using Hat.Models;

namespace Hat.Helpers;

/// <summary>
/// Processes file attachments for chat messages.
/// Port of attachment(from:), extractPDFText, extractTextFileContent from ContentView.swift.
/// </summary>
public static class FileAttachmentHelper
{
    private static readonly string[] ImageExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp", ".tiff" };
    private static readonly string[] TextExtensions = { ".txt", ".md", ".json", ".xml", ".csv", ".log", ".yaml", ".yml",
        ".toml", ".ini", ".cfg", ".conf", ".html", ".htm", ".css", ".js", ".ts", ".py", ".cs", ".swift", ".java",
        ".c", ".cpp", ".h", ".hpp", ".rs", ".go", ".rb", ".php", ".sh", ".bat", ".ps1", ".sql", ".r", ".m" };

    /// <summary>
    /// Process a file and return a ChatAttachment.
    /// Detects type and extracts content accordingly.
    /// </summary>
    public static async Task<ChatAttachment?> ProcessFileAsync(string filePath)
    {
        return await Task.Run(() => ProcessFileSync(filePath));
    }

    private static ChatAttachment? ProcessFileSync(string filePath)
    {
        if (!File.Exists(filePath)) return null;

        var ext = Path.GetExtension(filePath).ToLowerInvariant();
        var fileName = Path.GetFileName(filePath);

        // Image
        if (ImageExtensions.Contains(ext))
        {
            try
            {
                var data = File.ReadAllBytes(filePath);
                var base64 = ImageHelper.ResizeAndCompressBase64(data);
                return new ChatAttachment
                {
                    FileName = fileName,
                    FilePath = filePath,
                    Type = AttachmentType.Image,
                    Data = data,
                    Base64Content = base64
                };
            }
            catch { return null; }
        }

        // PDF
        if (ext == ".pdf")
        {
            var pdfText = ExtractPdfText(filePath);
            var content = string.IsNullOrWhiteSpace(pdfText)
                ? $"[PDF anexado: {fileName}]\nNao foi possivel extrair texto pesquisavel deste PDF."
                : pdfText;

            return new ChatAttachment
            {
                FileName = fileName,
                FilePath = filePath,
                Type = AttachmentType.Pdf,
                TextContent = content
            };
        }

        // Text files
        if (TextExtensions.Contains(ext) || IsTextFile(filePath))
        {
            var textContent = ExtractTextContent(filePath);
            if (textContent != null)
            {
                return new ChatAttachment
                {
                    FileName = fileName,
                    FilePath = filePath,
                    Type = AttachmentType.Text,
                    TextContent = textContent
                };
            }
        }

        // Binary / unknown
        return new ChatAttachment
        {
            FileName = fileName,
            FilePath = filePath,
            Type = AttachmentType.Other,
            TextContent = $"[Arquivo binario anexado: {fileName}]\nO conteudo nao e texto e nao pode ser extraido automaticamente."
        };
    }

    /// <summary>
    /// Extract text from PDF. Simple approach without heavy dependencies.
    /// </summary>
    private static string ExtractPdfText(string filePath)
    {
        try
        {
            // Basic PDF text extraction: read raw bytes and find text between BT/ET markers
            // For production, consider adding a NuGet PDF library
            var bytes = File.ReadAllBytes(filePath);
            var raw = Encoding.ASCII.GetString(bytes);

            // Try to find readable text segments
            var sb = new StringBuilder();
            var inText = false;
            for (int i = 0; i < raw.Length - 1; i++)
            {
                if (raw[i] == '(' && !inText)
                {
                    inText = true;
                    continue;
                }
                if (raw[i] == ')' && inText)
                {
                    inText = false;
                    sb.Append(' ');
                    continue;
                }
                if (inText && raw[i] >= 32 && raw[i] < 127)
                {
                    sb.Append(raw[i]);
                }
            }

            return sb.ToString().Trim();
        }
        catch
        {
            return "";
        }
    }

    /// <summary>
    /// Try multiple encodings to extract text content.
    /// Port of extractTextFileContent from ContentView.swift.
    /// </summary>
    private static string? ExtractTextContent(string filePath)
    {
        // Try UTF-8 first
        try
        {
            var utf8 = File.ReadAllText(filePath, Encoding.UTF8);
            if (!string.IsNullOrWhiteSpace(utf8)) return utf8;
        }
        catch { }

        // Try other encodings
        var encodings = new[] { Encoding.Unicode, Encoding.GetEncoding("iso-8859-1"),
            Encoding.GetEncoding(1252), Encoding.ASCII };

        var data = File.ReadAllBytes(filePath);
        foreach (var encoding in encodings)
        {
            try
            {
                var decoded = encoding.GetString(data);
                if (!string.IsNullOrWhiteSpace(decoded)) return decoded;
            }
            catch { }
        }

        return null;
    }

    /// <summary>
    /// Heuristic: check if file is likely text by reading first bytes.
    /// </summary>
    private static bool IsTextFile(string filePath)
    {
        try
        {
            var buffer = new byte[Math.Min(8192, new FileInfo(filePath).Length)];
            using var fs = File.OpenRead(filePath);
            var read = fs.Read(buffer, 0, buffer.Length);

            // If most bytes are printable ASCII, it's likely text
            int printable = 0;
            for (int i = 0; i < read; i++)
            {
                if (buffer[i] >= 32 && buffer[i] < 127 || buffer[i] == '\n' || buffer[i] == '\r' || buffer[i] == '\t')
                    printable++;
            }
            return read > 0 && (double)printable / read > 0.85;
        }
        catch { return false; }
    }
}
