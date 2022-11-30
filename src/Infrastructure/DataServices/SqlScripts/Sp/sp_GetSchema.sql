IF OBJECT_ID('sp_GetSchema') IS NULL
    EXEC('CREATE PROCEDURE dbo.sp_GetSchema AS');

GO
ALTER PROCEDURE
[dbo].[sp_GetSchema]
     @name varchar(50)
AS
BEGIN

SELECT *
FROM dbo.Schemas
WHERE Name=@name;

END

GO

  