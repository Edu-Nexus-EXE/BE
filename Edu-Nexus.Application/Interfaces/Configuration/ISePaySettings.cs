namespace Edu_Nexus.Application.Interfaces.Configuration;

public interface ISePaySettings
{
    string ApiKey { get; }
    string BankAccount { get; }
    string BankCode { get; }
    string AccountName { get; }
}
