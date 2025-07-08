using System;
using Avalonia.Controls;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Avalonia.Interactivity;
using TiltMachine.Models;
using TiltMachine.Services;

namespace TiltMachine
{
    public partial class EnsaiosWindow : Window, INotifyPropertyChanged
    {   
        private readonly DatabaseService _db = new();

        public ObservableCollection<Ensaio> Ensaios { get; set; } = new()
        {
            new Ensaio { AmostraNumero = 1, AmostraNome = "Amostra A", Local = "São Paulo", Responsavel = "João", DataEnsaio = "04/07/2025", FormatoCorpoProva = "Cilíndrico" },
            new Ensaio { AmostraNumero = 2, AmostraNome = "Amostra B", Local = "Rio", Responsavel = "Maria", DataEnsaio = "04/07/2025", FormatoCorpoProva = "Cúbico" },
            new Ensaio { AmostraNumero = 3, AmostraNome = "Amostra C", Local = "BH", Responsavel = "Carlos", DataEnsaio = "04/07/2025", FormatoCorpoProva = "Prismático" }
        };

        private Ensaio? _ensaioSelecionado;
        public Ensaio? EnsaioSelecionado
        {
            get => _ensaioSelecionado;
            set
            {
                _ensaioSelecionado = value;
                OnPropertyChanged();
            }
        }

        public EnsaiosWindow()
        {
            InitializeComponent();
            _db.Inicializar();
            DataContext = this;
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
    }

    public class Ensaio
    {
        public int AmostraNumero { get; set; }
        public string AmostraNome { get; set; } = "";
        public string Local { get; set; } = "";
        public string Responsavel { get; set; } = "";
        public string DataEnsaio { get; set; } = "";
        public string FormatoCorpoProva { get; set; } = "";
    }
}