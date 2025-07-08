using Microsoft.Data.Sqlite;
using System.Collections.Generic;

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
                CREATE TABLE IF NOT EXISTS Ensaios (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    AmostraNumero INTEGER,
                    AmostraNome TEXT,
                    Local TEXT,
                    Responsavel TEXT,
                    DataEnsaio TEXT,
                    FormatoCorpoProva TEXT
                );
            ";
            command.ExecuteNonQuery();
        }

        public List<Ensaio> ObterEnsaios()
        {
            var lista = new List<Ensaio>();
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = "SELECT * FROM Ensaios";

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                lista.Add(new Ensaio
                {
                    AmostraNumero = reader.GetInt32(1),
                    AmostraNome = reader.GetString(2),
                    Local = reader.GetString(3),
                    Responsavel = reader.GetString(4),
                    DataEnsaio = reader.GetString(5),
                    FormatoCorpoProva = reader.GetString(6)
                });
            }

            return lista;
        }

        public void InserirEnsaio(Ensaio e)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText =
            @"
                INSERT INTO Ensaios (AmostraNumero, AmostraNome, Local, Responsavel, DataEnsaio, FormatoCorpoProva)
                VALUES ($numero, $nome, $local, $resp, $data, $formato)
            ";
            command.Parameters.AddWithValue("$numero", e.AmostraNumero);
            command.Parameters.AddWithValue("$nome", e.AmostraNome);
            command.Parameters.AddWithValue("$local", e.Local);
            command.Parameters.AddWithValue("$resp", e.Responsavel);
            command.Parameters.AddWithValue("$data", e.DataEnsaio);
            command.Parameters.AddWithValue("$formato", e.FormatoCorpoProva);
            command.ExecuteNonQuery();
        }
    }
}
