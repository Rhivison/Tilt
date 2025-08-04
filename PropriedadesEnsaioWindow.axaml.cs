using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using System;
using System.Linq;
using System.Threading.Tasks;
using TiltMachine.Models;
using TiltMachine.Services;

namespace TiltMachine;

public partial class PropriedadesEnsaioWindow : Window
{
    public PropriedadesEnsaio _ensaioPropriedades = new PropriedadesEnsaio();
    public PropriedadesEnsaioWindow()
    {
        InitializeComponent();
        this.DataContext = this;
        
    }

    public PropriedadesEnsaioWindow(PropriedadesEnsaio propriedades) : this()
    {   
       
        if (txtAmostra != null) txtAmostra.Text = propriedades.Amostra ?? string.Empty;
        if (txtAmostraNum != null) txtAmostraNum.Text = propriedades.AmostraNumero.ToString();
        if (txtLocal != null) txtLocal.Text = propriedades.Local ?? string.Empty;
        if (txtResponsavel != null) txtResponsavel.Text = propriedades.Responsavel ?? string.Empty;
        if (txtTipoRocha != null) txtTipoRocha.Text = propriedades.TipoRocha ?? string.Empty;
        
        if (cmbFormato != null)
        {
            cmbFormato.SelectedItem = EncontrarItemPorTexto(cmbFormato, propriedades.FormatoCorpoProva);
        }
        
        if (txtAltura != null) txtAltura.Text = propriedades.Altura.ToString("F2");
        if (txtLargura != null) txtLargura.Text = propriedades.Largura.ToString("F2");
        if (txtProfundidade != null) txtProfundidade.Text = propriedades.Profundidade.ToString("F2");
        if (txtAreaContato != null) txtAreaContato.Text = propriedades.AreaContato.ToString("F2");
        if (txtTaxaInclinacao != null) txtTaxaInclinacao.Text = propriedades.TaxaInclinacao.ToString("F2");
        if (txtInclinacaoMaxima != null) txtInclinacaoMaxima.Text = propriedades.InclinacaoMaxima.ToString("F2");
        if (txtDeslocamentoMaximo != null) txtDeslocamentoMaximo.Text = propriedades.DeslocamentoMaximo.ToString("F2");
        if (txtObservacoes != null) txtObservacoes.Text = propriedades.Observacoes ?? string.Empty;

        if (btnSalvar != null) btnSalvar.IsEnabled = false;
        if (btnEnsaiar != null) btnEnsaiar.IsEnabled = true;
    }
    
    private ComboBoxItem? EncontrarItemPorTexto(ComboBox combo, string texto)
    {
        if (string.IsNullOrEmpty(texto)) return null;
        
        return combo.Items.OfType<ComboBoxItem>()
            .FirstOrDefault(item => item.Content?.ToString() == texto);
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
        
        // Inicializar todos os controles
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
        btnSalvar = this.FindControl<Button>("btnSalvar");
        btnCancelar = this.FindControl<Button>("btnCancelar");
        btnEnsaiar = this.FindControl<Button>("btnEnsaiar");
        if (btnEnsaiar != null) btnEnsaiar.IsEnabled = false;
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
            
            var itemSelecionado = cmbFormato?.SelectedItem as ComboBoxItem;
            var formatoTexto = itemSelecionado?.Content?.ToString() ?? "Prismático";
            
            // Criar o objeto com os dados do formulário
            var propriedades = new PropriedadesEnsaio
            {
                Amostra = txtAmostra?.Text ?? string.Empty,
                AmostraNumero = int.TryParse(txtAmostraNum?.Text, out int num) ? num : 0,
                Local = txtLocal?.Text ?? string.Empty,
                Responsavel = txtResponsavel?.Text ?? string.Empty,
                TipoRocha = txtTipoRocha?.Text ?? string.Empty,
                FormatoCorpoProva = formatoTexto,
                Altura = ParseDouble(txtAltura?.Text),
                Largura = ParseDouble(txtLargura?.Text),
                Profundidade = ParseDouble(txtProfundidade?.Text),
                AreaContato = ParseDouble(txtAreaContato?.Text),
                TaxaInclinacao = ParseDouble(txtTaxaInclinacao?.Text),
                InclinacaoMaxima = ParseDouble(txtInclinacaoMaxima?.Text),
                DeslocamentoMaximo = ParseDouble(txtDeslocamentoMaximo?.Text),
                Observacoes = txtObservacoes?.Text ?? string.Empty
            };

            _ensaioPropriedades = propriedades;

            // Salvar as propriedades
            await SalvarPropriedades(propriedades);
            
            await ShowMessageAsync("Sucesso", "Propriedades salvas com sucesso!");
            
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

    // Método para salvar as propriedades
    private async Task SalvarPropriedades(PropriedadesEnsaio propriedades)
    {
        try
        {
            var db = new DatabaseService();
            db.Inicializar();
            db.Inserir(propriedades);
            btnEnsaiar.IsEnabled = true;
        }
        catch (Exception ex)
        {
            await ShowMessageAsync("Erro ao salvar no banco", ex.Message);
        }
        
        Console.WriteLine($"Propriedades salvas: {propriedades.Amostra}");
    }

    private void OnEnsaiarClick(object sender, RoutedEventArgs e)
    {   
        var janela = new EnsaioGraficoWindow(_ensaioPropriedades);
        janela.Show();
    }
}