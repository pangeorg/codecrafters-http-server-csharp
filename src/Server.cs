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
        "/" => new Response(StatusCode.Ok, []),
        "/index.html" => new Response(StatusCode.Ok, []),
        "/user-agent" => 
            request.GetHeader("User-Agent") != null ? 
            new Response(StatusCode.Ok, request.GetHeader("User-Agent")!.ToAsciiBytes()) :
            new Response(StatusCode.InternalServerError, []),
        var echo when Regex.IsMatch(echo, "/echo/.*") => new Response(StatusCode.Ok, request.Target.Split("/")[^1].ToAsciiBytes()),
        var echo when Regex.IsMatch(echo, "/files/.*") => HandleFileRequest(request),
        _ => new Response(StatusCode.NotFound, []),
    };
    byte[] response_raw = response.ToBytes();
    stream.Write(response_raw, 0, response_raw.Length);

    string message = Encoding.ASCII.GetString(response_raw);
    Console.WriteLine("Message Sent /> : " + message);
    return Task.CompletedTask;
}

static Response HandleFileRequest(Request request) {
    var args = Environment.GetCommandLineArgs();
    string directory;
    if (args.Length == 3) {
        directory = args[2];
    }
    else {
        directory = "./";
    }
    string filename = Path.Combine(directory, request.Target.Split("/")[^1]);
    switch (request.Method) {
        case "POST":
            try {
                File.WriteAllBytes(filename, request.Body);
                return new(StatusCode.Created, [], contentType: "application/octet-stream");
            }
            catch (Exception ex) {
                return new(StatusCode.InternalServerError, ex.Message.ToAsciiBytes());
            }
        case "GET":
            if (File.Exists(filename))
            {
                byte[] content = File.ReadAllBytes(filename);
                return new(StatusCode.Ok, content, contentType: "application/octet-stream");
            };
            return new(StatusCode.NotFound, []);
        default:
            return new(StatusCode.NotFound, []);
    }
}

enum StatusCode
{
    [Description("OK")]
    Ok = 200,

    [Description("Created")]
    Created = 201,

    [Description("Not Found")]
    NotFound = 404,

    [Description("Internal Server Error")]
    InternalServerError = 500,
}


class Response(StatusCode status, byte[] body, string contentType = "text/plain", string version = "HTTP/1.1")
{
    public StatusCode Status { get; } = status;
    public byte[] Body { get; } = body;
    public string ContentType { get; } = contentType;
    public string Version { get; } = version;
    private string GetPrefix() => $"{Version} {(int)Status} {Status.GetDescription()}\r\nContent-Type: {ContentType}\r\nContent-Length: {Body.Length}\r\n\r\n";
    public override string ToString() => GetPrefix() + $"{Encoding.ASCII.GetString(Body)}";
    public byte[] ToBytes() => Encoding.ASCII.GetBytes(GetPrefix()).Concat(Body).ToArray();
}

struct Request(string method, string target, string version, byte[] body)
{
    public Dictionary<string, string> Headers { get; private set; } = [];
    public string Method { get; } = method;
    public string Target { get; } = target;
    public string Version { get; } = version;
    public byte[] Body { get; } = body;
    public void AddHeader(string key, string value) => Headers.Add(key, value);
    public void RemoveHeader(string key) => Headers.Remove(key);
    public string? GetHeader(string key) {
        Headers.TryGetValue(key.ToLower(), out var value);
        return value;
    }

    public static Request Parse(byte[] buffer)
    {
        int count = buffer.IndexOf(i => i == 0);
        var requestText = Encoding.ASCII.GetString(buffer, 0, count);
        var lines = requestText.Split("\r\n");
        var parts = lines[0].Split(" ");
        var method = parts[0];
        var target = parts[1];
        var version = parts[2];
        var body = lines[^1].Trim().ToAsciiBytes();
        var request = new Request(method, target, version, body);
        
        for (int i = 1; i < lines.Length - 2; i++) {
            string[] content = lines[i].Trim().Split(": ");
            string key = content[0];
            string value = content[1];
            request.AddHeader(key.ToLower(), value);
        }
        
        return request;
    }
}