using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using System;
using System.Threading.Tasks;
using TiltMachine.Models;
using TiltMachine.Services;
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
        txtAmostra = this.FindControl<TextBox>("txtAmostra");
        txtAmostraNum = this.FindControl<TextBox>("txtAmostraNum");
        txtLocal = this.FindControl<TextBox>("txtLocal");
        txtResponsavel = this.FindControl<TextBox>("txtResponsavel");
        txtTipoRocha = this.FindControl<TextBox>("txtTipoRocha");
        cmbFormato = this.FindControl<ComboBox>("cmbFormato");
        txtAltura = this.FindControl<TextBox>("txtAltura");
        txtLargura = this.FindControl<TextBox>("txtLargura");
        txtProfundidade = this.FindControl<TextBox>("txtProfundidade");
        txtAreaContato = this.FindControl<TextBox>("txtAreaContato");
        txtTaxaInclinacao = this.FindControl<TextBox>("txtTaxaInclinacao");
        txtInclinacaoMaxima = this.FindControl<TextBox>("txtInclinacaoMaxima");
        txtDeslocamentoMaximo = this.FindControl<TextBox>("txtDeslocamentoMaximo");
        txtObservacoes = this.FindControl<TextBox>("txtObservacoes");
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
            var itemSelecionado = cmbFormato.SelectedItem as ComboBoxItem;
            var formatoTexto = itemSelecionado?.Content?.ToString() ?? "Prismático";
            // Aqui você pode adicionar a lógica para salvar os dados
            var propriedades = new PropriedadesEnsaio
            {
                Amostra = txtAmostra.Text,
                AmostraNumero = int.TryParse(txtAmostraNum.Text, out int num) ? num : 0,
                Local = txtLocal.Text,
                Responsavel = txtResponsavel.Text,
                TipoRocha = txtTipoRocha.Text,
                FormatoCorpoProva = formatoTexto,
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
        try
        {
            var db = new DatabaseService();
            db.Inicializar();
            db.Inserir(propriedades);
        }
        catch (Exception ex)
        {
            await ShowMessageAsync("Erro ao salvar no banco", ex.Message);
        }
        
        Console.WriteLine($"Propriedades salvas: {propriedades.Amostra}");
    }
}
