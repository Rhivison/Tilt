using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.VisualTree;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using LiveChartsCore.SkiaSharpView.SKCharts;
using Microsoft.Data.Sqlite;
using QuestPDF.Fluent;
using SkiaSharp;
using TiltMachine.Models;
using TiltMachine.Services;
using Colors = QuestPDF.Helpers.Colors;

namespace TiltMachine;

public partial class EnsaioGraficoWindow : BaseWindow, INotifyPropertyChanged
{
    private PropriedadesEnsaio _ensaio;
    private Axis[] _xAxes;
    private double? lastAngle = 0;
    private double? _anguloFinal;
    private CoeficienteCalibracao _coeficienteCalibracao;
    DatabaseService _service = new DatabaseService();
    private List<DadoEnsaio> _dadosEnsaio;
    public event Action? EnsaioSalvo;
    
    private bool _isBtnPararEnabled;
    public bool IsBtnPararEnabled
    {
        get => _isBtnPararEnabled;
        set
        {
            _isBtnPararEnabled = value;
            OnPropertyChanged();
        }
    }

    private bool _isBtnExportarEnabled;
    public bool IsBtnExportarEnabled
    {
        get => _isBtnExportarEnabled;
        set
        {
            _isBtnExportarEnabled = value;
            OnPropertyChanged();
        }
    }

