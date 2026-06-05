using System.Collections.Generic;
using System.Threading.Tasks;
using PigmyPro.Domain.Entities;

namespace PigmyPro.Data.Interfaces
{
    public interface IMobileDataRepository
    {
        // Strictly Monitoring ONLY - No reconciliation or archiving yet.
        Task<IEnumerable<MobileTransaction>> GetPendingByBankAsync(int bankId, decimal? agentCode = null, DateTime? date = null);
    }
}
