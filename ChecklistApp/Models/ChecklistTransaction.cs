namespace ChecklistApp.Models
{
	public class ChecklistTransaction
	{
		public int QID { get; set; }

		public int TID { get; set; }

		public string PNo { get; set; } = string.Empty;

		public string CID { get; set; } = string.Empty;

		public DateTime UpdatedAt { get; set; }
	}
}