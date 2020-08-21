DROP PROCEDURE IF EXISTS [dbo].[GetAllStreams];
GO

CREATE PROCEDURE [dbo].[GetAllStreams]
AS
BEGIN
	SELECT [StreamId], [Version], [Name], [CreatedAt], [UpdatedAt]
    FROM [dbo].[Streams];
END