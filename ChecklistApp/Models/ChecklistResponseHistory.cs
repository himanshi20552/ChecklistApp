namespace ChecklistApp.Models
{
	public class ChecklistResponseHistory
	{
		public long HistoryID { get; set; }

		public int QID { get; set; }

		public int TID { get; set; }

		public string PNo { get; set; } = string.Empty;

		public string EmployeeID { get; set; }
			= string.Empty;

		public int? OptionID { get; set; }

		public string? UserRemarks { get; set; }

		public DateTime CreatedAt { get; set; }
		public string? OptionValue { get; set; }
	}
}