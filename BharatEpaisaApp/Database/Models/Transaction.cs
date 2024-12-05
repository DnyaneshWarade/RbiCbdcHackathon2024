using SQLite;

namespace BharatEpaisaApp.Database.Models
{
    public class Transaction
    {
        [PrimaryKey]
        public string ReqId { get; set; }
        public string Desc { get; set; }
        public double Amount { get; set; }
        public string From { get; set; }
        public string To { get; set; }
        public string Status { get; set; }
        public string AmtColor { get; set; }  
        public bool IsAnonymous { get; set; }  

        public Transaction Clone() => MemberwiseClone() as Transaction;
    }
}
