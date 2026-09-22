using System.Runtime.CompilerServices;

namespace SpeedtestWatcher.TestSupport;

public static class Approvals
{
    private const string ApproveVariable = "SPEEDTEST_WATCHER_APPROVE";

    public static void AssertMatchesApproved(string fileName, string actual, [CallerFilePath] string testFile = "")
    {
        var approvedPath = Path.Combine(Path.GetDirectoryName(testFile)!, fileName);
        if (Environment.GetEnvironmentVariable(ApproveVariable) == "1")
        {
            File.WriteAllText(approvedPath, actual.ReplaceLineEndings("\n"));
            return;
        }

        Assert.True(File.Exists(approvedPath), $"{fileName} is missing. Run the tests once with {ApproveVariable}=1 and review the file.");
        Assert.Equal(File.ReadAllText(approvedPath).ReplaceLineEndings("\n"), actual.ReplaceLineEndings("\n"));
    }
}
