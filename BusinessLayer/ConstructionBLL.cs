using BuildWise.Models;
using BuildWise.DataLayer;

namespace BuildWise.BusinessLayer
{
    /// <summary>
    /// Business Logic Layer for Construction phases and task management
    /// </summary>
    public class ConstructionBLL
    {
        private PhaseDAL phaseDal;
        private TaskDAL taskDal;

        public ConstructionBLL(string connectionString)
        {
            phaseDal = new PhaseDAL(connectionString);
            taskDal = new TaskDAL(connectionString);
        }

        public List<ConstructionPhase> GetFullProjectStructure(int? projectId = null, int? userId = null)
        {
            List<ConstructionPhase> phases = phaseDal.GetAll(projectId, userId);
            foreach (var phase in phases)
            {
                phase.Tasks = taskDal.GetByPhaseId(phase.PhaseId);
                phase.Progress = CalculatePhaseProgress(phase.Tasks);
            }
            return phases;
        }

        public decimal CalculateOverallProgress(int? projectId = null, int? userId = null)
        {
            List<ConstructionPhase> phases = GetFullProjectStructure(projectId, userId);
            decimal overall = 0;
            decimal totalWeight = 0;

            foreach (var phase in phases)
            {
                overall += (phase.Weight * phase.Progress) / 100;
                totalWeight += phase.Weight;
            }

            // Normalize to 100 if weights don't perfectly add up, or just return as is
            return Math.Round(overall, 2);
        }

        private decimal CalculatePhaseProgress(List<PhaseTask> tasks)
        {
            if (tasks == null || tasks.Count == 0) return 0;

            decimal progress = 0;
            foreach (var task in tasks)
            {
                if (task.Status == "Completed")
                {
                    progress += task.Weight;
                }
            }
            return Math.Round(progress, 2);
        }

        // Phase CRUD
        public bool AddPhase(ConstructionPhase phase) => phaseDal.Add(phase);
        public bool UpdatePhase(ConstructionPhase phase) => phaseDal.Update(phase);
        public bool DeletePhase(int id) => phaseDal.Delete(id);
        public bool PhaseBelongsToUser(int phaseId, int userId) => phaseDal.BelongsToUser(phaseId, userId);

        // Task CRUD
        public bool AddTask(PhaseTask task) => taskDal.Add(task);
        public bool UpdateTask(PhaseTask task) => taskDal.Update(task);
        public bool DeleteTask(int id) => taskDal.Delete(id);
        public bool TaskBelongsToUser(int taskId, int userId) => taskDal.BelongsToUser(taskId, userId);
    }
}
