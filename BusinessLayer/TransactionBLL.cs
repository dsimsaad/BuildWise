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

        public List<TransactionLog> GetAllTransactions()
        {
            return dal.GetAll();
        }

        public List<TransactionLog> GetFilteredTransactions(string category, string type, DateTime? fromDate, DateTime? toDate)
        {
            return dal.GetFiltered(category, type, fromDate, toDate);
        }

        public int GetTotalTransactionsCount()
        {
            return dal.GetTotalCount();
        }

        public decimal GetTotalTransactionAmount()
        {
            return dal.GetTotalAmount();
        }
    }
}
