DROP PROCEDURE IF EXISTS [dbo].[GetEvents];
GO

CREATE PROCEDURE [dbo].[GetEvents] 
    @StreamName [NVARCHAR](256), 
    @StartAtVersion [INT]
AS
BEGIN
	SELECT [Events].[EventId], [Events].[StreamId], [Events].[Type], [Events].[Version], [Events].[Data], [Events].[CreatedAt]
    FROM [dbo].[Events]
    INNER JOIN [dbo].[Streams]
    ON [Events].[StreamId] = [Streams].[StreamId]
    WHERE [Streams].[Name] = @StreamName
    AND [Events].[Version] >= @StartAtVersion
    ORDER BY [Events].[Version];
END