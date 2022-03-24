namespace ImageChecker.Const;

public class CommonConst
{
    public const string LONG_PATH_PREFIX = @"\\?\"; // long file path syntax. Its need to prefix this to the original path when exceeding the length limit.

    public const string PROJECT_PAGE_URL = "https://github.com/marcelgoldstein/ImageChecker";

    public const string TEMP_FILES_RESULTVIEW_BACKUPS = "ResultView_Backups";
}
