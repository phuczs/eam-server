using EAM.Application.Common;

namespace EAM.Application.Interfaces.Infrastructures;

/// <summary>Resolves the authenticated user for the current request.</summary>
public interface ICurrentUserAccessor
{
    CurrentUser? Current { get; }
    CurrentUser Require();
}
