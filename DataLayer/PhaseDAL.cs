using Microsoft.Data.SqlClient;
using BuildWise.Models;

namespace BuildWise.DataLayer
{
    // Reads and writes phases.
    public class PhaseDAL
    {
        private string connectionString;

        public PhaseDAL(string connectionString)
        {
            this.connectionString = connectionString;
            EnsureProjectColumn();
        }

        private void EnsureProjectColumn()
        {
            using SqlConnection conn = new SqlConnection(connectionString);
            conn.Open();
            string query = @"
IF COL_LENGTH('ConstructionPhases', 'ProjectId') IS NULL
BEGIN
    ALTER TABLE ConstructionPhases ADD ProjectId INT NULL;
END";
            using SqlCommand cmd = new SqlCommand(query, conn);
            cmd.ExecuteNonQuery();
        }

        public List<ConstructionPhase> GetAll(int? projectId = null, int? userId = null)
        {
            List<ConstructionPhase> list = new List<ConstructionPhase>();

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = @"
SELECT cp.PhaseId, cp.ProjectId, cp.PhaseName, cp.Weight, cp.SortOrder, cp.CreatedAt
FROM ConstructionPhases cp
INNER JOIN Projects p ON cp.ProjectId = p.ProjectId
WHERE (@ProjectId IS NULL OR cp.ProjectId = @ProjectId)
  AND (@UserId IS NULL OR p.UserId = @UserId)
ORDER BY cp.SortOrder";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@ProjectId", (object?)projectId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@UserId", (object?)userId ?? DBNull.Value);
                SqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    ConstructionPhase item = new ConstructionPhase();
                    item.PhaseId = Convert.ToInt32(reader["PhaseId"]);
                    item.ProjectId = reader["ProjectId"] != DBNull.Value ? Convert.ToInt32(reader["ProjectId"]) : null;
                    item.PhaseName = reader["PhaseName"]?.ToString() ?? "";
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
                string query = "INSERT INTO ConstructionPhases (ProjectId, PhaseName, Weight, SortOrder) VALUES (@ProjectId, @PhaseName, @Weight, @SortOrder)";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@ProjectId", (object?)item.ProjectId ?? DBNull.Value);
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
                // Related tasks are deleted with the phase.
                string query = "DELETE FROM ConstructionPhases WHERE PhaseId = @Id";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Id", id);
                return cmd.ExecuteNonQuery() > 0;
            }
        }

        public bool BelongsToUser(int phaseId, int userId)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = @"
SELECT COUNT(1)
FROM ConstructionPhases cp
INNER JOIN Projects p ON cp.ProjectId = p.ProjectId
WHERE cp.PhaseId = @PhaseId AND p.UserId = @UserId";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@PhaseId", phaseId);
                cmd.Parameters.AddWithValue("@UserId", userId);
                return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
            }
        }
    }
}
