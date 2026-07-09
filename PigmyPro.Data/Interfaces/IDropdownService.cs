using System.Collections.Generic;
using System.Threading.Tasks;

namespace PigmyPro.Data.Interfaces
{
    public interface IDropdownService
    {
        Task<IEnumerable<BankDropdownItem>> GetBankDropdownAsync();
        Task<IEnumerable<BranchDropdownItem>> GetBranchDropdownAsync(int bankId);
        Task<IEnumerable<AgentDropdownItem>> GetAgentDropdownAsync(int bankId, int branchId);
        Task<string> GetBankNameAsync(int bankId);
    }
}
