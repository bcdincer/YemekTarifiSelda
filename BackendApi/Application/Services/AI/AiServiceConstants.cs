namespace BackendApi.Application.Services.AI;

/// <summary>
/// AI servis sabitleri
/// </summary>
public static class AiServiceConstants
{
    public const int MinRequestIntervalSeconds = 2;
    public const int MaxRetries = 3;
    public const int DefaultRetryAfterSeconds = 5;
    public const string UserAgent = "RecipeSite/1.0";
}

