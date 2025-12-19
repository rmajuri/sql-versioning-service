namespace SqlVersioningService.Services;

public interface IHashingService
{
    string ComputeHash(string input);
}
