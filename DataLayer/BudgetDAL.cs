using Microsoft.Data.SqlClient;
using BuildWise.Models;

namespace BuildWise.DataLayer
{
    public class BudgetDAL
    {
        private string connectionString;

        public BudgetDAL(string connectionString)
        {
            this.connectionString = connectionString;
            EnsureProjectColumn();
        }

        private void EnsureProjectColumn()
        {
            using SqlConnection conn = new SqlConnection(connectionString);
            conn.Open();
            string query = @"
IF COL_LENGTH('BudgetItems', 'ProjectId') IS NULL
BEGIN
    ALTER TABLE BudgetItems ADD ProjectId INT NULL;
END";
            using SqlCommand cmd = new SqlCommand(query, conn);
            cmd.ExecuteNonQuery();
        }

        public List<BudgetItem> GetAll(int? projectId = null, int? userId = null)
        {
            List<BudgetItem> list = new List<BudgetItem>();

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = @"
SELECT b.BudgetId, b.ProjectId, b.Category, b.Amount, b.CreatedAt, b.UpdatedAt
FROM BudgetItems b
INNER JOIN Projects p ON b.ProjectId = p.ProjectId
WHERE (@ProjectId IS NULL OR b.ProjectId = @ProjectId)
  AND (@UserId IS NULL OR p.UserId = @UserId)
ORDER BY b.Category";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@ProjectId", (object?)projectId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@UserId", (object?)userId ?? DBNull.Value);
                SqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    BudgetItem item = new BudgetItem();
                    item.BudgetId = Convert.ToInt32(reader["BudgetId"]);
                    item.ProjectId = reader["ProjectId"] != DBNull.Value ? Convert.ToInt32(reader["ProjectId"]) : null;
                    item.Category = reader["Category"]?.ToString() ?? "";
                    item.Amount = Convert.ToDecimal(reader["Amount"]);
                    item.CreatedAt = Convert.ToDateTime(reader["CreatedAt"]);
                    item.UpdatedAt = Convert.ToDateTime(reader["UpdatedAt"]);
                    list.Add(item);
                }
            }

            return list;
        }

        public BudgetItem? GetById(int id)
        {
            BudgetItem? item = null;

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = "SELECT BudgetId, ProjectId, Category, Amount, CreatedAt, UpdatedAt FROM BudgetItems WHERE BudgetId = @Id";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Id", id);
                SqlDataReader reader = cmd.ExecuteReader();

                if (reader.Read())
                {
                    item = new BudgetItem();
                    item.BudgetId = Convert.ToInt32(reader["BudgetId"]);
                    item.ProjectId = reader["ProjectId"] != DBNull.Value ? Convert.ToInt32(reader["ProjectId"]) : null;
                    item.Category = reader["Category"]?.ToString() ?? "";
                    item.Amount = Convert.ToDecimal(reader["Amount"]);
                    item.CreatedAt = Convert.ToDateTime(reader["CreatedAt"]);
                    item.UpdatedAt = Convert.ToDateTime(reader["UpdatedAt"]);
                }
            }

            return item;
        }

        public bool Add(BudgetItem item)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = "INSERT INTO BudgetItems (ProjectId, Category, Amount) VALUES (@ProjectId, @Category, @Amount)";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@ProjectId", (object?)item.ProjectId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Category", item.Category);
                cmd.Parameters.AddWithValue("@Amount", item.Amount);
                return cmd.ExecuteNonQuery() > 0;
            }
        }

        public bool Update(BudgetItem item)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = "UPDATE BudgetItems SET Category = @Category, Amount = @Amount, UpdatedAt = GETDATE() WHERE BudgetId = @Id";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Id", item.BudgetId);
                cmd.Parameters.AddWithValue("@Category", item.Category);
                cmd.Parameters.AddWithValue("@Amount", item.Amount);
                return cmd.ExecuteNonQuery() > 0;
            }
        }

        public bool Delete(int id)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = "DELETE FROM BudgetItems WHERE BudgetId = @Id";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Id", id);
                return cmd.ExecuteNonQuery() > 0;
            }
        }

        public decimal GetTotalBudget(int? projectId = null)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = "SELECT ISNULL(SUM(Amount), 0) FROM BudgetItems WHERE (@ProjectId IS NULL OR ProjectId = @ProjectId)";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@ProjectId", (object?)projectId ?? DBNull.Value);
                object result = cmd.ExecuteScalar();
                return Convert.ToDecimal(result);
            }
        }

        public decimal GetTotalBudgetForUser(int userId)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = @"
                    SELECT ISNULL(SUM(b.Amount), 0) 
                    FROM BudgetItems b
                    INNER JOIN Projects p ON b.ProjectId = p.ProjectId
                    WHERE p.UserId = @UserId";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@UserId", userId);
                object result = cmd.ExecuteScalar();
                return Convert.ToDecimal(result);
            }
        }

        public decimal GetApprovedProjectBudget(int? projectId = null)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = "SELECT ISNULL(SUM(TotalBudget), 0) FROM Projects WHERE (@ProjectId IS NULL OR ProjectId = @ProjectId)";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@ProjectId", (object?)projectId ?? DBNull.Value);
                object result = cmd.ExecuteScalar();
                return Convert.ToDecimal(result);
            }
        }

        public decimal GetApprovedProjectBudgetForUser(int userId)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = "SELECT ISNULL(SUM(TotalBudget), 0) FROM Projects WHERE UserId = @UserId";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@UserId", userId);
                object result = cmd.ExecuteScalar();
                return Convert.ToDecimal(result);
            }
        }
    }
}
