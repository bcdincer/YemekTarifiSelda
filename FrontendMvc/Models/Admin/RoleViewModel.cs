namespace FrontendMvc.Models.Admin;

public class RoleViewModel
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int UserCount { get; set; }
}

public class AssignRoleViewModel
{
    public string UserId { get; set; } = string.Empty;
    public string UserEmail { get; set; } = string.Empty;
    public string UserFullName { get; set; } = string.Empty;
    public List<string> AvailableRoles { get; set; } = new();
    public List<string> UserRoles { get; set; } = new();
}

