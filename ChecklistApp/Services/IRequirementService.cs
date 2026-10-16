using ChecklistApp.Models;
using ChecklistApp.ViewModels;
namespace ChecklistApp.Services

{
    public interface IRequirementService
    {
		Task<(List<ChecklistItemViewModel> Items, List<RequirementDisplayRule> DisplayRules)> GetChecklistItemsAsync(int tid, string pno, string cid);
	}
}