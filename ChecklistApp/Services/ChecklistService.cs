
using System.Data;
using Dapper;
using ChecklistApp.Data;
using ChecklistApp.Models;
using ChecklistApp.ViewModels;

namespace ChecklistApp.Services
{
	public class ChecklistService : IChecklistService
	{
		private readonly ISqlConnectionFactory _connectionFactory;

		public ChecklistService(ISqlConnectionFactory connectionFactory)
		{
			_connectionFactory = connectionFactory;
		}

		public async Task SaveChecklistAsync(ChecklistPageViewModel checklist)
		{
			using var connection = _connectionFactory.CreateConnection();
			await connection.OpenAsync();

			using var transaction = connection.BeginTransaction();

			try
			{
				await SaveChecklistCoreAsync(
					checklist,
					connection,
					transaction);

				transaction.Commit();
			}
			catch
			{
				transaction.Rollback();
				throw;
			}
		}

		public async Task SaveChecklistAsync(
			ChecklistPageViewModel checklist,
			IDbConnection connection,
			IDbTransaction transaction)
		{
			if (connection == null ||
				transaction == null ||
				connection.State != ConnectionState.Open ||
				transaction.Connection == null ||
				!ReferenceEquals(transaction.Connection, connection))
			{
				throw new InvalidOperationException(
					"An open connection and its active transaction are required.");
			}

			await SaveChecklistCoreAsync(
				checklist,
				connection,
				transaction);
		}

