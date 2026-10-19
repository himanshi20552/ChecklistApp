
using ChecklistApp.Services;
using ChecklistApp.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace ChecklistApp.Components.Checklist
{
	public class ChecklistViewComponent : ViewComponent
	{ private readonly IRequirementService _requirementService;
public ChecklistViewComponent(IRequirementService requirementService)
		{_requirementService = requirementService;}
public async Task<IViewComponentResult> InvokeAsync(int tid,string contextType,string contextId,string contextCode,string employeeId,string? pno = null,
			string? cid = null)
		{
			var (items, displayRules) =
				await _requirementService.GetChecklistItemsAsync(
					tid,contextType,contextId,contextCode);

			var model = new ChecklistPageViewModel
			{
				TID = tid,
				ContextType = contextType,
				ContextID = contextId,
				ContextCode = contextCode,
				EmployeeID = employeeId,
				PNo = pno,
				CID = cid,
				Items = items,
				DisplayRules = displayRules
			};

			return View(model);
		}
	}
}
