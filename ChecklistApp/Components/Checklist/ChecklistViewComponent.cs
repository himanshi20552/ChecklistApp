using ChecklistApp.Services;
using ChecklistApp.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace ChecklistApp.Components.Checklist
{
	public class ChecklistViewComponent : ViewComponent
	{
		private readonly IRequirementService _requirementService;

		public ChecklistViewComponent(
			IRequirementService requirementService)
		{
			_requirementService = requirementService;
		}

		public async Task<IViewComponentResult> InvokeAsync(
			int tid,
			string pno,
			string cid,
			string employeeId)
		{
			var (items,displayRules) = await _requirementService
				.GetChecklistItemsAsync(tid, pno, cid);

			var model = new ChecklistPageViewModel
			{
				TID = tid,
				PNo = pno,
				CID = cid,
				EmployeeID = employeeId,
				Items = items,
				DisplayRules = displayRules
			};

			return View(model);
		}
	}
}