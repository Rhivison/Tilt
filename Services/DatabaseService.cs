using System;
using Microsoft.Data.Sqlite;
using System.Collections.Generic;
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
        }

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
