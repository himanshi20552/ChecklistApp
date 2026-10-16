namespace ChecklistApp.Models
{
    public class Requirement
    {
        public int QID { get; set; }
        public string RequirementDescription { get; set; } = string.Empty;
        public bool IsDoc { get; set; }
        public bool IsActive { get; set; }
    }
}
