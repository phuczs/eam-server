using AutoMapper;
using EAM.Application.DTOs.Audits;
using EAM.Application.DTOs.Users.Request;
using EAM.Application.DTOs.Users.Response;
using EAM.Application.Helpers;
using EAM.Domain.Entities;

namespace EAM.Application.Mappings;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        //User
        CreateMap<User, UserResponse>()
            .ForMember(dest => dest.OfficialId, opt => opt.MapFrom(src => StringExtensions.MaskNric(src.OfficialId)));
        CreateMap<CreateUserRequest, User>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(_ => DateTime.UtcNow))
            .ForMember(dest => dest.CreatedAccounts, opt => opt.Ignore())
            .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));
        CreateMap<User, UserDetailResponse>()
            .ForMember(dest => dest.BankAccountNumber, opt => opt.MapFrom(src =>
            src.BankAccounts.FirstOrDefault() != null
            ? src.BankAccounts.FirstOrDefault().EncryptedAccountNumber.MaskBankAccount()
            : null));

        // E-Service Portal: self-view profile.
        // OfficialId (NRIC) is returned unmasked — the user is viewing their own
        // record and already knows their own ID number. Masking is only applied
        // on admin-facing endpoints (UserResponse) where bulk exposure is a risk.
        CreateMap<User, UserProfileResponse>();
        //Audit
        CreateMap<AuditLog, AuditLogResponse>();
    }
}

