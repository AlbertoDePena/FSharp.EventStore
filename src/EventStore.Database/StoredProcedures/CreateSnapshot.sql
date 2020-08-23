DROP PROCEDURE IF EXISTS [dbo].[CreateSnapshot];
GO

CREATE PROCEDURE [dbo].[CreateSnapshot] 
    @SnapshotId [NVARCHAR](50),
    @StreamId [NVARCHAR](50), 
    @Description [NVARCHAR](256), 
    @Data [NVARCHAR](MAX), 
    @Version [INT]
AS
BEGIN
	INSERT INTO [dbo].[Snapshots] ([SnapshotId], [StreamId], [Description], [Data], [Version])
    VALUES (@SnapshotId, @StreamId, @Description, @Data, @Version);
END