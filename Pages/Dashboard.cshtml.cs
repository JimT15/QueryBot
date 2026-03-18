using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace QueryBot.Pages;

[Authorize]
public sealed class DashboardModel : PageModel
{
}
