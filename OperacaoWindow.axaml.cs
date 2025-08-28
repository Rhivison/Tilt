using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace TiltMachine;

public partial class OperacaoWindow : BaseWindow
{
    
    public OperacaoWindow()
    {
        InitializeComponent();
        InitializeControls();
        AtualizarStatusConexao(App.Arduino.Conectado);
        EnviarComando($"SPEED={10.0:F1}");
    }

    private void InitializeControls()
    {
        // Buscar todos os controles pelo nome
        StatusLabel = this.FindControl<TextBlock>("StatusLabel");
        StatusIndicator = this.FindControl<Ellipse>("StatusIndicator");
        SpeedSlider = this.FindControl<Slider>("SpeedSlider");
        SpeedTextBox = this.FindControl<TextBox>("SpeedTextBox");
        SetSpeedButton = this.FindControl<Button>("SetSpeedButton");
        Speed1Button = this.FindControl<Button>("Speed1Button");
        Speed10Button = this.FindControl<Button>("Speed10Button");
        Speed30Button = this.FindControl<Button>("Speed30Button");
        SpeedPlusButton = this.FindControl<Button>("SpeedPlusButton");
        SpeedMinusButton = this.FindControl<Button>("SpeedMinusButton");
    }
    
    private void Conectar_Click(object? sender, RoutedEventArgs e)
    {
        App.Arduino.ReconnectAsync(); // chama o método que forçamos no ArduinoService
    }

    private void Start1_Click(object? sender, RoutedEventArgs e)
    {
        EnviarComando("START1");
    }

    private void Start2_Click(object? sender, RoutedEventArgs e)
    {
        EnviarComando("START2");
    }

    private void Stop_Click(object? sender, RoutedEventArgs e)
    {
        EnviarComando("STOP");
    }

    private void Manual_Click(object? sender, RoutedEventArgs e)
    {
        EnviarComando("MANUAL");
    }

    private void Reposicionar_Click(object? sender, RoutedEventArgs e)
    {
        EnviarComando("REPOSICIONAR");
    }

    // Event handlers para controle de velocidade
    private void SpeedSlider_ValueChanged(object? sender, RangeBaseValueChangedEventArgs e)
    {
        if (SpeedTextBox != null)
            SpeedTextBox.Text = e.NewValue.ToString("F1");
    }

    private void SpeedTextBox_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
            SetSpeedButton_Click(sender, e);
    }

    private void SetSpeedButton_Click(object? sender, RoutedEventArgs e)
    {
        if (SpeedTextBox != null && double.TryParse(SpeedTextBox.Text, out double speed))
        {
            SetSpeed(speed);
        }
    }

    private void Speed1Button_Click(object? sender, RoutedEventArgs e) => SetSpeed(1);
    private void Speed10Button_Click(object? sender, RoutedEventArgs e) => SetSpeed(10);
    private void Speed30Button_Click(object? sender, RoutedEventArgs e) => SetSpeed(30);
    private void SpeedPlusButton_Click(object? sender, RoutedEventArgs e) => AdjustSpeed(5);
    private void SpeedMinusButton_Click(object? sender, RoutedEventArgs e) => AdjustSpeed(-5);

    // Métodos auxiliares para controle de velocidade
    private void SetSpeed(double speed)
    {
        // Validar range
        speed = Math.Max(0.1, Math.Min(100, speed));
        
        if (SpeedSlider != null)
            SpeedSlider.Value = speed;
        
        if (SpeedTextBox != null)
            SpeedTextBox.Text = speed.ToString("F1");
        
        // Enviar comando para o Arduino
        EnviarComando($"SPEED={speed:F1}");
    }

    private void AdjustSpeed(double increment)
    {
        if (SpeedSlider != null)
        {
            double currentSpeed = SpeedSlider.Value;
            SetSpeed(currentSpeed + increment);
        }
    }
    protected override void AtualizarStatusConexao(bool conectado)
    {
        base.AtualizarStatusConexao(conectado);

        if (SpeedSlider != null) SpeedSlider.IsEnabled = conectado;
        if (SpeedTextBox != null) SpeedTextBox.IsEnabled = conectado;
        if (SetSpeedButton != null) SetSpeedButton.IsEnabled = conectado;
        if (Speed1Button != null) Speed1Button.IsEnabled = conectado;
        if (Speed10Button != null) Speed10Button.IsEnabled = conectado;
        if (Speed30Button != null) Speed30Button.IsEnabled = conectado;
        if (SpeedPlusButton != null) SpeedPlusButton.IsEnabled = conectado;
        if (SpeedMinusButton != null) SpeedMinusButton.IsEnabled = conectado;
        UpdateConnectionStatus(conectado);
    }
    // Método para habilitar/desabilitar controles baseado na conexão
    public void UpdateConnectionStatus(bool isConnected)
    {
        if (StatusLabel != null)
        {
            StatusLabel.Text = isConnected ? "Conectado" : "Desconectado";
            StatusLabel.Foreground = isConnected ? 
                Avalonia.Media.Brushes.Green : 
                Avalonia.Media.Brushes.Red;
        }

        if (StatusIndicator != null)
        {
            StatusIndicator.Fill = isConnected ? 
                Avalonia.Media.Brushes.Green : 
                Avalonia.Media.Brushes.Red;
        }

        // Habilitar/desabilitar controles de velocidade
        if (SpeedSlider != null) SpeedSlider.IsEnabled = isConnected;
        if (SpeedTextBox != null) SpeedTextBox.IsEnabled = isConnected;
        if (SetSpeedButton != null) SetSpeedButton.IsEnabled = isConnected;
        if (Speed1Button != null) Speed1Button.IsEnabled = isConnected;
        if (Speed10Button != null) Speed10Button.IsEnabled = isConnected;
        if (Speed30Button != null) Speed30Button.IsEnabled = isConnected;
        if (SpeedPlusButton != null) SpeedPlusButton.IsEnabled = isConnected;
        if (SpeedMinusButton != null) SpeedMinusButton.IsEnabled = isConnected;
    }

    
}