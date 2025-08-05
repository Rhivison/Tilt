using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using TiltMachine.Services;
namespace TiltMachine
{   
    public partial class MainWindow : Window
    {   
        private ArduinoService _arduinoService;
        public MainWindow()
        {
            InitializeComponent();
            App.Arduino.StatusConexaoAlterado += conectado =>
            {
                Dispatcher.UIThread.Post(() =>
                {
                    StatusLabel.Text = conectado ? "Conectado" : "Desconectado";
                    StatusLabel.Foreground = conectado ? Brushes.Green : Brushes.Red;
                });
            };
            
        }

        private void AmostraButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var ensaiosWindow = new EnsaiosWindow();
                ensaiosWindow.Show();
            }
            catch (Exception ex)
            {
                // Aqui você pode logar o erro para entender o que está acontecendo
                Console.WriteLine("Erro ao abrir EnsaiosWindow: " + ex);
                // Ou usar MessageBox para exibir ao usuário
                // Avalonia.Controls.MessageBox.Avalonia.MessageBoxManager.GetMessageBoxStandardWindow("Erro", ex.Message).Show();
            }
        }

        private void EnsaiosButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var propriedadeEnsaiosWindow = new PropriedadesEnsaioWindow();
                propriedadeEnsaiosWindow.Show();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{ex.Message}");
            }
        }

        private void OperacaoButton_OnClick(object? sender, RoutedEventArgs e)
        {
            var operacaoWindow = new OperacaoWindow();
            operacaoWindow.Show();
        }
    }
}