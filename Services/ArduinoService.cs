using System;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

public class ArduinoService
{
    private readonly TcpClient _tcp = new TcpClient();

    public async Task ConnectAsync(string ip, int port)
        => await _tcp.ConnectAsync(ip, port);

    public async Task<string> SendReceiveAsync(string msg)
    {
        var stream = _tcp.GetStream();
        var buffer = Encoding.ASCII.GetBytes(msg + "\n");
        await stream.WriteAsync(buffer, 0, buffer.Length);

        var readBuf = new byte[256];
        int count = await stream.ReadAsync(readBuf, 0, readBuf.Length);
        return Encoding.ASCII.GetString(readBuf, 0, count);
    }
}