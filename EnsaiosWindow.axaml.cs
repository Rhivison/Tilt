using System;
using Avalonia.Controls;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Interactivity;
using Avalonia.Layout;
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
        
        private async Task MostrarMensagemAsync(string titulo, string mensagem)
        {
            var msgBox = new Window
            {
                Title = titulo,
                Width = 300,
                Height = 150,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                CanResize = false
            };

            var panel = new StackPanel { Margin = new Thickness(20), Spacing = 10 };
            panel.Children.Add(new TextBlock
            {
                Text = mensagem,
                TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
            });

            var botao = new Button
            {
                Content = "OK",
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                Padding = new Thickness(20, 5)
            };
            botao.Click += (s, e) => msgBox.Close();
            panel.Children.Add(botao);

            msgBox.Content = panel;
            await msgBox.ShowDialog(this);
        }

        private async Task<bool> MostrarConfirmacaoAsync(string titulo, string mensagem)
        {
            var confirmBox = new Window
            {
                Title = titulo,
                Width = 350,
                Height = 160,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                CanResize = false
            };

            var panel = new StackPanel { Margin = new Thickness(20), Spacing = 15 };
            panel.Children.Add(new TextBlock
            {
                Text = mensagem,
                TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
            });

            var botoes = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center };
            var btnSim = new Button { Content = "Sim", Margin = new Thickness(10, 0), Padding = new Thickness(20, 5) };
            var btnNao = new Button { Content = "Não", Margin = new Thickness(10, 0), Padding = new Thickness(20, 5) };

            var tcs = new TaskCompletionSource<bool>();
            btnSim.Click += (_, _) => { confirmBox.Close(); tcs.TrySetResult(true); };
            btnNao.Click += (_, _) => { confirmBox.Close(); tcs.TrySetResult(false); };

            botoes.Children.Add(btnSim);
            botoes.Children.Add(btnNao);
            panel.Children.Add(botoes);

            confirmBox.Content = panel;
            await confirmBox.ShowDialog(this);

            return await tcs.Task;
        }

    }
    
}