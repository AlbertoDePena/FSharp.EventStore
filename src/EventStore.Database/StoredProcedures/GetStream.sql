DROP PROCEDURE IF EXISTS [dbo].[GetStream];
GO

CREATE PROCEDURE [dbo].[GetStream] 
    @StreamName NVARCHAR(256)
AS
BEGIN
	SELECT [StreamId], [Version], [Name], [CreatedAt], [UpdatedAt]
    FROM dbo.[Streams]
    WHERE [Name] = @StreamName;
END