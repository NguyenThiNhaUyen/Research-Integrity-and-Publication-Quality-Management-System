using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PublicationQualitySystem.Application.Services.Interfaces
{
    public interface IPdfOcrService
    {
        Task<string> ExtractTextAsync(string objectKey);
    }
}
