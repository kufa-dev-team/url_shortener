using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Domain.Exceptions
{
    public class CacheNotFoundException : Exception
    {
        public CacheNotFoundException(string shortCode)
            : base($"ShortCode '{shortCode}' not found in cache.")
        {
        }
    }
}