using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using CommandLine;
using CsvHelper;
using CsvHelper.Configuration;
using System.Linq;
using System.Text.RegularExpressions;
using System.Dynamic;
using System.Diagnostics.CodeAnalysis;

namespace moodle_evaluation_data_helper;

class Program
{
    private static readonly string urlMatchingRegex = @"(https):\/\/([\w_-]+(?:(?:\.[\w_-]+)+))([\w.,@?^=%&:\/~+#-]*[\w@?^=%&\/~+#-])";

    class Options
    {
        //see nullable behaviour: https://docs.microsoft.com/en-us/dotnet/csharp/nullable-warnings#nonnullable-reference-not-initialized

        [Option('i', "input", Required = true, HelpText = "Moodle evaluations csv file")]
        public string Input { get; set; } = null!;

        [Option('o', "output", Required = true, HelpText = "Output csv file")]
        public string Output { get; set; } = null!;

        [Option('c', "selected-columns", Required = true, HelpText = "Selected columns from the input")]
        public IEnumerable<string> Columns { get; set; } = null!;

        [Option('u', "url-column", Required = false, HelpText = "The column from input where to parse url")]
        public string? UrlColumn { get; set; }

    }

    static async Task Main(string[] args)
    {
        var result = await CommandLine.Parser.Default.ParseArguments<Options>(args)
          .WithParsedAsync(RunOptions);
        result.WithNotParsed(HandleParseError);
    }

    static bool IsNotNullOrEmpty([NotNullWhen(true)]string? data) => false == string.IsNullOrEmpty(data);

    static async Task RunOptions(Options opts)
    {
        CsvConfiguration moodleCsvConfig = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            Delimiter = ",",
            HasHeaderRecord = true,
            Quote = '"'
        };
        CsvConfiguration outputCsvConfig = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            Delimiter = ";",
            HasHeaderRecord = true,
            Quote = '"'
        };

        if (false == System.IO.File.Exists(opts.Input))
        {
            Console.WriteLine($"Input csv file ({opts.Input}) does not exist.");
            return;
        }

        var records = new List<dynamic>();
        // read input file
        using (var reader = new StreamReader(opts.Input))
        {
            int counter = 1;
            
            bool parseUrl = IsNotNullOrEmpty(opts.UrlColumn);

            await foreach (var line in ReadAsync(reader, moodleCsvConfig))
            {
                IDictionary<string, Object> lineData = (IDictionary<string, Object>)line;

                // process a line from moodle csv
                string urlData = "";
                if (IsNotNullOrEmpty(opts.UrlColumn))
                {
                    urlData = lineData[opts.UrlColumn]?.ToString() ?? "";
                    Match m = Regex.Match(urlData, urlMatchingRegex, RegexOptions.IgnoreCase);
                    if (m.Success)
                    {
                        urlData = m.Value;
                    }
                }

                Console.WriteLine($"{counter++}: {string.Join(", ", opts.Columns.Select(c => lineData[c]))} | {(parseUrl ? urlData : "")}");
                dynamic record = new ExpandoObject();
                foreach (var col in opts.Columns)
                {
                    ((IDictionary<string, Object>)record).TryAdd(col, lineData[col]);
                }
                if (parseUrl)
                {
                    record.Url = urlData;
                }
                records.Add(record);
            }

            using (var writer = new StreamWriter(opts.Output, false, System.Text.Encoding.UTF8))
            using (var csv = new CsvWriter(writer, outputCsvConfig))
            {
                csv.WriteRecords(records);
            }
        }
    }

    private static async IAsyncEnumerable<dynamic> ReadAsync(StreamReader reader, CsvConfiguration config)
    {
        using (var csv = new CsvReader(reader, config))
        {
            var records = csv.GetRecordsAsync<dynamic>();
            await foreach (var line in records)
            {
                yield return line;
            }
        }
    }

    static void HandleParseError(IEnumerable<Error> errs)
    {
        //handle errors
        Console.WriteLine("Reads csv file from Moodle evaluations and parses selected columns to an output csv. Can try to read urls from a selected column.");
        System.Console.WriteLine("Usual parameters are -i csv-file-from-moodle -o target-csv -c \"Koko nimi\" Sähköpostiosoite -u Verkkoteksti");
    }
}
