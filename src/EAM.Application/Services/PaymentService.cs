using AutoMapper;
using EAM.Application.Common;
using EAM.Application.DTOs.Payment.Response;
using EAM.Application.Interfaces.Repositories;
using EAM.Application.Interfaces.Services;
using System;
using System.Collections.Generic;
using System.Text;

namespace EAM.Application.Services
{
    public class PaymentService : IPaymentService
    {
        private readonly IPaymentRepository _paymentRepository;
        private readonly IMapper _mapper;

        public PaymentService(IPaymentRepository paymentRepository, IMapper mapper)
        {
            _paymentRepository = paymentRepository;
            _mapper = mapper;
        }

        public async Task<PagedResult<PaymentResponse>> GetPagedByUserIdAsync(Guid userId, PageRequest request)
        {
            var (items, total) = await _paymentRepository.GetPagedByUserIdAsync(userId, request.Skip, request.Size);

            return new PagedResult<PaymentResponse>
            {
                Items = _mapper.Map<IReadOnlyList<PaymentResponse>>(items),
                Total = total,
                Page = request.Page,
                Size = request.Size
            };
        }
    }
}
