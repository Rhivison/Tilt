using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using TiltMachine.Models;
using TiltMachine.Services;

namespace TiltMachine
{
    public partial class EnsaiosWindow : Window, INotifyPropertyChanged
    {   
        private readonly DatabaseService _db = new();

        public ObservableCollection<PropriedadesEnsaio> Ensaios { get; set; } = new();
        private ObservableCollection<PropriedadesEnsaio> _todosEnsaios = new();
        private PropriedadesEnsaio? _ensaioSelecionado;
        public PropriedadesEnsaio? EnsaioSelecionado
        {
            get => _ensaioSelecionado;
            set
            {
                _ensaioSelecionado = value;
                OnPropertyChanged();
            }
        }
        private string _termoBusca = "";
        public string TermoBusca
        {
            get => _termoBusca;
            set
            {
                _termoBusca = value;
                OnPropertyChanged();
                // Só filtra se já tiver dados carregados
                if (_todosEnsaios.Count > 0)
                {
                    FiltrarEnsaios();
                }
            }
        }


        public EnsaiosWindow()
        {
            InitializeComponent();
            _db.Inicializar();
            DataContext = this;
            CarregarEnsaios();
        }
        
        
        private void FiltrarEnsaios()
        {
            Ensaios.Clear();

            foreach (var ensaio in _todosEnsaios)
            {
                // Se o termo de busca estiver vazio OU se a amostra contém o termo
                if (string.IsNullOrWhiteSpace(TermoBusca) ||
                    ensaio.Amostra.Contains(TermoBusca, StringComparison.OrdinalIgnoreCase) || ensaio.Local.Contains(TermoBusca, StringComparison.OrdinalIgnoreCase) ||
                    ensaio.Responsavel.Contains(TermoBusca, StringComparison.OrdinalIgnoreCase))
                {
                    Ensaios.Add(ensaio);
                }
            }
        }
        
        private void BuscarButton_Click(object sender, RoutedEventArgs e)
        {
            FiltrarEnsaios();
        }
        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        private void NovoEnsaioButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var propriedadeEnsaioWindow = new PropriedadesEnsaioWindow();
                propriedadeEnsaioWindow.Show();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private void AbrirEnsaioButton_Click(object sender, RoutedEventArgs e)
        {
            if (EnsaioSelecionado == null)
            {
                Console.WriteLine("Nenhum ensaio selecionado.");
                return;
            }
            var propriedades = new PropriedadesEnsaio
            {
                Amostra = EnsaioSelecionado.Amostra,
                AmostraNumero = EnsaioSelecionado.AmostraNumero,
                Local = EnsaioSelecionado.Local,
                Responsavel = EnsaioSelecionado.Responsavel,
                FormatoCorpoProva = EnsaioSelecionado.FormatoCorpoProva,
                DataEnsaio = EnsaioSelecionado.DataEnsaio,
                TipoRocha = EnsaioSelecionado.TipoRocha,
                Altura = EnsaioSelecionado.Altura,
                Largura = EnsaioSelecionado.Largura,
                Profundidade = EnsaioSelecionado.Profundidade,
                AreaContato = EnsaioSelecionado.AreaContato,
                TaxaInclinacao =  EnsaioSelecionado.TaxaInclinacao,
                //InclinacaoMaxima = EnsaioSelecionado.InclinacaoMaxima,
                //DeslocamentoMaximo = EnsaioSelecionado.DeslocamentoMaximo,
                Observacoes = EnsaioSelecionado.Observacoes
               
            };

            var janela = new PropriedadesEnsaioWindow(propriedades);
            janela.Show();
        }

        private async void DeletarEnsaioButton_Click(object sender, RoutedEventArgs e)
        {
            if (EnsaioSelecionado == null)
            {
                await MostrarMensagemAsync("Atenção", "Selecione um ensaio para deletar.");
                return;
            }
            var confirmacao = await MostrarConfirmacaoAsync("Confirmação", $"Deseja deletar o ensaio '{EnsaioSelecionado.Amostra}'?");
            if (!confirmacao)
                return;
            try
            {
                var db = new DatabaseService();
                db.Inicializar();
                db.Deletar(EnsaioSelecionado.Id);

                // Recarregar os dados do banco após deletar
                CarregarEnsaios(); // Isso já vai aplicar o filtro atual
                EnsaioSelecionado = null;

                await MostrarMensagemAsync("Sucesso", "Ensaio deletado com sucesso!");
            }
            catch (Exception ex)
            {
                await MostrarMensagemAsync("Erro", $"Erro ao deletar: {ex.Message}");
            }
        }

        private void SairButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close(false);
        }
        private void CarregarEnsaios()
        {
            Ensaios.Clear();
            _todosEnsaios.Clear(); // Limpar a coleção completa também
    
            var lista = _db.ObterTodos(); // método do DatabaseService
            foreach (var ensaio in lista)
            {
                _todosEnsaios.Add(ensaio);  // Primeiro adiciona na coleção completa
            }
    
            // Depois aplica o filtro (se houver) ou mostra todos
            FiltrarEnsaios();
        }
        
