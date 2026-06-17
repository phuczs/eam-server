using System;
using System.Collections.Generic;
using System.Text;

namespace EAM.Application.DTOs.Auth.Request
{
    public record AzureAdCallbackRequest(string Code, string State);

}
