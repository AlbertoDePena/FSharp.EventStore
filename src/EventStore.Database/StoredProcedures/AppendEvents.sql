DROP PROCEDURE IF EXISTS [dbo].[AppendEvents];
GO

CREATE PROCEDURE [dbo].[AppendEvents] 
	@StreamId [NVARCHAR](50), 
    @StreamName [NVARCHAR](256),
	@Version [INT],
    @NewEvents [NewEvent] READONLY
AS
BEGIN
    IF NOT EXISTS (SELECT [StreamId] FROM [dbo].[Streams] WHERE [Name] = @StreamName)
    BEGIN
        SET @Version = 0;
        INSERT INTO [dbo].[Streams] ([StreamId], [Name], [Version]) 
        VALUES (@StreamId, @StreamName, @Version);
    END

	INSERT INTO [dbo].[Events] ([EventId], [StreamId], [Type], [Data], [Version])
	SELECT [EventId], [StreamId], [Type], [Data], [Version]
    FROM @NewEvents;

    SELECT @Version = MAX([Version])
    FROM [dbo].[Events]
    WHERE [StreamId] = @StreamId;

    UPDATE [dbo].[Streams]
    SET [Version] = @Version, [UpdatedAt] = SYSDATETIMEOFFSET()
    WHERE [StreamId] = @StreamId;
END