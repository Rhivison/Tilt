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
    private string arduinoIp = "192.168.0.200";
    private const int arduinoPort = 5000;
    private TcpClient? _client;
    private NetworkStream? _stream;
    private CancellationTokenSource? _cts;
    
    //variáveis para ensaio
    private bool _ensaioAtivo = false;
    private List<string> _dadosEnsaio = new List<string>();
    private int _contadorDados = 0;

    private async void Connect_ToArduino(string ip, int port)
    {
        try
        {
            _cts?.Cancel();
            _stream?.Close();
            _client?.Close();
            _client = new TcpClient();
            await _client.ConnectAsync(arduinoIp, arduinoPort);
            _stream = _client.GetStream();
            _cts = new CancellationTokenSource();
            _ = Task.Run(() => ListenToArduinoAsync(_cts.Token));
        }
        catch (Exception ex)
        {
            
        }
    }

    private async void ListenToArduinoAsync(CancellationToken cancellationToken)
    {
        byte[] buffer = new byte[4096]; // Buffer maior
        StringBuilder messageBuilder = new StringBuilder();
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                if (_stream == null || !_client!.Connected) break;
                int bytesRead = await _stream.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken);
                if (bytesRead > 0)
                {
                    string dados = Encoding.ASCII.GetString(buffer, 0, bytesRead);
                    messageBuilder.Append(dados);
                    string mensagemCompleta = messageBuilder.ToString();
                    string[] linhas = mensagemCompleta.Split('\n');
                    for (int i = 0; i < linhas.Length - 1; i++)
                    {
                        string linha = linhas[i].Trim();
                        if (!string.IsNullOrEmpty(linha))
                        {
                            await ProcessarLinha(linha);
                        }
                    }
                    // Mantém a última linha incompleta no buffer
                    messageBuilder.Clear();
                    if (linhas.Length > 0 && !mensagemCompleta.EndsWith('\n'))
                    {
                        messageBuilder.Append(linhas[^1]);
                    }
                }
                else
                {
                    break;
                }
            }
            catch (Exception ex)
            {
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    
                });
                break;
            }
        }
    }

    private async Task ProcessarLinha(string linha)
    {
       

    }


}