        private async Task MostrarMensagemAsync(string titulo, string mensagem, string icone = "ℹ️")
        {
            var messageBox = new Window
            {
                Title = titulo,
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
                Background = GetIconBackground(icone),
                CornerRadius = new CornerRadius(25),
                Width = 50,
                Height = 50
            };

            var iconText = new TextBlock
            {
                Text = icone,
                FontSize = 24,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            iconBorder.Child = iconText;
            headerPanel.Children.Add(iconBorder);

            // Título
            var titleText = new TextBlock
            {
                Text = titulo,
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
                Text = mensagem,
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
                Content = "OK",
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

        private async Task<bool> MostrarConfirmacaoAsync(string titulo, string mensagem, string icone = "❓")
        {
            var confirmBox = new Window
            {
                Title = titulo,
                Width = 450,
                Height = 220,
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
                Background = GetIconBackground(icone),
                CornerRadius = new CornerRadius(25),
                Width = 50,
                Height = 50
            };

            var iconText = new TextBlock
            {
                Text = icone,
                FontSize = 24,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            iconBorder.Child = iconText;
            headerPanel.Children.Add(iconBorder);

            // Título
            var titleText = new TextBlock
            {
                Text = titulo,
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
                Text = mensagem,
                TextWrapping = TextWrapping.Wrap,
                HorizontalAlignment = HorizontalAlignment.Center,
                TextAlignment = TextAlignment.Center,
                FontSize = 14,
                Foreground = new SolidColorBrush(Color.Parse("#34495E")),
                LineHeight = 20,
                MaxWidth = 380
            };

            mainPanel.Children.Add(messageText);

            // Botões
            var buttonsPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
                Spacing = 15
            };

            var tcs = new TaskCompletionSource<bool>();

            // Botão Sim
            var btnSim = new Button
            {
                Content = "Sim",
                Padding = new Thickness(25, 10),
                Background = new SolidColorBrush(Color.Parse("#27AE60")),
                Foreground = new SolidColorBrush(Colors.White),
                FontWeight = FontWeight.SemiBold,
                FontSize = 14,
                CornerRadius = new CornerRadius(8),
                BorderThickness = new Thickness(0),
                Cursor = new Cursor(StandardCursorType.Hand),
                MinWidth = 80
            };

            // Botão Não
            var btnNao = new Button
            {
                Content = "Não",
                Padding = new Thickness(25, 10),
                Background = new SolidColorBrush(Color.Parse("#E74C3C")),
                Foreground = new SolidColorBrush(Colors.White),
                FontWeight = FontWeight.SemiBold,
                FontSize = 14,
                CornerRadius = new CornerRadius(8),
                BorderThickness = new Thickness(0),
                Cursor = new Cursor(StandardCursorType.Hand),
                MinWidth = 80
            };

            // Efeitos hover
            ConfigureButtonHover(btnSim, "#27AE60", "#229954");
            ConfigureButtonHover(btnNao, "#E74C3C", "#C0392B");

            // Eventos
            btnSim.Click += (s, e) => { confirmBox.Close(); tcs.TrySetResult(true); };
            btnNao.Click += (s, e) => { confirmBox.Close(); tcs.TrySetResult(false); };

            buttonsPanel.Children.Add(btnSim);
            buttonsPanel.Children.Add(btnNao);
            mainPanel.Children.Add(buttonsPanel);

            mainBorder.Child = mainPanel;
            confirmBox.Content = mainBorder;

            await confirmBox.ShowDialog(this);
            return await tcs.Task;
        }

        // Método auxiliar para configurar efeitos hover nos botões
        private void ConfigureButtonHover(Button button, string normalColor, string hoverColor)
        {
            button.PointerEntered += (s, e) => 
            {
                button.Background = new SolidColorBrush(Color.Parse(hoverColor));
                button.RenderTransform = new ScaleTransform(1.05, 1.05);
            };

            button.PointerExited += (s, e) => 
            {
                button.Background = new SolidColorBrush(Color.Parse(normalColor));
                button.RenderTransform = new ScaleTransform(1.0, 1.0);
            };
        }

        // Método auxiliar para cores dos ícones
        private IBrush GetIconBackground(string icon)
        {
            return icon switch
            {
                "✅" or "✔️" => new SolidColorBrush(Color.Parse("#E8F8F5")), // Verde - Sucesso
                "❌" or "❗" or "⚠️" => new SolidColorBrush(Color.Parse("#FFEBEE")), // Vermelho - Erro/Aviso
                "❓" or "❔" => new SolidColorBrush(Color.Parse("#E3F2FD")), // Azul - Pergunta
                "📋" or "📊" or "ℹ️" => new SolidColorBrush(Color.Parse("#E3F2FD")), // Azul - Informação
                "⏸️" or "🔄" => new SolidColorBrush(Color.Parse("#FFF3E0")), // Laranja - Ação
                _ => new SolidColorBrush(Color.Parse("#F0F4F8")) // Cinza - Padrão
            };
        }

        // Métodos de conveniência para diferentes tipos de mensagem
        private async Task MostrarSucessoAsync(string titulo, string mensagem)
        {
            await MostrarMensagemAsync(titulo, mensagem, "✅");
        }

        private async Task MostrarErroAsync(string titulo, string mensagem)
        {
            await MostrarMensagemAsync(titulo, mensagem, "❌");
        }

        private async Task MostrarAvisoAsync(string titulo, string mensagem)
        {
            await MostrarMensagemAsync(titulo, mensagem, "⚠️");
        }

        private async Task MostrarInfoAsync(string titulo, string mensagem)
        {
            await MostrarMensagemAsync(titulo, mensagem, "ℹ️");
        }

        private async Task<bool> MostrarConfirmacaoPerigosaAsync(string titulo, string mensagem)
        {
            return await MostrarConfirmacaoAsync(titulo, mensagem, "❗");
        }

    }
    
}