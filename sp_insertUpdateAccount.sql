IF OBJECT_ID('usp_InsertAccount', 'P') IS NOT NULL DROP PROCEDURE usp_InsertAccount;
IF OBJECT_ID('usp_UpdateAccount', 'P') IS NOT NULL DROP PROCEDURE usp_UpdateAccount;
IF OBJECT_ID('usp_DeleteAccount', 'P') IS NOT NULL DROP PROCEDURE usp_DeleteAccount;
IF OBJECT_ID('usp_ManageAccount', 'P') IS NOT NULL DROP PROCEDURE usp_ManageAccount;
GO

CREATE OR ALTER PROCEDURE [dbo].[sp_insertUpdateAccount]
    @Flag       CHAR(1),
    @BankID     INT,
    @CODE1      NUMERIC(5,0),
    @brnc_code  NUMERIC(10,0),
    @CODE2      NUMERIC(18,0),
    @name       NVARCHAR(80) = NULL,
    @ADDR       NVARCHAR(200) = NULL,
    @BALANCE    NUMERIC(18,2) = 0,
    @OPN_DATE   DATETIME = NULL,
    @AgnCode    NUMERIC(18,0) = NULL,
    @Mobile_No  NVARCHAR(50) = NULL,
    @ChangeBy   NVARCHAR(50) = NULL,
    @ChangeIP   NVARCHAR(50) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    IF @Flag = 'I'
    BEGIN
        INSERT INTO acmaster
            (BankID, CODE1, brnc_code, CODE2, name, ADDR, BALANCE, OPN_DATE, AgnCode, Mobile_No, Entry_Date)
        VALUES
            (@BankID, @CODE1, @brnc_code, @CODE2, @name, @ADDR, @BALANCE, @OPN_DATE, @AgnCode, @Mobile_No, GETDATE());
    END
    ELSE IF @Flag = 'U'
    BEGIN
        UPDATE acmaster
        SET 
            name = @name,
            ADDR = @ADDR,
            BALANCE = @BALANCE,
            OPN_DATE = @OPN_DATE,
            AgnCode = @AgnCode,
            Mobile_No = @Mobile_No
        WHERE BankID = @BankID 
          AND CODE1 = @CODE1 
          AND brnc_code = @brnc_code 
          AND CODE2 = @CODE2;
    END
    ELSE IF @Flag = 'D'
    BEGIN
        DELETE FROM acmaster
        WHERE BankID = @BankID 
          AND CODE1 = @CODE1 
          AND brnc_code = @brnc_code 
          AND CODE2 = @CODE2;
    END
END
GO
