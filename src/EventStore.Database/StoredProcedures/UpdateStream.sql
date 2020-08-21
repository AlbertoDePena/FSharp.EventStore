DROP PROCEDURE IF EXISTS [dbo].[UpdateStream];
GO

CREATE PROCEDURE [dbo].[UpdateStream] 
    @StreamId [BIGINT], 
    @Version [INT]
AS
BEGIN
	UPDATE [dbo].[Streams]
    SET [Version] = @Version, [UpdatedAt] = SYSDATETIMEOFFSET()
    WHERE [StreamId] = @StreamId;
END