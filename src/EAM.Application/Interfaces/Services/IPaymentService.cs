using EAM.Application.Common;
using EAM.Application.DTOs.Payment.Response;
using System;
using System.Collections.Generic;
using System.Text;

namespace EAM.Application.Interfaces.Services
{
    public interface IPaymentService
    {
        Task<PagedResult<PaymentResponse>> GetPagedByUserIdAsync(Guid userId, PageRequest request);
    }
}
