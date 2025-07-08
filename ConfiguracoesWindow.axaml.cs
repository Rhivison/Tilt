using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace TiltMachine;

public partial class ConfiguracoesWindow : Window
{
    private string? _imagemSelecionada;

    public ConfiguracoesWindow()
    {
        InitializeComponent();
        this.DataContext = this;
        CarregarConfiguracoes();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    // Carregar configurações salvas
    private void CarregarConfiguracoes()
    {
        // Aqui você pode carregar configurações de um arquivo/banco de dados
        // Por exemplo:
        // txtTaxaAquisicao.Text = ConfigManager.TaxaAquisicao.ToString();
        // cmbInclinacaoUnidade.SelectedIndex = ConfigManager.InclinacaoUnidadeIndex;
    }

    // Evento do botão Calibração
    private async void OnCalibracaoClick(object sender, RoutedEventArgs e)
    {
        try
        {
            // Aqui você pode implementar a lógica de calibração
            await ShowMessageAsync("Calibração", "Iniciando processo de calibração...");
            
            // Exemplo de processo de calibração
            await Task.Delay(2000); // Simula processo de calibração
            
            await ShowMessageAsync("Calibração", "Calibração concluída com sucesso!");
        }
        catch (Exception ex)
        {
            await ShowMessageAsync("Erro", $"Erro durante calibração: {ex.Message}");
        }
    }

    // Evento do botão Salvar
    private async void OnSalvarClick(object sender, RoutedEventArgs e)
    {
        try
        {
            // Validar dados
            if (!ValidarDados())
                return;

            // Criar objeto de configuração
            var configuracoes = new ConfiguracoesEnsaio
            {
                InclinacaoUnidade = cmbInclinacaoUnidade.SelectedItem?.ToString() ?? "°",
                InclinacaoFatorCorrecao = ParseDouble(txtInclinacaoFator.Text),
                DeslocamentoUnidade = cmbDeslocamentoUnidade.SelectedItem?.ToString() ?? "mm",
                DeslocamentoFatorCorrecao = ParseDouble(txtDeslocamentoFator.Text),
                TaxaAquisicao = ParseDouble(txtTaxaAquisicao.Text),
                ImagemRelatorio = _imagemSelecionada
            };

            // Salvar configurações
            await SalvarConfiguracoes(configuracoes);
            
            await ShowMessageAsync("Sucesso", "Configurações salvas com sucesso!");
            
            // Fechar a janela
            this.Close(true);
        }
        catch (Exception ex)
        {
            await ShowMessageAsync("Erro", $"Erro ao salvar configurações: {ex.Message}");
        }
    }

    // Evento do botão Cancelar
    private void OnCancelarClick(object sender, RoutedEventArgs e)
    {
        this.Close(false);
    }

    // Evento do botão Selecionar Imagem
    private async void OnSelecionarImagemClick(object sender, RoutedEventArgs e)
    {
        try
        {
            var topLevel = GetTopLevel(this);
            if (topLevel == null) return;

            var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Selecionar Imagem para Relatório",
                AllowMultiple = false,
                FileTypeFilter = new[]
                {
                    new FilePickerFileType("Imagens")
                    {
                        Patterns = new[] { "*.jpg", "*.jpeg", "*.png", "*.bmp", "*.gif" }
                    }
                }
            });

            if (files.Count >= 1)
            {
                _imagemSelecionada = files[0].Path.LocalPath;
                
                // Atualizar UI para mostrar que imagem foi selecionada
                var border = this.FindControl<Border>("BorderImagem");
                if (border != null)
                {
                    var grid = border.Child as Grid;
                    if (grid != null)
                    {
                        var textBlock = grid.Children[0] as TextBlock;
                        if (textBlock != null)
                        {
                            textBlock.Text = $"Imagem selecionada: {Path.GetFileName(_imagemSelecionada)}";
                            textBlock.FontStyle = Avalonia.Media.FontStyle.Normal;
                            textBlock.Foreground = Avalonia.Media.Brushes.Black;
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            await ShowMessageAsync("Erro", $"Erro ao selecionar imagem: {ex.Message}");
        }
    }

    // Validar dados do formulário
    private bool ValidarDados()
    {
        // Validar taxa de aquisição
        if (!double.TryParse(txtTaxaAquisicao.Text?.Replace(",", "."), out double taxa) || taxa <= 0)
        {
            ShowMessageAsync("Erro", "Taxa de aquisição deve ser um número positivo.").ConfigureAwait(false);
            return false;
        }

        // Validar fatores de correção
        if (ParseDouble(txtInclinacaoFator.Text) <= 0)
        {
            ShowMessageAsync("Erro", "Fator de correção da inclinação deve ser um número positivo.").ConfigureAwait(false);
            return false;
        }

        if (ParseDouble(txtDeslocamentoFator.Text) <= 0)
        {
            ShowMessageAsync("Erro", "Fator de correção do deslocamento deve ser um número positivo.").ConfigureAwait(false);
            return false;
        }

        return true;
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

    // Método para salvar configurações
    private async Task SalvarConfiguracoes(ConfiguracoesEnsaio configuracoes)
    {
        // Simular salvamento assíncrono
        await Task.Delay(100);
        
        // Aqui você pode implementar:
        // - Salvamento em arquivo JSON/XML
        // - Salvamento em banco de dados
        // - Salvamento em configurações do sistema
        // etc.
        
        Console.WriteLine($"Configurações salvas: Taxa={configuracoes.TaxaAquisicao}");
    }

    // Método para mostrar mensagens
    private async Task ShowMessageAsync(string title, string message)
    {
        var messageBox = new Window
        {
            Title = title,
            Width = 400,
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
}

// Classe modelo para representar as configurações
public class ConfiguracoesEnsaio
{
    public string InclinacaoUnidade { get; set; } = "°";
    public double InclinacaoFatorCorrecao { get; set; } = 1.0;
    public string DeslocamentoUnidade { get; set; } = "mm";
    public double DeslocamentoFatorCorrecao { get; set; } = 1.0;
    public double TaxaAquisicao { get; set; } = 1.0;
    public string? ImagemRelatorio { get; set; }
}