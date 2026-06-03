using PublicationQualitySystem.Infrastructure.Services.Implementations;

namespace PublicationQualitySystem.Infrastructure.Tests;

public class GrobidTeiParserTests
{
    [Fact]
    public void Parse_CleansScientificPaperMetadata()
    {
        const string xml = """
            <TEI xmlns="http://www.tei-c.org/ns/1.0">
              <teiHeader>
                <fileDesc>
                  <titleStmt>
                    <title>Hammer PDF: An Intelligent PDF Reader for Scientific Papers</title>
                  </titleStmt>
                  <publicationStmt>
                    <publisher/>
                  </publicationStmt>
                  <sourceDesc>
                    <biblStruct>
                      <analytic>
                        <title level="a">Hammer PDF: An Intelligent PDF Reader for Scientific Papers</title>
                        <author>
                          <persName>
                            <forename type="first">Sheng-Fu</forename>
                            <surname>Wang</surname>
                          </persName>
                          <email>wangsf@bit.edu.cn</email>
                        </author>
                        <author>
                          <persName>
                            <forename type="first">Shu-Hang</forename>
                            <surname>Liu</surname>
                          </persName>
                        </author>
                        <author>
                          <persName>
                            <forename type="first">Yi-Fan</forename>
                            <surname>Lu</surname>
                          </persName>
                          <email>luyifan@bit.edu.cn</email>
                        </author>
                        <author>
                          <affiliation key="aff0">
                            <orgName>Beijing Institute of Technology</orgName>
                            <address>
                              <addrLine>Haidian Distr.</addrLine>
                              <settlement>Beijing</settlement>
                              <country>China</country>
                            </address>
                          </affiliation>
                        </author>
                        <idno type="DOI">https://doi.org/XXXXXXX.XXXXXXX</idno>
                      </analytic>
                      <monogr>
                        <title level="m">CIKM '22</title>
                        <imprint>
                          <date when="2022"/>
                        </imprint>
                      </monogr>
                      <idno type="arXiv">arXiv:2204.02809v2[cs.DL]</idno>
                    </biblStruct>
                  </sourceDesc>
                </fileDesc>
                <profileDesc>
                  <abstract>Paper abstract.</abstract>
                  <textClass>
                    <keywords>
                      <term>CCS CONCEPTS</term>
                      <term>Information systems -> Web applications</term>
                      <term>Search interfaces</term>
                      <term>Information extraction PDF Reader, Literature Search, Information Extraction</term>
                    </keywords>
                  </textClass>
                </profileDesc>
              </teiHeader>
              <text>
                <front>
                  <div>
                    <p>ACM Reference Format: In Proceedings of the 31st ACM International Conference on Information and Knowledge Management (CIKM '22)</p>
                  </div>
                </front>
                <back>
                  <listBibl>
                    <biblStruct>
                      <analytic>
                        <title level="a">A useful paper</title>
                        <author>
                          <persName>
                            <forename type="first">Jane</forename>
                            <surname>Doe</surname>
                          </persName>
                        </author>
                      </analytic>
                      <monogr>
                        <title level="j">Journal of Tests</title>
                        <imprint>
                          <publisher>Test Publisher</publisher>
                          <date when="2020"/>
                        </imprint>
                      </monogr>
                    </biblStruct>
                  </listBibl>
                </back>
              </text>
            </TEI>
            """;

        var metadata = GrobidTeiParser.Parse(xml);

        Assert.Equal("Hammer PDF: An Intelligent PDF Reader for Scientific Papers", metadata.Title);
        Assert.Null(metadata.Doi);
        Assert.Null(metadata.DoiSource);
        Assert.Equal("2204.02809v2", metadata.ArxivId);
        Assert.Equal(2022, metadata.PublicationYear);
        Assert.Equal("ACM", metadata.Publisher);
        Assert.Equal("CIKM '22", metadata.Venue);
        Assert.Equal("31st ACM International Conference on Information and Knowledge Management", metadata.ConferenceName);
        Assert.Equal(3, metadata.Authors.Count);
        Assert.Equal("Sheng-Fu Wang", metadata.Authors[0].FullName);
        Assert.Equal("wangsf@bit.edu.cn", metadata.Authors[0].Email);
        Assert.All(metadata.Authors, author =>
            Assert.Equal("Beijing Institute of Technology, Haidian Distr., Beijing, China", author.Affiliation));
        Assert.DoesNotContain(metadata.Authors, author => author.FullName == "Beijing Institute of Technology Haidian Distr. Beijing China");
        Assert.Contains("Information extraction", metadata.Keywords);
        Assert.Contains("PDF Reader", metadata.Keywords);
        Assert.Single(metadata.Keywords, x => x.Equals("Information extraction", StringComparison.OrdinalIgnoreCase));
        Assert.Single(metadata.References);
        Assert.Equal("A useful paper", metadata.References[0].Title);
        Assert.Equal("Jane Doe", metadata.References[0].Authors[0].FullName);
        Assert.Null(metadata.References[0].Doi);
        Assert.Equal("Test Publisher", metadata.References[0].Publisher);
        Assert.Equal(2020, metadata.References[0].PublicationYear);
        Assert.False(string.IsNullOrWhiteSpace(metadata.References[0].RawText));
    }

