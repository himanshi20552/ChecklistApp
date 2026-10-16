using ChecklistApp.Models;

namespace ChecklistApp.Services
{
	public interface IDemoPolicyService
	{
		Task<List<DemoPolicy>> GetPoliciesAsync();
		Task<int> StartTransactionAsync(int demoPolicyId);
	}
}