    private bool _isBtnReposicionarEnabled;
    public bool IsBtnReposicionarEnabled
    {
        get => _isBtnReposicionarEnabled;
        set
        {
            _isBtnReposicionarEnabled = value;
            OnPropertyChanged();
        }
    }
    public double? AnguloFinal
    {
        get => _anguloFinal;
        set
        {
            _anguloFinal = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CoeficienteAtrito)); // Atualiza junto
        }
    }

    public string CoeficienteAtrito =>
        AnguloFinal.HasValue
            ? $"{Math.Tan(AnguloFinal.Value * Math.PI / 180.0):F2}"
            : "-";
    public Axis[] XAxes
    {
        get => _xAxes;
        set
        {
            _xAxes = value;
            OnPropertyChanged();
        }
    }
    private Axis[] _yAxes;
    public Axis[] YAxes
    {
        get => _yAxes;
        set
        {
            _yAxes = value;
            OnPropertyChanged();
        }
    }
    public PropriedadesEnsaio Ensaio
    {
        get => _ensaio;
        set
        {
            _ensaio = value;
            OnPropertyChanged();
        }
    }

    private IEnumerable<ISeries> _chartSeries;
    public IEnumerable<ISeries> ChartSeries
    {
        get => _chartSeries;
        set
        {
            _chartSeries = value;
            OnPropertyChanged();
        }
    }

    private void ObterCoeficienteAtivo(string ip)
    {
        _coeficienteCalibracao = _service.ObterCoeficienteAtivo(ip);

    }

    public async Task<double> AplicarCalibração(double leituraSensor, CoeficienteCalibracao coeficienteCalibracao)
    {
        if (coeficienteCalibracao == null)
            await ShowMessageAsync("Erro", "Erro ao calcular resultado do ensaio, execute calibração!");
        var x = coeficienteCalibracao.CoeficienteA * Math.Pow(leituraSensor, 2) 
                + coeficienteCalibracao.CoeficienteB * leituraSensor 
                + coeficienteCalibracao.CoeficienteC;
        return coeficienteCalibracao.CoeficienteA * Math.Pow(leituraSensor, 2) 
               + coeficienteCalibracao.CoeficienteB * leituraSensor 
               + coeficienteCalibracao.CoeficienteC;
    }
    public EnsaioGraficoWindow()
    {
        InitializeComponent();
        DataContext = this;
        XAxes = new Axis[]
        {
            new Axis
            {
                Name = "Tempo (s)",
                LabelsRotation = 0,
                MinLimit = 0,
                TextSize = 14,
                UnitWidth = 1
            }
        };

        YAxes = new Axis[]
        {
            new Axis
            {
                Name = "Ângulo (°)",
                TextSize = 14,
                MinLimit = null, // Pode definir limite mínimo, ex: 0
                MaxLimit = null, // Pode definir limite máximo, ex: 90
                SeparatorsPaint = new SolidColorPaint(SKColors.LightGray)
            }
        };


        //Ensaio = GerarExemplo();
        //ChartSeries = GerarGraficoExemplo();
        ChartSeries = CriarGraficoVazio();

    }
    
    public EnsaioGraficoWindow(PropriedadesEnsaio _propriedades) : this()
    {
        Ensaio = _propriedades;
        _ensaio = _propriedades;
        OnPropertyChanged(nameof(Ensaio));
    
        if (App.Arduino != null && App.Arduino.IpConectado != null)
        {   
            ObterCoeficienteAtivo(App.Arduino.IpConectado);
            App.Arduino.EnsaioFinalizado += OnEnsaioFinalizado;
        }

        // Use as propriedades em vez de acessar os controles diretamente
        IsBtnPararEnabled = false;
        IsBtnExportarEnabled = false;
        IsBtnReposicionarEnabled = false;
    }
    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
    private IEnumerable<ISeries> CriarGraficoVazio()
    {
        return new ISeries[]
        {
            new LineSeries<ObservablePoint>
            {
                Values = new List<ObservablePoint> { new(0, 0) },
                Name = "Inclinômetro",
                Stroke = new SolidColorPaint(SKColors.Gray, 2),
                GeometrySize = 5,
                GeometryStroke = new SolidColorPaint(SKColors.Gray),
                GeometryFill = new SolidColorPaint(SKColors.Gray)
            }
        };
    }
    
    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    private void OnIniciarEnsaioClick(object sender, RoutedEventArgs e)
    {
        try
        {
            string command = $"ENSAIO={Ensaio.TaxaInclinacao:F1}";
            EnviarComando(command);
            IsBtnPararEnabled = true;
            IsBtnReposicionarEnabled = true;
        }
        catch (Exception exception)
        {
            ShowErrorAsync("Erro", exception.Message);
        }
    }
    private void OnEnsaioFinalizado(List<DadoEnsaio> dados)
    {
        try
        {   
            if (dados == null || dados.Count == 0)
            {   
                
                ShowErrorAsync("ERRO", "Nenhum ponto coletado durante o ensaio!");
                return;
            }

            _dadosEnsaio = new List<DadoEnsaio>();
            _dadosEnsaio = dados;
            var pontos = ConverterDadosParaGrafico(dados);
    
            ChartSeries = new ISeries[]
            {
                new LineSeries<ObservablePoint>
                {
                    Values = pontos,
                    Name = "Inclinômetro",
                    Stroke = new SolidColorPaint(SKColors.Red, 2),
                    GeometrySize = 5,
                    GeometryStroke = new SolidColorPaint(SKColors.Red),
                    GeometryFill = new SolidColorPaint(SKColors.Red)
                }
            };
    
            // Debug - mostra informações do ensaio
            if (pontos.Count > 0)
            {
                var tempoTotal = pontos.Last().X;
                var anguloInicial = pontos.First().Y;
                var anguloFinal = pontos.Last().Y;
                //var variacaoAngulo = Math.Abs(anguloFinal - anguloInicial);
        
                Console.WriteLine($"=== RESUMO DO ENSAIO ===");
                Console.WriteLine($"Pontos coletados: {pontos.Count}");
                Console.WriteLine($"Duração total: {tempoTotal:F1} segundos");
                Console.WriteLine($"Ângulo inicial: {anguloInicial:F2}°");
                Console.WriteLine($"Ângulo final: {anguloFinal:F2}°");
                //Console.WriteLine($"Variação total: {variacaoAngulo:F2}°");
                //Console.WriteLine($"Taxa média: {(variacaoAngulo / tempoTotal):F3}°/s");
                Console.WriteLine($"========================");
                lastAngle = anguloFinal;
                AnguloFinal = anguloFinal;
            }
            IsBtnExportarEnabled = true;
            _ensaio.EnsaioRealizado = "Sim";
            _ensaio.DataEnsaio = DateTime.Today;
            _service.Atualizar(_ensaio.Id, _ensaio);
            EnsaioSalvo?.Invoke();
        }
        catch (Exception ex)
        {
            ShowErrorAsync("ERRO", ex.Message);
        }
        

        // Converte milissegundos para segundos relativos ao início do ensaio
        
        
    }

    public async void ExportarCsv()
    {
        if (_dadosEnsaio == null || _dadosEnsaio.Count == 0)
        {
            await ShowErrorAsync("Erro", "Não há dados do ensaio para exportar.");
            return;
        }

        var dlg = new SaveFileDialog
        {
            Title = "Salvar dados em CSV",
            DefaultExtension = "csv",
            InitialFileName = $"Ensaio_{Ensaio.Amostra}_{Ensaio.AmostraNumero}.csv",
            Filters = new List<FileDialogFilter>
            {
                new FileDialogFilter { Name = "CSV", Extensions = { "csv" } }
            }
        };

        var filePath = await dlg.ShowAsync(this);

        if (string.IsNullOrWhiteSpace(filePath))
            return; // usuário cancelou

        try
        {
            using (var writer = new System.IO.StreamWriter(filePath, false, System.Text.Encoding.UTF8))
            {
                // Cabeçalho com informações do ensaio
                writer.WriteLine("=== RELATÓRIO DE ENSAIO ===");
                writer.WriteLine($"Amostra:,{Ensaio.Amostra}");
                writer.WriteLine($"Número da Amostra:,{Ensaio.AmostraNumero}");
                writer.WriteLine($"Local:,{Ensaio.Local}");
                writer.WriteLine($"Responsável:,{Ensaio.Responsavel}");
                writer.WriteLine($"Data do Ensaio:,{Ensaio.DataEnsaio:dd/MM/yyyy HH:mm}");
                writer.WriteLine($"Tipo de Rocha:,{Ensaio.TipoRocha}");
                writer.WriteLine($"Formato:,{Ensaio.FormatoCorpoProva}");
                writer.WriteLine($"Dimensões (A x L x P):,{Ensaio.Altura} x {Ensaio.Largura} x {Ensaio.Profundidade}");
                writer.WriteLine($"Área de Contato:,{Ensaio.AreaContato}");
                writer.WriteLine($"Taxa de Inclinação:,{Ensaio.TaxaInclinacao} °/min");
                writer.WriteLine($"Ângulo Final:,{AnguloFinal:F2}°");
                writer.WriteLine($"Coeficiente de Atrito:,{CoeficienteAtrito}");
                writer.WriteLine($"Observações:,\"{Ensaio.Observacoes?.Replace("\"", "\"\"")}\"");
                writer.WriteLine(); // Linha em branco
                
                // Cabeçalho dos dados
                writer.WriteLine("=== DADOS DO ENSAIO ===");
                writer.WriteLine("Tempo (s),Ângulo (°),Leitura Sensor,Tempo Absoluto (ms)");
                
                // Ordena dados por tempo
                var dadosOrdenados = _dadosEnsaio.OrderBy(d => d.Tempo).ToList();
                double tempoInicial = dadosOrdenados.First().Tempo;
                
                // Dados do ensaio
                foreach (var dado in dadosOrdenados)
                {
                    double tempoSegundos = (dado.Tempo - tempoInicial) / 1000.0;
                    double anguloCalibrado = await AplicarCalibração(dado.Angulo, _coeficienteCalibracao);
                    
                    writer.WriteLine($"{tempoSegundos:F3},{anguloCalibrado:F4},{dado.Angulo:F4},{dado.Tempo}");
                }
            }
            
            await ShowSuccessAsync("Sucesso", "Arquivo CSV exportado com sucesso!");
        }
        catch (Exception ex)
        {
            await ShowErrorAsync("Erro ao exportar CSV", ex.Message);
        }
    }

    private void OnExportarCsvClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            ExportarCsv();
        }
        catch (Exception exception)
        {
            ShowErrorAsync("Erro ao exportar CSV", exception.Message);
        }
    }
    private List<ObservablePoint> ConverterDadosParaGrafico(List<DadoEnsaio> dados)
    {
        if (dados.Count == 0) return new List<ObservablePoint>();
        
        // Ordena dados por tempo (caso chegem fora de ordem)
        var dadosOrdenados = dados.OrderBy(d => d.Tempo).ToList();
        
        // Pega o tempo inicial (primeiro ponto)
        double tempoInicial = dadosOrdenados.First().Tempo;
        
        // Converte para pontos do gráfico usando cultura invariante
        var cultura = System.Globalization.CultureInfo.InvariantCulture;
        var pontos = dadosOrdenados.Select(d => new ObservablePoint(
            (d.Tempo - tempoInicial) / 1000.0,  // Converte para segundos relativos
            (AplicarCalibração(d.Angulo, _coeficienteCalibracao).Result)                  // Inverte ângulo como estava fazendo
        )).ToList();
        
        // Log dos primeiros e últimos pontos para debug
        if (pontos.Count > 0)
        {
            Console.WriteLine($"Primeiro ponto: {pontos.First().X:F1}s, {pontos.First().Y:F2}°");
            if (pontos.Count > 1)
            {
                Console.WriteLine($"Último ponto: {pontos.Last().X:F1}s, {pontos.Last().Y:F2}°");
            }
        }
        
        return pontos;
    }
    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);
        App.Arduino.EnsaioFinalizado -= OnEnsaioFinalizado;
    }
    private byte[] ExportarGraficoComoImagem()
    {
        var chart = new SKCartesianChart
        {
            Series = ChartSeries.ToArray(),
            XAxes = XAxes,
            YAxes = YAxes,
            LegendPosition = LiveChartsCore.Measure.LegendPosition.Bottom
        };
        using var image = chart.GetImage(); // SKImage
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }
    public async void ExportarPdf()
    {   
        var dlg = new SaveFileDialog
        {
            Title = "Salvar relatório em PDF",
            DefaultExtension = "pdf",
            InitialFileName = "RelatorioEnsaio.pdf",
            Filters = new List<FileDialogFilter>
            {
                new FileDialogFilter { Name = "PDF", Extensions = { "pdf" } }
            }
        };

        
        var filePath = await dlg.ShowAsync(this);

        if (string.IsNullOrWhiteSpace(filePath))
            return; // usuário cancelou

        var imgBytes = ExportarGraficoComoImagem();
        var ensaio = Ensaio;

        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(40);

                page.Header().Text("Relatório de Ensaio")
                    .SemiBold().FontSize(20).FontColor(Colors.Blue.Medium);

                page.Content().Column(col =>
                {
                    col.Item().Text($"Amostra: {ensaio.Amostra} (Nº {ensaio.AmostraNumero})");
                    col.Item().Text($"Local: {ensaio.Local}");
                    col.Item().Text($"Responsável: {ensaio.Responsavel}");
                    col.Item().Text($"Data: {ensaio.DataEnsaio:g}");
                    col.Item().Text($"Tipo de Rocha: {ensaio.TipoRocha}");
                    col.Item().Text($"Formato: {ensaio.FormatoCorpoProva}");
                    col.Item().Text($"Dimensões: {ensaio.Altura}x{ensaio.Largura}x{ensaio.Profundidade}");
                    col.Item().Text($"Área de Contato: {ensaio.AreaContato}");
                    col.Item().PaddingVertical(20).Element(e =>
                    {
                        e.Image(imgBytes);
                    });
                    col.Item().Text($"Velocidade: {ensaio.TaxaInclinacao} °/min");
                    col.Item().Text($"Ângulo de Inclinação final: {lastAngle} ");
                    col.Item().Text(
                        lastAngle.HasValue
                            ? $"Coeficiente de atrito calculado: {Math.Tan(lastAngle.Value * Math.PI / 180.0):F2}"
                            : "Coeficiente de atrito não calculado (sem ângulo)."
                    );
                    col.Item().Text("Observações:")
                        .Bold().FontSize(14);
                    col.Item().Text(ensaio.Observacoes);
                });

                page.Footer()
                    .AlignCenter()
                    .Text(x =>
                    {
                        x.Span("Página ");
                        x.CurrentPageNumber();
                        x.Span(" | Relatório deve ser avaliado por um profissional devidamente qualificado.");
                    });
            });
        }).GeneratePdf(filePath);
    }
    private void OnExportarPdfClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        try
        {
            ExportarPdf();
            Console.WriteLine("PDF gerado com sucesso!");
        }
        catch (Exception exception)
        {
            ShowErrorAsync("Erro ao gerar pdf", exception.Message);
        }
    }
    private void OnReposicionarClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        try
        {
            EnviarComando("REPOSICIONAR");
        }
        catch (Exception ex)
        {
            ShowMessageAsync("Erro", ex.Message);
        }
    }
    private void OnPararEnsaioClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        try
        {
            EnviarComando("STOP");
            Console.WriteLine("Ensaio parado pelo usuário.");
        }
        catch (Exception exception)
        {
            ShowErrorAsync("Erro", exception.Message);
        }
    }
    
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
    
    private void OnSairEnsaioClick(object sender, RoutedEventArgs e)
    {
        this.Close(false);
    }
}
