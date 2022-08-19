using System;
using MBank.Data;
using MBank.Lib;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.ComponentModel.Design;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

// See https://aka.ms/new-console-template for more information

var builder = new ConfigurationBuilder();
builder.SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                    .AddEnvironmentVariables();

// Initiated the dependency injection container 
var host = Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                services.AddTransient<ApplicationInfo>();
                services.AddTransient<IAccountServices, AccountServices>();
                //services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer("blah-blah"));
            })
            .ConfigureLogging(logBuilder =>
            {
                logBuilder.ClearProviders();
                logBuilder.AddConsole();
            })
            //.UseSerilog()
            .Build();

#region Host Configuration Options
//Host Configuration Option-1
//---------------------------
//IHost host = Host.CreateDefaultBuilder(args)
//    .ConfigureServices((hostContext, services) =>
//    {
//        //services.AddHostedService<Worker>();
//        //services.SetBasePath(Directory.GetCurrentDirectory());
//        //services.ConfigureAppConfiguration("appsettings.json");
//        services.AddLogging();
//        services.AddTransient<IAccountServices, AccountServices>();

//    })
//.Build();

//Host Configuration Option-2
//---------------------------
//using var host = Host.CreateDefaultBuilder(args)
//.ConfigureHostConfiguration (builder =>
//{
//    // Use the test server and point to the application's startup
//    builder.SetBasePath(Directory.GetCurrentDirectory());
//    builder.AddJsonFile("SharedSettings.json", optional: true,reloadOnChange: true);
//    builder.AddEnvironmentVariables();
//    //builder.ConfigureLogging((ctx, log) => { /* elided for brevity */ });
//    //builder.AddJsonFile(e=> e.Path)
//})
//.ConfigureServices(services =>
//{
//    // Replace the service
//    services.AddSingleton<IAccountServices, AccountServices>();
//})
//.Build();

#endregion


var logger = host.Services.GetService<ILoggerFactory>().CreateLogger<Program>();
logger.LogInformation("************ Welcome to MBank ************");

Console.WriteLine("************ Welcome to MBank ************");

//Enum to Object conversion technique// 
var jsonBankBSB = JsonConvert.SerializeObject(Banks.MBankBSB, new StringEnumConverter());
var strBankBSB = JsonConvert.DeserializeObject<string>(jsonBankBSB);
var permissionType = JsonConvert.DeserializeObject<Banks>(jsonBankBSB, new StringEnumConverter());



var appInfo = host.Services.GetService<ApplicationInfo>();
logger.LogInformation($"Application Name:{appInfo.GetApplication()}");
logger.LogInformation($"Application version:{appInfo.GetApplicationVersion()}");
logger.LogInformation($"Application Designed By:{appInfo.GetDesignerInfo()}");



var accountSrv = host.Services.GetService<IAccountServices>();
//--Get accounts--//
Guid aGuid = new Guid("460995c3-869b-47fb-bbca-3b9201308776");
var getAccount = await accountSrv.GetAccountAsync(strBankBSB, aGuid);
logger.LogInformation($"Found Account for {getAccount.AccountHolderName}");

//--Get ALL accounts--//
var getAllAccount = await accountSrv.GetAllAccountsAsync(strBankBSB);
logger.LogInformation($"Total Accounts found {getAllAccount.Count()}");

//--Add Account--//
AccountEntity oNewAccount = new AccountEntity(strBankBSB, Guid.NewGuid())
{
    AccountHolderName = "Deepak Shaw",
    AccountNumber = "1234567890",
    PhoneNumber = "123-456-7890",
    Email = "deepak.shaw@gmail.com"
};

//--Add new Account--//
var newAccount = await accountSrv?.AddAccountAsync(oNewAccount);
if (newAccount)
{
    //If Account has been created successfully then delete it.
    var deleteAccount = await accountSrv.DeleteAccountAsync(strBankBSB, new Guid(oNewAccount.RowKey));
    if (deleteAccount) logger.LogInformation($"Account has been delete successfully");
}
//host.Run();