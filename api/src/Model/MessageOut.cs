using System;

namespace Api.Model;

public class MessageOut
{
    public string User { get; }
    public string Text { get; }
    public string Sent { get; }

    public MessageOut(string text, string user = "(QSECOFR)")
    {
        Text = text;
        User = user;
        Sent = DateTime.Now.ToString("H:mm");
    }
}
