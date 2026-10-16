
using Dapper;
using ChecklistApp.Data;
using ChecklistApp.Models;

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

			const string sql = @"
                SELECT DemoPolicyID, PolicyName, PNo, CID
                FROM dbo.DemoPolicies
                WHERE IsActive = 1
                ORDER BY PolicyName;";

			var policies = await connection.QueryAsync<DemoPolicy>(sql);
			return policies.ToList();
		}


		public async Task<int> StartTransactionAsync(int demoPolicyId)
		{
			using var connection = _connectionFactory.CreateConnection();
			await connection.OpenAsync();

			using var transaction = connection.BeginTransaction(
				System.Data.IsolationLevel.Serializable);

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
				var existingTid = await connection.QuerySingleOrDefaultAsync<int?>(
					"""
            SELECT TID
            FROM dbo.ChecklistTransactions WITH (UPDLOCK, HOLDLOCK)
            WHERE CID = @CID AND PNo = @PNo;
            """,
					new { policy.CID, policy.PNo },
					transaction);

				if (existingTid.HasValue)
				{
					await transaction.CommitAsync();
					return existingTid.Value;
				}

				var qids = (await connection.QueryAsync<int>(
					"""
            SELECT QID
            FROM dbo.DemoPolicyRequirements
            WHERE DemoPolicyID = @DemoPolicyID;
            """,
					new { DemoPolicyID = demoPolicyId },
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
                (TID, PNo, CID, Status, CreatedBy)
            VALUES
                (@TID, @PNo, @CID, 'InProgress', @CreatedBy);
            """,
					new
					{
						TID = tid,
						policy.PNo,
						policy.CID,
						CreatedBy = "EMP001"
					},
					transaction);

				foreach (int qid in qids)
				{
					await connection.ExecuteAsync(
						"""
                INSERT INTO dbo.ChecklistTransactionRequirements (TID, QID)
                VALUES (@TID, @QID);
                """,
						new { TID = tid, QID = qid },
						transaction);
				}
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
