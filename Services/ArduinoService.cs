using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using TiltMachine.Models;

public class ArduinoService
{
    private TcpClient? _client;
    private NetworkStream? _stream;
    private CancellationTokenSource? _cts;
    private readonly StringBuilder _messageBuilder = new();

    private readonly string _ip = "192.168.0.200";
    private readonly int _port = 5000;

    private bool _ensaioAtivo = false;
    private readonly List<DadoEnsaio> _dadosEnsaio = new();

    public event Action? EnsaioIniciado;
    public event Action<List<DadoEnsaio>>? EnsaioFinalizado;
    public event Action<DadoEnsaio>? DadoRecebido;

    public event Action<string>? LinhaRecebida;
    public event Action<bool>? StatusConexaoAlterado;

    public IReadOnlyList<DadoEnsaio> DadosEnsaio => _dadosEnsaio.AsReadOnly();

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
        _ = ConnectAsync(); // Conecta automaticamente ao iniciar
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
            await Task.Delay(5000);
            _ = ConnectAsync();
        }
    }

    public async Task ReconnectAsync()
    {
        await ConnectAsync();
    }

    private async Task ListenAsync(CancellationToken ct)
    {
        byte[] buffer = new byte[4096];

        while (!ct.IsCancellationRequested && _client?.Connected == true)
        {
            try
            {
                int bytesRead = await _stream!.ReadAsync(buffer, 0, buffer.Length, ct);
                if (bytesRead == 0) break;

                string dados = Encoding.ASCII.GetString(buffer, 0, bytesRead);
                _messageBuilder.Append(dados);

                string completo = _messageBuilder.ToString();
                var linhas = completo.Split('\n');

                for (int i = 0; i < linhas.Length - 1; i++)
                {
                    string linha = linhas[i].Trim();
                    if (string.IsNullOrWhiteSpace(linha)) continue;

                    LinhaRecebida?.Invoke(linha);

                    if (linha.Contains("tempo_ms,angulo_graus"))
                    {
                        _ensaioAtivo = true;
                        _dadosEnsaio.Clear();
                        EnsaioIniciado?.Invoke();
                    }
                    else if (linha.Contains("ENSAIO_FINALIZADO"))
                    {
                        _ensaioAtivo = false;
                        EnsaioFinalizado?.Invoke(new List<DadoEnsaio>(_dadosEnsaio));
                    }
                    else if (_ensaioAtivo && linha.Contains(","))
                    {
                        var partes = linha.Split(',');
                        if (partes.Length == 2 &&
                            double.TryParse(partes[0], out double tempo) &&
                            double.TryParse(partes[1], out double angulo))
                        {
                            var dado = new DadoEnsaio(tempo, angulo);
                            _dadosEnsaio.Add(dado);
                            DadoRecebido?.Invoke(dado);
                        }
                    }
                }

                _messageBuilder.Clear();

                // Se não terminou com \n, guarda a última linha incompleta
                if (!completo.EndsWith('\n'))
                    _messageBuilder.Append(linhas[^1]);
            }
            catch
            {
                break;
            }
        }

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
