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
        }

        public List<ExpenseItem> GetAll()
        {
            List<ExpenseItem> list = new List<ExpenseItem>();

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = "SELECT ExpenseId, Category, Description, Amount, ExpenseDate, CreatedAt, UpdatedAt FROM ExpenseItems ORDER BY ExpenseDate DESC";
                SqlCommand cmd = new SqlCommand(query, conn);
                SqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    ExpenseItem item = new ExpenseItem();
                    item.ExpenseId = Convert.ToInt32(reader["ExpenseId"]);
                    item.Category = reader["Category"].ToString();
                    item.Description = reader["Description"] != DBNull.Value ? reader["Description"].ToString() : "";
                    item.Amount = Convert.ToDecimal(reader["Amount"]);
                    item.ExpenseDate = Convert.ToDateTime(reader["ExpenseDate"]);
                    item.CreatedAt = Convert.ToDateTime(reader["CreatedAt"]);
                    item.UpdatedAt = Convert.ToDateTime(reader["UpdatedAt"]);
                    list.Add(item);
                }
            }

            return list;
        }

        public ExpenseItem GetById(int id)
        {
            ExpenseItem item = null;

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = "SELECT ExpenseId, Category, Description, Amount, ExpenseDate, CreatedAt, UpdatedAt FROM ExpenseItems WHERE ExpenseId = @Id";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Id", id);
                SqlDataReader reader = cmd.ExecuteReader();

                if (reader.Read())
                {
                    item = new ExpenseItem();
                    item.ExpenseId = Convert.ToInt32(reader["ExpenseId"]);
                    item.Category = reader["Category"].ToString();
                    item.Description = reader["Description"] != DBNull.Value ? reader["Description"].ToString() : "";
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
                string query = "INSERT INTO ExpenseItems (Category, Description, Amount, ExpenseDate) VALUES (@Category, @Description, @Amount, @ExpenseDate)";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Category", item.Category);
                cmd.Parameters.AddWithValue("@Description", (object)item.Description ?? DBNull.Value);
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
                cmd.Parameters.AddWithValue("@Description", (object)item.Description ?? DBNull.Value);
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

        public decimal GetTotalExpenses()
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = "SELECT ISNULL(SUM(Amount), 0) FROM ExpenseItems";
                SqlCommand cmd = new SqlCommand(query, conn);
                object result = cmd.ExecuteScalar();
                return Convert.ToDecimal(result);
            }
        }

        /// <summary>
        /// Returns total expenses grouped by category
        /// </summary>
        public List<BudgetItem> GetExpensesByCategory()
        {
            List<BudgetItem> list = new List<BudgetItem>();

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = "SELECT Category, SUM(Amount) AS Amount FROM ExpenseItems GROUP BY Category ORDER BY Category";
                SqlCommand cmd = new SqlCommand(query, conn);
                SqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    BudgetItem item = new BudgetItem();
                    item.Category = reader["Category"].ToString();
                    item.Amount = Convert.ToDecimal(reader["Amount"]);
                    list.Add(item);
                }
            }

            return list;
        }
    }
}
