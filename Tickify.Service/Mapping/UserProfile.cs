using AutoMapper;
using Tickify.Core.DTOs.Auth;
using Tickify.Core.DTOs.Users;
using Tickify.Core.Entities;

namespace Tickify.Service.Mapping;

public class UserProfile : Profile
{
    public UserProfile()
    {
        // PasswordHash is set by AuthService after hashing; Role is always User on registration.
        CreateMap<RegisterRequest, User>()
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.PasswordHash, o => o.Ignore())
            .ForMember(d => d.Role, o => o.Ignore())
            .ForMember(d => d.CreatedAt, o => o.Ignore())
            .ForMember(d => d.Orders, o => o.Ignore());

        // Token and ExpiresAt are filled in by ITokenService.
        CreateMap<User, AuthResponse>()
            .ForMember(d => d.UserId, o => o.MapFrom(s => s.Id))
            .ForMember(d => d.Token, o => o.Ignore())
            .ForMember(d => d.ExpiresAt, o => o.Ignore());

        CreateMap<User, UserResponse>();
    }
}
