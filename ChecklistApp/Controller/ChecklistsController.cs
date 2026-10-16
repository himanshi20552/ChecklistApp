
using ChecklistApp.ApiModels;
using ChecklistApp.Services;
using ChecklistApp.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace ChecklistApp.Controllers
{
	[ApiController]
	[Route("api/checklists")]
	public class ChecklistsController : ControllerBase
	{
		private readonly IRequirementService _requirementService;
		private readonly IChecklistService _checklistService;

		public ChecklistsController(
			IRequirementService requirementService,
			IChecklistService checklistService)
		{
			_requirementService = requirementService;
			_checklistService = checklistService;
		}

		[HttpGet("transactions/{tid:int}")]
		public async Task<IActionResult> GetChecklist(
			int tid,
			[FromQuery] string pno,
			[FromQuery] string cid)
		{
			var definition = await _requirementService
		.GetChecklistItemsAsync(tid, pno, cid);

			if (definition.Items.Count == 0)
				return NotFound(new { message = "No checklist requirements were found." });

			return Ok(definition.Items);
		}

		[HttpPost]
		public async Task<IActionResult> SaveChecklist(
			[FromBody] ChecklistSaveRequest request)
		{
			var checklist = new ChecklistPageViewModel
			{
				TID = request.TID,
				PNo = request.PNo,
				CID = request.CID,
				EmployeeID = request.EmployeeID,
				Items = request.Items
			};

			try
			{
				await _checklistService.SaveChecklistAsync(checklist);

				return Ok(new
				{
					message = "Checklist saved successfully."
				});
			}
			catch (InvalidOperationException ex)
			{
				return BadRequest(new { message = ex.Message });
			}
		}
	}
}