		private static async Task SaveChecklistCoreAsync(
			ChecklistPageViewModel checklist,
			IDbConnection connection,
			IDbTransaction transaction)
		{
			if (checklist.TID <= 0 ||
				string.IsNullOrWhiteSpace(checklist.ContextType) ||
				checklist.ContextType.Length > 30 ||
				string.IsNullOrWhiteSpace(checklist.ContextID) ||
				checklist.ContextID.Length > 100 ||
				string.IsNullOrWhiteSpace(checklist.ContextCode) ||
				checklist.ContextCode.Length > 50 ||
				string.IsNullOrWhiteSpace(checklist.EmployeeID) ||
				checklist.EmployeeID.Length > 50)
			{
				throw new InvalidOperationException(
					"Valid transaction, context, and employee identifiers are required.");
			}

			if (checklist.Items == null || checklist.Items.Count == 0)
			{
				throw new InvalidOperationException(
					"No checklist items were submitted.");
			}

			if (checklist.Items.Any(item =>
					item.QID <= 0 ||
					item.NewRemark?.Length > 2000) ||
				checklist.Items.Select(item => item.QID).Distinct().Count()
					!= checklist.Items.Count)
			{
				throw new InvalidOperationException(
					"The submitted checklist contains invalid or duplicate items.");
			}

			const string requirementsSql = """
                SELECT r.QID
                FROM dbo.ChecklistTransactions ct
                INNER JOIN dbo.ChecklistTransactionRequirements ctr
                    ON ctr.TID = ct.TID
                INNER JOIN dbo.Requirements r
                    ON r.QID = ctr.QID
                WHERE ct.TID = @TID
                  AND ct.ContextType = @ContextType
                  AND ct.ContextID = @ContextID
                  AND ct.ContextCode = @ContextCode
                  AND ct.Status = 'InProgress'
                  AND r.IsActive = 1;
                """;

			var transactionQids = (
				await connection.QueryAsync<int>(
					requirementsSql,
					new
					{
						checklist.TID,
						checklist.ContextType,
						checklist.ContextID,
						checklist.ContextCode
					},
					transaction)
			).ToHashSet();

			if (transactionQids.Count == 0)
			{
				throw new InvalidOperationException(
					"No active requirements were found for this checklist transaction, or the transaction is not editable.");
			}

			var submittedQids = checklist.Items
				.Select(item => item.QID)
				.ToHashSet();

			if (!transactionQids.SetEquals(submittedQids))
			{
				throw new InvalidOperationException(
					"The submitted questions do not match this transaction's checklist.");
			}

			const string rulesSql = """
                SELECT RuleID, TriggerQID, TriggerOptionID, DependentQID
                FROM dbo.RequirementDisplayRules
                WHERE IsActive = 1
                  AND TriggerQID IN @QIDs
                  AND DependentQID IN @QIDs;
                """;

			var rules = (
				await connection.QueryAsync<RequirementDisplayRule>(
					rulesSql,
					new { QIDs = transactionQids.ToArray() },
					transaction)
			).ToList();

			const string optionSql = """
                SELECT COUNT(1)
                FROM dbo.RequirementResponseOptions
                WHERE QID = @QID
                  AND OptionID = @OptionID
                  AND IsActive = 1;
                """;

			foreach (var item in checklist.Items)
			{
				if (!item.SelectedOptionID.HasValue)
				{
					continue;
				}

				var optionExists = await connection.ExecuteScalarAsync<int>(
					optionSql,
					new
					{
						item.QID,
						OptionID = item.SelectedOptionID.Value
					},
					transaction);

				if (optionExists == 0)
				{
					throw new InvalidOperationException(
						$"The selected option is invalid for QID {item.QID}.");
				}
			}

			var submittedByQid = checklist.Items.ToDictionary(
				item => item.QID);

			var dependentQids = rules
				.Select(rule => rule.DependentQID)
				.ToHashSet();

			var applicableQids = transactionQids
				.Where(qid => !dependentQids.Contains(qid))
				.ToHashSet();

			bool changed;
			int passes = 0;

			do
			{
				changed = false;
				passes++;

				foreach (var rule in rules)
				{
					if (!applicableQids.Contains(rule.TriggerQID))
					{
						continue;
					}

					var triggerAnswer =
						submittedByQid[rule.TriggerQID].SelectedOptionID;

					if (triggerAnswer == rule.TriggerOptionID &&
						applicableQids.Add(rule.DependentQID))
					{
						changed = true;
					}
				}
			}
			while (changed && passes <= transactionQids.Count);

			foreach (var item in checklist.Items)
			{
				bool hasFile = item.File is { Length: > 0 };

				if (!applicableQids.Contains(item.QID) &&
					(item.SelectedOptionID.HasValue ||
					 !string.IsNullOrWhiteSpace(item.NewRemark) ||
					 hasFile))
				{
					throw new InvalidOperationException(
						$"QID {item.QID} is not applicable for the selected answers.");
				}
			}

			const string clearSql = """
                UPDATE dbo.ChecklistResponses
                SET OptionID = NULL,
                    UpdatedBy = @EmployeeID,
                    UpdatedAt = SYSUTCDATETIME()
                WHERE TID = @TID
                  AND QID = @QID;
                """;

			const string currentSql = """
                SELECT OptionID
                FROM dbo.ChecklistResponses
                WHERE TID = @TID
                  AND QID = @QID;
                """;

			const string saveResponseSql = """
                UPDATE dbo.ChecklistResponses WITH (UPDLOCK, SERIALIZABLE)
                SET OptionID = @OptionID,
                    UpdatedBy = @EmployeeID,
                    UpdatedAt = SYSUTCDATETIME()
                WHERE TID = @TID
                  AND QID = @QID;

                IF @@ROWCOUNT = 0
                BEGIN
                    INSERT INTO dbo.ChecklistResponses
                        (TID, QID, OptionID, UpdatedBy)
                    VALUES
                        (@TID, @QID, @OptionID, @EmployeeID);
                END;
                """;

			const string historySql = """
                INSERT INTO dbo.ChecklistResponseHistory
                    (TID, QID, EmployeeID, OptionID, UserRemarks)
                VALUES
                    (@TID, @QID, @EmployeeID, @OptionID, @UserRemarks);
                """;

			foreach (var item in checklist.Items)
			{
				if (!applicableQids.Contains(item.QID))
				{
					await connection.ExecuteAsync(
						clearSql,
						new
						{
							checklist.TID,
							item.QID,
							checklist.EmployeeID
						},
						transaction);

					continue;
				}

				var previousOptionID =
					await connection.QuerySingleOrDefaultAsync<int?>(
						currentSql,
						new
						{
							checklist.TID,
							item.QID
						},
						transaction);

				bool responseChanged =
					previousOptionID != item.SelectedOptionID;

				await connection.ExecuteAsync(
					saveResponseSql,
					new
					{
						checklist.TID,
						item.QID,
						OptionID = item.SelectedOptionID,
						EmployeeID = checklist.EmployeeID
					},
					transaction);

				bool hasNewRemark =
					!string.IsNullOrWhiteSpace(item.NewRemark);

				if (responseChanged || hasNewRemark)
				{
					await connection.ExecuteAsync(
						historySql,
						new
						{
							checklist.TID,
							item.QID,
							EmployeeID = checklist.EmployeeID,
							OptionID = item.SelectedOptionID,
							UserRemarks = hasNewRemark
								? item.NewRemark!.Trim()
								: null
						},
						transaction);
				}
			}
		}
	}
}
