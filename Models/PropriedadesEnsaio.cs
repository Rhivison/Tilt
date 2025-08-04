using System;

namespace TiltMachine.Models;

public class PropriedadesEnsaio
{   
    public int Id { get; set; }
    public string Amostra { get; set; } = string.Empty;
    public int AmostraNumero { get; set; }
    public string Local { get; set; } = string.Empty;
    public string Responsavel { get; set; } = string.Empty;
    public DateTime DataEnsaio { get; set; } = DateTime.Now;
    public string TipoRocha { get; set; } = string.Empty;
    public string FormatoCorpoProva { get; set; } = "Prismático";
    public double Altura { get; set; }
    public double Largura { get; set; }
    public double Profundidade { get; set; }
    public double AreaContato { get; set; }
    public double TaxaInclinacao { get; set; }
    public double InclinacaoMaxima { get; set; }
    public double DeslocamentoMaximo { get; set; }
    public string Observacoes { get; set; } = string.Empty;
}