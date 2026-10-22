namespace ChecklistApp.Models
{
	public class SubmittedDocumentStatus
	{
		public int QID { get; set; }

		public int OptionID { get; set; }

		public string Status { get; set; } = string.Empty;
	}
}