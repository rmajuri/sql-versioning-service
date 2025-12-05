using System;

namespace SqlVersioningService.Models;

public class OrganizationMember
{
    public Guid OrganizationId { get; set; }


    public Guid UserId { get; set; }

    public MemberRole Role { get; set; } = MemberRole.Member;

    public Boolean IsDeleted { get; set; }


    public DateTimeOffset JoinedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public DateTimeOffset? DeletedAt { get; set; }

    public User? User { get; set; }

    public Organization? Organization { get; set; }
}
