using API.DTOs.UrlMapping;
using FluentValidation;

namespace API.Validators.UrlMapping;

public class CreateUrlMappingRequestValidator : AbstractValidator<CreateUrlMappingRequest>
{
    public CreateUrlMappingRequestValidator()
    {
        RuleFor(x => x.OriginalUrl)
            .NotEmpty()
            .WithMessage("Original URL is required")
            .Must(BeAValidUrl)
            .WithMessage("Please provide a valid URL")
            .MaximumLength(2048)
            .WithMessage("URL cannot exceed 2048 characters");

        RuleFor(x => x.CustomShortCode)
            .Matches(@"^[a-zA-Z0-9_-]+$")
            .WithMessage("Short code can only contain letters, numbers, hyphens, and underscores")
            .MinimumLength(3)
            .WithMessage("Custom short code must be at least 3 characters")
            .MaximumLength(20)
            .WithMessage("Custom short code cannot exceed 20 characters")
            .When(x => !string.IsNullOrEmpty(x.CustomShortCode));

        RuleFor(x => x.ExpiresAt)
            .GreaterThan(DateTime.UtcNow)
            .WithMessage("Expiration date must be in the future")
            .When(x => x.ExpiresAt.HasValue);

        RuleFor(x => x.Title)
            .MaximumLength(200)
            .WithMessage("Title cannot exceed 200 characters")
            .When(x => !string.IsNullOrEmpty(x.Title));

        RuleFor(x => x.Description)
            .MaximumLength(500)
            .WithMessage("Description cannot exceed 500 characters")
            .When(x => !string.IsNullOrEmpty(x.Description));
    }

    private static bool BeAValidUrl(string? url)
    {
        return Uri.TryCreate(url, UriKind.Absolute, out var uriResult)
               && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
    }
}