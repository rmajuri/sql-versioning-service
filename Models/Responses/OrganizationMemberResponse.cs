using System;

namespace SqlVersioningService.Models.Responses;

public class OrganizationMemberResponse
{
    public Guid OrganizationId { get; set; }


    public Guid UserId { get; set; }

    public MemberRole Role { get; set; } = MemberRole.Member;

    public DateTimeOffset JoinedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public User? User { get; set; }
}
