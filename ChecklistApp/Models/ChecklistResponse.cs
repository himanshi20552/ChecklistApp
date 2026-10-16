namespace ChecklistApp.Models
{
	public class ChecklistResponse
	{
		public int QID { get; set; }

		public int TID { get; set; }

		public string PNo { get; set; } = string.Empty;

		public int? OptionID { get; set; }

		public string UpdatedBy { get; set; } = string.Empty;

		public DateTime UpdatedAt { get; set; }
	}
}