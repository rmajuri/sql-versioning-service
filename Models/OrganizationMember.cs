using System;

namespace SqlVersioningService.Models;

public class OrganizationMember
{
    public Guid OrganizationId { get; set; }

    public Guid UserId { get; set; }

    public MemberRole Role { get; set; } = MemberRole.Member;

    public DateTimeOffset JoinedAt { get; set; }

    public User? User { get; set; }
}
