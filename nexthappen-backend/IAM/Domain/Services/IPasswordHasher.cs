namespace nexthappen_backend.IAM.Domain.Services;

public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string hash);
}
