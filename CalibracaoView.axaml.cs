using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Interactivity;
using Avalonia.Threading;
using TiltMachine.Models;
using TiltMachine.Services;

namespace TiltMachine
{
    public partial class CalibracaoView : BaseWindow
    {
        private bool _calibracaoAtiva = false;
        private bool _inicializada = false;
        private bool _eventosConfigurados = false;
        private readonly ObservableCollection<string> _pontosDisplay = new();
        private readonly List<PontoCalibracao> _pontosCalibracao = new();
        private (double a, double b, double c, double rmse) _coeficientes = (0, 0, 0, 0);
        private DatabaseService databaseService = new DatabaseService();
        private List<CoeficienteCalibracao> dadosUltimaCalibracao = new List<CoeficienteCalibracao>();
        

        public CalibracaoView()
        {
            try
            {
                InitializeComponent();
                var itemsControl = this.FindControl<ItemsControl>("itemsControlPontos");
                itemsControl.ItemsSource = _pontosDisplay;
                // IMPORTANTE: Configurar eventos IMEDIATAMENTE após InitializeComponent
                ConfigurarEventosArduino();
                dadosUltimaCalibracao = databaseService.ObterTodosCoeficientes();
                this.AttachedToVisualTree += OnAttachedToVisualTree;
            }
            catch (Exception ex)
            {
                MostrarMensagemAsync("Erro", ex.Message);
            }
        }
        private void InitializeComponent()
        {
            Avalonia.Markup.Xaml.AvaloniaXamlLoader.Load(this);
            //lblStatus = this.FindControl<TextBlock>("lblStatus");
            //StatusIndicator = this.FindControl<Ellipse>("StatusIndicator");
        }
        private void OnAttachedToVisualTree(object? sender, Avalonia.VisualTreeAttachmentEventArgs e)
        {
            if (!_inicializada)
            {
                InicializarComponentes();
                _inicializada = true;
            }
            AtualizarStatusConexao(App.Arduino.Conectado);
        }
        private void InicializarComponentes()
        {   
            try
            {
                Console.WriteLine("Inicializando componentes da CalibracaoView...");
                
                // Verificar se eventos já foram configurados, se não, configurar novamente
                if (!_eventosConfigurados)
                {
                    ConfigurarEventosArduino();
                }
        
                // Sincronizar dados existentes
                _ = SincronizarDadosArduino();
        
                AtualizarStatus("Sistema inicializado");
                AtualizarTotalPontos();
                
                Console.WriteLine("Inicialização concluída com sucesso");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro na inicialização: {ex.Message}");
                AtualizarStatus($"Erro na inicialização: {ex.Message}");
            }
        }
        private void ConfigurarEventosArduino()
        {
            try
            {
                Console.WriteLine("=== DEBUG: Configurando eventos Arduino ===");
                
                if (App.Arduino != null && App.Arduino.Conectado)
                {
                    // IMPORTANTE: Remover eventos existentes antes de adicionar (evita duplicação)
                    App.Arduino.CalibracaoIniciada -= OnCalibracaoIniciada;
                    App.Arduino.CalibracaoFinalizada -= OnCalibracaoFinalizada;
                    
                    // Adicionar eventos
                    App.Arduino.CalibracaoIniciada += OnCalibracaoIniciada;
                    App.Arduino.CalibracaoFinalizada += OnCalibracaoFinalizada;
                    
                    _eventosConfigurados = true;
                    
                    Console.WriteLine($"DEBUG: Eventos configurados. CalibracaoAtiva no Arduino: {App.Arduino.CalibracaoAtiva}");
                    Console.WriteLine($"DEBUG: Dados existentes no Arduino: {App.Arduino.DadosCalibracao.Count}");
                    var ultimaCalibração = dadosUltimaCalibracao.OrderBy(x => x.Id).ToList().First();
                    if (ultimaCalibração != null)
                    {
                        string initialText =
                            $"Equipamento conectado - Data da última Calibração: {ultimaCalibração.DataCalibracao.ToString()}";
                        AtualizarStatus(initialText);
                    }
                    else
                    {
                        string initialText =
                            $"Equipamento conectado pronto para calibrar";
                        AtualizarStatus(initialText);
                    }
                    
                }
                else
                {
                    Console.WriteLine("DEBUG: App.Arduino é null!");
                    AtualizarStatus("Equipamento não disponível - Modo offline");
                    DesabilitarControlesArduino();
                    _eventosConfigurados = false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao configurar eventos Arduino: {ex.Message}");
                AtualizarStatus("Erro ao conectar com Arduino");
                DesabilitarControlesArduino();
                _eventosConfigurados = false;
            }
        }
        private void DesabilitarControlesArduino()
        {
            var btnIniciar = this.FindControl<Button>("btnIniciar");
            var btnFinalizar = this.FindControl<Button>("btnFinalizar");
            
            if (btnIniciar != null) btnIniciar.IsEnabled = false;
            if (btnFinalizar != null) btnFinalizar.IsEnabled = false;
        }
        // ==== MÉTODOS AUXILIARES SEGUROS ====
        private void AtualizarStatus(string mensagem)
        {
            try
            {
                var txtStatus = this.FindControl<TextBlock>("txtStatus");
                if (txtStatus != null)
                {
                    if (Dispatcher.UIThread.CheckAccess())
                    {
                        txtStatus.Text = mensagem;
                    }
                    else
                    {
                        Dispatcher.UIThread.Post(() => txtStatus.Text = mensagem);
                    }
                }
                Console.WriteLine($"Status: {mensagem}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao atualizar status: {ex.Message}");
            }
        }
        private void AtualizarTotalPontos()
        {
            try
            {
                var txtTotalPontos = this.FindControl<TextBlock>("txtTotalPontos");
                if (txtTotalPontos != null)
                {
                    var texto = $"Total de pontos: {_pontosCalibracao.Count}";
                    if (Dispatcher.UIThread.CheckAccess())
                    {
                        txtTotalPontos.Text = texto;
                    }
                    else
                    {
                        Dispatcher.UIThread.Post(() => txtTotalPontos.Text = texto);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao atualizar total de pontos: {ex.Message}");
            }
        }
        private void AtualizarDisplayPontos()
        {
            try
            {
                Console.WriteLine($"=== AtualizarDisplayPontos ===");
                Console.WriteLine($"_pontosCalibracao.Count: {_pontosCalibracao.Count}");
                Console.WriteLine($"_pontosDisplay.Count antes: {_pontosDisplay.Count}");
                _pontosDisplay.Clear();
                foreach (var ponto in _pontosCalibracao)
                {
                    string status = ponto.ValorReferencia == 0
                        ? "Pendente"
                        : $"{ponto.ValorReferencia:F2}°";

                    string display = $"{ponto.Timestamp:HH:mm:ss} - " +
                                     $"Sensor: {ponto.LeituraSensor:F3} - Referência: {status}";
                    _pontosDisplay.Add(display);
                    Console.WriteLine($"Adicionado ao display: {display}");
                }
                Console.WriteLine($"_pontosDisplay.Count depois: {_pontosDisplay.Count}");
            }
            catch (Exception ex)
            {
                MostrarMensagemAsync("Erro", ex.Message);
            }
            /*try
            {
                // Se não estou no UI thread, reagendo a chamada
                if (!Dispatcher.UIThread.CheckAccess())
                {
                    Dispatcher.UIThread.Post(() => AtualizarDisplayPontos());
                    return;
                }
                Console.WriteLine($"=== AtualizarDisplayPontos ===");
                Console.WriteLine($"_pontosCalibracao.Count: {_pontosCalibracao.Count}");
                Console.WriteLine($"_pontosDisplay.Count antes: {_pontosDisplay.Count}");
                if (_pontosCalibracao.Count == 0)
                {
                    Console.WriteLine("DEBUG: Nenhum ponto em _pontosCalibracao para exibir");
                    _pontosDisplay.Clear(); // limpa para refletir que não há pontos
                    return;
                }

                Console.WriteLine($"DEBUG: Atualizando display com {_pontosCalibracao.Count} pontos");

                _pontosDisplay.Clear();
                foreach (var ponto in _pontosCalibracao)
                {
                    string status = ponto.ValorReferencia == 0
                        ? "Pendente"
                        : $"{ponto.ValorReferencia:F2}°";

                    string display = $"{ponto.Timestamp:HH:mm:ss} - " +
                                     $"Sensor: {ponto.LeituraSensor:F3} - Referência: {status}";

                    _pontosDisplay.Add(display);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERRO em AtualizarDisplayPontos: {ex.Message}\n{ex.StackTrace}");
            }*/
        }
        // ==== EVENTOS DOS BOTÕES ====
        private async void BtnIniciar_Click(object? sender, RoutedEventArgs e)
        {
            try
            {
                Console.WriteLine("=== DEBUG: BtnIniciar_Click ===");
                AtualizarStatus("Iniciando calibração...");
        
                // Garantir que eventos estão configurados
                if (!_eventosConfigurados)
                {
                    ConfigurarEventosArduino();
                }
        
                // Sincronizar dados já existentes no ArduinoService
                await SincronizarDadosArduino();
        
                _calibracaoAtiva = true;
        
                var btnIniciar = this.FindControl<Button>("btnIniciar");
                var btnFinalizar = this.FindControl<Button>("btnFinalizar");
                var btnAdicionarPonto = this.FindControl<Button>("btnAdicionarPonto");
        
                if (btnIniciar != null) btnIniciar.IsEnabled = false;
                if (btnFinalizar != null) btnFinalizar.IsEnabled = false;
                if (btnAdicionarPonto != null) btnAdicionarPonto.IsEnabled = true;

                if (App.Arduino != null)
                {
                    await App.Arduino.IniciarCalibracaoAsync();
                    AtualizarStatus("Calibração iniciada - Aguardando leituras do sensor...");
                    Console.WriteLine("DEBUG: IniciarCalibracaoAsync() chamado");
                }
                else
                {
                    AtualizarStatus("Modo offline - Use valores manuais");
                }
            }
            catch (Exception ex)
            {
                var mensagem = $"Erro ao iniciar calibração: {ex.Message}";
                Console.WriteLine(mensagem);
                AtualizarStatus(mensagem);
            }
        }
        private async void BtnFinalizar_Click(object? sender, RoutedEventArgs e)
        {
            try
            {
                if (App.Arduino == null)
                {
                    AtualizarStatus("Erro: Arduino não disponível");
                    return;
                }

                AtualizarStatus("Finalizando calibração...");
                _pontosCalibracao.Clear();
                await App.Arduino.FinalizarCalibracaoAsync(); 
                App.Arduino.LimparCalibracao();
            }
            catch (Exception ex)
            {
                var mensagem = $"Erro ao finalizar calibração: {ex.Message}";
                Console.WriteLine(mensagem);
                AtualizarStatus(mensagem);
            }
        }
        private async Task SincronizarDadosArduino()
        {
            try
            {
                if (App.Arduino != null)
                {
                    Console.WriteLine("=== DEBUG: Sincronizando dados do Arduino ===");
                    
                    // Limpar lista atual
                    _pontosCalibracao.Clear();
            
                    // Adicionar todos os dados já recebidos pelo ArduinoService
                    foreach (var dado in App.Arduino.DadosCalibracao)
                    {
                        var ponto = new PontoCalibracao
                        {
                            LeituraSensor = dado.entradaSensor,
                            ValorReferencia = dado.saidaReal,
                            Timestamp = DateTime.Now
                        };
                        _pontosCalibracao.Add(ponto);
                        Console.WriteLine($"DEBUG: Sincronizado - Sensor: {dado.entradaSensor:F3}, Real: {dado.saidaReal:F2}");
                    }
            
                    AtualizarDisplayPontos();
                    AtualizarTotalPontos();
                    Console.WriteLine($"DEBUG: Sincronizados {_pontosCalibracao.Count} pontos do ArduinoService");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao sincronizar dados: {ex.Message}");
            }
        }
        private void BtnLimpar_Click(object? sender, RoutedEventArgs e)
        {
            try
            {
                _pontosCalibracao.Clear();
                _pontosDisplay.Clear();
                _coeficientes = (0, 0, 0, 0);
                // Limpar também no ArduinoService
                if (App.Arduino != null)
                {
                    App.Arduino.LimparCalibracao();
                }

                AtualizarStatus("Calibração limpa");
                
                /*var panelEquacao = this.FindControl<StackPanel>("panelEquacao");
                if (panelEquacao != null)
                {
                    panelEquacao.IsVisible = false;
                }*/
                
                AtualizarTotalPontos();
            }
            catch (Exception ex)
            {
                var mensagem = $"Erro ao limpar calibração: {ex.Message}";
                Console.WriteLine(mensagem);
                AtualizarStatus(mensagem);
            }
        }
        private void BtnAdicionarPonto_Click(object? sender, RoutedEventArgs e)
        {
            try
            {
                // Ao adicionar o ponto devo pegar o valor de referência digitado e obter o último valor de saida real do sensor, sendo assim
                //Obtenho o valor de referência
                var txtValorReferencia = this.FindControl<TextBox>("txtValorReferencia");
                if (txtValorReferencia == null || string.IsNullOrWhiteSpace(txtValorReferencia.Text))
                {
                    AtualizarStatus("Digite um valor de referência válido");
                    return;
                }
                //converto para double e obtenho a última leitura estável da instância do arduino
                if (double.TryParse(txtValorReferencia.Text.Replace(',', '.'), out double valorReferencia))
                {   
                    //obtive o valor do sensor
                    var valorSensor = App.Arduino._dadosCalibracao.Last().saidaReal;
                    var timestamp = DateTime.Now;
                    var ponto = new PontoCalibracao
                    {
                        Timestamp =  timestamp,
                        LeituraSensor = valorSensor,
                        ValorReferencia = valorReferencia
                    };
                    _pontosCalibracao.Add(ponto);
                    AtualizarDisplayPontos();
                    AtualizarStatus($"Ponto Adicionado - Valor de Referência: {ponto.ValorReferencia:F3} - Valor Medido: {ponto.LeituraSensor:F3}");
                    txtValorReferencia.Text = string.Empty;
                    AtualizarTotalPontos();
                }
            }
            catch (Exception ex)
            {
                MostrarMensagemAsync("Erro", $"Erro ao adicionar ponto: {ex.Message}");
            }
        }
        private void BtnCalcularCurva_Click(object? sender, RoutedEventArgs e)
        {
            try
            {   
                if (btnFinalizar != null) btnFinalizar.IsEnabled = true;
                string tipo = "Linear";

                if (tipo == "Linear")
                {
                    var (k, b, rmse) = CalcularCurvaLinear(_pontosCalibracao);
                    _coeficientes.a = 0;
                    _coeficientes.b = k;
                    _coeficientes.c = b;
                    _coeficientes.rmse = rmse;
                    SalvarCoeficientes();
                    AtualizarStatus($"Linear: y = {k:F4}*x + {b:F4}, RMSE={rmse:F4}");
                }
                else if (tipo == "Quadrática")
                {
                    _coeficientes = CalcularCurvaQuadratica(_pontosCalibracao);
                    SalvarCoeficientes();
                    AtualizarStatus($"Quadrática: y = {_coeficientes.a:F4}x² + {_coeficientes.b:F4}x + {_coeficientes.c:F4}, RMSE={_coeficientes.rmse:F4}");
                 
                }
            }
            catch (Exception ex)
            {
                MostrarMensagemAsync("Erro", $"Falha ao calcular curva: {ex.Message}");
            }
        }
        // ==== EVENTOS DO ARDUINO ====
        private void OnCalibracaoIniciada()
        {
            try
            {
                Console.WriteLine("=== DEBUG: OnCalibracaoIniciada chamado ===");
                Dispatcher.UIThread.Post(() =>
                {
                    _calibracaoAtiva = true;
                    
                    var btnIniciar = this.FindControl<Button>("btnIniciar");
                    var btnFinalizar = this.FindControl<Button>("btnFinalizar");
                    var btnAdicionarPonto = this.FindControl<Button>("btnAdicionarPonto");
                    
                    if (btnIniciar != null) btnIniciar.IsEnabled = false;
                    if (btnFinalizar != null) btnFinalizar.IsEnabled = true;
                    if (btnAdicionarPonto != null) btnAdicionarPonto.IsEnabled = true;
                    
                    AtualizarStatus("Calibração em andamento - aguardando leituras...");
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro em OnCalibracaoIniciada: {ex.Message}");
            }
        }
        private void OnCalibracaoFinalizada()
        {
            try
            {   
                //BtnCalcularCurva_Click()
                Console.WriteLine("=== DEBUG: OnCalibracaoFinalizada chamado ===");
                Dispatcher.UIThread.Post(() =>
                {
                    _calibracaoAtiva = false;
                    
                    var btnIniciar = this.FindControl<Button>("btnIniciar");
                    var btnFinalizar = this.FindControl<Button>("btnFinalizar");
                    var btnAdicionarPonto = this.FindControl<Button>("btnAdicionarPonto");
                    
                    if (btnIniciar != null) btnIniciar.IsEnabled = true;
                    if (btnFinalizar != null) btnFinalizar.IsEnabled = false;
                    if (btnAdicionarPonto != null) btnAdicionarPonto.IsEnabled = false;
                    
                    AtualizarStatus("Calibração finalizada");
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro em OnCalibracaoFinalizada: {ex.Message}");
            }
        }
        // ==== LÓGICA DE NEGÓCIO (mantida igual) ====
        private void SalvarCoeficientes()
        {
            try
            {
                var databaseService = new DatabaseService();

                //double? erroMedioQuadratico = CalcularErroMedioQuadratico();

                var coeficiente = new CoeficienteCalibracao
                {
                    DataCalibracao = DateTime.Now,
                    SensorIdentificador = "SensorPrincipal",
                    CoeficienteA = _coeficientes.a,
                    CoeficienteB = _coeficientes.b,
                    CoeficienteC = _coeficientes.c,
                    QuantidadePontos = _pontosCalibracao.Count,
                    ErroMedioQuadratico = _coeficientes.rmse,
                    Observacoes = "Calibração automática",
                    Ativa = true
                };

                int coeficienteId = databaseService.InserirCoeficiente(coeficiente);

                var pontosDb = _pontosCalibracao.Select(p => new PontoCalibracaoDb
                {
                    CoeficienteId = coeficienteId,
                    LeituraSensor = p.LeituraSensor,
                    AnguloReferencia = p.ValorReferencia,
                    Timestamp = p.Timestamp
                }).ToList();

                databaseService.InserirPontosCalibracao(coeficienteId, pontosDb);
                databaseService.DesativarOutrosCoeficientes(coeficiente.SensorIdentificador, coeficienteId);
                
                Console.WriteLine("Coeficientes salvos com sucesso no banco de dados");
                AtualizarStatus("Coeficientes salvos com sucesso!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao salvar coeficientes: {ex.Message}");
                AtualizarStatus($"Erro ao salvar: {ex.Message}");
            }
        }
        private (double a, double b, double c, double rmse) CalcularCurvaQuadratica(List<PontoCalibracao> pontos)
        {
            int n = pontos.Count;
            if (n < 3) throw new InvalidOperationException("São necessários pelo menos 3 pontos para ajuste quadrático");

            var x = pontos.Select(p => p.LeituraSensor).ToArray();
            var y = pontos.Select(p => p.ValorReferencia).ToArray();

            // Somatórios
            double sumX = x.Sum();
            double sumX2 = x.Sum(v => v * v);
            double sumX3 = x.Sum(v => v * v * v);
            double sumX4 = x.Sum(v => v * v * v * v);
            double sumY = y.Sum();
            double sumXY = x.Zip(y, (xi, yi) => xi * yi).Sum();
            double sumX2Y = x.Zip(y, (xi, yi) => xi * xi * yi).Sum();

            // Monta sistema normal 3x3
            double[,] A = {
                { n,    sumX,  sumX2 },
                { sumX, sumX2, sumX3 },
                { sumX2,sumX3, sumX4 }
            };
            double[] B = { sumY, sumXY, sumX2Y };

            double[] coef = ResolverSistema3x3(A, B);

            double a = coef[2], b = coef[1], c = coef[0];

            double mse = pontos.Average(p =>
            {
                double yPred = a * p.LeituraSensor * p.LeituraSensor + b * p.LeituraSensor + c;
                return Math.Pow(p.ValorReferencia - yPred, 2);
            });
            double rmse = Math.Sqrt(mse);

            return (a, b, c, rmse);
        }
        private double[] ResolverSistema3x3(double[,] A, double[] B)
        {
            var m = (double[,])A.Clone();
            var v = (double[])B.Clone();

            int n = 3;
            for (int i = 0; i < n; i++)
            {
                // pivô
                int max = i;
                for (int j = i + 1; j < n; j++)
                    if (Math.Abs(m[j, i]) > Math.Abs(m[max, i])) max = j;

                // troca linhas
                for (int k = 0; k < n; k++)
                {
                    double tmp = m[i, k]; m[i, k] = m[max, k]; m[max, k] = tmp;
                }
                double t2 = v[i]; v[i] = v[max]; v[max] = t2;

                // normaliza
                double diag = m[i, i];
                for (int k = 0; k < n; k++) m[i, k] /= diag;
                v[i] /= diag;

                // elimina
                for (int j = 0; j < n; j++)
                {
                    if (j == i) continue;
                    double fator = m[j, i];
                    for (int k = 0; k < n; k++) m[j, k] -= fator * m[i, k];
                    v[j] -= fator * v[i];
                }
            }
            return v;
        }
        private (double k, double b, double rmse) CalcularCurvaLinear(List<PontoCalibracao> pontos)
        {
            int n = pontos.Count;
            if (n < 2) throw new InvalidOperationException("São necessários ao menos 2 pontos para calibração linear");
            
            double sumX = pontos.Sum(p => p.LeituraSensor);
            double sumY = pontos.Sum(p => p.ValorReferencia);
            double sumXY = pontos.Sum(p => p.LeituraSensor * p.ValorReferencia);
            double sumX2 = pontos.Sum(p => p.LeituraSensor * p.LeituraSensor);
            
            double k = (n * sumXY - sumX * sumY) / (n * sumX2 - sumX * sumX);
            double b = (sumY - k * sumX) / n;
            
            double mse = pontos.Average(p =>
            {
                double yPred = k * p.LeituraSensor + b;
                return Math.Pow(p.ValorReferencia - yPred, 2);
            });
            double rmse = Math.Sqrt(mse);

            return (k, b, rmse);

        }
        private double? CalcularErroMedioQuadratico()
        {
            if (_pontosCalibracao.Count < 2) return null;
            
            try
            {
                double somaQuadrados = 0;
                foreach (var ponto in _pontosCalibracao)
                {
                    double valorCalculado = _coeficientes.a * Math.Pow(ponto.LeituraSensor, 2) 
                                          + _coeficientes.b * ponto.LeituraSensor 
                                          + _coeficientes.c;
                    
                    double erro = ponto.ValorReferencia - valorCalculado;
                    somaQuadrados += Math.Pow(erro, 2);
                }
                
                return Math.Sqrt(somaQuadrados / _pontosCalibracao.Count);
            }
            catch
            {
                return null;
            }
        }
        private (double a, double b, double c) CalcularCoeficientesQuadraticos()
        {
            var pontos = _pontosCalibracao.ToList();
            int n = pontos.Count;
            double sumX = 0, sumX2 = 0, sumX3 = 0, sumX4 = 0;
            double sumY = 0, sumXY = 0, sumX2Y = 0;

            foreach (var ponto in pontos)
            {
                double x = ponto.LeituraSensor;
                double y = ponto.ValorReferencia;

                sumX += x;
                sumX2 += x * x;
                sumX3 += x * x * x;
                sumX4 += x * x * x * x;
                sumY += y;
                sumXY += x * y;
                sumX2Y += x * x * y;
            }

            double[,] matriz = {
                { n, sumX, sumX2 },
                { sumX, sumX2, sumX3 },
                { sumX2, sumX3, sumX4 }
            };

            double[] resultados = { sumY, sumXY, sumX2Y };

            for (int i = 0; i < 3; i++)
            {
                double max = Math.Abs(matriz[i, i]);
                int maxRow = i;
                for (int k = i + 1; k < 3; k++)
                {
                    if (Math.Abs(matriz[k, i]) > max)
                    {
                        max = Math.Abs(matriz[k, i]);
                        maxRow = k;
                    }
                }

                for (int k = i; k < 3; k++)
                {
                    (matriz[maxRow, k], matriz[i, k]) = (matriz[i, k], matriz[maxRow, k]);
                }
                (resultados[maxRow], resultados[i]) = (resultados[i], resultados[maxRow]);

                for (int k = i + 1; k < 3; k++)
                {
                    double factor = matriz[k, i] / matriz[i, i];
                    for (int j = i; j < 3; j++)
                    {
                        matriz[k, j] -= factor * matriz[i, j];
                    }
                    resultados[k] -= factor * resultados[i];
                }
            }

            double[] solucao = new double[3];
            for (int i = 2; i >= 0; i--)
            {
                solucao[i] = resultados[i];
                for (int j = i + 1; j < 3; j++)
                {
                    solucao[i] -= matriz[i, j] * solucao[j];
                }
                solucao[i] /= matriz[i, i];
            }

            return (solucao[2], solucao[1], solucao[0]);
        }
        protected override void ProcessarLinhaRecebida(string linha)
        {
            base.ProcessarLinhaRecebida(linha);
        }
        protected override void OnClosed(EventArgs e)
        {
            try
            {
                Console.WriteLine("=== DEBUG: Fechando CalibracaoView - Removendo eventos ===");
               
                
                this.AttachedToVisualTree -= OnAttachedToVisualTree;
                
                if (App.Arduino != null)
                {   
                    App.Arduino.FinalizarCalibracaoAsync();
                    App.Arduino.CalibracaoIniciada -= OnCalibracaoIniciada;
                    App.Arduino.CalibracaoFinalizada -= OnCalibracaoFinalizada;
                }
                
                _eventosConfigurados = false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao fechar CalibracaoView: {ex.Message}");
            }
            
            base.OnClosed(e);
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
        protected override void AtualizarStatusConexao(bool conectado)
        {
            base.AtualizarStatusConexao(conectado);
            UpdateConnectionStatus(conectado);
        }
        public void UpdateConnectionStatus(bool isConnected)
        {
            /*if (lblStatus != null)
            {
                lblStatus.Text = isConnected ? "Conectado" : "Desconectado";
                lblStatus.Foreground = isConnected ? 
                    Avalonia.Media.Brushes.Green : 
                    Avalonia.Media.Brushes.Red;
            }

            if (StatusIndicator != null)
            {
                StatusIndicator.Fill = isConnected ? 
                    Avalonia.Media.Brushes.Green : 
                    Avalonia.Media.Brushes.Red;
            }*/
        }

        private void OnSairCalibracaoClick(object sender, RoutedEventArgs e)
        {
            this.Close(false);
        }
    }
    public class PontoCalibracao
    {
        public double LeituraSensor { get; set; }
        public double ValorReferencia { get; set; }
        public DateTime Timestamp { get; set; }
    }
}