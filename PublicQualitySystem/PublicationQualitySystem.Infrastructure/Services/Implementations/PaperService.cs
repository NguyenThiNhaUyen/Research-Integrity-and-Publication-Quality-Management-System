using PublicationQualitySystem.Application.Repositories.Interfaces;

namespace PublicationQualitySystem.Infrastructure.Services.Implementations
{
    public class PaperService
    {
        private readonly IPaperRepository _paperRepository;

        public PaperService(IPaperRepository paperRepository)
        {
            _paperRepository = paperRepository;
        }
   
    }
}
