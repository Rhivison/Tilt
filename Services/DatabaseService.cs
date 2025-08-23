using System;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;
using TiltMachine.Models;
namespace TiltMachine.Services // Ajuste para o namespace do seu projeto
{
    public class DatabaseService
    {
        private readonly string _connectionString = "Data Source=meu_banco.db";

        public void Inicializar()
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText =
                @"
                CREATE TABLE IF NOT EXISTS PropriedadesEnsaio (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Amostra TEXT,
                    AmostraNumero INTEGER,
                    Local TEXT,
                    Responsavel TEXT,
                    DataEnsaio TEXT,
                    TipoRocha TEXT,
                    FormatoCorpoProva TEXT,
                    Altura REAL,
                    Largura REAL,
                    Profundidade REAL,
                    AreaContato REAL,
                    TaxaInclinacao REAL,
                    InclinacaoMaxima REAL,
                    DeslocamentoMaximo REAL,
                    Observacoes TEXT
                );
            ";
            command.ExecuteNonQuery();
            
            var commandCalibracao = connection.CreateCommand();
            commandCalibracao.CommandText =
                @"
                CREATE TABLE IF NOT EXISTS CoeficientesCalibracao (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    DataCalibracao TEXT NOT NULL,
                    SensorIdentificador TEXT NOT NULL,
                    CoeficienteA REAL NOT NULL,
                    CoeficienteB REAL NOT NULL,
                    CoeficienteC REAL NOT NULL,
                    QuantidadePontos INTEGER NOT NULL,
                    ErroMedioQuadratico REAL,
                    Observacoes TEXT,
                    Ativa INTEGER DEFAULT 0,
                    DataCriacao TEXT DEFAULT CURRENT_TIMESTAMP,
                    
                    UNIQUE(SensorIdentificador, DataCalibracao)
                );
            ";
            commandCalibracao.ExecuteNonQuery();

            // Tabela de Pontos de Calibração (para histórico detalhado)
            var commandPontosCalibracao = connection.CreateCommand();
            commandPontosCalibracao.CommandText =
                @"
                CREATE TABLE IF NOT EXISTS PontosCalibracao (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    CoeficienteId INTEGER NOT NULL,
                    LeituraSensor REAL NOT NULL,
                    AnguloReferencia REAL NOT NULL,
                    Timestamp TEXT NOT NULL,
                    
                    FOREIGN KEY (CoeficienteId) REFERENCES CoeficientesCalibracao(Id) ON DELETE CASCADE
                );
            ";
            commandPontosCalibracao.ExecuteNonQuery();
        }
        #region Métodos para Coeficientes de Calibração
        public int InserirCoeficiente(CoeficienteCalibracao coeficiente)
        {
           
    
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();
    
            var command = connection.CreateCommand();
            command.CommandText =
                @"
                INSERT INTO CoeficientesCalibracao (
                    DataCalibracao, SensorIdentificador, CoeficienteA, CoeficienteB, CoeficienteC,
                    QuantidadePontos, ErroMedioQuadratico, Observacoes, Ativa
                )
                VALUES (
                    $dataCalibracao, $sensorId, $coefA, $coefB, $coefC,
                    $quantidadePontos, $erroMedio, $observacoes, $ativa
                );
                SELECT last_insert_rowid();
            ";
    
            command.Parameters.AddWithValue("$dataCalibracao", coeficiente.DataCalibracao.ToString("yyyy-MM-dd HH:mm:ss"));
            command.Parameters.AddWithValue("$sensorId", coeficiente.SensorIdentificador);
            command.Parameters.AddWithValue("$coefA", coeficiente.CoeficienteA);
            command.Parameters.AddWithValue("$coefB", coeficiente.CoeficienteB);
            command.Parameters.AddWithValue("$coefC", coeficiente.CoeficienteC);
            command.Parameters.AddWithValue("$quantidadePontos", coeficiente.QuantidadePontos);
    
            // CORREÇÃO: Tratamento correto para valores NULL
            if (coeficiente.ErroMedioQuadratico.HasValue)
            {
                command.Parameters.AddWithValue("$erroMedio", coeficiente.ErroMedioQuadratico.Value);
            }
            else
            {
                command.Parameters.AddWithValue("$erroMedio", DBNull.Value);
            }
    
            command.Parameters.AddWithValue("$observacoes", coeficiente.Observacoes ?? string.Empty); // Garante não ser null
            command.Parameters.AddWithValue("$ativa", coeficiente.Ativa ? 1 : 0);
    
            var newId = Convert.ToInt32(command.ExecuteScalar());
            return newId;
        }
        public void InserirPontosCalibracao(int coeficienteId, List<PontoCalibracaoDb> pontos)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();
            
