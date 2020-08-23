DROP PROCEDURE IF EXISTS [dbo].[GetSnapshots];
GO

CREATE PROCEDURE [dbo].[GetSnapshots] 
    @StreamName [NVARCHAR](256)
AS
BEGIN
	SELECT [Snapshots].[SnapshotId], [Snapshots].[StreamId], [Snapshots].[Version], [Snapshots].[Data], [Snapshots].[Description], [Snapshots].[CreatedAt]
    FROM [dbo].[Snapshots]
    INNER JOIN [dbo].[Streams]
    ON [Snapshots].[StreamId] = [Streams].[StreamId]
    WHERE [Streams].[Name] = @StreamName
    ORDER BY [Snapshots].[Version] DESC;
END