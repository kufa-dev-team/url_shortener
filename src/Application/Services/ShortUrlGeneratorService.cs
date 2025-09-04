using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Domain.Interfaces;

namespace Application.Services
{
    public class ShortUrlGeneratorService : IShortUrlGeneratorService
    {
        const string Chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

        public async Task<string> GenerateShortUrlAsync(int length)
        {
            if (length <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(length), "Length must be greater than zero.");
            }
            try
            {
                return await Task.Run(() =>
                {
                    var bytes = new byte[length];
                    var result = new StringBuilder(length);

                    // Fill bytes with cryptographically strong random values
                    RandomNumberGenerator.Fill(bytes);

                    for (int i = 0; i < length; i++)
                    {
                        // Use modulo to map byte to character index
                        int index = bytes[i] % Chars.Length;
                        result.Append(Chars[index]);
                    }
                    return result.ToString();
                });
            }

            catch (Exception ex)
            {
                // Log the exception (logging mechanism not shown here)
                throw new InvalidOperationException("Error generating short URL.", ex);
            }
        }
    }
}