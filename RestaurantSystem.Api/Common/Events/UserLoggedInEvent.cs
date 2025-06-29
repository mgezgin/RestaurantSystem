namespace RestaurantSystem.Api.Common.Events;

public record UserLoggedInEvent(Guid UserId, string SessionId);
