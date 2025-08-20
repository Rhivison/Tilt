using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
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
    private bool _calibracaoAtiva = false;
    private readonly List<DadoEnsaio> _dadosEnsaio = new();

    public event Action? EnsaioIniciado;
    
    public event Action<List<DadoEnsaio>>? EnsaioFinalizado;
    public event Action<DadoEnsaio>? DadoRecebido;
    
    public event Action? CalibracaoIniciada;
    public event Action? CalibracaoFinalizada;
    public event Action<double, double>? DadoCalibracao; // entrada_sensor, saida_real

    public event Action<string>? LinhaRecebida;
    public event Action<bool>? StatusConexaoAlterado;

    public IReadOnlyList<DadoEnsaio> DadosEnsaio => _dadosEnsaio.AsReadOnly();
    public bool CalibracaoAtiva => _calibracaoAtiva;
    
    private readonly List<(double entradaSensor, double saidaReal)> _dadosCalibracao = new();
    public IReadOnlyList<(double entradaSensor, double saidaReal)> DadosCalibracao => _dadosCalibracao.AsReadOnly();

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
    public async Task IniciarCalibracaoAsync()
    {
        _calibracaoAtiva = true;
        _dadosCalibracao.Clear();
        CalibracaoIniciada?.Invoke();
        await EnviarComandoAsync("CALIBRACAO_START"); // comando para Arduino
    }
    public async Task FinalizarCalibracaoAsync()
    {
        _calibracaoAtiva = false;
        CalibracaoFinalizada?.Invoke();
        await EnviarComandoAsync("CALIBRACAO_STOP"); // comando para Arduino
    }
    
    public void AdicionarPontoCalibracao(double valorReferencia)
    {
        if (!_calibracaoAtiva) return;

        // Aqui pega o último valor recebido do sensor (supondo que DadoCalibracao é invocado sempre)
        if (_dadosCalibracao.Count > 0)
        {
            var ultimo = _dadosCalibracao.Last();
            // Substitui o valor de saída real pelo valor de referência inserido
            _dadosCalibracao[_dadosCalibracao.Count - 1] = (ultimo.entradaSensor, valorReferencia);
            DadoCalibracao?.Invoke(ultimo.entradaSensor, valorReferencia);
        }
    }
    public void AdicionarDadoCalibracao(double entradaSensor, double saidaReal)
    {
        _dadosCalibracao.Add((entradaSensor, saidaReal));
        DadoCalibracao?.Invoke(entradaSensor, saidaReal);
    }
    
    public void LimparCalibracao()
    {
        _dadosCalibracao.Clear();
    }
    
    public void FinalizarCalibracao()
    {
        _calibracaoAtiva = false;
        CalibracaoFinalizada?.Invoke();
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
                    if (linha == "CALIBRACAO_INICIADA")
                    {
                        _calibracaoAtiva = true;
                        CalibracaoIniciada?.Invoke();
                        Console.WriteLine("DEBUG: CALIBRAÇÃO INICIADA!");
                    }
                    else if (linha == "CALIBRACAO_FINALIZADA")
                    {
                        _calibracaoAtiva = false;
                        CalibracaoFinalizada?.Invoke();
                        Console.WriteLine("DEBUG: CALIBRAÇÃO FINALIZADA!");
                    }
                    else if (linha == "entrada_sensor,saida_real")
                    {
                        Console.WriteLine("DEBUG: Header de calibração recebido");
                    }
                    else if (_calibracaoAtiva)
                    {
                        ProcessarDadosCalibracaoServico(linha);
                    }
                    // Verifica se é comando de controle
                    if (linha.Contains("tempo_ms,angulo_graus") || linha.Contains("ENSAIO_INICIADO"))
                    {
                        if (!_ensaioAtivo) // Só inicia se não estiver já ativo
                        {
                            _ensaioAtivo = true;
                            _dadosEnsaio.Clear();
                            EnsaioIniciado?.Invoke();
                            Console.WriteLine("DEBUG: ENSAIO INICIADO!");
                        }
                    }
                    // Se recebemos dados no formato tempo,angulo e ainda não estamos em modo ensaio,
                    // provavelmente o cabeçalho se perdeu - inicia automaticamente
                    else if (!_ensaioAtivo && linha.Contains(",") && !linha.Contains(" "))
                    {
                        var partes = linha.Split(',');
                        if (partes.Length == 2 &&
                            double.TryParse(partes[0], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double tempo) &&
                            double.TryParse(partes[1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double angulo) &&
                            tempo > 1000) // tempo em ms deve ser > 1000 para ser válido
                        {
                            _ensaioAtivo = true;
                            _dadosEnsaio.Clear();
                            EnsaioIniciado?.Invoke();
                            Console.WriteLine("DEBUG: ENSAIO AUTO-INICIADO pelos dados!");
                            
                            // Processa este primeiro dado
                            var dado = new DadoEnsaio(tempo, angulo);
                            _dadosEnsaio.Add(dado);
                            DadoRecebido?.Invoke(dado);
                        }
                    }
                    else if (linha.Contains("ENSAIO_FINALIZADO"))
                    {
                        if (_ensaioAtivo) // Só finaliza se estiver ativo
                        {
                            _ensaioAtivo = false;
                            Console.WriteLine($"DEBUG: ENSAIO FINALIZADO! {_dadosEnsaio.Count} dados coletados");
                            EnsaioFinalizado?.Invoke(new List<DadoEnsaio>(_dadosEnsaio));
                        }
                        else
                        {
                            Console.WriteLine("DEBUG: ENSAIO_FINALIZADO recebido, mas ensaio não estava ativo!");
                        }
                    }
                    else if (_ensaioAtivo)
                    {
                        // Processa dados do ensaio
                        ProcessarDadosEnsaio(linha);
                    }
                    else if (linha.Contains(",") && !linha.Contains(" "))
                    {
                        // Se não estamos em modo ensaio mas recebemos dados válidos,
                        // pode ser que o cabeçalho se perdeu
                        Console.WriteLine($"DEBUG: Dados recebidos fora do modo ensaio: {linha}");
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

    private void ProcessarDadosCalibracaoServico(string linha)
    {
        var cultura = System.Globalization.CultureInfo.InvariantCulture;
        if (linha.Contains(",") && !linha.Contains(" "))
        {
            var partes = linha.Split(',');
            if (partes.Length == 2 &&
                double.TryParse(partes[0], System.Globalization.NumberStyles.Float, cultura, out double entradaSensor) &&
                double.TryParse(partes[1], System.Globalization.NumberStyles.Float, cultura, out double saidaReal))
            {
                // Armazena internamente
                AdicionarDadoCalibracao(entradaSensor, saidaReal);
                Console.WriteLine($"DEBUG: Calibração - Entrada: {entradaSensor:F3}, Saída: {saidaReal:F2}");
                return;
            }
        }
        Console.WriteLine($"DEBUG: Linha de calibração não processada: {linha}");
    }
    private void ProcessarDadosEnsaio(string linha)
    {
        // Usa cultura invariante para parsing correto dos números
        var cultura = System.Globalization.CultureInfo.InvariantCulture;
        
        // Primeiro tenta o formato esperado: tempo,angulo
        if (linha.Contains(",") && !linha.Contains(" "))
        {
            var partes = linha.Split(',');
            if (partes.Length == 2 &&
                double.TryParse(partes[0], System.Globalization.NumberStyles.Float, cultura, out double tempo) &&
                double.TryParse(partes[1], System.Globalization.NumberStyles.Float, cultura, out double angulo))
            {
                var dado = new DadoEnsaio(tempo, angulo);
                _dadosEnsaio.Add(dado);
                DadoRecebido?.Invoke(dado);
                Console.WriteLine($"DEBUG: Processado - Tempo: {tempo}ms, Ângulo: {angulo:F2}°");
                return;
            }
        }

        // Se não funcionou, tenta o formato que está chegando: angulo,tempo angulo,tempo
        if (linha.Contains(",") && linha.Contains(" "))
        {
            // Separa por espaços para pegar múltiplos pares
            var pares = linha.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            
            foreach (var par in pares)
            {
                if (par.Contains(","))
                {
                    var partes = par.Split(',');
                    if (partes.Length == 2 &&
                        double.TryParse(partes[0], System.Globalization.NumberStyles.Float, cultura, out double angulo) &&
                        double.TryParse(partes[1], System.Globalization.NumberStyles.Float, cultura, out double tempo))
                    {
                        var dado = new DadoEnsaio(tempo, angulo); // Note: tempo primeiro, angulo segundo no construtor
                        _dadosEnsaio.Add(dado);
                        DadoRecebido?.Invoke(dado);
                        Console.WriteLine($"DEBUG: Processado - Tempo: {tempo}ms, Ângulo: {angulo:F2}°");
                    }
                }
            }
        }
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