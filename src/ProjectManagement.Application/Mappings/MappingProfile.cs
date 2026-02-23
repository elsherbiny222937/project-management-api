using AutoMapper;
using ProjectManagement.Application.DTOs;
using ProjectManagement.Domain.Entities;

namespace ProjectManagement.Application.Mappings;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<Project, ProjectDto>()
            .ForMember(d => d.OwnerName, o => o.MapFrom(s => s.Owner != null ? s.Owner.FullName : null))
            .ForMember(d => d.TaskCount, o => o.MapFrom(s => s.Tasks.Count))
            .ForMember(d => d.MemberCount, o => o.MapFrom(s => s.Members.Count))
            .ForMember(d => d.Members, o => o.MapFrom(s => s.Members));

        CreateMap<ProjectMember, ProjectMemberDto>()
            .ForMember(d => d.UserName, o => o.MapFrom(s => s.User.UserName))
            .ForMember(d => d.FullName, o => o.MapFrom(s => s.User.FullName));

        CreateMap<ProjectTask, TaskDto>()
            .ForMember(d => d.EpicTitle, o => o.MapFrom(s => s.Epic != null ? s.Epic.Title : null))
            .ForMember(d => d.SprintTitle, o => o.MapFrom(s => s.Sprint != null ? s.Sprint.Title : null))
            .ForMember(d => d.AssignedToName, o => o.MapFrom(s => s.AssignedTo != null ? s.AssignedTo.FullName : null))
            .ForMember(d => d.RequestedByName, o => o.MapFrom(s => s.RequestedBy != null ? s.RequestedBy.FullName : null))
            .ForMember(d => d.Tags, o => o.MapFrom(s => s.Tags.Select(t => t.Name).ToList()))
            .ForMember(d => d.BlockedByTaskIds, o => o.MapFrom(s => s.BlockedBy.Select(b => b.Id)))
            .ForMember(d => d.BlocksTaskIds, o => o.MapFrom(s => s.Blocks.Select(b => b.Id)));

        CreateMap<Epic, EpicDto>()
            .ForMember(d => d.OwnerName, o => o.MapFrom(s => s.Owner != null ? s.Owner.FullName : null))
            .ForMember(d => d.Tags, o => o.MapFrom(s => s.Tags.Select(t => t.Name).ToList()));

        CreateMap<Sprint, SprintDto>();

        CreateMap<SubTask, SubTaskDto>();

        CreateMap<ApplicationUser, UserDto>();
        
        CreateMap<Domain.Entities.TaskStatus, TaskStatusDto>();
        
        CreateMap<Comment, CommentDto>()
            .ForMember(d => d.UserName, o => o.MapFrom(s => s.User.FullName));
    }
}
