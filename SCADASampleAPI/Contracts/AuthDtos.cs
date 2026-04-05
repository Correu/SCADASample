using System.ComponentModel.DataAnnotations;

namespace SCADASampleAPI.Contracts;

public class LoginRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = "";

    [Required]
    public string Password { get; set; } = "";
}

public class LoginResponse
{
    public string Token { get; set; } = "";
    public string Email { get; set; } = "";
    public IReadOnlyList<string> Roles { get; set; } = Array.Empty<string>();
}

public class RegisterUserRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = "";

    [Required]
    [MinLength(8)]
    public string Password { get; set; } = "";

    [Required]
    public string Role { get; set; } = "Viewer";
}

public class UserListItemDto
{
    public string Id { get; set; } = "";
    public string? Email { get; set; }
    public string? UserName { get; set; }
    public IReadOnlyList<string> Roles { get; set; } = Array.Empty<string>();
}

public class UpdateUserRolesRequest
{
    [Required]
    public IReadOnlyList<string> Roles { get; set; } = Array.Empty<string>();
}
