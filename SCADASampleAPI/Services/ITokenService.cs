using System.Security.Claims;
using SCADASampleAPI.Models;

namespace SCADASampleAPI.Services;

public interface ITokenService
{
    string CreateToken(ApplicationUser user, IEnumerable<string> roles);
}
