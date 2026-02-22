using CommonUI.Common;
using CommonUI.Models;

namespace CommonUI.ModelServices
{
    public interface IZalogaService
    {
        Task<Result<IEnumerable<Zaloga>>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<Result<IEnumerable<Zaloga>>> GetByKategoriaAsync(string kategoria, CancellationToken cancellationToken = default);
        Task<Result> RefreshAsync(CancellationToken cancellationToken = default);
    }
}
