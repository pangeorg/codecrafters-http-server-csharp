using System.ComponentModel;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;

// Uncomment this block to pass the first stage
TcpListener server = new(IPAddress.Any, 4221);
server.Start();

while (true)
{
    var client = server.AcceptTcpClient();
    _ = Task.Run(() => HandleClient(client));
}

static Task HandleClient(TcpClient client){
    var stream = client.GetStream();
    byte[] buffer = new byte[1024];
    stream.Read(buffer, 0, buffer.Length);

    var request = Request.Parse(buffer);
    Response response = request.Target switch
    {
        "/" => new Response(StatusCode.Ok, string.Empty),
        "/index.html" => new Response(StatusCode.Ok, string.Empty),
        "/user-agent" => request.GetHeader("User-Agent") != null ? new Response(StatusCode.Ok, request.GetHeader("User-Agent")!) : new Response(StatusCode.InternalServerError, string.Empty),
        var echo when Regex.IsMatch(echo, "/echo/.*") => new Response(StatusCode.Ok, request.Target.Split("/")[^1]),
        _ => new Response(StatusCode.NotFound, string.Empty),
    };
    byte[] response_raw = response.ToBytes();
    stream.Write(response_raw, 0, response_raw.Length);

    string message = Encoding.ASCII.GetString(response_raw);
    Console.WriteLine("Message Sent /> : " + message);
    return Task.CompletedTask;

}

enum StatusCode
{
    [Description("OK")]
    Ok = 200,

    [Description("Not Found")]
    NotFound = 404,

    [Description("Internal Server Error")]
    InternalServerError = 500,
}


class Response(StatusCode status, string body, string contentType = "text/plain", string version = "HTTP/1.1")
{
    public StatusCode Status { get; } = status;
    public string Body { get; } = body;
    public string ContentType { get; } = contentType;
    public string Version { get; } = version;
    public override string ToString() => $"{Version} {(int)Status} {Status.GetDescription()}\r\nContent-Type: {ContentType}\r\nContent-Length: {Body.Length}\r\n\r\n{Body}";
    public byte[] ToBytes() => Encoding.ASCII.GetBytes(ToString());
}

class Request(string method, string target, string version)
{
    public Dictionary<string, string> Headers { get; private set; } = [];
    public string Method { get; } = method;
    public string Target { get; } = target;
    public string Version { get; } = version;
    public void AddHeader(string key, string value) => Headers.Add(key, value);
    public void RemoveHeader(string key) => Headers.Remove(key);
    public string? GetHeader(string key) {
        Headers.TryGetValue(key.ToLower(), out var value);
        return value;
    }

    public static Request Parse(byte[] buffer)
    {
        var requestText = Encoding.ASCII.GetString(buffer);
        var lines = requestText.Split("\r\n");
        var parts = lines[0].Split(" ");
        var method = parts[0];
        var target = parts[1];
        var version = parts[2];
        var request = new Request(method, target, version);
        
        for (int i = 1; i < lines.Length - 2; i++) {
            string[] content = lines[i].Trim().Split(": ");
            string key = content[0];
            string value = content[1];
            request.AddHeader(key.ToLower(), value);
        }
        
        return request;
    }
}

