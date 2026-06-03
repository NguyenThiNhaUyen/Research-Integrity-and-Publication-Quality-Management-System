# Metadata Quality Score Gate

## Purpose

The Metadata Quality Score Gate checks whether extracted scholarly metadata is strong enough before a paper proceeds to Integrity Screening.

The score is calculated from the existing `PaperMetadata` record after GROBID/Crossref metadata extraction finishes. The gate does not change public API contracts and does not require a database migration in this version.

## Scoring Table

| Category | Field | Points | Validation |
| --- | --- | ---: | --- |
| Core | Title | 10 | `Title` is not empty |
| Core | Authors | 10 | At least one author has `fullName` |
| Core | DOI | 15 | DOI is not empty and matches a DOI-like pattern |
| Core | Abstract | 10 | `Abstract` is not empty |
| Core | Source name | 10 | Journal requires `Journal`; conference requires `ConferenceName` |
| Core | Publication date/year | 5 | `PublicationYear` exists |
| Core | References | 10 | References list has at least one item |
| Extended | Affiliations | 5 | At least one author has `affiliation` |
| Extended | Keywords | 5 | Keywords list has at least one item |
| Extended | Publisher | 5 | `Publisher` is not empty |
| Extended | Funding | 5 | Funding metadata exists |
| Enrichment | ORCID | 4 | At least one author JSON object has `orcid` |
| Enrichment | Author email | 3 | At least one author has `email` |
| Enrichment | Corresponding author | 3 | `CorrespondingAuthor` is not empty |

Maximum score: 100.

## Grade Table

| Score | Grade | Can proceed |
| ---: | --- | --- |
| 90-100 | EXCELLENT | Yes |
| 80-89 | GOOD | Yes |
| 70-79 | ACCEPTABLE | Yes, with warning |
| 50-69 | POOR | No, human review required |
| < 50 | FAIL | No |

## Field Validation Rules

- DOI must look like `10.xxxx/yyyy`.
- Authors are valid when at least one author has a non-empty `fullName`.
- Affiliations are valid when at least one author has a non-empty `affiliation`.
- Keywords are valid when the keywords JSON array contains at least one non-empty value.
- References are valid when the references JSON array contains at least one item.
- ORCID is optional and only affects enrichment score.
- Author email is optional and only affects enrichment score.
- Funding is important but does not block the pipeline by itself.

## JOURNAL vs CONFERENCE Notes

The current metadata model does not persist an explicit source type. The gate infers source type:

- If `conferenceName` or `venue` exists and `journal` is empty, the paper is treated as a conference paper.
- Otherwise, the paper is treated as a journal article.

For journal articles, `conferenceName` and `venue` are not required. For conference papers, `journal` is not required.

## Example Input

```json
{
  "title": "A Complete Metadata Paper",
  "abstract": "This paper has rich metadata.",
  "doi": "10.1000/xyz123",
  "journal": "Journal of Metadata Quality",
  "publisher": "RIPQMS Press",
  "publicationYear": 2026,
  "correspondingAuthor": "Ada Lovelace <ada@example.org>",
  "authors": [
    {
      "fullName": "Ada Lovelace",
      "email": "ada@example.org",
      "affiliation": "RIPQMS University",
      "ORCID": "0000-0002-1825-0097"
    }
  ],
  "keywords": ["metadata quality", "GROBID"],
  "references": [
    {
      "title": "Reference Paper"
    }
  ]
}
```

## Example Output

```json
{
  "totalScore": 95,
  "coreScore": 70,
  "extendedScore": 15,
  "enrichmentScore": 10,
  "grade": "EXCELLENT",
  "canProceed": true,
  "missingFields": ["funding"],
  "warnings": [
    "Funding metadata is missing. Funding is important but does not block the pipeline by itself."
  ],
  "fieldScores": {
    "title": 10,
    "authors": 10,
    "doi": 15,
    "abstract": 10,
    "sourceName": 10,
    "publicationDateOrYear": 5,
    "references": 10,
    "affiliations": 5,
    "keywords": 5,
    "publisher": 5,
    "funding": 0,
    "orcid": 4,
    "authorEmail": 3,
    "correspondingAuthor": 3
  }
}
```

## Gate Before Integrity Screening

After metadata extraction completes and `PaperMetadata` is saved, the background worker calculates the quality score.

The worker logs:

- Total score
- Grade
- Whether the paper can proceed
- Missing fields
- Warnings

If `canProceed` is `false`, Integrity Screening should not run automatically. The paper should go to human review first.
