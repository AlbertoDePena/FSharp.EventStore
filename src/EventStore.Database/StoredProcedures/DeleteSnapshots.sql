DROP PROCEDURE IF EXISTS [dbo].[DeleteSnapshots];
GO

CREATE PROCEDURE [dbo].[DeleteSnapshots] 
    @StreamName [NVARCHAR](256)
AS
BEGIN
	DELETE FROM [dbo].[Snapshots] 
    WHERE [StreamId] IN 
    (SELECT [StreamId] FROM [dbo].[Streams] WHERE [Name] = @StreamName);
END