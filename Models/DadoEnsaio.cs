namespace TiltMachine.Models;

public class DadoEnsaio
{
    public double Tempo { get; set; }
    public double Angulo { get; set; }
    
    public DadoEnsaio(double tempo, double angulo)
    {
        Tempo = tempo;
        Angulo = angulo;
    }
   
}