
using Dapper;
using ChecklistApp.Data;
using ChecklistApp.Models;
using System.Data;

namespace ChecklistApp.Services
{
	public class DemoPolicyService : IDemoPolicyService
	{
		private readonly ISqlConnectionFactory _connectionFactory;

		public DemoPolicyService(ISqlConnectionFactory connectionFactory)
		{
			_connectionFactory = connectionFactory;
		}

		public async Task<List<DemoPolicy>> GetPoliciesAsync()
		{
			using var connection = _connectionFactory.CreateConnection();
			await connection.OpenAsync();

			const string sql = """
                SELECT DemoPolicyID, PolicyName, PNo, CID
                FROM dbo.DemoPolicies
                WHERE IsActive = 1
                ORDER BY PolicyName;
                """;

			var policies = await connection.QueryAsync<DemoPolicy>(sql);
			return policies.ToList();
		}

		public async Task<int> StartTransactionAsync(int demoPolicyId)
		{
			using var connection = _connectionFactory.CreateConnection();
			await connection.OpenAsync();

			using var transaction =
				connection.BeginTransaction(IsolationLevel.Serializable);

			try
			{
				var policy = await connection.QuerySingleOrDefaultAsync<DemoPolicy>(
					"""
                    SELECT DemoPolicyID, PolicyName, PNo, CID
                    FROM dbo.DemoPolicies
                    WHERE DemoPolicyID = @DemoPolicyID
                      AND IsActive = 1;
                    """,
					new { DemoPolicyID = demoPolicyId },
					transaction);

				if (policy == null)
				{
					throw new InvalidOperationException(
						"The selected demo policy does not exist or is inactive.");
				}

				string contextId = $"{policy.PNo}|{policy.CID}";
				string definitionCode = $"DEMO_POLICY_{policy.DemoPolicyID}";

				var existingTid = await connection.QuerySingleOrDefaultAsync<int?>(
					"""
                    SELECT TID
                    FROM dbo.ChecklistTransactions WITH (UPDLOCK, HOLDLOCK)
                    WHERE ContextType = @ContextType
                      AND ContextID = @ContextID
                      AND ContextCode = @ContextCode;
                    """,
					new
					{
						ContextType = "Policy",
						ContextID = contextId,
						ContextCode = "Demo"
					},
					transaction);

				if (existingTid.HasValue)
				{
					await transaction.CommitAsync();
					return existingTid.Value;
				}

				var definitionExists = await connection.ExecuteScalarAsync<int>(
					"""
                    SELECT COUNT(1)
                    FROM dbo.ChecklistDefinitions
                    WHERE DefinitionCode = @DefinitionCode
                      AND IsActive = 1;
                    """,
					new { DefinitionCode = definitionCode },
					transaction);

				if (definitionExists == 0)
				{
					throw new InvalidOperationException(
						"The checklist definition for this demo policy is missing or inactive.");
				}

				var qids = (await connection.QueryAsync<int>(
					"""
                    SELECT QID
                    FROM dbo.ChecklistDefinitionRequirements
                    WHERE DefinitionCode = @DefinitionCode
                    ORDER BY QID;
                    """,
					new { DefinitionCode = definitionCode },
					transaction)).ToList();

				if (qids.Count == 0)
				{
					throw new InvalidOperationException(
						"No requirements are configured for this demo policy.");
				}

				long nextTid = await connection.ExecuteScalarAsync<long>(
					"SELECT NEXT VALUE FOR dbo.ChecklistTransactionIdSequence;",
					transaction: transaction);

				int tid = checked((int)nextTid);

				await connection.ExecuteAsync(
					"""
                    INSERT INTO dbo.ChecklistTransactions
                        (TID, ContextType, ContextID, ContextCode,
                         DefinitionCode, Status, CreatedBy)
                    VALUES
                        (@TID, @ContextType, @ContextID, @ContextCode,
                         @DefinitionCode, 'InProgress', @CreatedBy);
                    """,
					new
					{
						TID = tid,
						ContextType = "Policy",
						ContextID = contextId,
						ContextCode = "Demo",
						DefinitionCode = definitionCode,
						CreatedBy = "EMP001"
					},
					transaction);

				await connection.ExecuteAsync(
					"""
                    INSERT INTO dbo.ChecklistTransactionRequirements (TID, QID)
                    SELECT @TID, QID
                    FROM dbo.ChecklistDefinitionRequirements
                    WHERE DefinitionCode = @DefinitionCode;
                    """,
					new
					{
						TID = tid,
						DefinitionCode = definitionCode
					},
					transaction);

				await transaction.CommitAsync();

				return tid;
			}
			catch
			{
				await transaction.RollbackAsync();
				throw;
			}
		}
	}
}