    [Fact]
    public void Parse_NormalizesValidDoiUrl()
    {
        const string xml = """
            <TEI xmlns="http://www.tei-c.org/ns/1.0">
              <teiHeader>
                <fileDesc>
                  <sourceDesc>
                    <biblStruct>
                      <analytic>
                        <title level="a">Paper</title>
                        <idno type="DOI">https://doi.org/10.1145/example</idno>
                      </analytic>
                    </biblStruct>
                  </sourceDesc>
                </fileDesc>
              </teiHeader>
            </TEI>
            """;

        var metadata = GrobidTeiParser.Parse(xml);

        Assert.Equal("10.1145/example", metadata.Doi);
        Assert.Equal("GROBID", metadata.DoiSource);
    }

    [Fact]
    public void Parse_FallsBackToBodyTextForDoiAndParsesJournalIssuePages()
    {
        const string xml = """
            <TEI xmlns="http://www.tei-c.org/ns/1.0">
              <teiHeader>
                <fileDesc>
                  <sourceDesc>
                    <biblStruct>
                      <analytic>
                        <title level="a">Body DOI Paper</title>
                        <author role="corresp">
                          <persName>
                            <forename type="first">Ada</forename>
                            <surname>Lovelace</surname>
                          </persName>
                          <email>ada@example.org</email>
                        </author>
                      </analytic>
                      <monogr>
                        <title level="j">Journal of Metadata Systems</title>
                        <imprint>
                          <publisher>Metadata Press</publisher>
                          <biblScope unit="volume">12</biblScope>
                          <biblScope unit="issue">4</biblScope>
                          <biblScope unit="page" from="101" to="120"/>
                          <date when="2025-03-01"/>
                        </imprint>
                      </monogr>
                    </biblStruct>
                  </sourceDesc>
                </fileDesc>
              </teiHeader>
              <text>
                <body>
                  <p>This article is available at doi:10.1000/xyz123.</p>
                </body>
              </text>
            </TEI>
            """;

        var metadata = GrobidTeiParser.Parse(xml);

        Assert.Equal("10.1000/xyz123", metadata.Doi);
        Assert.Equal("REGEX", metadata.DoiSource);
        Assert.Equal("Journal of Metadata Systems", metadata.Journal);
        Assert.Equal("GROBID", metadata.JournalSource);
        Assert.Equal("Metadata Press", metadata.Publisher);
        Assert.Equal(2025, metadata.PublicationYear);
        Assert.Equal("12", metadata.Volume);
        Assert.Equal("4", metadata.Issue);
        Assert.Equal("101-120", metadata.Pages);
        Assert.Equal("Ada Lovelace <ada@example.org>", metadata.CorrespondingAuthor);
    }

    [Fact]
    public void Parse_ExtractsKeywordsFromTermElements()
    {
        const string xml = """
            <TEI xmlns="http://www.tei-c.org/ns/1.0">
              <teiHeader>
                <profileDesc>
                  <textClass>
                    <keywords>
                      <term>Edge computing</term>
                      <term>Energy efficiency</term>
                    </keywords>
                  </textClass>
                </profileDesc>
              </teiHeader>
            </TEI>
            """;

        var metadata = GrobidTeiParser.Parse(xml);

        Assert.Equal(
            ["Edge computing", "Energy efficiency"],
            metadata.Keywords);
    }

