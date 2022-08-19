using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.Azure.Documents;
using MBank.Data;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System.Collections.Specialized;
using System.Collections.Generic;

namespace MBank.Lib
{
    public interface IAccountServices
    {
        public Task<bool> AddAccountAsync(AccountEntity accountEntity);
        public Task<bool> UpdateAccountAsync(AccountEntity accountEntity);
        public Task<AccountEntity> GetAccountAsync(string bankBSBCode, Guid accountID);
        public Task<bool> DeleteAccountAsync(string bankBSBCode, Guid accountID);
        public Task<List<AccountEntity>> GetAllAccountsAsync(string bankBSBCode);

    }
    public class AccountServices : IAccountServices
    {
        //private readonly string _connection = "DefaultEndpointsProtocol=https;AccountName=storagepocfunction;AccountKey=t7LcEs0ZOBHPm/Eb2xqYfMTY6kwxMdlHCRFem7iGLFpJJcBkGp/r6xxjs/YUYqOelCbAcf3onSBYYIwV79A0yQ==;EndpointSuffix=core.windows.net";
        //private readonly string _table = "TMBankAccount";

        private readonly CloudTable _stroragetable;
        private readonly ILogger<AccountServices> _logger;
        private readonly IConfiguration _config;
        public AccountServices(ILogger<AccountServices> logger, IConfiguration config)
        {
            _logger = logger;
            _config = config;

            string connection = _config.GetSection("AzureStorage:TableConnectionString").Value;
            string table = _config.GetSection("AzureStorage:Table").Value;
            if (string.IsNullOrEmpty(connection) || string.IsNullOrEmpty(table)) throw new Exception("connection details was not found in configuration file");

            CloudStorageAccount storageAccount;
            storageAccount = CloudStorageAccount.Parse(connection);
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient(new TableClientConfiguration());
            _stroragetable = tableClient.GetTableReference(table);
        }
        public async Task<bool> AddAccountAsync(AccountEntity accountEntity)
        {
            _logger.LogInformation($"AddAccountAsync:: Started - {DateTime.UtcNow.ToLocalTime()}");
            bool operationStatus = false;
            try
            {
                //Check whether Account already Exist
                TableOperation toFindAccount = TableOperation.Retrieve<AccountEntity>(accountEntity.PartitionKey, accountEntity.RowKey);
                //TableOperation toFindAccount = TableOperation.Retrieve(account.PartitionKey, account.RowKey);
                TableResult trAccountExist = await _stroragetable.ExecuteAsync(toFindAccount);
                if (trAccountExist.Result == null)
                {
                    //TableOperation insertOrMergeOperation = TableOperation.InsertOrMerge(accountEntity);
                    TableOperation insertOperation = TableOperation.Insert(accountEntity);
                    TableResult result = await _stroragetable.ExecuteAsync(insertOperation);
                    if (result.Result != null)
                        operationStatus = true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error occurred {ex.Message}");
            }

            _logger.LogInformation($"AddAccountAsync:: Ended - {DateTime.UtcNow.ToLocalTime()}");
            return operationStatus;
        }
        public async Task<bool> UpdateAccountAsync(AccountEntity accountEntity)
        {
            _logger.LogInformation($"UpdateAccountAsync:: Started - {DateTime.UtcNow.ToLocalTime()}");
            bool operationStatus = false;
            try
            {
                //Check whether Account already Exist
                TableOperation toFindAccount = TableOperation.Retrieve<AccountEntity>(accountEntity.PartitionKey, accountEntity.RowKey);
                //TableOperation toFindAccount = TableOperation.Retrieve(account.PartitionKey, account.RowKey);
                TableResult trAccountExist = await _stroragetable.ExecuteAsync(toFindAccount);
                if (trAccountExist != null)
                {
                    //TableOperation insertOrMergeOperation = TableOperation.InsertOrMerge(accountEntity);
                    TableOperation mergeOperation = TableOperation.Merge(accountEntity);
                    TableResult result = await _stroragetable.ExecuteAsync(mergeOperation);
                    if (result.Result != null)
                        operationStatus = true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error occurred {ex.Message}");
            }

            _logger.LogInformation($"UpdateAccountAsync:: Ended - {DateTime.UtcNow.ToLocalTime()}");
            return operationStatus;
        }
        public async Task<bool> DeleteAccountAsync(string bankBSBCode, Guid accountID)
        {
            _logger.LogInformation($"DeleteAccountAsync:: Started - {DateTime.UtcNow.ToLocalTime()}");
            bool operationStatus = false;
            var accID = accountID.ToString();

            try
            {
                //Check whether Account already Exist
                TableOperation toFindAccount = TableOperation.Retrieve<AccountEntity>(bankBSBCode, accID);
                TableResult trAccountExist = await _stroragetable.ExecuteAsync(toFindAccount);
                if (trAccountExist != null)
                {
                    TableOperation deleteOperation = TableOperation.Delete((AccountEntity)trAccountExist.Result);
                    TableResult result = await _stroragetable.ExecuteAsync(deleteOperation);
                    if(result.Result != null) operationStatus = true;
                }
            }
            catch (StorageException ex)
            {
                _logger.LogError($"Error: {ex.Message}");
            }
            _logger.LogInformation($"DeleteAccountAsync:: Ended - {DateTime.UtcNow.ToLocalTime()}");
            return operationStatus;
        }

        public async Task<AccountEntity> GetAccountAsync(string bankBSBCode, Guid accountID)
        {
            _logger.LogInformation($"GetAccountAsync:: Started - {DateTime.UtcNow.ToLocalTime()}");
            AccountEntity vReturn = new();
            //bankBSBCode = "882-618";
            var accID = accountID.ToString(); //"460995c3-869b-47fb-bbca-3b9201308776";
            try
            {
                TableOperation retrieveOperation = TableOperation.Retrieve<AccountEntity>(bankBSBCode, accID);
                TableResult result = await _stroragetable.ExecuteAsync(retrieveOperation);
                vReturn = (AccountEntity)result?.Result;

                //if (result != null)
                //    _logger.LogWarning("account found");
                //if (result.RequestCharge.HasValue)
                //    _logger.LogWarning($"Request Charge of Retrieve Operation: {result.RequestCharge}");
            }
            catch (StorageException ex)
            {
                _logger.LogError($"Error: {ex.Message}");
            }
            //await Task.Delay(1000);
            _logger.LogInformation($"GetAccountAsync:: Ended - {DateTime.UtcNow.ToLocalTime()}");
            return vReturn;
        }

        public async Task<List<AccountEntity>> GetAllAccountsAsync(string bankBSBCode)
        {
            var accountReturn = new List<AccountEntity>();
            _logger.LogInformation($"GetAllAccountsAsync:: Started - {DateTime.UtcNow.ToLocalTime()}");
            try
            {
                TableQuery<AccountEntity> query = new TableQuery<AccountEntity>()
                        .Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, bankBSBCode));
                accountReturn = _stroragetable.ExecuteQuery(query).ToList();

                //await Task.Delay(100);
            }
            catch (StorageException ex)
            {
                _logger.LogError($"Error: {ex.Message}");
            }
            _logger.LogInformation($"GetAllAccountsAsync:: Started - {DateTime.UtcNow.ToLocalTime()}");
            return accountReturn;
        }


        //public async Task<string> AddAccountAsync_TEST()
        //{
        //    CloudStorageAccount storageAccount;
        //    storageAccount = CloudStorageAccount.Parse(_connection);

        //    CloudTableClient tableClient = storageAccount.CreateCloudTableClient(new TableClientConfiguration());
        //    CloudTable table = tableClient.GetTableReference(_table);

        //    CustomerEntity customer = new CustomerEntity("Harp", "Walter")
        //    {
        //        Email = "Walter@contoso.com",
        //        PhoneNumber = "425-555-0101"
        //    };

        //    TableOperation insertOrMergeOperation = TableOperation.InsertOrMerge(customer);
        //    TableResult result = await table.ExecuteAsync(insertOrMergeOperation);
        //    CustomerEntity insertedCustomer = result.Result as CustomerEntity;
        //    Console.WriteLine("Added user.");
        //    return "done";
        //}

        //public class CustomerEntity : TableEntity
        //{
        //    public CustomerEntity() { }
        //    public CustomerEntity(string lastName, string firstName)
        //    {
        //        PartitionKey = lastName;
        //        RowKey = firstName;
        //    }

        //    public string Email { get; set; }
        //    public string PhoneNumber { get; set; }
        //}
    }
}