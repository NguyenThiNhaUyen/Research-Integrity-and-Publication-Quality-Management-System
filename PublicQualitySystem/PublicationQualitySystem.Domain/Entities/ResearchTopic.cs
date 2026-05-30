using PublicationQualitySystem.Shared.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PublicationQualitySystem.Domain.Entities
{
    public class ResearchTopic:BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public ICollection<PaperTopic> PaperTopics { get; set; } = new List<PaperTopic>();
    }
}
