using OfficeOpenXml;

namespace Services
{
    public interface IExcelFileService
    {
        string GetFilePath(string defaultPath);
        ExcelPackage GetPackage();
        Task<ExcelPackage> OpenFileAsync(string filePath, CancellationToken cancellationToken = default);
        string PromptForFilePath(string defaultPath);
    }
}
