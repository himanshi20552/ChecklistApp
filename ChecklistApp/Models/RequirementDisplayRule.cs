namespace ChecklistApp.Models
{
    public class RequirementDisplayRule
    {
        public int RuleId { get; set; }
        public int TriggerQID { get; set; }
		public int TriggerOptionID { get; set; }
		public int DependentQID { get; set; }
	}
}
