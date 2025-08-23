using System;

namespace TiltMachine.Models;

public class PontoCalibracaoDb
{
    public int Id { get; set; }
    public int CoeficienteId { get; set; }
    public double LeituraSensor { get; set; }
    public double AnguloReferencia { get; set; }
    public DateTime Timestamp { get; set; }
}