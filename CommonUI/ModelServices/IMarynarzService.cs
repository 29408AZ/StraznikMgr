using CommonUI.Common;
using CommonUI.Models;

namespace CommonUI.ModelServices
{
    public interface IMarynarzService
    {
        Task<Result<IEnumerable<Marynarz>>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<Result> UpdateAsync(Marynarz updatedMarynarz);
        Task<Result<IEnumerable<Swiadectwo>>> GetSwiadectwaAsync(int marynarzId, CancellationToken cancellationToken = default);
        Task<Result<IEnumerable<Swiadectwo>>> GetAllSwiadectwaAsync(CancellationToken cancellationToken = default);
        Task<Result> RefreshAsync(CancellationToken cancellationToken = default);
    }
}