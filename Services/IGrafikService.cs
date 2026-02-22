using CommonUI.Common;
using CommonUI.Models;

namespace Services
{
    public interface IGrafikService
    {
        Task<Result<GrafikMiesieczny>> WczytajGrafikAsync(string miesiac, string nazwa, CancellationToken cancellationToken = default);
        Task RefreshAsync(CancellationToken cancellationToken = default);
        Task<IEnumerable<string>> GetDostepneMiesiaceAsync(CancellationToken cancellationToken = default);
    }
}