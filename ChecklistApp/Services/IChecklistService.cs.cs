using ChecklistApp.ViewModels;

namespace ChecklistApp.Services
{
	public interface IChecklistService
	{
		Task SaveChecklistAsync(
			ChecklistPageViewModel checklist);
	}
}