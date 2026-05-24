using Microsoft.Data.SqlClient;
using BuildWise.Models;

namespace BuildWise.DataLayer
{
    /// <summary>
    /// Data Access Layer for ExpenseItems table
    /// </summary>
    public class ExpenseDAL
    {
        private string connectionString;

        public ExpenseDAL(string connectionString)
        {
            this.connectionString = connectionString;
            EnsureProjectColumn();
        }

        private void EnsureProjectColumn()
        {
            using SqlConnection conn = new SqlConnection(connectionString);
            conn.Open();
            string query = @"
IF COL_LENGTH('ExpenseItems', 'ProjectId') IS NULL
BEGIN
    ALTER TABLE ExpenseItems ADD ProjectId INT NULL;
END";
            using SqlCommand cmd = new SqlCommand(query, conn);
            cmd.ExecuteNonQuery();
        }

        public List<ExpenseItem> GetAll(int? projectId = null, int? userId = null)
        {
            List<ExpenseItem> list = new List<ExpenseItem>();

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = @"
SELECT e.ExpenseId, e.ProjectId, e.Category, e.Description, e.Amount, e.ExpenseDate, e.CreatedAt, e.UpdatedAt
FROM ExpenseItems e
INNER JOIN Projects p ON e.ProjectId = p.ProjectId
WHERE (@ProjectId IS NULL OR e.ProjectId = @ProjectId)
  AND (@UserId IS NULL OR p.UserId = @UserId)
ORDER BY e.ExpenseDate DESC";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@ProjectId", (object?)projectId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@UserId", (object?)userId ?? DBNull.Value);
                SqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    ExpenseItem item = new ExpenseItem();
                    item.ExpenseId = Convert.ToInt32(reader["ExpenseId"]);
                    item.ProjectId = reader["ProjectId"] != DBNull.Value ? Convert.ToInt32(reader["ProjectId"]) : null;
                    item.Category = reader["Category"]?.ToString() ?? "";
                    item.Description = reader["Description"] != DBNull.Value ? reader["Description"]?.ToString() ?? "" : "";
                    item.Amount = Convert.ToDecimal(reader["Amount"]);
                    item.ExpenseDate = Convert.ToDateTime(reader["ExpenseDate"]);
                    item.CreatedAt = Convert.ToDateTime(reader["CreatedAt"]);
                    item.UpdatedAt = Convert.ToDateTime(reader["UpdatedAt"]);
                    list.Add(item);
                }
            }

            return list;
        }

        public ExpenseItem? GetById(int id)
        {
            ExpenseItem? item = null;

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = "SELECT ExpenseId, ProjectId, Category, Description, Amount, ExpenseDate, CreatedAt, UpdatedAt FROM ExpenseItems WHERE ExpenseId = @Id";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Id", id);
                SqlDataReader reader = cmd.ExecuteReader();

                if (reader.Read())
                {
                    item = new ExpenseItem();
                    item.ExpenseId = Convert.ToInt32(reader["ExpenseId"]);
                    item.ProjectId = reader["ProjectId"] != DBNull.Value ? Convert.ToInt32(reader["ProjectId"]) : null;
                    item.Category = reader["Category"]?.ToString() ?? "";
                    item.Description = reader["Description"] != DBNull.Value ? reader["Description"]?.ToString() ?? "" : "";
                    item.Amount = Convert.ToDecimal(reader["Amount"]);
                    item.ExpenseDate = Convert.ToDateTime(reader["ExpenseDate"]);
                    item.CreatedAt = Convert.ToDateTime(reader["CreatedAt"]);
                    item.UpdatedAt = Convert.ToDateTime(reader["UpdatedAt"]);
                }
            }

            return item;
        }

        public bool Add(ExpenseItem item)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = "INSERT INTO ExpenseItems (ProjectId, Category, Description, Amount, ExpenseDate) VALUES (@ProjectId, @Category, @Description, @Amount, @ExpenseDate)";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@ProjectId", (object?)item.ProjectId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Category", item.Category);
                cmd.Parameters.AddWithValue("@Description", (object?)item.Description ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Amount", item.Amount);
                cmd.Parameters.AddWithValue("@ExpenseDate", item.ExpenseDate);
                return cmd.ExecuteNonQuery() > 0;
            }
        }

        public bool Update(ExpenseItem item)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = "UPDATE ExpenseItems SET Category = @Category, Description = @Description, Amount = @Amount, ExpenseDate = @ExpenseDate, UpdatedAt = GETDATE() WHERE ExpenseId = @Id";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Id", item.ExpenseId);
                cmd.Parameters.AddWithValue("@Category", item.Category);
                cmd.Parameters.AddWithValue("@Description", (object?)item.Description ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Amount", item.Amount);
                cmd.Parameters.AddWithValue("@ExpenseDate", item.ExpenseDate);
                return cmd.ExecuteNonQuery() > 0;
            }
        }

        public bool Delete(int id)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = "DELETE FROM ExpenseItems WHERE ExpenseId = @Id";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Id", id);
                return cmd.ExecuteNonQuery() > 0;
            }
        }

        public decimal GetTotalExpenses(int? projectId = null)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = @"
