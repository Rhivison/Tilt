using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using System;
using System.Threading.Tasks;

namespace TiltMachine;

public partial class PropriedadesEnsaioWindow : Window
{
    public PropriedadesEnsaioWindow()
    {
        InitializeComponent();
        this.DataContext = this;
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    // Evento do botão Salvar
    private async void OnSalvarClick(object sender, RoutedEventArgs e)
    {
        try
        {
            // Validar campos obrigatórios
            if (string.IsNullOrWhiteSpace(txtAmostra?.Text))
            {
                await ShowMessageAsync("Erro", "O campo 'Amostra' é obrigatório.");
                return;
            }

            // Aqui você pode adicionar a lógica para salvar os dados
            var propriedades = new PropriedadesEnsaio
            {
                Amostra = txtAmostra.Text,
                AmostraNumero = int.TryParse(txtAmostraNum.Text, out int num) ? num : 0,
                Local = txtLocal.Text,
                Responsavel = txtResponsavel.Text,
                TipoRocha = txtTipoRocha.Text,
                FormatoCorpoProva = cmbFormato.SelectedItem?.ToString() ?? "Prismático",
                Altura = ParseDouble(txtAltura.Text),
                Largura = ParseDouble(txtLargura.Text),
                Profundidade = ParseDouble(txtProfundidade.Text),
                AreaContato = ParseDouble(txtAreaContato.Text),
                TaxaInclinacao = ParseDouble(txtTaxaInclinacao.Text),
                InclinacaoMaxima = ParseDouble(txtInclinacaoMaxima.Text),
                DeslocamentoMaximo = ParseDouble(txtDeslocamentoMaximo.Text),
                Observacoes = txtObservacoes.Text
            };

            // Exemplo de salvamento (você pode adaptar para sua necessidade)
            await SalvarPropriedades(propriedades);
            
            await ShowMessageAsync("Sucesso", "Propriedades salvas com sucesso!");
            
            // Fechar a janela após salvar
            this.Close(true);
        }
        catch (Exception ex)
        {
            await ShowMessageAsync("Erro", $"Erro ao salvar: {ex.Message}");
        }
    }

    // Evento do botão Cancelar
    private void OnCancelarClick(object sender, RoutedEventArgs e)
    {
        this.Close(false);
    }

    // Método auxiliar para converter string em double
    private double ParseDouble(string value)
    {
        if (string.IsNullOrEmpty(value))
            return 0.0;
        
        // Substituir vírgula por ponto para parsing
        value = value.Replace(",", ".");
        
        return double.TryParse(value, System.Globalization.NumberStyles.Float, 
                              System.Globalization.CultureInfo.InvariantCulture, out double result) ? result : 0.0;
    }

    // Método para mostrar mensagens
    private async Task ShowMessageAsync(string title, string message)
    {
        var messageBox = new Window
        {
            Title = title,
            Width = 350,
            Height = 150,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false
        };

        var panel = new StackPanel
        {
            Margin = new Thickness(20),
            Spacing = 15
        };

        panel.Children.Add(new TextBlock
        {
            Text = message,
            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
        });

        var button = new Button
        {
            Content = "OK",
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            Padding = new Thickness(20, 5)
        };

        button.Click += (s, e) => messageBox.Close();
        panel.Children.Add(button);

        messageBox.Content = panel;
        await messageBox.ShowDialog(this);
    }

    // Método para salvar (você pode implementar conforme sua necessidade)
    private async Task SalvarPropriedades(PropriedadesEnsaio propriedades)
    {
        // Simular salvamento assíncrono
        await Task.Delay(100);
        
        // Aqui você pode implementar:
        // - Salvamento em banco de dados
        // - Salvamento em arquivo
        // - Envio para API
        // etc.
        
        Console.WriteLine($"Propriedades salvas: {propriedades.Amostra}");
    }
}

// Classe modelo para representar as propriedades
public class PropriedadesEnsaio
{
    public string Amostra { get; set; } = string.Empty;
    public int AmostraNumero { get; set; }
    public string Local { get; set; } = string.Empty;
    public string Responsavel { get; set; } = string.Empty;
    public string TipoRocha { get; set; } = string.Empty;
    public string FormatoCorpoProva { get; set; } = "Prismático";
    public double Altura { get; set; }
    public double Largura { get; set; }
    public double Profundidade { get; set; }
    public double AreaContato { get; set; }
    public double TaxaInclinacao { get; set; }
    public double InclinacaoMaxima { get; set; }
    public double DeslocamentoMaximo { get; set; }
    public string Observacoes { get; set; } = string.Empty;
}