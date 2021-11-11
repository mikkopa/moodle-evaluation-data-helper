# Moodle Evaluation Data Converter

A program to parse Moodle's task evaluation csv file.

Reads csv data which is fetched from Moodle answers.

Gets selected columns.
Can get url from a column

Usual parameters with Savonia's moodle

-i csv-file-from-moodle -o target-csv -c "Koko nimi" Sähköpostiosoite -u Verkkoteksti


## Publish

1. add to project file 
   ```
    <PropertyGroup>
        <PublishSingleFile>true</PublishSingleFile>
    </PropertyGroup>   
   ```
2. run `dotnet publish -c Release -r win-x64`

https://docs.microsoft.com/en-us/dotnet/core/deploying/single-file#publish-a-single-file-app---cli