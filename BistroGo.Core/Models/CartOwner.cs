namespace BistroGo.Core.Models
{
    // Exactly one of the two is set: a logged-in user OR a guest cookie token.
    public record CartOwner(string? UserId, Guid? GuestToken)
    {
        public static CartOwner ForUser(string userId) => new(userId, null);
        public static CartOwner ForGuest(Guid token) => new(null, token);
    }
}