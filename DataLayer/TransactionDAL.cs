using Microsoft.Data.SqlClient;
using BuildWise.Models;

namespace BuildWise.DataLayer
{
    public class TransactionDAL
    {
        private string connectionString;

        public TransactionDAL(string connectionString)
        {
            this.connectionString = connectionString;
            EnsureTransactionTable();
        }

        private void EnsureTransactionTable()
        {
            using SqlConnection conn = new SqlConnection(connectionString);
            conn.Open();
            string query = @"
IF OBJECT_ID('TransactionLogs', 'U') IS NULL
BEGIN
    CREATE TABLE TransactionLogs (
        TransactionId INT IDENTITY(1,1) PRIMARY KEY,
        ProjectId INT NULL,
        TransactionDate DATETIME NOT NULL DEFAULT GETDATE(),
        TransactionType NVARCHAR(50) NOT NULL,
        Category NVARCHAR(100) NOT NULL,
        Description NVARCHAR(500) NULL,
        Amount DECIMAL(18,2) NOT NULL,
        BudgetEffect DECIMAL(5,2) NULL
    );
END;

IF COL_LENGTH('TransactionLogs', 'ProjectId') IS NULL
BEGIN
    ALTER TABLE TransactionLogs ADD ProjectId INT NULL;
END";
            using SqlCommand cmd = new SqlCommand(query, conn);
            cmd.ExecuteNonQuery();
        }

        public List<TransactionLog> GetAll(int? projectId = null, int? userId = null)
        {
            List<TransactionLog> list = new List<TransactionLog>();

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = @"
SELECT t.TransactionId, t.ProjectId, t.TransactionDate, t.TransactionType, t.Category, t.Description, t.Amount, t.BudgetEffect
FROM TransactionLogs t
INNER JOIN Projects p ON t.ProjectId = p.ProjectId
WHERE (@ProjectId IS NULL OR t.ProjectId = @ProjectId)
  AND (@UserId IS NULL OR p.UserId = @UserId)
ORDER BY t.TransactionDate DESC";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@ProjectId", (object?)projectId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@UserId", (object?)userId ?? DBNull.Value);
                SqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    list.Add(new TransactionLog {
                        TransactionId = Convert.ToInt32(reader["TransactionId"]),
                        ProjectId = reader["ProjectId"] != DBNull.Value ? Convert.ToInt32(reader["ProjectId"]) : null,
                        TransactionDate = Convert.ToDateTime(reader["TransactionDate"]),
                        TransactionType = reader["TransactionType"].ToString(),
                        Category = reader["Category"].ToString(),
                        Description = reader["Description"]?.ToString() ?? "",
                        Amount = Convert.ToDecimal(reader["Amount"]),
                        BudgetEffect = reader["BudgetEffect"] != DBNull.Value ? Convert.ToDecimal(reader["BudgetEffect"]) : 0
                    });
                }
            }
            return list;
        }

        public List<TransactionLog> GetFiltered(string category, string type, DateTime? fromDate, DateTime? toDate, int? projectId = null, int? userId = null)
        {
            List<TransactionLog> list = new List<TransactionLog>();

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = @"
SELECT t.TransactionId, t.ProjectId, t.TransactionDate, t.TransactionType, t.Category, t.Description, t.Amount, t.BudgetEffect 
FROM TransactionLogs t
INNER JOIN Projects p ON t.ProjectId = p.ProjectId
WHERE (@ProjectId IS NULL OR t.ProjectId = @ProjectId)
  AND (@UserId IS NULL OR p.UserId = @UserId)";

                if (!string.IsNullOrEmpty(category)) query += " AND t.Category = @Category";
                if (!string.IsNullOrEmpty(type)) query += " AND t.TransactionType = @Type";
                if (fromDate != null) query += " AND t.TransactionDate >= @FromDate";
                if (toDate != null) query += " AND t.TransactionDate <= @ToDate";

                query += " ORDER BY t.TransactionDate DESC";

                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@ProjectId", (object?)projectId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@UserId", (object?)userId ?? DBNull.Value);
                if (!string.IsNullOrEmpty(category)) cmd.Parameters.AddWithValue("@Category", category);
                if (!string.IsNullOrEmpty(type)) cmd.Parameters.AddWithValue("@Type", type);
                if (fromDate != null) cmd.Parameters.AddWithValue("@FromDate", fromDate.Value);
                if (toDate != null) cmd.Parameters.AddWithValue("@ToDate", toDate.Value);

                SqlDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    list.Add(new TransactionLog {
                        TransactionId = Convert.ToInt32(reader["TransactionId"]),
                        ProjectId = reader["ProjectId"] != DBNull.Value ? Convert.ToInt32(reader["ProjectId"]) : null,
                        TransactionDate = Convert.ToDateTime(reader["TransactionDate"]),
                        TransactionType = reader["TransactionType"].ToString(),
                        Category = reader["Category"].ToString(),
                        Description = reader["Description"]?.ToString() ?? "",
                        Amount = Convert.ToDecimal(reader["Amount"]),
                        BudgetEffect = reader["BudgetEffect"] != DBNull.Value ? Convert.ToDecimal(reader["BudgetEffect"]) : 0
                    });
                }
            }
            return list;
        }

        public int GetTotalCount(int? projectId = null, int? userId = null)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = @"
SELECT COUNT(*) FROM TransactionLogs t
INNER JOIN Projects p ON t.ProjectId = p.ProjectId
WHERE (@ProjectId IS NULL OR t.ProjectId = @ProjectId)
  AND (@UserId IS NULL OR p.UserId = @UserId)";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@ProjectId", (object?)projectId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@UserId", (object?)userId ?? DBNull.Value);
                return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }

        public decimal GetTotalAmount(int? projectId = null, int? userId = null)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = @"
SELECT ISNULL(SUM(Amount), 0) FROM TransactionLogs t
INNER JOIN Projects p ON t.ProjectId = p.ProjectId
WHERE (@ProjectId IS NULL OR t.ProjectId = @ProjectId)
  AND (@UserId IS NULL OR p.UserId = @UserId)";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@ProjectId", (object?)projectId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@UserId", (object?)userId ?? DBNull.Value);
                return Convert.ToDecimal(cmd.ExecuteScalar());
            }
        }

        public bool Add(TransactionLog item)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = "INSERT INTO TransactionLogs (ProjectId, TransactionType, Category, Description, Amount, BudgetEffect) VALUES (@ProjectId, @Type, @Category, @Description, @Amount, @BudgetEffect)";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@ProjectId", (object?)item.ProjectId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Type", item.TransactionType);
                cmd.Parameters.AddWithValue("@Category", item.Category);
                cmd.Parameters.AddWithValue("@Description", (object)item.Description ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Amount", item.Amount);
                cmd.Parameters.AddWithValue("@BudgetEffect", item.BudgetEffect);
                return cmd.ExecuteNonQuery() > 0;
            }
        }
    }
}
