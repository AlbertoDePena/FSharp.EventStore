DROP PROCEDURE IF EXISTS [dbo].[AddEvent];
GO

CREATE PROCEDURE [dbo].[AddEvent] 
	@StreamId [BIGINT], 
	@Type [NVARCHAR](256), 
	@Data [NVARCHAR](MAX), 
	@Version [INT]
AS
BEGIN
	INSERT INTO [dbo].[Events] ([StreamId], [Type], [Data], [Version])
	VALUES (@StreamId, @Type, @Data, @Version);

	SELECT SCOPE_IDENTITY() AS EventId;
END