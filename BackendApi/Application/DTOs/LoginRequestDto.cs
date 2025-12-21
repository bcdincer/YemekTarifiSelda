namespace BackendApi.Application.DTOs;

public class LoginRequestDto
{
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string? Email { get; set; }
}

