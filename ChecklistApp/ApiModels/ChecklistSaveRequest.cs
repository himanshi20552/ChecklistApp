
using System.ComponentModel.DataAnnotations;
using ChecklistApp.ViewModels;

namespace ChecklistApp.ApiModels
{
	public class ChecklistSaveRequest
	{
		[Range(1, int.MaxValue)]
		public int TID { get; set; }

		[Required]
		[StringLength(50)]
		public string PNo { get; set; } = string.Empty;

		[Required]
		[StringLength(50)]
		public string CID { get; set; } = string.Empty;

		[Required]
		[StringLength(50)]
		public string EmployeeID { get; set; } = string.Empty;

		[Required]
		public List<ChecklistItemViewModel> Items { get; set; } = new();
	}
}
