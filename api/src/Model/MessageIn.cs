namespace Api.Model;

public class MessageIn
{
    public string Session { get; set; }
    public string Text { get; set; }

    public MessageIn(string session, string text)
    {
        Session = session;
        Text = text;
    }
}
