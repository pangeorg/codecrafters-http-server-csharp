using System.Net;
using System.Net.Sockets;
using System.Text;

// You can use print statements as follows for debugging, they'll be visible when running tests.
Console.WriteLine("Logs from your program will appear here!");

const string OK = "HTTP/1.1 200 OK\r\n\r\n";

// Uncomment this block to pass the first stage
TcpListener server = new(IPAddress.Any, 4221);
server.Start();
while (true)
{
    var socket = server.AcceptSocket(); // wait for client
    var sendBytes = Encoding.ASCII.GetBytes(OK);
    int i = socket.Send(sendBytes);
    Console.WriteLine("Message Sent /> : " + OK);
}