            using var transaction = connection.BeginTransaction();
            
            try
            {
                foreach (var ponto in pontos)
                {
                    var command = connection.CreateCommand();
                    command.CommandText =
                        @"
                        INSERT INTO PontosCalibracao (
                            CoeficienteId, LeituraSensor, AnguloReferencia, Timestamp
                        )
                        VALUES (
                            $coefId, $leitura, $angulo, $timestamp
                        )
                    ";
                    
                    command.Parameters.AddWithValue("$coefId", coeficienteId);
                    command.Parameters.AddWithValue("$leitura", ponto.LeituraSensor);
                    command.Parameters.AddWithValue("$angulo", ponto.AnguloReferencia);
                    command.Parameters.AddWithValue("$timestamp", ponto.Timestamp.ToString("yyyy-MM-dd HH:mm:ss"));
                    
                    command.ExecuteNonQuery();
                }
                
                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }
        public CoeficienteCalibracao ObterCoeficienteAtivo(string sensorIdentificador)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = 
                @"SELECT * FROM CoeficientesCalibracao 
                  WHERE SensorIdentificador = $sensorId AND Ativa = 1 
                  ORDER BY DataCalibracao DESC LIMIT 1";
            
            command.Parameters.AddWithValue("$sensorId", sensorIdentificador);

            using var reader = command.ExecuteReader();
            if (reader.Read())
            {
                return new CoeficienteCalibracao
                {
                    Id = reader.GetInt32(reader.GetOrdinal("Id")),
                    DataCalibracao = DateTime.Parse(reader.GetString(reader.GetOrdinal("DataCalibracao"))),
                    SensorIdentificador = reader.GetString(reader.GetOrdinal("SensorIdentificador")),
                    CoeficienteA = reader.GetDouble(reader.GetOrdinal("CoeficienteA")),
                    CoeficienteB = reader.GetDouble(reader.GetOrdinal("CoeficienteB")),
                    CoeficienteC = reader.GetDouble(reader.GetOrdinal("CoeficienteC")),
                    QuantidadePontos = reader.GetInt32(reader.GetOrdinal("QuantidadePontos")),
                    ErroMedioQuadratico = reader.IsDBNull(reader.GetOrdinal("ErroMedioQuadratico")) ? 
                        null : (double?)reader.GetDouble(reader.GetOrdinal("ErroMedioQuadratico")),
                    Observacoes = reader.GetString(reader.GetOrdinal("Observacoes")),
                    Ativa = reader.GetInt32(reader.GetOrdinal("Ativa")) == 1
                };
            }
            
