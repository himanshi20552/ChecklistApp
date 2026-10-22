using System.Data;
using ChecklistApp.ViewModels;

namespace ChecklistApp.Services
{
	public interface IChecklistService
	{
		Task SaveChecklistAsync(ChecklistPageViewModel checklist);
		Task SaveChecklistAsync( ChecklistPageViewModel checklist,IDbConnection connection,
		 IDbTransaction transaction);
	}

}