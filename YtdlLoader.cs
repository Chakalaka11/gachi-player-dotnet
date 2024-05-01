using System.Diagnostics;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

public class YtdlLoader
{
    private const string LoaderPath = @"./ytdl-cli/index.js";
    private const string SavedAtRegex = "Saved at [./A-z-0-9]+";
    private const string SavedAtStatement = "Saved at ";
    private ILogger logger;
    public YtdlLoader()
    {
        using ILoggerFactory factory = LoggerFactory.Create(builder => builder.AddConsole());
        logger = factory.CreateLogger(nameof(YtdlLoader));
    }

    public string? LoadFromUrl(string ytVideoId)
    {
        var processParams = new ProcessStartInfo
        {
            FileName = "node",
            Arguments = $@" {LoaderPath} {ytVideoId}",
            RedirectStandardOutput = true,
            UseShellExecute = false
        };

        using var proc = Process.Start(processParams);
        var output = proc.StandardOutput.ReadToEnd();
        proc.WaitForExit();

        var filePath = Regex.Match(output, SavedAtRegex).Value;
        filePath = filePath.Replace(SavedAtStatement, "");
        logger.LogInformation(@$"File path for file - {filePath}");
        return filePath;
    }

    public void ClearOldFiles()
    {
        
    }
}