using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Threading;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public class ArduinoService
{
    private TcpClient? _client;
    private NetworkStream? _stream;
    private CancellationTokenSource? _cts;
    private StringBuilder _messageBuilder = new();

    private readonly string _ip = "192.168.0.200";
    private readonly int _port = 5000;

    public event Action<string>? LinhaRecebida;
    public event Action<bool>? StatusConexaoAlterado;

    private bool _conectado;
    public bool Conectado
    {
        get => _conectado;
        private set
        {
            if (_conectado != value)
            {
                _conectado = value;
                StatusConexaoAlterado?.Invoke(_conectado);
            }
        }
    }
    
    public ArduinoService()
    {
        // Inicia a conexão assim que a instância é criada
        _ = ConnectAsync();
    }

    private async Task ConnectAsync()
    {
        try
        {
            _cts?.Cancel();
            _stream?.Close();
            _client?.Close();

            _client = new TcpClient();
            await _client.ConnectAsync(_ip, _port);
            _stream = _client.GetStream();

            Conectado = true;

            _cts = new CancellationTokenSource();
            _ = Task.Run(() => ListenAsync(_cts.Token));
        }
        catch
        {
            Conectado = false;
            // Pode tentar reconectar depois
            await Task.Delay(5000);
            _ = ConnectAsync();
        }
    }
    
    private async Task ListenAsync(CancellationToken ct)
    {
        byte[] buffer = new byte[4096];

        while (!ct.IsCancellationRequested && _client?.Connected == true)
        {
            try
            {
                int bytesRead = await _stream!.ReadAsync(buffer, 0, buffer.Length, ct);
                if (bytesRead == 0) break; // conexão fechada

                string dados = Encoding.ASCII.GetString(buffer, 0, bytesRead);
                _messageBuilder.Append(dados);

                string completo = _messageBuilder.ToString();
                var linhas = completo.Split('\n');

                for (int i = 0; i < linhas.Length - 1; i++)
                {
                    string linha = linhas[i].Trim();
                    if (!string.IsNullOrEmpty(linha))
                        LinhaRecebida?.Invoke(linha);
                }

                _messageBuilder.Clear();
                if (!completo.EndsWith('\n'))
                    _messageBuilder.Append(linhas[^1]);
            }
            catch
            {
                break;
            }
        }

        // Se saiu do loop de escuta, está desconectado, tenta reconectar
        Conectado = false;
        await Task.Delay(5000);
        await ConnectAsync();
    }
  
    public async Task EnviarComandoAsync(string comando)
    {
        if (!Conectado || _stream == null) return;

        try
        {
            var data = Encoding.ASCII.GetBytes(comando + "\n");
            await _stream.WriteAsync(data, 0, data.Length);
            await _stream.FlushAsync();
        }
        catch
        {
            Conectado = false;
        }
    }
    
    public void Dispose()
    {
        _cts?.Cancel();
        _stream?.Close();
        _client?.Close();
        Conectado = false;
    }


}