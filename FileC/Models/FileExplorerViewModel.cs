namespace FileC.Models;

public class FileExplorerViewModel
{
    public List<FileModel> Files { get; set; } = new List<FileModel>();
    public List<DirectoryModel> Directories { get; set; } = new List<DirectoryModel>();
    public int? CurrentDirectoryId { get; set; }
}
