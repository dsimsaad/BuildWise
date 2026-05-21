using Microsoft.Data.SqlClient;
using BuildWise.Models;

namespace BuildWise.DataLayer
{
    /// <summary>
    /// Data Access Layer for PhaseTasks table
    /// </summary>
    public class TaskDAL
    {
        private string connectionString;

        public TaskDAL(string connectionString)
        {
            this.connectionString = connectionString;
        }

        public List<PhaseTask> GetByPhaseId(int phaseId)
        {
            List<PhaseTask> list = new List<PhaseTask>();

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = "SELECT TaskId, PhaseId, TaskName, Status, StartDate, EndDate, Weight, CreatedAt FROM PhaseTasks WHERE PhaseId = @PhaseId ORDER BY CreatedAt";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@PhaseId", phaseId);
                SqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    PhaseTask item = new PhaseTask();
                    item.TaskId = Convert.ToInt32(reader["TaskId"]);
                    item.PhaseId = Convert.ToInt32(reader["PhaseId"]);
                    item.TaskName = reader["TaskName"]?.ToString() ?? "";
                    item.Status = reader["Status"]?.ToString() ?? "";
                    item.StartDate = reader["StartDate"] != DBNull.Value ? Convert.ToDateTime(reader["StartDate"]) : null;
                    item.EndDate = reader["EndDate"] != DBNull.Value ? Convert.ToDateTime(reader["EndDate"]) : null;
                    item.Weight = Convert.ToDecimal(reader["Weight"]);
                    item.CreatedAt = Convert.ToDateTime(reader["CreatedAt"]);
                    list.Add(item);
                }
            }

            return list;
        }

        public List<PhaseTask> GetAll(int? userId = null)
        {
            List<PhaseTask> list = new List<PhaseTask>();

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = @"
SELECT t.TaskId, t.PhaseId, t.TaskName, t.Status, t.StartDate, t.EndDate, t.Weight, t.CreatedAt 
FROM PhaseTasks t
INNER JOIN ConstructionPhases cp ON t.PhaseId = cp.PhaseId
INNER JOIN Projects p ON cp.ProjectId = p.ProjectId
WHERE (@UserId IS NULL OR p.UserId = @UserId)
ORDER BY t.PhaseId, t.CreatedAt";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@UserId", (object?)userId ?? DBNull.Value);
                SqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    PhaseTask item = new PhaseTask();
                    item.TaskId = Convert.ToInt32(reader["TaskId"]);
                    item.PhaseId = Convert.ToInt32(reader["PhaseId"]);
                    item.TaskName = reader["TaskName"]?.ToString() ?? "";
                    item.Status = reader["Status"]?.ToString() ?? "";
                    item.StartDate = reader["StartDate"] != DBNull.Value ? Convert.ToDateTime(reader["StartDate"]) : null;
                    item.EndDate = reader["EndDate"] != DBNull.Value ? Convert.ToDateTime(reader["EndDate"]) : null;
                    item.Weight = Convert.ToDecimal(reader["Weight"]);
                    item.CreatedAt = Convert.ToDateTime(reader["CreatedAt"]);
                    list.Add(item);
                }
            }

            return list;
        }

        public bool Add(PhaseTask item)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = "INSERT INTO PhaseTasks (PhaseId, TaskName, Status, StartDate, EndDate, Weight) VALUES (@PhaseId, @TaskName, @Status, @StartDate, @EndDate, @Weight)";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@PhaseId", item.PhaseId);
                cmd.Parameters.AddWithValue("@TaskName", item.TaskName);
                cmd.Parameters.AddWithValue("@Status", item.Status);
                cmd.Parameters.AddWithValue("@StartDate", (object?)item.StartDate ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@EndDate", (object?)item.EndDate ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Weight", item.Weight);
                return cmd.ExecuteNonQuery() > 0;
            }
        }

        public bool Update(PhaseTask item)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = "UPDATE PhaseTasks SET TaskName = @TaskName, Status = @Status, StartDate = @StartDate, EndDate = @EndDate, Weight = @Weight WHERE TaskId = @Id";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Id", item.TaskId);
                cmd.Parameters.AddWithValue("@TaskName", item.TaskName);
                cmd.Parameters.AddWithValue("@Status", item.Status);
                cmd.Parameters.AddWithValue("@StartDate", (object?)item.StartDate ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@EndDate", (object?)item.EndDate ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Weight", item.Weight);
                return cmd.ExecuteNonQuery() > 0;
            }
        }

        public bool Delete(int id)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = "DELETE FROM PhaseTasks WHERE TaskId = @Id";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Id", id);
                return cmd.ExecuteNonQuery() > 0;
            }
        }

        public bool BelongsToUser(int taskId, int userId)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = @"
SELECT COUNT(1)
FROM PhaseTasks t
INNER JOIN ConstructionPhases cp ON t.PhaseId = cp.PhaseId
INNER JOIN Projects p ON cp.ProjectId = p.ProjectId
WHERE t.TaskId = @TaskId AND p.UserId = @UserId";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@TaskId", taskId);
                cmd.Parameters.AddWithValue("@UserId", userId);
                return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
            }
        }
    }
}
