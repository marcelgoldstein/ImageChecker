using System.Collections.Generic;

namespace ImageChecker.DataClass;

public class WorkItem
{
    public FileImage ItemToCheck { get; set; }
    public List<FileImage> ItemsToCheckAgainst { get; set; }
}
