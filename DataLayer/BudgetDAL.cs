using Microsoft.Data.SqlClient;
using BuildWise.Models;

namespace BuildWise.DataLayer
{
    /// <summary>
    /// Data Access Layer for BudgetItems table
    /// </summary>
    public class BudgetDAL
    {
        private string connectionString;

        public BudgetDAL(string connectionString)
        {
            this.connectionString = connectionString;
        }

        public List<BudgetItem> GetAll()
        {
            List<BudgetItem> list = new List<BudgetItem>();

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = "SELECT BudgetId, Category, Amount, CreatedAt, UpdatedAt FROM BudgetItems ORDER BY Category";
                SqlCommand cmd = new SqlCommand(query, conn);
                SqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    BudgetItem item = new BudgetItem();
                    item.BudgetId = Convert.ToInt32(reader["BudgetId"]);
                    item.Category = reader["Category"].ToString();
                    item.Amount = Convert.ToDecimal(reader["Amount"]);
                    item.CreatedAt = Convert.ToDateTime(reader["CreatedAt"]);
                    item.UpdatedAt = Convert.ToDateTime(reader["UpdatedAt"]);
                    list.Add(item);
                }
            }

            return list;
        }

        public BudgetItem GetById(int id)
        {
            BudgetItem item = null;

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = "SELECT BudgetId, Category, Amount, CreatedAt, UpdatedAt FROM BudgetItems WHERE BudgetId = @Id";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Id", id);
                SqlDataReader reader = cmd.ExecuteReader();

                if (reader.Read())
                {
                    item = new BudgetItem();
                    item.BudgetId = Convert.ToInt32(reader["BudgetId"]);
                    item.Category = reader["Category"].ToString();
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
                string query = "INSERT INTO BudgetItems (Category, Amount) VALUES (@Category, @Amount)";
                SqlCommand cmd = new SqlCommand(query, conn);
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

        public decimal GetTotalBudget()
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = "SELECT ISNULL(SUM(Amount), 0) FROM BudgetItems";
                SqlCommand cmd = new SqlCommand(query, conn);
                object result = cmd.ExecuteScalar();
                return Convert.ToDecimal(result);
            }
        }
    }
}
