namespace BackendApi.Domain.Interfaces;

/// <summary>
/// Base interface for external service integrations
/// </summary>
public interface IExternalService
{
    /// <summary>
    /// Service name/identifier
    /// </summary>
    string ServiceName { get; }

    /// <summary>
    /// Checks if the service is available/healthy
    /// </summary>
    Task<bool> IsAvailableAsync();

    /// <summary>
    /// Gets service status/health information
    /// </summary>
    Task<ServiceHealthStatus> GetHealthStatusAsync();
}

public class ServiceHealthStatus
{
    public bool IsHealthy { get; set; }
    public string? Message { get; set; }
    public DateTime CheckedAt { get; set; } = DateTime.UtcNow;
}