            return null;
        }
        public List<CoeficienteCalibracao> ObterTodosCoeficientes(string sensorIdentificador = null)
        {
            var lista = new List<CoeficienteCalibracao>();

            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            
            if (string.IsNullOrEmpty(sensorIdentificador))
            {
                command.CommandText = "SELECT * FROM CoeficientesCalibracao ORDER BY DataCalibracao DESC";
            }
            else
            {
                command.CommandText = "SELECT * FROM CoeficientesCalibracao WHERE SensorIdentificador = $sensorId ORDER BY DataCalibracao DESC";
                command.Parameters.AddWithValue("$sensorId", sensorIdentificador);
            }

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                lista.Add(new CoeficienteCalibracao
                {
                    Id = reader.GetInt32(reader.GetOrdinal("Id")),
                    DataCalibracao = DateTime.Parse(reader.GetString(reader.GetOrdinal("DataCalibracao"))),
                    SensorIdentificador = reader.GetString(reader.GetOrdinal("SensorIdentificador")),
                    CoeficienteA = reader.GetDouble(reader.GetOrdinal("CoeficienteA")),
                    CoeficienteB = reader.GetDouble(reader.GetOrdinal("CoeficienteB")),
                    CoeficienteC = reader.GetDouble(reader.GetOrdinal("CoeficienteC")),
                    QuantidadePontos = reader.GetInt32(reader.GetOrdinal("QuantidadePontos")),
                    ErroMedioQuadratico = reader.IsDBNull(reader.GetOrdinal("ErroMedioQuadratico")) ? 
                                         null : (double?)reader.GetDouble(reader.GetOrdinal("ErroMedioQuadratico")),
                    Observacoes = reader.GetString(reader.GetOrdinal("Observacoes")),
                    Ativa = reader.GetInt32(reader.GetOrdinal("Ativa")) == 1
                });
            }
            return lista;
        }
        public List<PontoCalibracaoDb> ObterPontosCalibracao(int coeficienteId)
        {
            var lista = new List<PontoCalibracaoDb>();

            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = "SELECT * FROM PontosCalibracao WHERE CoeficienteId = $coefId ORDER BY Timestamp";
            command.Parameters.AddWithValue("$coefId", coeficienteId);

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                lista.Add(new PontoCalibracaoDb
                {
                    Id = reader.GetInt32(reader.GetOrdinal("Id")),
                    CoeficienteId = reader.GetInt32(reader.GetOrdinal("CoeficienteId")),
                    LeituraSensor = reader.GetDouble(reader.GetOrdinal("LeituraSensor")),
                    AnguloReferencia = reader.GetDouble(reader.GetOrdinal("AnguloReferencia")),
                    Timestamp = DateTime.Parse(reader.GetString(reader.GetOrdinal("Timestamp")))
                });
            }
            return lista;
        }
        public void AtualizarCoeficiente(int id, CoeficienteCalibracao coeficiente)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();
            
            var command = connection.CreateCommand();
            command.CommandText =
                @"
                UPDATE CoeficientesCalibracao SET
                    DataCalibracao = $dataCalibracao,
                    SensorIdentificador = $sensorId,
                    CoeficienteA = $coefA,
                    CoeficienteB = $coefB,
                    CoeficienteC = $coefC,
                    QuantidadePontos = $quantidadePontos,
                    ErroMedioQuadratico = $erroMedio,
                    Observacoes = $observacoes,
                    Ativa = $ativa
                WHERE Id = $id
            ";
            
            command.Parameters.AddWithValue("$id", id);
            command.Parameters.AddWithValue("$dataCalibracao", coeficiente.DataCalibracao.ToString("yyyy-MM-dd HH:mm:ss"));
            command.Parameters.AddWithValue("$sensorId", coeficiente.SensorIdentificador);
            command.Parameters.AddWithValue("$coefA", coeficiente.CoeficienteA);
            command.Parameters.AddWithValue("$coefB", coeficiente.CoeficienteB);
            command.Parameters.AddWithValue("$coefC", coeficiente.CoeficienteC);
            command.Parameters.AddWithValue("$quantidadePontos", coeficiente.QuantidadePontos);
            command.Parameters.AddWithValue("$erroMedio", coeficiente.ErroMedioQuadratico ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("$observacoes", coeficiente.Observacoes);
            command.Parameters.AddWithValue("$ativa", coeficiente.Ativa ? 1 : 0);
            
            command.ExecuteNonQuery();
        }
        public void DesativarOutrosCoeficientes(string sensorIdentificador, int coeficienteIdAtivo)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();
            
            var command = connection.CreateCommand();
            command.CommandText =
                @"
                UPDATE CoeficientesCalibracao 
                SET Ativa = 0 
                WHERE SensorIdentificador = $sensorId AND Id != $idAtivo
            ";
            
            command.Parameters.AddWithValue("$sensorId", sensorIdentificador);
            command.Parameters.AddWithValue("$idAtivo", coeficienteIdAtivo);
            
            command.ExecuteNonQuery();
        }
        public void DeletarCoeficiente(int id)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();
            
            var command = connection.CreateCommand();
            command.CommandText = "DELETE FROM CoeficientesCalibracao WHERE Id = $id";
            command.Parameters.AddWithValue("$id", id);
            
            command.ExecuteNonQuery();
        }

        #endregion
        public void Inserir(PropriedadesEnsaio e)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();
            
            var command = connection.CreateCommand();
            command.CommandText =
                @"
                INSERT INTO PropriedadesEnsaio (
                    Amostra, AmostraNumero, Local, Responsavel, DataEnsaio,
                    TipoRocha, FormatoCorpoProva, Altura, Largura, Profundidade,
                    AreaContato, TaxaInclinacao, InclinacaoMaxima, DeslocamentoMaximo, Observacoes
                )
                VALUES (
                    $amostra, $numero, $local, $resp, $data,
                    $tipo, $formato, $altura, $largura, $profundidade,
                    $area, $taxa, $inclinacao, $deslocamento, $obs
                )
            ";
            
            command.Parameters.AddWithValue("$amostra", e.Amostra);
            command.Parameters.AddWithValue("$numero", e.AmostraNumero);
            command.Parameters.AddWithValue("$local", e.Local);
            command.Parameters.AddWithValue("$resp", e.Responsavel);
            command.Parameters.AddWithValue("$data", e.DataEnsaio.ToString("yyyy-MM-dd HH:mm:ss"));
            command.Parameters.AddWithValue("$tipo", e.TipoRocha);
            command.Parameters.AddWithValue("$formato", e.FormatoCorpoProva);
            command.Parameters.AddWithValue("$altura", e.Altura);
            command.Parameters.AddWithValue("$largura", e.Largura);
            command.Parameters.AddWithValue("$profundidade", e.Profundidade);
            command.Parameters.AddWithValue("$area", e.AreaContato);
            command.Parameters.AddWithValue("$taxa", e.TaxaInclinacao);
            command.Parameters.AddWithValue("$inclinacao", e.InclinacaoMaxima);
            command.Parameters.AddWithValue("$deslocamento", e.DeslocamentoMaximo);
            command.Parameters.AddWithValue("$obs", e.Observacoes);
            
            command.ExecuteNonQuery();
        }

        public List<PropriedadesEnsaio> ObterTodos()
        {
            var lista = new List<PropriedadesEnsaio>();

            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = "SELECT * FROM PropriedadesEnsaio";
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                lista.Add(new PropriedadesEnsaio
                {   
                    Id = reader.GetInt32(reader.GetOrdinal("Id")),
                    Amostra = reader.GetString(reader.GetOrdinal("Amostra")),
                    AmostraNumero = reader.GetInt32(reader.GetOrdinal("AmostraNumero")),
                    Local = reader.GetString(reader.GetOrdinal("Local")),
                    Responsavel = reader.GetString(reader.GetOrdinal("Responsavel")),
                    DataEnsaio = DateTime.Parse(reader.GetString(reader.GetOrdinal("DataEnsaio"))),
                    TipoRocha = reader.GetString(reader.GetOrdinal("TipoRocha")),
                    FormatoCorpoProva = reader.GetString(reader.GetOrdinal("FormatoCorpoProva")),
                    Altura = reader.GetDouble(reader.GetOrdinal("Altura")),
                    Largura = reader.GetDouble(reader.GetOrdinal("Largura")),
                    Profundidade = reader.GetDouble(reader.GetOrdinal("Profundidade")),
                    AreaContato = reader.GetDouble(reader.GetOrdinal("AreaContato")),
                    TaxaInclinacao = reader.GetDouble(reader.GetOrdinal("TaxaInclinacao")),
                    InclinacaoMaxima = reader.GetDouble(reader.GetOrdinal("InclinacaoMaxima")),
                    DeslocamentoMaximo = reader.GetDouble(reader.GetOrdinal("DeslocamentoMaximo")),
                    Observacoes = reader.GetString(reader.GetOrdinal("Observacoes"))
                });
            }
            return lista;
        }

        public void Atualizar(int id, PropriedadesEnsaio e)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();
            var command = connection.CreateCommand();
            command.CommandText =
                @"
                UPDATE PropriedadesEnsaio SET
                    Amostra = $amostra,
                    AmostraNumero = $numero,
                    Local = $local,
                    Responsavel = $resp,
                    DataEnsaio = $data,
                    TipoRocha = $tipo,
                    FormatoCorpoProva = $formato,
                    Altura = $altura,
                    Largura = $largura,
                    Profundidade = $profundidade,
                    AreaContato = $area,
                    TaxaInclinacao = $taxa,
                    InclinacaoMaxima = $inclinacao,
                    DeslocamentoMaximo = $deslocamento,
                    Observacoes = $obs
                WHERE Id = $id
            ";
            command.Parameters.AddWithValue("$id", id);
            command.Parameters.AddWithValue("$amostra", e.Amostra);
            command.Parameters.AddWithValue("$numero", e.AmostraNumero);
            command.Parameters.AddWithValue("$local", e.Local);
            command.Parameters.AddWithValue("$resp", e.Responsavel);
            command.Parameters.AddWithValue("$data", e.DataEnsaio.ToString("yyyy-MM-dd HH:mm:ss"));
            command.Parameters.AddWithValue("$tipo", e.TipoRocha);
            command.Parameters.AddWithValue("$formato", e.FormatoCorpoProva);
            command.Parameters.AddWithValue("$altura", e.Altura);
            command.Parameters.AddWithValue("$largura", e.Largura);
            command.Parameters.AddWithValue("$profundidade", e.Profundidade);
            command.Parameters.AddWithValue("$area", e.AreaContato);
            command.Parameters.AddWithValue("$taxa", e.TaxaInclinacao);
            command.Parameters.AddWithValue("$inclinacao", e.InclinacaoMaxima);
            command.Parameters.AddWithValue("$deslocamento", e.DeslocamentoMaximo);
            command.Parameters.AddWithValue("$obs", e.Observacoes);
            command.ExecuteNonQuery();
        }

        public void Deletar(int id)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();
            var command = connection.CreateCommand();
            command.CommandText = "DELETE FROM PropriedadesEnsaio WHERE Id = $id";
            command.Parameters.AddWithValue("$id", id);
            command.ExecuteNonQuery();
        }
        
    }
}
