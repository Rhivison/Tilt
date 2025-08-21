using System;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using TiltMachine;

public abstract class BaseWindow : Window
{
    protected TextBlock? StatusLabel;
    private bool _eventosInscritos = false;
    
    public BaseWindow()
    {
        // Não inscrever eventos no construtor - fazer depois que a janela carregar
        this.Opened += BaseWindow_Opened;
    }
    
    private void BaseWindow_Opened(object? sender, EventArgs e)
    {
        // Agora que a janela está aberta, inscrever nos eventos do Arduino
        InscreverEventosArduino();
    }
    
    private void InscreverEventosArduino()
    {
        if (App.Arduino != null && !_eventosInscritos)
        {
            App.Arduino.StatusConexaoAlterado += OnStatusConexaoAlterado;
            App.Arduino.LinhaRecebida += OnLinhaRecebida;
            _eventosInscritos = true;
            
            // Atualizar status inicial se possível
            AtualizarStatusConexaoInicial();
        }
    }
    
    private void AtualizarStatusConexaoInicial()
    {
        // Verifica se há uma propriedade para obter o status atual
        // Você pode implementar isso no ArduinoService se necessário
        try
        {
            // Exemplo: AtualizarStatusConexao(App.Arduino.EstaConectado);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao obter status inicial: {ex.Message}");
        }
    }
    
    private void OnStatusConexaoAlterado(bool conectado)
    {
        Dispatcher.UIThread.Post(() =>
        {
            AtualizarStatusConexao(conectado);
        });
    }
    
    private void OnLinhaRecebida(string linha)
    {
        Dispatcher.UIThread.Post(() =>
        {
            ProcessarLinhaRecebida(linha);
        });
    }
    
    protected virtual void AtualizarStatusConexao(bool conectado)
    {
        if (StatusLabel != null)
        {
            StatusLabel.Text = conectado ? "Conectado" : "Desconectado";
            StatusLabel.Foreground = conectado ? Brushes.Green : Brushes.Red;
        }
    }
    
    protected virtual void ProcessarLinhaRecebida(string linha)
    {
        // Implementação padrão - pode ser sobrescrita nas classes filhas
        Console.WriteLine($"Linha recebida: {linha}");
    }
    
    protected async void EnviarComando(string comando)
    {
        if (App.Arduino != null)
        {
            await App.Arduino.EnviarComandoAsync(comando);
        }
        else
        {
            Console.WriteLine("Arduino não disponível para envio de comando");
        }
    }
    
    protected override void OnClosed(EventArgs e)
    {
        // Desinscrever dos eventos apenas se foram inscritos
        if (App.Arduino != null && _eventosInscritos)
        {
            App.Arduino.StatusConexaoAlterado -= OnStatusConexaoAlterado;
            App.Arduino.LinhaRecebida -= OnLinhaRecebida;
            _eventosInscritos = false;
        }
        
        this.Opened -= BaseWindow_Opened;
        base.OnClosed(e);
    }
}