using System.Security.Claims;
using BistroGo.Core.Models;

namespace BistroGo.Web.Cart;

public static class CartOwnerResolver
{
    private const string CookieName = "BistroGo.GuestToken";

    // Read-only: who owns the cart for this request? null = nobody yet.
    public static CartOwner? Find(HttpContext http)
    {
        var userId = http.User.FindFirstValue(ClaimTypes.NameIdentifier); // Gets the user ID from the program-assigned cookie
        if (http.User.Identity?.IsAuthenticated == true && userId != null) // If the user is authenticated and has a user ID, return a CartOwner for that user
            return CartOwner.ForUser(userId);

        var token = ReadGuestToken(http);
        return token == null ? null : CartOwner.ForGuest(token.Value);
    }

    // For writes (Add): a guest with no cookie gets a new one.
    public static CartOwner FindOrCreate(HttpContext http)
    {
        var existing = Find(http); // Returns an existing user from http cookie
        if (existing != null) 
            return existing; // If an existing user is found, return it

        var token = Guid.NewGuid(); // Generate a new GUID for the guest token

        // Applies the cookie to the response with the guest token and sets options for security and expiration
        http.Response.Cookies.Append(CookieName, token.ToString(), new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Lax,
            IsEssential = true,
            Expires = DateTimeOffset.UtcNow.AddDays(30)
        });
        return CartOwner.ForGuest(token);
    }

    public static Guid? ReadGuestToken(HttpContext http) =>
        Guid.TryParse(http.Request.Cookies[CookieName], out var t) ? t : null; // t holds the parsed GUID if successful, otherwise returns null

    public static void ClearGuestToken(HttpContext http) =>
        http.Response.Cookies.Delete(CookieName);
}