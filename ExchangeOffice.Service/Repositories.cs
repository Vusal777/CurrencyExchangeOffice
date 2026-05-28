using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace ExchangeOffice.Service
{
    public interface IExchangeOfficeRepository
    {
        UserDto CreateUser(string fullName, string email, string password);
        UserDto GetUser(int userId);
        UserDto LoginUser(string email, string password);
        IList<UserDto> GetUsers();
        BalanceDto AddToBalance(int userId, string currencyCode, decimal amount);
        BalanceDto GetBalance(int userId, string currencyCode);
        IList<BalanceDto> GetBalances(int userId);
        TransactionDto AddTransaction(TransactionDto transaction);
        IList<TransactionDto> GetTransactions(int userId);
    }

    public class MemoryExchangeOfficeRepository : IExchangeOfficeRepository
    {
        private static readonly object Sync = new object();
        private static readonly List<UserDto> Users = new List<UserDto>();
        private static readonly Dictionary<int, string> PasswordHashes = new Dictionary<int, string>();
        private static readonly List<BalanceDto> Balances = new List<BalanceDto>();
        private static readonly List<TransactionDto> Transactions = new List<TransactionDto>();
        private static int _nextUserId = 1;
        private static int _nextTransactionId = 1;

        public UserDto CreateUser(string fullName, string email, string password)
        {
            if (string.IsNullOrWhiteSpace(fullName)) throw new InvalidOperationException("Full name is required.");
            if (string.IsNullOrWhiteSpace(email)) throw new InvalidOperationException("Email is required.");
            if (string.IsNullOrWhiteSpace(password) || password.Length < 4) throw new InvalidOperationException("Password must have at least 4 characters.");

            lock (Sync)
            {
                if (Users.Any(x => string.Equals(x.Email, email.Trim(), StringComparison.OrdinalIgnoreCase)))
                {
                    throw new InvalidOperationException("A user with this email already exists. Use Login instead.");
                }

                var user = new UserDto
                {
                    Id = _nextUserId++,
                    FullName = fullName.Trim(),
                    Email = email.Trim(),
                    CreatedAt = DateTime.Now
                };
                Users.Add(user);
                PasswordHashes[user.Id] = PasswordHasher.Hash(password);
                Balances.Add(new BalanceDto { UserId = user.Id, CurrencyCode = "PLN", Amount = 0m });
                return user;
            }
        }

        public UserDto LoginUser(string email, string password)
        {
            if (string.IsNullOrWhiteSpace(email)) throw new InvalidOperationException("Email is required.");
            if (string.IsNullOrWhiteSpace(password)) throw new InvalidOperationException("Password is required.");

            lock (Sync)
            {
                var user = Users.FirstOrDefault(x => string.Equals(x.Email, email.Trim(), StringComparison.OrdinalIgnoreCase));
                if (user == null) throw new InvalidOperationException("User was not found.");
                if (!PasswordHashes.TryGetValue(user.Id, out var passwordHash) || passwordHash != PasswordHasher.Hash(password))
                {
                    throw new InvalidOperationException("Invalid email or password.");
                }

                return user;
            }
        }

        public IList<UserDto> GetUsers()
        {
            lock (Sync)
            {
                return Users.OrderBy(x => x.FullName).ThenBy(x => x.Email).ToList();
            }
        }

        public UserDto GetUser(int userId)
        {
            lock (Sync)
            {
                var user = Users.FirstOrDefault(x => x.Id == userId);
                if (user == null) throw new InvalidOperationException("User was not found.");
                return user;
            }
        }

        public BalanceDto AddToBalance(int userId, string currencyCode, decimal amount)
        {
            lock (Sync)
            {
                GetUser(userId);
                var balance = Balances.FirstOrDefault(x => x.UserId == userId && x.CurrencyCode == currencyCode);
                if (balance == null)
                {
                    balance = new BalanceDto { UserId = userId, CurrencyCode = currencyCode, Amount = 0m };
                    Balances.Add(balance);
                }

                var newAmount = balance.Amount + amount;
                if (newAmount < 0m) throw new InvalidOperationException("Insufficient funds on " + currencyCode + " balance.");
                balance.Amount = decimal.Round(newAmount, 4, MidpointRounding.AwayFromZero);
                return balance;
            }
        }

        public BalanceDto GetBalance(int userId, string currencyCode)
        {
            lock (Sync)
            {
                GetUser(userId);
                return Balances.FirstOrDefault(x => x.UserId == userId && x.CurrencyCode == currencyCode)
                    ?? new BalanceDto { UserId = userId, CurrencyCode = currencyCode, Amount = 0m };
            }
        }

        public IList<BalanceDto> GetBalances(int userId)
        {
            lock (Sync)
            {
                GetUser(userId);
                return Balances.Where(x => x.UserId == userId).OrderBy(x => x.CurrencyCode).ToList();
            }
        }

        public TransactionDto AddTransaction(TransactionDto transaction)
        {
            lock (Sync)
            {
                transaction.Id = _nextTransactionId++;
                transaction.CreatedAt = DateTime.Now;
                Transactions.Add(transaction);
                return transaction;
            }
        }

        public IList<TransactionDto> GetTransactions(int userId)
        {
            lock (Sync)
            {
                GetUser(userId);
                return Transactions.Where(x => x.UserId == userId).OrderByDescending(x => x.CreatedAt).ToList();
            }
        }
    }

    public class SqlExchangeOfficeRepository : IExchangeOfficeRepository
    {
        private readonly string _connectionString;

        public SqlExchangeOfficeRepository()
        {
            _connectionString = ConfigurationManager.ConnectionStrings["ExchangeOfficeDb"].ConnectionString;
        }

        public UserDto CreateUser(string fullName, string email, string password)
        {
            if (string.IsNullOrWhiteSpace(password) || password.Length < 4) throw new InvalidOperationException("Password must have at least 4 characters.");

            using (var connection = OpenConnection())
            using (var command = new SqlCommand(@"IF EXISTS (SELECT 1 FROM Users WHERE Email = @Email)
BEGIN
    RAISERROR('A user with this email already exists. Use Login instead.', 16, 1);
    RETURN;
END

INSERT INTO Users(FullName, Email, PasswordHash)
OUTPUT INSERTED.Id, INSERTED.FullName, INSERTED.Email, INSERTED.CreatedAt
VALUES(@FullName, @Email, @PasswordHash)", connection))
            {
                command.Parameters.AddWithValue("@FullName", fullName.Trim());
                command.Parameters.AddWithValue("@Email", email.Trim());
                command.Parameters.AddWithValue("@PasswordHash", PasswordHasher.Hash(password));
                using (var reader = command.ExecuteReader())
                {
                    reader.Read();
                    var user = ReadUser(reader);
                    reader.Close();
                    AddToBalance(user.Id, "PLN", 0m);
                    return user;
                }
            }
        }

        public UserDto LoginUser(string email, string password)
        {
            using (var connection = OpenConnection())
            using (var command = new SqlCommand("SELECT Id, FullName, Email, CreatedAt FROM Users WHERE Email = @Email AND PasswordHash = @PasswordHash", connection))
            {
                command.Parameters.AddWithValue("@Email", email.Trim());
                command.Parameters.AddWithValue("@PasswordHash", PasswordHasher.Hash(password));
                using (var reader = command.ExecuteReader())
                {
                    if (!reader.Read()) throw new InvalidOperationException("Invalid email or password.");
                    return ReadUser(reader);
                }
            }
        }

        public IList<UserDto> GetUsers()
        {
            using (var connection = OpenConnection())
            using (var command = new SqlCommand("SELECT Id, FullName, Email, CreatedAt FROM Users ORDER BY FullName, Email", connection))
            using (var reader = command.ExecuteReader())
            {
                var result = new List<UserDto>();
                while (reader.Read()) result.Add(ReadUser(reader));
                return result;
            }
        }

        public UserDto GetUser(int userId)
        {
            using (var connection = OpenConnection())
            using (var command = new SqlCommand("SELECT Id, FullName, Email, CreatedAt FROM Users WHERE Id = @Id", connection))
            {
                command.Parameters.AddWithValue("@Id", userId);
                using (var reader = command.ExecuteReader())
                {
                    if (!reader.Read()) throw new InvalidOperationException("User was not found.");
                    return ReadUser(reader);
                }
            }
        }

        public BalanceDto AddToBalance(int userId, string currencyCode, decimal amount)
        {
            using (var connection = OpenConnection())
            using (var transaction = connection.BeginTransaction())
            {
                try
                {
                    EnsureBalanceRow(connection, transaction, userId, currencyCode);
                    using (var check = new SqlCommand("SELECT Amount FROM Balances WHERE UserId = @UserId AND CurrencyCode = @CurrencyCode", connection, transaction))
                    {
                        check.Parameters.AddWithValue("@UserId", userId);
                        check.Parameters.AddWithValue("@CurrencyCode", currencyCode);
                        var current = (decimal)check.ExecuteScalar();
                        if (current + amount < 0m) throw new InvalidOperationException("Insufficient funds on " + currencyCode + " balance.");
                    }

                    using (var command = new SqlCommand("UPDATE Balances SET Amount = ROUND(Amount + @Amount, 4) WHERE UserId = @UserId AND CurrencyCode = @CurrencyCode", connection, transaction))
                    {
                        command.Parameters.AddWithValue("@Amount", amount);
                        command.Parameters.AddWithValue("@UserId", userId);
                        command.Parameters.AddWithValue("@CurrencyCode", currencyCode);
                        command.ExecuteNonQuery();
                    }

                    transaction.Commit();
                    return GetBalance(userId, currencyCode);
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
        }

        public BalanceDto GetBalance(int userId, string currencyCode)
        {
            using (var connection = OpenConnection())
            using (var command = new SqlCommand("SELECT UserId, CurrencyCode, Amount FROM Balances WHERE UserId = @UserId AND CurrencyCode = @CurrencyCode", connection))
            {
                command.Parameters.AddWithValue("@UserId", userId);
                command.Parameters.AddWithValue("@CurrencyCode", currencyCode);
                using (var reader = command.ExecuteReader())
                {
                    if (!reader.Read()) return new BalanceDto { UserId = userId, CurrencyCode = currencyCode, Amount = 0m };
                    return ReadBalance(reader);
                }
            }
        }

        public IList<BalanceDto> GetBalances(int userId)
        {
            using (var connection = OpenConnection())
            using (var command = new SqlCommand("SELECT UserId, CurrencyCode, Amount FROM Balances WHERE UserId = @UserId ORDER BY CurrencyCode", connection))
            {
                command.Parameters.AddWithValue("@UserId", userId);
                using (var reader = command.ExecuteReader())
                {
                    var result = new List<BalanceDto>();
                    while (reader.Read()) result.Add(ReadBalance(reader));
                    return result;
                }
            }
        }

        public TransactionDto AddTransaction(TransactionDto transaction)
        {
            using (var connection = OpenConnection())
            using (var command = new SqlCommand(@"INSERT INTO Transactions(UserId, Type, CurrencyCode, CurrencyAmount, PlnAmount, Rate)
OUTPUT INSERTED.Id, INSERTED.UserId, INSERTED.Type, INSERTED.CurrencyCode, INSERTED.CurrencyAmount, INSERTED.PlnAmount, INSERTED.Rate, INSERTED.CreatedAt
VALUES(@UserId, @Type, @CurrencyCode, @CurrencyAmount, @PlnAmount, @Rate)", connection))
            {
                command.Parameters.AddWithValue("@UserId", transaction.UserId);
                command.Parameters.AddWithValue("@Type", transaction.Type);
                command.Parameters.AddWithValue("@CurrencyCode", transaction.CurrencyCode);
                command.Parameters.AddWithValue("@CurrencyAmount", transaction.CurrencyAmount);
                command.Parameters.AddWithValue("@PlnAmount", transaction.PlnAmount);
                command.Parameters.AddWithValue("@Rate", transaction.Rate);
                using (var reader = command.ExecuteReader())
                {
                    reader.Read();
                    return ReadTransaction(reader);
                }
            }
        }

        public IList<TransactionDto> GetTransactions(int userId)
        {
            using (var connection = OpenConnection())
            using (var command = new SqlCommand(@"SELECT Id, UserId, Type, CurrencyCode, CurrencyAmount, PlnAmount, Rate, CreatedAt
FROM Transactions WHERE UserId = @UserId ORDER BY CreatedAt DESC", connection))
            {
                command.Parameters.AddWithValue("@UserId", userId);
                using (var reader = command.ExecuteReader())
                {
                    var result = new List<TransactionDto>();
                    while (reader.Read()) result.Add(ReadTransaction(reader));
                    return result;
                }
            }
        }

        private SqlConnection OpenConnection()
        {
            var connection = new SqlConnection(_connectionString);
            connection.Open();
            return connection;
        }

        private static void EnsureBalanceRow(SqlConnection connection, SqlTransaction transaction, int userId, string currencyCode)
        {
            using (var command = new SqlCommand(@"IF NOT EXISTS (SELECT 1 FROM Balances WHERE UserId = @UserId AND CurrencyCode = @CurrencyCode)
INSERT INTO Balances(UserId, CurrencyCode, Amount) VALUES(@UserId, @CurrencyCode, 0)", connection, transaction))
            {
                command.Parameters.AddWithValue("@UserId", userId);
                command.Parameters.AddWithValue("@CurrencyCode", currencyCode);
                command.ExecuteNonQuery();
            }
        }

        private static UserDto ReadUser(IDataRecord record)
        {
            return new UserDto
            {
                Id = (int)record["Id"],
                FullName = (string)record["FullName"],
                Email = (string)record["Email"],
                CreatedAt = (DateTime)record["CreatedAt"]
            };
        }

        private static BalanceDto ReadBalance(IDataRecord record)
        {
            return new BalanceDto
            {
                UserId = (int)record["UserId"],
                CurrencyCode = (string)record["CurrencyCode"],
                Amount = (decimal)record["Amount"]
            };
        }

        private static TransactionDto ReadTransaction(IDataRecord record)
        {
            return new TransactionDto
            {
                Id = (int)record["Id"],
                UserId = (int)record["UserId"],
                Type = (string)record["Type"],
                CurrencyCode = (string)record["CurrencyCode"],
                CurrencyAmount = (decimal)record["CurrencyAmount"],
                PlnAmount = (decimal)record["PlnAmount"],
                Rate = (decimal)record["Rate"],
                CreatedAt = (DateTime)record["CreatedAt"]
            };
        }
    }

    public static class PasswordHasher
    {
        public static string Hash(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return BitConverter.ToString(bytes).Replace("-", string.Empty);
            }
        }
    }
}
