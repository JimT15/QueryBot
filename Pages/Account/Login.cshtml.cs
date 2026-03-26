using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using QueryBot.Auth;

namespace QueryBot.Pages.Account;

[AllowAnonymous]
public sealed class LoginModel : PageModel
{
    private readonly QueryBotAuthService _authService;

    public LoginModel(QueryBotAuthService authService)
    {
        _authService = authService;
    }

    [BindProperty]
    public LoginInput Input { get; set; } = new();

    public string? ErrorMessage { get; set; }

    public string? ReturnUrl { get; set; }

    public void OnGet(string? returnUrl = null)
    {
        ReturnUrl = returnUrl;
    }

    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        returnUrl ??= Url.Content("~/Dashboard");

        if (!ModelState.IsValid)
            return Page();

        var result = await _authService.AuthenticateAsync(Input.Email, Input.Password);

        if (result is null)
        {
            ErrorMessage = "Invalid login attempt.";
            return Page();
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, result.Value.Nickname),
            new(ClaimTypes.Email, result.Value.Email),
        };

        if (result.Value.ClientId.HasValue)
            claims.Add(new Claim("ClientId", result.Value.ClientId.Value.ToString()));

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity));

        return LocalRedirect(returnUrl);
    }

    public sealed class LoginInput
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;
    }
}
