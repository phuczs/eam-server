using System;
using System.Collections.Generic;
using System.Text;

namespace EAM.Application.Interfaces.Infrastructures
{
    public interface IAzureAdOidcService
    {
        /// <summary>Creates a backend-owned AAD login transaction and returns the authorization URL.</summary>
        Task<AzureAdAuthorizationStart> BuildAuthorizationUrlAsync(CancellationToken ct = default);

        /// <summary>Exchanges an authorization code and returns the stable Azure AD subject for account mapping.</summary>
        Task<string> ExchangeCodeForSubjectAsync(
            string code,
            string state,
            string sessionId,
            CancellationToken ct = default);
    }

    public sealed record AzureAdAuthorizationStart(string Url, string SessionId);

}
