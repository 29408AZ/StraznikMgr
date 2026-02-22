using CommonUI.Common;
using CommonUI.Models;

namespace CommonUI.ModelServices
{
    public interface IPatrolService
    {
        Task<Result<bool>> ZapiszPatrolDoGrafikuAsync(Patrol patrol, CancellationToken cancellationToken = default);
    }
}
