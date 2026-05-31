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
    }
}
