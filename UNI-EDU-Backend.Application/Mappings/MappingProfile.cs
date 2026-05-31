using AutoMapper;
using UNI_EDU_Backend.Application.DTOs.Request.Authentication;
using UNI_EDU_Backend.Domain.Enums;
using UNI_EDU_Backend.Domain.Models;

namespace UNI_EDU_Backend.Application.Mappings
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            // Bỏ qua Password vì chúng ta sẽ hash mật khẩu ở tầng Service chứ không map trực tiếp
            CreateMap<TutorRegister, User>()
                .ForMember(dest => dest.HashedPassword, opt => opt.Ignore())
                .ForMember(dest => dest.Role, opt => opt.MapFrom(src => UserRole.Tutor))
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow));

            CreateMap<StudentRegister, User>()
                .ForMember(dest => dest.HashedPassword, opt => opt.Ignore())
                .ForMember(dest => dest.Role, opt => opt.MapFrom(src => UserRole.Student))
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow));

            CreateMap<ParentRegister, User>()
                .ForMember(dest => dest.HashedPassword, opt => opt.Ignore())
                .ForMember(dest => dest.Role, opt => opt.MapFrom(src => UserRole.Parent))
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow));

            // Mapping data riêng biệt vào các Role Entities
            CreateMap<TutorRegister, Tutor>()
                .ForMember(dest => dest.AverageRating, opt => opt.MapFrom(src => 0));

            CreateMap<StudentRegister, Student>();
            CreateMap<ParentRegister, Parent>();
        }
    }
}