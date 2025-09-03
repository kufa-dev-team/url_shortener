using Domain.Result;

namespace Domain.Interfaces
{
    public interface IShortUrlGeneratorService
    {
        Task <string> GenerateShortUrlAsync(int length);
    }
}