using System;

namespace TiltMachine.Models;

public class CoeficienteCalibracao
{
    public int Id { get; set; }
    public string Ip {get; set;}
    public DateTime DataCalibracao { get; set; }
    public string SensorIdentificador { get; set; }
    public double CoeficienteA { get; set; }
    public double CoeficienteB { get; set; }
    public double CoeficienteC { get; set; }
    public int QuantidadePontos { get; set; }
    public double? ErroMedioQuadratico { get; set; }
    public string Observacoes { get; set; }
    public bool Ativa { get; set; }
}