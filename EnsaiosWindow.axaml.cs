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

        public ObservableCollection<PropriedadesEnsaio> Ensaios { get; set; } = new();

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

        public EnsaiosWindow()
        {
            InitializeComponent();
            _db.Inicializar();
            DataContext = this;
            CarregarEnsaios();
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
        
        private void CarregarEnsaios()
        {
            Ensaios.Clear();
            var lista = _db.ObterTodos(); // método do DatabaseService
            foreach (var ensaio in lista)
                Ensaios.Add(ensaio);
        }

    }
    
}