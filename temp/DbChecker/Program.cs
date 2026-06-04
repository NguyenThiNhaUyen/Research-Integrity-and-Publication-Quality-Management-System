using System;
using Npgsql;

var connString = "Host=localhost;Port=5432;Database=PublicationQualityDB;Username=postgres;Password=123456";

try
{
    using var conn = new NpgsqlConnection(connString);
    conn.Open();
    
    using var checkTable = new NpgsqlCommand(@"
        SELECT EXISTS (
            SELECT 1 
            FROM information_schema.tables 
            WHERE table_schema = 'public' 
            AND table_name = 'paper_processing_trackers'
        )", conn);
        
    var tableExists = (bool)checkTable.ExecuteScalar();
    Console.WriteLine($"Table 'paper_processing_trackers' exists: {tableExists}");
    
    if (tableExists)
    {
        var legacyCols = new[] { "current_step", "upload_status", "markdown_status", "metadata_extraction_status", "metadata_quality_status", "openalex_status", "crossref_status", "ai_review_status", "integrity_screening_status" };
        
        foreach (var col in legacyCols)
        {
            using var checkCol = new NpgsqlCommand($@"
                SELECT EXISTS (
                    SELECT 1 
                    FROM information_schema.columns 
                    WHERE table_schema = 'public' 
                    AND table_name = 'paper_processing_trackers' 
                    AND column_name = '{col}'
                )", conn);
            var colExists = (bool)checkCol.ExecuteScalar();
            Console.WriteLine($"Column '{col}' exists: {colExists}");
        }
    }
}
catch (Exception ex)
{
    Console.WriteLine("Error: " + ex.Message);
}
