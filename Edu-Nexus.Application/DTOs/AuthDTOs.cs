namespace Edu_Nexus.Application.DTOs;

public record RegisterRequest(string Email, string Password, string FullName);

public record LoginRequest(string Email, string Password);

public record GoogleLoginRequest(string IdToken);

public record TokenRefreshRequest(string RefreshToken);

public record LogoutRequest(string RefreshToken);

public record AuthResponseData(
    Guid UserId,
    string Email,
    string FullName,
    string Role,
    bool IsSurveyCompleted,
    string AccessToken,
    string RefreshToken
);

public record TokenRefreshResponseData(string AccessToken, string RefreshToken);

public record GoogleTokenPayloadDto(string Email, string Name, string Subject, string Picture);

public record UserProfileResponseData(
    Guid Id,
    string Email,
    string FullName,
    string? AvatarUrl,
    string Role,
    bool IsSurveyCompleted,
    string? PortfolioUrlSlug,
    SubscriptionDto? Subscription
);

public record SubscriptionDto(
    string TierCode,
    string DisplayName,
    string Status,
    DateTime? ExpiresAt
);
