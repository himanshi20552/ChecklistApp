namespace ChecklistApp.Models
{
	public class RequirementResponseOption
	{
		public int OptionID { get; set; }

		public int QID { get; set; }

		public string OptionValue { get; set; }
			= string.Empty;

		public bool IsActive { get; set; }
	}
}