using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PublicationQualitySystem.Domain.Entities
{
    public class PaperTopic
    {
        public long PaperId { get; set; }
        public Paper Paper { get; set; } = null!;

        public long ResearchTopicId { get; set; }
        public ResearchTopic ResearchTopic { get; set; } = null!;
    }
}
