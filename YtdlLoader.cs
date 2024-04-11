using System.Diagnostics;
using System.Text.RegularExpressions;

public class YtdlLoader
{
    private const string LoaderPath = @"./ytdl-cli/index.js";
    private const string SavedAtRegex = "Saved at [./A-z-0-9]+";
    private const string SavedAtStatement = "Saved at ";
    public YtdlLoader()
    { }

    public string LoadFromUrl(string ytVideoId)
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
        return filePath;
    }
}