namespace TiltMachine.Models;

public class DadoEnsaio
{
    public double Tempo { get; set; }
    public double Angulo { get; set; }
    public double Corrente_mA { get; set; }
    
    public DadoEnsaio(double tempo, double angulo, double corrente_mA)
    {
        Tempo = tempo;
        Angulo = angulo;
        Corrente_mA = corrente_mA;
    }
   
}