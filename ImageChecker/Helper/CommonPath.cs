using System.IO;

namespace ImageChecker.Helper;

public static class CommonPath
{
    public static string Find(IEnumerable<DirectoryInfo> folders)
    {
        var matchingChars =
                from len in Enumerable.Range(0, folders.Min(s => s.FullName.Length)).Reverse()
                let possibleMatch = folders.First().FullName[..len]
                where folders.All(f => f.FullName.StartsWith(possibleMatch))
                select possibleMatch;

        if (string.IsNullOrEmpty(matchingChars.First()))
            return string.Empty;

        string longestDir = Path.GetDirectoryName(matchingChars.First());

        if (string.IsNullOrEmpty(longestDir))
            return string.Empty;
        else
            return longestDir;
    }
}
