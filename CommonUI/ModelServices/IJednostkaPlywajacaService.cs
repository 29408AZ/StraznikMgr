using CommonUI.Common;
using CommonUI.Models;

namespace CommonUI.ModelServices
{
    public interface IJednostkaPlywajacaService
    {
        Task<Result<IEnumerable<JednostkaPlywajaca>>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<Result<IEnumerable<JednostkaPlywajaca>>> GetByKategoriaAsync(string kategoria, CancellationToken cancellationToken = default);
        Task<Result> RefreshAsync(CancellationToken cancellationToken = default);
    }
}