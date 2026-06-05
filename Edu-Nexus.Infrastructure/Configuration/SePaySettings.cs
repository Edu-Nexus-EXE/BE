using Edu_Nexus.Application.Interfaces.Configuration;
using Microsoft.Extensions.Configuration;

namespace Edu_Nexus.Infrastructure.Configuration;

public class SePaySettings : ISePaySettings
{
    public string ApiKey { get; }
    public string BankAccount { get; }
    public string BankCode { get; }
    public string AccountName { get; }

    public SePaySettings(IConfiguration config)
    {
        ApiKey = config["SePay:ApiKey"] ?? "";
        BankAccount = config["SePay:BankAccount"] ?? "";
        BankCode = config["SePay:BankCode"] ?? "";
        AccountName = config["SePay:AccountName"] ?? "";
    }
}
