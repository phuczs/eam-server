using System;
using System.Collections.Generic;
using System.Text;

namespace EAM.Application.DTOs.Auth.Request
{
    /// <summary>Payload the frontend sends after receiving the Singpass authorization code.</summary>
    public record SingpassCallbackRequest(string Code, string State);
}
