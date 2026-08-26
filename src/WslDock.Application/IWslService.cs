using WslDock.Domain;

namespace WslDock.Application;

public interface IWslService
{
    Task<IReadOnlyList<WslDistribution>> GetDistributionsAsync(CancellationToken cancellationToken = default);
}
