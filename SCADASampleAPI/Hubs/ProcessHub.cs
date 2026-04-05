using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace SCADASampleAPI.Hubs;

[Authorize]
public class ProcessHub : Hub
{
}