    [Fact]
    public void Parse_ExtractsKeywordsFromPlainTextKeywordsNode()
    {
        const string xml = """
            <TEI xmlns="http://www.tei-c.org/ns/1.0">
              <teiHeader>
                <profileDesc>
                  <textClass>
                    <keywords>
                      Edge computing;
                      Energy efficiency;
                      5G;
                      Wireless networks;
                      Sustainable communications
                    </keywords>
                  </textClass>
                </profileDesc>
              </teiHeader>
            </TEI>
            """;

        var metadata = GrobidTeiParser.Parse(xml);

        Assert.Equal(
            [
                "Edge computing",
                "Energy efficiency",
                "5G",
                "Wireless networks",
                "Sustainable communications"
            ],
            metadata.Keywords);
    }

    [Fact]
    public void Parse_ExtractsKeywordsFromAuthorSchemeKeywords()
    {
        const string xml = """
            <TEI xmlns="http://www.tei-c.org/ns/1.0">
              <teiHeader>
                <profileDesc>
                  <keywords scheme="author">
                    <term>Edge computing</term>
                    <term>Wireless networks</term>
                  </keywords>
                </profileDesc>
              </teiHeader>
            </TEI>
            """;

        var metadata = GrobidTeiParser.Parse(xml);

        Assert.Equal(
            ["Edge computing", "Wireless networks"],
            metadata.Keywords);
    }

    [Fact]
    public void Parse_DeduplicatesAndIgnoresEmptyKeywords()
    {
        const string xml = """
            <TEI xmlns="http://www.tei-c.org/ns/1.0">
              <teiHeader>
                <profileDesc>
                  <textClass>
                    <keywords>
                      Edge computing;; EDGE COMPUTING,

                      Energy efficiency,
                      energy efficiency;
                    </keywords>
                  </textClass>
                </profileDesc>
              </teiHeader>
            </TEI>
            """;

        var metadata = GrobidTeiParser.Parse(xml);

        Assert.Equal(
            ["Edge computing", "Energy efficiency"],
            metadata.Keywords);
    }

    [Fact]
    public void Parse_ExtractsLessOnFundingAndMultipleAuthorAffiliations()
    {
        const string xml = """
            <TEI xmlns="http://www.tei-c.org/ns/1.0">
              <teiHeader>
                <fileDesc>
                  <titleStmt>
                    <title>LESS-ON</title>
                    <funder>
                      <orgName type="full">European Social Fund</orgName>
                    </funder>
                    <funder>
                      <orgName type="full">EU NextGenerationEU/PRTR, MCIN</orgName>
                    </funder>
                    <funder>
                      <orgName type="full">EU's H2020</orgName>
                    </funder>
                    <funder>
                      <orgName type="full">EU, ERDF</orgName>
                    </funder>
                    <funder>
                      <orgName type="full">Universidad de Castilla-La Mancha</orgName>
                    </funder>
                    <funder>
                      <orgName type="full">AEI</orgName>
                    </funder>
                    <funder>
                      <orgName type="full">UCLM</orgName>
                    </funder>
                    <funder>
                      <orgName type="full">unknown</orgName>
                    </funder>
                    <funder>
                      <orgName type="full">AEI</orgName>
                    </funder>
                  </titleStmt>
                  <sourceDesc>
                    <biblStruct>
                      <analytic>
                        <title level="a">LESS-ON</title>
                        <author>
                          <persName>
                            <forename type="first">Maria</forename>
                            <surname>Garcia</surname>
                          </persName>
                          <affiliation>
                            <orgName type="department">Department of Computer Science</orgName>
                            <orgName type="institution">Universidad de Castilla-La Mancha</orgName>
                            <address>
                              <settlement>Albacete</settlement>
                              <country>Spain</country>
                            </address>
                          </affiliation>
                          <affiliation>
                            <orgName type="laboratory">Smart Networks Lab</orgName>
                            <address>
                              <settlement>Toledo</settlement>
                              <country>Spain</country>
                            </address>
                          </affiliation>
                        </author>
                      </analytic>
                    </biblStruct>
                  </sourceDesc>
                </fileDesc>
              </teiHeader>
            </TEI>
            """;

        var metadata = GrobidTeiParser.Parse(xml);

        Assert.Equal(
            [
                "European Social Fund",
                "EU NextGenerationEU/PRTR, MCIN",
                "EU's H2020",
                "EU, ERDF",
                "Universidad de Castilla-La Mancha",
                "AEI",
                "UCLM"
            ],
            metadata.FundingOrganizations);
        Assert.Single(metadata.Authors);
        Assert.Equal(
            "Department of Computer Science, Universidad de Castilla-La Mancha, Albacete, Spain; Smart Networks Lab, Toledo, Spain",
            metadata.Authors[0].Affiliation);
    }
}