SELECT
    ISNULL((SELECT SUM(Amount) FROM ExpenseItems WHERE (@ProjectId IS NULL OR ProjectId = @ProjectId)), 0)
  + ISNULL((SELECT SUM(TotalCost) FROM MaterialPurchases WHERE (@ProjectId IS NULL OR ProjectId = @ProjectId)), 0)
  + ISNULL((SELECT SUM(AmountPaid) FROM WagePayments WHERE (@ProjectId IS NULL OR ProjectId = @ProjectId)), 0)";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@ProjectId", (object?)projectId ?? DBNull.Value);
                object result = cmd.ExecuteScalar();
                return Convert.ToDecimal(result);
            }
        }

        public decimal GetTotalSpentForUser(int userId)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = @"
SELECT
    ISNULL((
        SELECT SUM(e.Amount)
        FROM ExpenseItems e
        INNER JOIN Projects p ON e.ProjectId = p.ProjectId
        WHERE p.UserId = @UserId
    ), 0)
  + ISNULL((
        SELECT SUM(mp.TotalCost)
        FROM MaterialPurchases mp
        INNER JOIN Projects p ON mp.ProjectId = p.ProjectId
        WHERE p.UserId = @UserId
    ), 0)
  + ISNULL((
        SELECT SUM(wp.AmountPaid)
        FROM WagePayments wp
        INNER JOIN Projects p ON wp.ProjectId = p.ProjectId
        WHERE p.UserId = @UserId
    ), 0)";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@UserId", userId);
                object result = cmd.ExecuteScalar();
                return Convert.ToDecimal(result);
            }
        }

        /// <summary>
        /// Returns total expenses grouped by category
        /// </summary>
        public List<BudgetItem> GetExpensesByCategory(int? projectId = null, int? userId = null)
        {
            List<BudgetItem> list = new List<BudgetItem>();

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = @"
SELECT Category, SUM(Amount) AS Amount
FROM (
    SELECT e.Category, e.Amount
    FROM ExpenseItems e
    INNER JOIN Projects p ON e.ProjectId = p.ProjectId
    WHERE (@ProjectId IS NULL OR e.ProjectId = @ProjectId)
      AND (@UserId IS NULL OR p.UserId = @UserId)

    UNION ALL

    SELECT 'Material' AS Category, CAST(mp.TotalCost AS DECIMAL(18,2)) AS Amount
    FROM MaterialPurchases mp
    INNER JOIN Projects p ON mp.ProjectId = p.ProjectId
    WHERE (@ProjectId IS NULL OR mp.ProjectId = @ProjectId)
      AND (@UserId IS NULL OR p.UserId = @UserId)

    UNION ALL

    SELECT 'Labor' AS Category, CAST(wp.AmountPaid AS DECIMAL(18,2)) AS Amount
    FROM WagePayments wp
    INNER JOIN Projects p ON wp.ProjectId = p.ProjectId
    WHERE (@ProjectId IS NULL OR wp.ProjectId = @ProjectId)
      AND (@UserId IS NULL OR p.UserId = @UserId)
) spending
GROUP BY Category
ORDER BY Category";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@ProjectId", (object?)projectId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@UserId", (object?)userId ?? DBNull.Value);
                SqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    BudgetItem item = new BudgetItem();
                    item.Category = reader["Category"]?.ToString() ?? "";
                    item.Amount = Convert.ToDecimal(reader["Amount"]);
                    list.Add(item);
                }
            }

            return list;
        }
    }
}
