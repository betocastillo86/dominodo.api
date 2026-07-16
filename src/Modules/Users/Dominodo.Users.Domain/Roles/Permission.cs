namespace Dominodo.Users.Domain.Roles;

// Global seed catalog keyed by int (domain-model §1.3). Reference data, not a Guid-identified
// aggregate, so it does not derive from the kernel's Guid-based Entity base.
public sealed class Permission
{
    private Permission() { } // EF Core

    public Permission(int id, string code, string description, string group)
    {
        Id = id;
        Code = code;
        Description = description;
        Group = group;
    }

    public int Id { get; private set; }
    public string Code { get; private set; } = null!;
    public string Description { get; private set; } = null!;
    public string Group { get; private set; } = null!;
}
