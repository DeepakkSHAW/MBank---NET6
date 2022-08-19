
using Microsoft.Azure.Cosmos.Table;
using Newtonsoft.Json.Linq;
using System.Runtime.Serialization;

namespace MBank.Data
{
    public enum Banks
    {
        [EnumMember(Value = "882-618")]
        MBankBSB,
        [EnumMember(Value = "991-001")]
        CBankBSB,
        [EnumMember(Value = "100-001")]
        DBankBSB
    }
    public class AccountEntity : TableEntity
    {
        public AccountEntity() { }
        public AccountEntity(string BSB, Guid AccountKey)
        {
            PartitionKey = BSB;
            RowKey = AccountKey.ToString();
        }
        public string AccountNumber { get; set; }
        public string AccountHolderName { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }

    }
}