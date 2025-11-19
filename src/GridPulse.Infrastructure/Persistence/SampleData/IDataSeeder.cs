using System.Threading;
using System.Threading.Tasks;

namespace GridPulse.Infrastructure.Persistence.SampleData;

public interface IDataSeeder
{
    Task SeedAsync(CancellationToken cancellationToken);
}
