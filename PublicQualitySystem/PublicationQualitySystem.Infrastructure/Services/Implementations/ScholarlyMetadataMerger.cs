using PublicationQualitySystem.Application.DTOs.Crossref;
using PublicationQualitySystem.Application.DTOs.Grobid;

namespace PublicationQualitySystem.Infrastructure.Services.Implementations;

public static class ScholarlyMetadataMerger
{
    public static GrobidMetadataResponse Merge(GrobidMetadataResponse grobid, CrossrefMetadataResponse? crossref)
    {
        if (crossref is null)
        {
            grobid.MetadataSource = string.IsNullOrWhiteSpace(grobid.DoiSource) ? "GROBID" : $"GROBID+{grobid.DoiSource}";
            return grobid;
        }

        var usedCrossref = false;

        grobid.Title = Prefer(grobid.Title, crossref.Title, ref usedCrossref);
        grobid.Abstract = Prefer(grobid.Abstract, crossref.Abstract, ref usedCrossref);
        grobid.Doi = Prefer(grobid.Doi, crossref.Doi, ref usedCrossref);

        if (string.IsNullOrWhiteSpace(grobid.Journal) && !string.IsNullOrWhiteSpace(crossref.Journal))
        {
            grobid.Journal = crossref.Journal;
            grobid.JournalSource = "CROSSREF";
            usedCrossref = true;
        }

        grobid.Publisher = Prefer(grobid.Publisher, crossref.Publisher, ref usedCrossref);
        grobid.PublicationYear ??= Use(crossref.PublicationYear, ref usedCrossref);
        grobid.Volume = Prefer(grobid.Volume, crossref.Volume, ref usedCrossref);
        grobid.Issue = Prefer(grobid.Issue, crossref.Issue, ref usedCrossref);
        grobid.Pages = Prefer(grobid.Pages, crossref.Pages, ref usedCrossref);

        if (grobid.Authors.Count == 0 && crossref.Authors.Count > 0)
        {
            grobid.Authors = crossref.Authors;
            usedCrossref = true;
        }
        else if (grobid.Authors.Count > 0 && crossref.Authors.Count > 0)
        {
            grobid.Authors = MergeAuthorOrcids(grobid.Authors, crossref.Authors, ref usedCrossref);
        }

        grobid.MetadataSource = usedCrossref ? "GROBID+CROSSREF" : "GROBID";
        return grobid;
    }

    private static IReadOnlyList<AuthorDto> MergeAuthorOrcids(
        IReadOnlyList<AuthorDto> grobidAuthors,
        IReadOnlyList<AuthorDto> crossrefAuthors,
        ref bool usedCrossref)
    {
        var merged = new List<AuthorDto>();
        foreach (var author in grobidAuthors)
        {
            if (!string.IsNullOrWhiteSpace(author.Orcid))
            {
                merged.Add(author);
                continue;
            }

            var crossref = crossrefAuthors.FirstOrDefault(candidate => NamesMatch(author, candidate));
            if (string.IsNullOrWhiteSpace(crossref?.Orcid))
            {
                merged.Add(author);
                continue;
            }

            usedCrossref = true;
            merged.Add(new AuthorDto
            {
                FirstName = author.FirstName,
                MiddleName = author.MiddleName,
                LastName = author.LastName,
                FullName = author.FullName,
                Orcid = crossref.Orcid,
                RawAuthorName = author.RawAuthorName,
                IsCorresponding = author.IsCorresponding,
                Email = author.Email,
                Affiliation = author.Affiliation
            });
        }

        return merged;
    }

    private static bool NamesMatch(AuthorDto left, AuthorDto right)
    {
        var leftName = NormalizeName(left.FullName);
        var rightName = NormalizeName(right.FullName);
        return !string.IsNullOrWhiteSpace(leftName)
            && !string.IsNullOrWhiteSpace(rightName)
            && (leftName == rightName
                || leftName.Contains(rightName, StringComparison.OrdinalIgnoreCase)
                || rightName.Contains(leftName, StringComparison.OrdinalIgnoreCase));
    }

    private static string? NormalizeName(string? name) =>
        string.IsNullOrWhiteSpace(name)
            ? null
            : string.Join(" ", name.ToLowerInvariant().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

    private static string? Prefer(string? current, string? candidate, ref bool usedCandidate)
    {
        if (!string.IsNullOrWhiteSpace(current) || string.IsNullOrWhiteSpace(candidate))
        {
            return current;
        }

        usedCandidate = true;
        return candidate;
    }

    private static int? Use(int? candidate, ref bool usedCandidate)
    {
        if (candidate is not null)
        {
            usedCandidate = true;
        }

        return candidate;
    }
}
