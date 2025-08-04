using System;
using Avalonia.Controls;
using Avalonia.Threading;
using Avalonia.Media;
using TiltMachine;

public abstract class BaseWindow : Window
{
    protected TextBlock? StatusLabel;
    
    public BaseWindow()
    {
        // Inscrever nos eventos do Arduino
        App.Arduino.StatusConexaoAlterado += OnStatusConexaoAlterado;
        App.Arduino.LinhaRecebida += OnLinhaRecebida;
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
        await App.Arduino.EnviarComandoAsync(comando);
    }
    
    protected override void OnClosed(EventArgs e)
    {
        // Desinscrever dos eventos
        App.Arduino.StatusConexaoAlterado -= OnStatusConexaoAlterado;
        App.Arduino.LinhaRecebida -= OnLinhaRecebida;
        base.OnClosed(e);
    }
}