using BuildWise.Models;
using BuildWise.DataLayer;

namespace BuildWise.BusinessLayer
{
    /// <summary>
    /// Business Logic Layer for Transaction Log viewing and filtering
    /// </summary>
    public class TransactionBLL
    {
        private TransactionDAL dal;

        public TransactionBLL(string connectionString)
        {
            dal = new TransactionDAL(connectionString);
        }

        public List<TransactionLog> GetAllTransactions(int? projectId = null)
        {
            return dal.GetAll(projectId);
        }

        public List<TransactionLog> GetFilteredTransactions(string category, string type, DateTime? fromDate, DateTime? toDate, int? projectId = null, int? userId = null)
        {
            return dal.GetFiltered(category, type, fromDate, toDate, projectId, userId);
        }

        public int GetTotalTransactionsCount(int? projectId = null, int? userId = null)
        {
            return dal.GetTotalCount(projectId, userId);
        }

        public decimal GetTotalTransactionAmount(int? projectId = null, int? userId = null)
        {
            return dal.GetTotalAmount(projectId, userId);
        }
    }
}
