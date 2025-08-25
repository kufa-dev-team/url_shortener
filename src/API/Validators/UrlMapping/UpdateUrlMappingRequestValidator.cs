using API.DTOs.UrlMapping;
using FluentValidation;

namespace API.Validators.UrlMapping;

public class UpdateUrlMappingRequestValidator : AbstractValidator<UpdateUrlMappingRequest>
{
    public UpdateUrlMappingRequestValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0)
            .WithMessage("ID must be greater than zero");

        RuleFor(x => x.CustomShortCode)
            .Matches(@"^[a-zA-Z0-9_-]+$")
            .WithMessage("Short code can only contain letters, numbers, hyphens, and underscores")
            .Length(8)
            .WithMessage("Custom short code must be exactly 8 characters long")
            .When(x => !string.IsNullOrEmpty(x.CustomShortCode));

        RuleFor(x => x.OriginalUrl)
            .Must(BeAValidUrl)
            .WithMessage("Please provide a valid URL")
            .MaximumLength(2048)
            .WithMessage("URL cannot exceed 2048 characters")
            .When(x => !string.IsNullOrEmpty(x.OriginalUrl));

        RuleFor(x => x.Title)
            .MaximumLength(200)
            .WithMessage("Title cannot exceed 200 characters")
            .When(x => !string.IsNullOrEmpty(x.Title));

        RuleFor(x => x.Description)
            .MaximumLength(500)
            .WithMessage("Description cannot exceed 500 characters")
            .When(x => !string.IsNullOrEmpty(x.Description));

        // RuleFor(x => x.ExpiresAt)
            // .GreaterThan(DateTime.UtcNow)
            // .WithMessage("Expiration date must be in the future")
            // .When(x => x.ExpiresAt.HasValue);
    }

    private static bool BeAValidUrl(string? url)
    {
        return Uri.TryCreate(url, UriKind.Absolute, out var uriResult)
               && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
    }
}