DROP PROCEDURE IF EXISTS [dbo].[AddStream];
GO

CREATE PROCEDURE [dbo].[AddStream] 
    @Name [NVARCHAR](256), 
    @Version [INT] 
AS
BEGIN
	INSERT INTO [dbo].[Streams] ([Name], [Version]) 
    VALUES (@Name, @Version);

    SELECT SCOPE_IDENTITY() AS StreamId;
END