using System;
using System.Collections.Generic;
using System.Text;

namespace EAM.Application.Interfaces.Infrastructures
{
    public interface ISingpassOidcService
    {
        /// <summary>Returns the Singpass authorization URL the browser should redirect to.</summary>
        Task<SingpassAuthorizationStart> BuildAuthorizationUrlAsync(CancellationToken ct = default);

        /// <summary>
        /// Exchanges an authorization <paramref name="code"/> at the token endpoint and returns
        /// the authenticated user's NRIC (the <c>sub</c> claim of the id_token).
        /// </summary>
        Task<string> ExchangeCodeForSubjectAsync(
            string code,
            string state,
            string sessionId,
            CancellationToken ct = default);

        /// <summary>Returns the RP public signing and encryption keys as a JSON Web Key Set string.</summary>
        string GetPublicKeyJwks();
    }

    public sealed record SingpassAuthorizationStart(string Url, string SessionId);
}
