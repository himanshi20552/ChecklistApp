
using System.Data;
using Dapper;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.SqlClient;

namespace ChecklistApp.Services
{
	public class DocumentService
	{
		private const long MaxFileSize = 10 * 1024 * 1024;

		private static readonly Dictionary<string, string> AllowedTypes =
			new(StringComparer.OrdinalIgnoreCase)
			{
				[".pdf"] = "application/pdf",
				[".jpg"] = "image/jpeg",
				[".jpeg"] = "image/jpeg",
				[".png"] = "image/png"
			};

		public async Task<int> SaveUploadAsync(
			IFormFile file,
			int tid,
			int qid,
			int optionId,
			string employeeId,
			SqlConnection connection,
			SqlTransaction transaction)
		{
			if (file == null || file.Length == 0)
			{
				throw new InvalidOperationException(
					"A non-empty file is required for this upload operation.");
			}

			if (tid <= 0 ||
				qid <= 0 ||
				string.IsNullOrWhiteSpace(employeeId) ||
				employeeId.Length > 50)
			{
				throw new InvalidOperationException(
					"Valid checklist and employee identifiers are required.");
			}

			if (file.Length > MaxFileSize)
			{
				throw new InvalidOperationException(
					"The maximum permitted file size is 10 MB.");
			}

			var extension = Path.GetExtension(
				Path.GetFileName(file.FileName));

			if (!AllowedTypes.TryGetValue(extension, out var expectedContentType) ||
				!string.Equals(
					file.ContentType,
					expectedContentType,
					StringComparison.OrdinalIgnoreCase))
			{
				throw new InvalidOperationException(
					"Only PDF, JPG, JPEG, and PNG files are permitted.");
			}

			const string requirementSql = """
    SELECT COUNT(1)
    FROM dbo.ChecklistTransactionRequirements ctr
    INNER JOIN dbo.ChecklistTransactions ct
        ON ct.TID = ctr.TID
    INNER JOIN dbo.Requirements r
        ON r.QID = ctr.QID
    INNER JOIN dbo.ChecklistResponses cr
        ON cr.TID = ctr.TID
       AND cr.QID = ctr.QID
    INNER JOIN dbo.RequirementResponseOptions o
        ON o.QID = cr.QID
       AND o.OptionID = cr.OptionID
    WHERE ctr.TID = @TID
      AND ctr.QID = @QID
      AND cr.OptionID = @OptionID
      AND ct.Status = 'InProgress'
      AND r.IsActive = 1
      AND r.IsDoc = 1
      AND o.IsActive = 1;
    """;

			var validRequirement = await connection.ExecuteScalarAsync<int>(
				requirementSql,
				new
				{
					TID = tid,
					QID = qid,
					OptionID = optionId
				},
				transaction);

			if (validRequirement == 0)
			{
				throw new InvalidOperationException(
					"The selected option is invalid or does not allow document uploads.");
			}

			const string createDocumentSql = """
    IF NOT EXISTS
    (
        SELECT 1
        FROM dbo.Documents WITH (UPDLOCK, HOLDLOCK)
        WHERE TID = @TID
          AND QID = @QID
          AND OptionID = @OptionID
    )
    BEGIN
        INSERT INTO dbo.Documents (TID, QID, OptionID)
        VALUES (@TID, @QID, @OptionID);
    END;

    SELECT DocumentID
    FROM dbo.Documents
    WHERE TID = @TID
      AND QID = @QID
      AND OptionID = @OptionID;
    """;

			var documentId = await connection.ExecuteScalarAsync<int>(
				createDocumentSql,
				new
				{
					TID = tid,
					QID = qid,
					OptionID = optionId
				},
				transaction);

			const string insertUploadSql = """
                INSERT INTO dbo.DocumentUploads
                (
                    DocumentID,
                    UploadedBy,
                    OriginalFileName,
                    ContentType,
                    FileSizeBytes,
                    FileData
                )
                OUTPUT INSERTED.UploadID
                VALUES
                (
                    @DocumentID,
                    @UploadedBy,
                    @OriginalFileName,
                    @ContentType,
                    @FileSizeBytes,
                    @FileData
                );
                """;

			using var fileStream = file.OpenReadStream();

			using var command = new SqlCommand(
				insertUploadSql,
				connection,
				transaction);

			command.Parameters.Add("@DocumentID", SqlDbType.Int)
				.Value = documentId;
			command.Parameters.Add("@UploadedBy", SqlDbType.VarChar, 50)
				.Value = employeeId;
			command.Parameters.Add("@OriginalFileName", SqlDbType.NVarChar, 255)
				.Value = Path.GetFileName(file.FileName);
			command.Parameters.Add("@ContentType", SqlDbType.VarChar, 100)
				.Value = expectedContentType;
			command.Parameters.Add("@FileSizeBytes", SqlDbType.BigInt)
				.Value = file.Length;
			command.Parameters.Add(
				new SqlParameter("@FileData", SqlDbType.VarBinary, -1)
				{
					Value = fileStream
				});

			var uploadId = Convert.ToInt32(
				await command.ExecuteScalarAsync());

			const string updateStatusSql = """
                UPDATE dbo.Documents
                SET Status = 'Submitted'
                WHERE DocumentID = @DocumentID;
                """;

			await connection.ExecuteAsync(
				updateStatusSql,
				new { DocumentID = documentId },
				transaction);

			return uploadId;
		}
	}
}
