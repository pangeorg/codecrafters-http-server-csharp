using System.ComponentModel;
using System.Net;
using System.Net.Sockets;
using System.Text;

// You can use print statements as follows for debugging, they'll be visible when running tests.
Console.WriteLine("Logs from your program will appear here!");

// Uncomment this block to pass the first stage
TcpListener server = new(IPAddress.Any, 4221);
server.Start();

byte[] buffer = new byte[1024];
while (true)
{
    var socket = server.AcceptSocket(); // wait for client
    socket.Receive(buffer);
    var request = Request.Parse(buffer);
    byte[] sendBytes = request.Target switch
    {
        "/" => new Response(StatusCode.Ok, string.Empty).ToBytes(),
        "/index.html" => new Response(StatusCode.Ok, string.Empty).ToBytes(),
        _ => new Response(StatusCode.NotFound, string.Empty).ToBytes(),
    };
    int i = socket.Send(sendBytes);
    string message = Encoding.ASCII.GetString(sendBytes);
    Console.WriteLine("Message Sent /> : " + message);
}

enum StatusCode
{
    [Description("OK")]
    Ok = 200,

    [Description("Not Found")]
    NotFound = 404,
}


class Response(StatusCode status, string body, string version = "HTTP/1.1")
{
    public StatusCode Status { get; } = status;
    public string Body { get; } = body;
    public string Version { get; } = version;

    public override string ToString() => $"{Version} {(int)Status} {Status.GetDescription()}\r\n\r\n{Body}";
    public byte[] ToBytes() => Encoding.ASCII.GetBytes(ToString());
}

class Request(string method, string target, string version)
{
    public Dictionary<string, string> Headers { get; set; } = [];
    public string Method { get; } = method;
    public string Target { get; } = target;
    public string Version { get; } = version;
    public static Request Parse(byte[] buffer)
    {
        var requestText = Encoding.ASCII.GetString(buffer);
        var lines = requestText.Split("\r\n");
        var parts = lines[0].Split(" ");
        var method = parts[0];
        var target = parts[1];
        var version = parts[2];
        return new Request(method, target, version);
    }
}

