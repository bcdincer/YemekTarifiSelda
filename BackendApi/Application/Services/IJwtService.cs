namespace BackendApi.Application.Services;

public interface IJwtService
{
    string GenerateToken(string userId, string userName, string email);
}

