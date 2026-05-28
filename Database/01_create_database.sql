IF DB_ID('ExchangeOfficeDb') IS NULL
BEGIN
    CREATE DATABASE ExchangeOfficeDb;
END
GO

USE ExchangeOfficeDb;
GO

IF OBJECT_ID('dbo.Transactions', 'U') IS NOT NULL DROP TABLE dbo.Transactions;
IF OBJECT_ID('dbo.Balances', 'U') IS NOT NULL DROP TABLE dbo.Balances;
IF OBJECT_ID('dbo.Users', 'U') IS NOT NULL DROP TABLE dbo.Users;
GO

CREATE TABLE dbo.Users
(
    Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Users PRIMARY KEY,
    FullName NVARCHAR(120) NOT NULL,
    Email NVARCHAR(160) NOT NULL,
    PasswordHash CHAR(64) NOT NULL,
    CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_Users_CreatedAt DEFAULT SYSUTCDATETIME()
);
GO

CREATE UNIQUE INDEX UX_Users_Email ON dbo.Users(Email);
GO

CREATE TABLE dbo.Balances
(
    UserId INT NOT NULL,
    CurrencyCode CHAR(3) NOT NULL,
    Amount DECIMAL(18,4) NOT NULL CONSTRAINT DF_Balances_Amount DEFAULT 0,
    CONSTRAINT PK_Balances PRIMARY KEY (UserId, CurrencyCode),
    CONSTRAINT FK_Balances_Users FOREIGN KEY (UserId) REFERENCES dbo.Users(Id),
    CONSTRAINT CK_Balances_Amount CHECK (Amount >= 0)
);
GO

CREATE TABLE dbo.Transactions
(
    Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Transactions PRIMARY KEY,
    UserId INT NOT NULL,
    Type NVARCHAR(20) NOT NULL,
    CurrencyCode CHAR(3) NOT NULL,
    CurrencyAmount DECIMAL(18,4) NOT NULL,
    PlnAmount DECIMAL(18,4) NOT NULL,
    Rate DECIMAL(18,4) NOT NULL,
    CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_Transactions_CreatedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_Transactions_Users FOREIGN KEY (UserId) REFERENCES dbo.Users(Id),
    CONSTRAINT CK_Transactions_Type CHECK (Type IN ('TOP_UP', 'BUY', 'SELL'))
);
GO

CREATE INDEX IX_Transactions_UserId_CreatedAt ON dbo.Transactions(UserId, CreatedAt DESC);
GO
