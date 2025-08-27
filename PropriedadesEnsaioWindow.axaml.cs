using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using TiltMachine.Models;
using TiltMachine.Services;

namespace TiltMachine;

public partial class PropriedadesEnsaioWindow : Window
{
    public PropriedadesEnsaio _ensaioPropriedades = new PropriedadesEnsaio();
    public event Action? EnsaioSalvo;
    public PropriedadesEnsaioWindow()
    {
        InitializeComponent();
        cmbFormato.SelectionChanged += CmbFormato_SelectionChanged;
        this.DataContext = this;
        
    }

    public PropriedadesEnsaioWindow(PropriedadesEnsaio propriedades) : this()
    {

        if (txtAmostra != null)
        {
            txtAmostra.Text = propriedades.Amostra ?? string.Empty;
            txtAmostra.IsEnabled = false;
        }

        if (txtAmostraNum != null)
        {
            txtAmostraNum.Text = propriedades.AmostraNumero.ToString();
            txtAmostraNum.IsEnabled = false;
        }

        if (txtLocal != null)
        {
            txtLocal.Text = propriedades.Local ?? string.Empty;
            txtLocal.IsEnabled = false;
        }

        if (txtResponsavel != null)
        {
            txtResponsavel.Text = propriedades.Responsavel ?? string.Empty;
            txtResponsavel.IsEnabled = false;
        }

        if (txtTipoRocha != null)
        {
            txtTipoRocha.Text = propriedades.TipoRocha ?? string.Empty;
            txtTipoRocha.IsEnabled = false;
            
        }
        
        if (cmbFormato != null)
        {
            cmbFormato.SelectedItem = EncontrarItemPorTexto(cmbFormato, propriedades.FormatoCorpoProva);
            cmbFormato.IsEnabled = false;
        }

        if (txtAltura != null)
        {
            txtAltura.Text = propriedades.Altura.ToString("F2");
            txtAltura.IsEnabled = false;
        }

        if (txtLargura != null)
        {
            txtLargura.Text = propriedades.Largura.ToString("F2");
            txtLargura.IsEnabled = false;
        }

        if (txtProfundidade != null)
        {
            txtProfundidade.Text = propriedades.Profundidade.ToString("F2");
            txtProfundidade.IsEnabled = false;

        }

        if (txtAreaContato != null)
        {
            txtAreaContato.Text = propriedades.AreaContato.ToString("F2");
            txtAreaContato.IsEnabled = false;
            
        }

        if (txtTaxaInclinacao != null)
        {
            txtTaxaInclinacao.Text = propriedades.TaxaInclinacao.ToString("F2");
            txtTaxaInclinacao.IsEnabled = false;
        }
        //if (txtInclinacaoMaxima != null) txtInclinacaoMaxima.Text = propriedades.InclinacaoMaxima.ToString("F2");
       // if (txtDeslocamentoMaximo != null) txtDeslocamentoMaximo.Text = propriedades.DeslocamentoMaximo.ToString("F2");
       if (txtObservacoes != null)
       {
           txtObservacoes.Text = propriedades.Observacoes ?? string.Empty;
           txtObservacoes.IsEnabled = false;
       }

        if (btnSalvar != null) btnSalvar.IsEnabled = false;
        if (btnEnsaiar != null) btnEnsaiar.IsEnabled = true;
        _ensaioPropriedades = propriedades;
    }
    private ComboBoxItem? EncontrarItemPorTexto(ComboBox combo, string texto)
    {
        if (string.IsNullOrEmpty(texto)) return null;
        
        return combo.Items.OfType<ComboBoxItem>()
            .FirstOrDefault(item => item.Content?.ToString()?.Contains(texto) == true);
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
        txtTaxaInclinacao = this.FindControl<NumericUpDown>("txtTaxaInclinacao");
        //txtInclinacaoMaxima = this.FindControl<TextBox>("txtInclinacaoMaxima");
        //txtDeslocamentoMaximo = this.FindControl<TextBox>("txtDeslocamentoMaximo");
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
                await ShowErrorAsync("Erro", "O campo 'Amostra' é obrigatório.");
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
                TaxaInclinacao = Convert.ToDouble(txtTaxaInclinacao.Value ?? 10),
                //InclinacaoMaxima = ParseDouble(txtInclinacaoMaxima?.Text),
                //DeslocamentoMaximo = ParseDouble(txtDeslocamentoMaximo?.Text),
                Observacoes = txtObservacoes?.Text ?? string.Empty
            };

            _ensaioPropriedades = propriedades;

