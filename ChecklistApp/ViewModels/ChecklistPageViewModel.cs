using System.ComponentModel.DataAnnotations;
using ChecklistApp.Models;

namespace ChecklistApp.ViewModels
{
	public class ChecklistPageViewModel
	{
		[Range(1, int.MaxValue)]
		public int TID { get; set; }

		[StringLength(50)]
		public string? PNo { get; set; }

		[StringLength(50)]
		public string? CID { get; set; }

		[Required]
		[StringLength(50)]
		public string EmployeeID { get; set; } = string.Empty;

		[Required]
		[StringLength(30)]
		public string ContextType { get; set; } = string.Empty;

		[Required]
		[StringLength(100)]
		public string ContextID { get; set; } = string.Empty;

		[Required]
		[StringLength(50)]
		public string ContextCode { get; set; } = string.Empty;

		public List<ChecklistItemViewModel> Items { get; set; } = new();

		public List<RequirementDisplayRule> DisplayRules { get; set; } = new();
	}
}
