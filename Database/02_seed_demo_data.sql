USE ExchangeOfficeDb;
GO

INSERT INTO dbo.Users(FullName, Email, PasswordHash)
VALUES ('Demo Student', 'student@example.com', '0EAD2060B65992DCA4769AF601A1B3A35EF38CFAD2C2C465BB160EA764157C5D');

DECLARE @UserId INT = SCOPE_IDENTITY();

INSERT INTO dbo.Balances(UserId, CurrencyCode, Amount)
VALUES
    (@UserId, 'PLN', 1000.0000),
    (@UserId, 'USD', 0.0000),
    (@UserId, 'EUR', 0.0000);

INSERT INTO dbo.Transactions(UserId, Type, CurrencyCode, CurrencyAmount, PlnAmount, Rate)
VALUES (@UserId, 'TOP_UP', 'PLN', 1000.0000, 1000.0000, 1.0000);
GO