            // Salvar as propriedades
            await SalvarPropriedades(propriedades);
            await ShowSuccessAsync("Sucesso", "Propriedades salvas com sucesso!");
            EnsaioSalvo?.Invoke();
            
            
        }
        catch (Exception ex)
        {
            await ShowErrorAsync("Erro", $"Erro ao salvar: {ex.Message}");
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
    private async Task ShowMessageAsync(string title, string message, string icon = "ℹ️")
    {
        var messageBox = new Window
        {
            Title = title,
            Width = 400,
            Height = 200,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false,
            Background = new SolidColorBrush(Color.Parse("#F8F9FA")),
            SystemDecorations = SystemDecorations.Full
        };

        // Container principal
        var mainBorder = new Border
        {
            Background = new SolidColorBrush(Colors.White),
            CornerRadius = new CornerRadius(16),
            Margin = new Thickness(15),
            BoxShadow = new BoxShadows(BoxShadow.Parse("0 8 32 0 #20000000"))
        };

        var mainPanel = new StackPanel
        {
            Margin = new Thickness(25),
            Spacing = 20
        };

        // Header com ícone e título
        var headerPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 15,
            HorizontalAlignment = HorizontalAlignment.Center
        };

        // Ícone
        var iconBorder = new Border
        {
            Background = GetIconBackground(icon),
            CornerRadius = new CornerRadius(25),
            Width = 50,
            Height = 50
        };

        var iconText = new TextBlock
        {
            Text = icon,
            FontSize = 24,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };

        iconBorder.Child = iconText;
        headerPanel.Children.Add(iconBorder);

        // Título
        var titleText = new TextBlock
        {
            Text = title,
            FontSize = 18,
            FontWeight = FontWeight.SemiBold,
            Foreground = new SolidColorBrush(Color.Parse("#2C3E50")),
            VerticalAlignment = VerticalAlignment.Center
        };

        headerPanel.Children.Add(titleText);
        mainPanel.Children.Add(headerPanel);

        // Mensagem
        var messageText = new TextBlock
        {
            Text = message,
            TextWrapping = TextWrapping.Wrap,
            HorizontalAlignment = HorizontalAlignment.Center,
            TextAlignment = TextAlignment.Center,
            FontSize = 14,
            Foreground = new SolidColorBrush(Color.Parse("#34495E")),
            LineHeight = 20,
            MaxWidth = 320
        };

        mainPanel.Children.Add(messageText);

        // Botão moderno
        var button = new Button
        {
            Content = "Entendi",
            HorizontalAlignment = HorizontalAlignment.Center,
            Padding = new Thickness(30, 12),
            Background = new SolidColorBrush(Color.Parse("#3498DB")),
            Foreground = new SolidColorBrush(Colors.White),
            FontWeight = FontWeight.SemiBold,
            FontSize = 14,
            CornerRadius = new CornerRadius(8),
            BorderThickness = new Thickness(0),
            Cursor = new Cursor(StandardCursorType.Hand)
        };

        // Efeito hover no botão
        button.PointerEntered += (s, e) => 
        {
            button.Background = new SolidColorBrush(Color.Parse("#2980B9"));
            button.RenderTransform = new ScaleTransform(1.05, 1.05);
        };

        button.PointerExited += (s, e) => 
        {
            button.Background = new SolidColorBrush(Color.Parse("#3498DB"));
            button.RenderTransform = new ScaleTransform(1.0, 1.0);
        };

        button.Click += (s, e) => messageBox.Close();

        mainPanel.Children.Add(button);
        mainBorder.Child = mainPanel;
        messageBox.Content = mainBorder;

        await messageBox.ShowDialog(this);
    }

    // Método auxiliar para cores dos ícones
    private IBrush GetIconBackground(string icon)
    {
        return icon switch
        {
            "✅" or "✔️" => new SolidColorBrush(Color.Parse("#E8F8F5")), // Verde - Sucesso
            "❌" or "❗" or "⚠️" => new SolidColorBrush(Color.Parse("#FFEBEE")), // Vermelho - Erro/Aviso
            "📋" or "📊" => new SolidColorBrush(Color.Parse("#E3F2FD")), // Azul - Informação
            "⏸️" or "🔄" => new SolidColorBrush(Color.Parse("#FFF3E0")), // Laranja - Ação
            _ => new SolidColorBrush(Color.Parse("#F0F4F8")) // Cinza - Padrão
        };
    }

    // Métodos de conveniência para diferentes tipos de mensagem
    private async Task ShowSuccessAsync(string title, string message)
    {
        await ShowMessageAsync(title, message, "✅");
    }

    private async Task ShowErrorAsync(string title, string message)
    {
        await ShowMessageAsync(title, message, "❌");
    }

    private async Task ShowWarningAsync(string title, string message)
    {
        await ShowMessageAsync(title, message, "⚠️");
    }

    private async Task ShowInfoAsync(string title, string message)
    {
        await ShowMessageAsync(title, message, "ℹ️");
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
    private void CmbFormato_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (cmbFormato.SelectedItem is ComboBoxItem selectedItem)
        {
            string formato = selectedItem.Content.ToString();
            if (formato == "Cilíndrico")
            {
                
            }
            
        }
    }
}