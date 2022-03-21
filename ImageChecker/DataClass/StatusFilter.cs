namespace ImageChecker.DataClass;

public class StatusFilter
{
    public int ID { get; private set; }
    public string Text { get; private set; }

    public StatusFilter(int id, string text)
    {
        ID = id;
        Text = text;
    }
}
