DROP PROCEDURE IF EXISTS [dbo].[AddSnapshot];
GO

CREATE PROCEDURE [dbo].[AddSnapshot] 
    @StreamId [BIGINT], 
    @Description [NVARCHAR](256), 
    @Data [NVARCHAR](MAX), 
    @Version [INT]
AS
BEGIN
	INSERT INTO [dbo].[Snapshots] ([StreamId], [Description], [Data], [Version])
    VALUES (@StreamId, @Description, @Data, @Version);

    SELECT SCOPE_IDENTITY() AS SnapshotId;
END