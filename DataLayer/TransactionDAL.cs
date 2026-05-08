using Microsoft.Data.SqlClient;
using BuildWise.Models;

namespace BuildWise.DataLayer
{
    /// <summary>
    /// Data Access Layer for TransactionLogs table
    /// </summary>
    public class TransactionDAL
    {
        private string connectionString;

        public TransactionDAL(string connectionString)
        {
            this.connectionString = connectionString;
        }

        public List<TransactionLog> GetAll()
        {
            List<TransactionLog> list = new List<TransactionLog>();

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = "SELECT TransactionId, TransactionDate, TransactionType, Category, Description, Amount, BudgetEffect FROM TransactionLogs ORDER BY TransactionDate DESC";
                SqlCommand cmd = new SqlCommand(query, conn);
                SqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    TransactionLog item = new TransactionLog();
                    item.TransactionId = Convert.ToInt32(reader["TransactionId"]);
                    item.TransactionDate = Convert.ToDateTime(reader["TransactionDate"]);
                    item.TransactionType = reader["TransactionType"].ToString();
                    item.Category = reader["Category"].ToString();
                    item.Description = reader["Description"] != DBNull.Value ? reader["Description"].ToString() : "";
                    item.Amount = Convert.ToDecimal(reader["Amount"]);
                    item.BudgetEffect = reader["BudgetEffect"] != DBNull.Value ? Convert.ToDecimal(reader["BudgetEffect"]) : 0;
                    list.Add(item);
                }
            }

            return list;
        }

        public bool Add(TransactionLog item)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = "INSERT INTO TransactionLogs (TransactionType, Category, Description, Amount, BudgetEffect) VALUES (@Type, @Category, @Description, @Amount, @BudgetEffect)";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Type", item.TransactionType);
                cmd.Parameters.AddWithValue("@Category", item.Category);
                cmd.Parameters.AddWithValue("@Description", (object)item.Description ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Amount", item.Amount);
                cmd.Parameters.AddWithValue("@BudgetEffect", item.BudgetEffect);
                return cmd.ExecuteNonQuery() > 0;
            }
        }

        /// <summary>
        /// Filter transactions by date range, category, and type
        /// </summary>
        public List<TransactionLog> GetFiltered(string category, string type, DateTime? fromDate, DateTime? toDate)
        {
            List<TransactionLog> list = new List<TransactionLog>();

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = "SELECT TransactionId, TransactionDate, TransactionType, Category, Description, Amount, BudgetEffect FROM TransactionLogs WHERE 1=1";

                if (!string.IsNullOrEmpty(category))
                    query += " AND Category = @Category";
                if (!string.IsNullOrEmpty(type))
                    query += " AND TransactionType = @Type";
                if (fromDate != null)
                    query += " AND TransactionDate >= @FromDate";
                if (toDate != null)
                    query += " AND TransactionDate <= @ToDate";

                query += " ORDER BY TransactionDate DESC";

                SqlCommand cmd = new SqlCommand(query, conn);

                if (!string.IsNullOrEmpty(category))
                    cmd.Parameters.AddWithValue("@Category", category);
                if (!string.IsNullOrEmpty(type))
                    cmd.Parameters.AddWithValue("@Type", type);
                if (fromDate != null)
                    cmd.Parameters.AddWithValue("@FromDate", fromDate.Value);
                if (toDate != null)
                    cmd.Parameters.AddWithValue("@ToDate", toDate.Value);

                SqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    TransactionLog item = new TransactionLog();
                    item.TransactionId = Convert.ToInt32(reader["TransactionId"]);
                    item.TransactionDate = Convert.ToDateTime(reader["TransactionDate"]);
                    item.TransactionType = reader["TransactionType"].ToString();
                    item.Category = reader["Category"].ToString();
                    item.Description = reader["Description"] != DBNull.Value ? reader["Description"].ToString() : "";
                    item.Amount = Convert.ToDecimal(reader["Amount"]);
                    item.BudgetEffect = reader["BudgetEffect"] != DBNull.Value ? Convert.ToDecimal(reader["BudgetEffect"]) : 0;
                    list.Add(item);
                }
            }

            return list;
        }

        public int GetTotalCount()
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = "SELECT COUNT(*) FROM TransactionLogs";
                SqlCommand cmd = new SqlCommand(query, conn);
                return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }

        public decimal GetTotalAmount()
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = "SELECT ISNULL(SUM(Amount), 0) FROM TransactionLogs";
                SqlCommand cmd = new SqlCommand(query, conn);
                return Convert.ToDecimal(cmd.ExecuteScalar());
            }
        }
    }
}
