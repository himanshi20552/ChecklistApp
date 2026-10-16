
using ChecklistApp.Services;
using ChecklistApp.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ChecklistApp.Pages
{
	public class IndexModel : PageModel
	{
		private readonly IChecklistService _checklistService;
		[BindProperty]
		public ChecklistPageViewModel Checklist { get; set; } = new();

		public string? ErrorMessage { get; private set; }

		public string? SuccessMessage { get; private set; }

		public IndexModel(IChecklistService checklistService)
		{
			_checklistService = checklistService;
		}

		public IActionResult OnGet(
			int? tid,
			string? pno,
			string? cid,
			string? employeeId,
			bool saved = false)
		{
			if (tid is null || tid <= 0 ||
				string.IsNullOrWhiteSpace(pno) ||
				string.IsNullOrWhiteSpace(cid) ||
				string.IsNullOrWhiteSpace(employeeId))
			{
				ErrorMessage =
					"Required transaction information is missing.";

				return Page();
			}

			if (pno.Length > 50 ||
				cid.Length > 50 ||
				employeeId.Length > 50)
			{
				ErrorMessage = "One or more identifiers are too long.";
				return Page();
			}

			Checklist = new ChecklistPageViewModel
			{
				TID = tid.Value,
				PNo = pno,
				CID = cid,
				EmployeeID = employeeId
			};

			if (saved)
			{
				SuccessMessage = "Checklist saved successfully.";
			}

			return Page();
		}

		public async Task<IActionResult> OnPostSaveChecklistAsync()
		{
			if (Checklist.TID <= 0 ||
				string.IsNullOrWhiteSpace(Checklist.PNo) ||
				string.IsNullOrWhiteSpace(Checklist.CID) ||
				string.IsNullOrWhiteSpace(Checklist.EmployeeID))
			{
				ErrorMessage =
					"Required transaction information is missing.";

				return Page();
			}

			if (Checklist.PNo.Length > 50 ||
				Checklist.CID.Length > 50 ||
				Checklist.EmployeeID.Length > 50)
			{
				ErrorMessage = "One or more identifiers are too long.";
				return Page();
			}

			if (Checklist.Items.Count == 0)
			{
				ErrorMessage = "There are no checklist items to save.";
				return Page();
			}

			if (!ModelState.IsValid)
			{
				ErrorMessage = "Please check the submitted data.";
				return Page();
			}

			try
			{
				await _checklistService.SaveChecklistAsync(Checklist);

				return RedirectToPage("/Index", new
				{
					tid = Checklist.TID,
					pno = Checklist.PNo,
					cid = Checklist.CID,
					employeeId = Checklist.EmployeeID,
					saved = true
				});
			}
			catch (InvalidOperationException ex)
			{
				ErrorMessage = ex.Message;
				return Page();
			}
		}
	}
}
