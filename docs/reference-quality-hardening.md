# Reference Quality Hardening

This note documents the v1 LESS-ON reference-quality hardening rules.

## Boundary Detection

Reference fields are marked with `REFERENCE_BOUNDARY_SUSPECT` when they look inherited from the parent paper instead of the cited work. A reference is suspect when at least two of these three fields match the main paper metadata:

- journal/venue
- volume
- pages or article number

The strong LESS-ON case is journal plus article number, for example `Computer Networks` and `110675`. Detection runs regardless of DOI state, including missing DOI, skipped validation, invalid DOI, or temporary Crossref/OpenAlex failure. Suspect fields remain available in raw/debug data but are excluded from reference quality scoring.

## DOI Title Validation

DOI title matching is valid when any rule passes:

- normalized Levenshtein similarity is at least `0.75`
- token Jaccard similarity is at least `0.65`
- one normalized title is an exact meaningful prefix of the other and the shared prefix is at least 12 characters

Levenshtein and Jaccard checks only run when both titles have at least three meaningful tokens. Short titles such as `Cloud Computing` only pass exact normalized match or exact meaningful prefix match. If the local title looks like an author fragment, the DOI match can remain valid and the issue becomes `REFERENCE_TITLE_PARSE_FAILED` instead of `DOI_TITLE_MISMATCH`.

## Author Parsing

The reference normalizer preserves surname particles such as `de`, `da`, `del`, `di`, `van`, `von`, `der`, `den`, `la`, `le`, `dos`, and `das`. Compact initial tokens are parsed as authors when confidence is high, including forms like:

- `BRamprasad`
- `ADa Silva`
- `MVeith`
- `EGabel`
- `J.-MPierson`
- `AVVasilakos`

Compact author fragments are not trusted as canonical titles. Low-confidence parsed authors are kept out of canonical references and reported through warning issue codes.

## Severity

`REFERENCE_DOI_MISSING` is a warning-level quality loss and does not make a reference invalid by itself. Invalid DOI format, unresolved DOI, service errors, and true title mismatches remain validation failures or mismatch states.
