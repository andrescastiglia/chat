namespace Api.Model;

public class Request
{
    public string Session { get; set; }
    public MessageIn Message { get; set; }

    public Request(string session, MessageIn message)
    {
        Session = session;
        Message = message;
    }
}