using Microsoft.Data.SqlClient;
using BuildWise.Models;

namespace BuildWise.DataLayer
{
    /// <summary>
    /// Data Access Layer for ConstructionPhases table
    /// </summary>
    public class PhaseDAL
    {
        private string connectionString;

        public PhaseDAL(string connectionString)
        {
            this.connectionString = connectionString;
        }

        public List<ConstructionPhase> GetAll()
        {
            List<ConstructionPhase> list = new List<ConstructionPhase>();

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = "SELECT PhaseId, PhaseName, Weight, SortOrder, CreatedAt FROM ConstructionPhases ORDER BY SortOrder";
                SqlCommand cmd = new SqlCommand(query, conn);
                SqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    ConstructionPhase item = new ConstructionPhase();
                    item.PhaseId = Convert.ToInt32(reader["PhaseId"]);
                    item.PhaseName = reader["PhaseName"].ToString();
                    item.Weight = Convert.ToDecimal(reader["Weight"]);
                    item.SortOrder = Convert.ToInt32(reader["SortOrder"]);
                    item.CreatedAt = Convert.ToDateTime(reader["CreatedAt"]);
                    list.Add(item);
                }
            }

            return list;
        }

        public bool Add(ConstructionPhase item)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = "INSERT INTO ConstructionPhases (PhaseName, Weight, SortOrder) VALUES (@PhaseName, @Weight, @SortOrder)";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@PhaseName", item.PhaseName);
                cmd.Parameters.AddWithValue("@Weight", item.Weight);
                cmd.Parameters.AddWithValue("@SortOrder", item.SortOrder);
                return cmd.ExecuteNonQuery() > 0;
            }
        }

        public bool Update(ConstructionPhase item)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = "UPDATE ConstructionPhases SET PhaseName = @PhaseName, Weight = @Weight, SortOrder = @SortOrder WHERE PhaseId = @Id";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Id", item.PhaseId);
                cmd.Parameters.AddWithValue("@PhaseName", item.PhaseName);
                cmd.Parameters.AddWithValue("@Weight", item.Weight);
                cmd.Parameters.AddWithValue("@SortOrder", item.SortOrder);
                return cmd.ExecuteNonQuery() > 0;
            }
        }

        public bool Delete(int id)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                // CASCADE delete will remove child tasks
                string query = "DELETE FROM ConstructionPhases WHERE PhaseId = @Id";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Id", id);
                return cmd.ExecuteNonQuery() > 0;
            }
        }
    }
}
