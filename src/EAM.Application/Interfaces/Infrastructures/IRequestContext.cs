namespace EAM.Application.Interfaces.Infrastructures;

/// <summary>Exposes per-request transport details (e.g. client IP) for auditing.</summary>
public interface IRequestContext
{
    string? SourceIp { get; }
}
