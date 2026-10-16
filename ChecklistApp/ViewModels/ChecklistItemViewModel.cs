using ChecklistApp.Models;

namespace ChecklistApp.ViewModels
{
	public class ChecklistItemViewModel
	{
		public int QID { get; set; }

		public string RequirementDescription { get; set; }
			= string.Empty;

		public bool IsDoc { get; set; }

		public List<RequirementResponseOption> Options { get; set; }
			= new();

		public int? SelectedOptionID { get; set; }

		public string? NewRemark { get; set; }

		public List<ChecklistResponseHistory> History { get; set; }
			= new();
	}
}