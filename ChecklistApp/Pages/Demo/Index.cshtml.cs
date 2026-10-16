
using ChecklistApp.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ChecklistApp.Pages.Demo
{
	public class IndexModel : PageModel
	{
		private readonly IDemoPolicyService _demoPolicyService;

		public List<ChecklistApp.Models.DemoPolicy> Policies { get; private set; }
			= new();

		public string? ErrorMessage { get; private set; }

		public IndexModel(IDemoPolicyService demoPolicyService)
		{
			_demoPolicyService = demoPolicyService;
		}

		public async Task OnGetAsync()
		{
			Policies = await _demoPolicyService.GetPoliciesAsync();
		}

		public async Task<IActionResult> OnPostStartAsync(int demoPolicyId)
		{
			try
			{
				var tid = await _demoPolicyService.StartTransactionAsync(demoPolicyId);
				var policy = (await _demoPolicyService.GetPoliciesAsync())
					.FirstOrDefault(p => p.DemoPolicyID == demoPolicyId);

				if (policy is null)
					return NotFound();

				return RedirectToPage("/Index", new
				{
					tid,
					pno = policy.PNo,
					cid = policy.CID,
					employeeId = "EMP001"
				});
			}
			catch (InvalidOperationException ex)
			{
				ErrorMessage = ex.Message;
				Policies = await _demoPolicyService.GetPoliciesAsync();
				return Page();
			}
		}
	}
}
