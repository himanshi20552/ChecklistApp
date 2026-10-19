
using Dapper;
using ChecklistApp.Data;
using ChecklistApp.Models;
using ChecklistApp.ViewModels;

namespace ChecklistApp.Services
{
	public class RequirementService : IRequirementService
	{
		private readonly ISqlConnectionFactory _connectionFactory;

		public RequirementService(ISqlConnectionFactory connectionFactory)
		{
			_connectionFactory = connectionFactory;
		}

		public async Task<(
			List<ChecklistItemViewModel> Items,
			List<RequirementDisplayRule> DisplayRules
		)> GetChecklistItemsAsync(
			int tid,
			string contextType,
			string contextId,
			string contextCode)
		{
			using var connection = _connectionFactory.CreateConnection();
			await connection.OpenAsync();

			const string requirementsSql = """
                SELECT
                    r.QID,
                    r.RequirementDescription,
                    r.IsDoc,
                    r.IsActive
                FROM dbo.ChecklistTransactions ct
                INNER JOIN dbo.ChecklistTransactionRequirements ctr
                    ON ctr.TID = ct.TID
                INNER JOIN dbo.Requirements r
                    ON r.QID = ctr.QID
                WHERE ct.TID = @TID
                  AND ct.ContextType = @ContextType
                  AND ct.ContextID = @ContextID
                  AND ct.ContextCode = @ContextCode
                  AND r.IsActive = 1
                ORDER BY r.QID;
                """;

			var parameters = new
			{
				TID = tid,
				ContextType = contextType,
				ContextID = contextId,
				ContextCode = contextCode
			};

			var requirements = (
				await connection.QueryAsync<Requirement>(
					requirementsSql,
					parameters)
			).ToList();

			if (requirements.Count == 0)
			{
				return (
					new List<ChecklistItemViewModel>(),
					new List<RequirementDisplayRule>()
				);
			}

			var qids = requirements.Select(r => r.QID).ToArray();

			const string optionsSql = """
                SELECT OptionID, QID, OptionValue, IsActive
                FROM dbo.RequirementResponseOptions
                WHERE QID IN @QIDs
                  AND IsActive = 1
                ORDER BY QID, OptionID;
                """;

			var options = (
				await connection.QueryAsync<RequirementResponseOption>(
					optionsSql,
					new { QIDs = qids })
			).ToList();

			const string responsesSql = """
                SELECT QID, TID, OptionID, UpdatedBy, UpdatedAt
                FROM dbo.ChecklistResponses
                WHERE TID = @TID
                  AND QID IN @QIDs;
                """;

			var responses = (
				await connection.QueryAsync<ChecklistResponse>(
					responsesSql,
					new { TID = tid, QIDs = qids })
			).ToList();

			const string historySql = """
                SELECT
                    h.HistoryID,
                    h.QID,
                    h.TID,
                    h.EmployeeID,
                    h.OptionID,
                    h.UserRemarks,
                    h.CreatedAt,
                    o.OptionValue
                FROM dbo.ChecklistResponseHistory h
                LEFT JOIN dbo.RequirementResponseOptions o
                    ON o.QID = h.QID
                   AND o.OptionID = h.OptionID
                WHERE h.TID = @TID
                  AND h.QID IN @QIDs
                ORDER BY h.CreatedAt DESC, h.HistoryID DESC;
                """;

			var history = (
				await connection.QueryAsync<ChecklistResponseHistory>(
					historySql,
					new { TID = tid, QIDs = qids })
			).ToList();

			const string rulesSql = """
                SELECT
                    RuleID,
                    TriggerQID,
                    TriggerOptionID,
                    DependentQID
                FROM dbo.RequirementDisplayRules
                WHERE IsActive = 1
                  AND TriggerQID IN @QIDs
                  AND DependentQID IN @QIDs;
                """;

			var rules = (
				await connection.QueryAsync<RequirementDisplayRule>(
					rulesSql,
					new { QIDs = qids })
			).ToList();

			var optionsByQid = options
				.GroupBy(o => o.QID)
				.ToDictionary(g => g.Key, g => g.ToList());

			var responsesByQid = responses
				.ToDictionary(r => r.QID);

			var historyByQid = history
				.GroupBy(h => h.QID)
				.ToDictionary(g => g.Key, g => g.ToList());

			var items = requirements.Select(requirement =>
			{
				responsesByQid.TryGetValue(
					requirement.QID, out var response);

				optionsByQid.TryGetValue(
					requirement.QID, out var requirementOptions);

				historyByQid.TryGetValue(
					requirement.QID, out var requirementHistory);

				return new ChecklistItemViewModel
				{
					QID = requirement.QID,
					RequirementDescription =
						requirement.RequirementDescription,
					IsDoc = requirement.IsDoc,
					Options = requirementOptions ?? new(),
					SelectedOptionID = response?.OptionID,
					History = requirementHistory ?? new()
				};
			}).ToList();

			return (items, rules);
		}
	}
}
