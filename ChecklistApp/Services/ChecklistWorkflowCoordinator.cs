
using ChecklistApp.Data;
using ChecklistApp.ViewModels;
using Microsoft.Data.SqlClient;

namespace ChecklistApp.Services
{
	public class ChecklistWorkflowCoordinator
	{
		private readonly ISqlConnectionFactory _connectionFactory;
		private readonly IChecklistService _checklistService;
		private readonly DocumentService _documentService;

		public ChecklistWorkflowCoordinator(
			ISqlConnectionFactory connectionFactory,
			IChecklistService checklistService,
			DocumentService documentService)
		{
			_connectionFactory = connectionFactory;
			_checklistService = checklistService;
			_documentService = documentService;
		}

		public async Task ProcessAtomicSaveAsync(
			ChecklistPageViewModel checklist)
		{
			using var connection = _connectionFactory.CreateConnection();
			await connection.OpenAsync();

			using var transaction = connection.BeginTransaction();

			try
			{
				await ProcessAtomicSaveAsync(
					checklist,
					connection,
					transaction);

				transaction.Commit();
			}
			catch
			{
				try
				{
					transaction.Rollback();
				}
				catch
				{
				}

				throw;
			}
		}

		public async Task ProcessAtomicSaveAsync(
			ChecklistPageViewModel checklist,
			SqlConnection connection,
			SqlTransaction transaction)
		{
			if (connection.State != System.Data.ConnectionState.Open)
			{
				throw new InvalidOperationException(
					"The supplied SQL connection must be open.");
			}

			if (!ReferenceEquals(transaction.Connection, connection))
			{
				throw new InvalidOperationException(
					"The supplied transaction must belong to the supplied connection.");
			}

			await _checklistService.SaveChecklistAsync(
				checklist,
				connection,
				transaction);

			foreach (var item in checklist.Items)
			{
				if (item.File == null || item.File.Length == 0)
				{
					continue;
				}

				if (!item.SelectedOptionID.HasValue)
				{
					throw new InvalidOperationException(
						$"Select a response before uploading a document for QID {item.QID}.");
				}

				await _documentService.SaveUploadAsync(
					item.File,
					checklist.TID,
					item.QID,
					item.SelectedOptionID.Value,
					checklist.EmployeeID,
					connection,
					transaction);
			}
		}
	}
}
