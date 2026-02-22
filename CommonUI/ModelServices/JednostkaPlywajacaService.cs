using CommonUI.Common;
using CommonUI.Models;

namespace CommonUI.ModelServices
{
    public class JednostkaPlywajacaService : IJednostkaPlywajacaService
    {
        private readonly List<JednostkaPlywajaca> _jednostki = new();

        public Task<Result<IEnumerable<JednostkaPlywajaca>>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Result<IEnumerable<JednostkaPlywajaca>>.Success(
                _jednostki.OrderBy(j => j.Kategoria).ThenBy(j => j.NumerBurtowy).AsEnumerable()));
        }

        public Task<Result<IEnumerable<JednostkaPlywajaca>>> GetByKategoriaAsync(string kategoria, CancellationToken cancellationToken = default)
        {
            var result = _jednostki
                .Where(j => j.Kategoria.Equals(kategoria, StringComparison.OrdinalIgnoreCase))
                .OrderBy(j => j.NumerBurtowy);

            return Task.FromResult(Result<IEnumerable<JednostkaPlywajaca>>.Success(result.AsEnumerable()));
        }

        public Task<Result> RefreshAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Result.Success());
        }
    }
}