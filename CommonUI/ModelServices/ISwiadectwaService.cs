using CommonUI.Models;

namespace CommonUI.ModelServices
{
    public interface ISwiadectwaService
    {
        Task<SwiadectwaKategorie> GetSwiadectwaForMarynarzAsync(int marynarzId, CancellationToken cancellationToken = default);
    }
}
