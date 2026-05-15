using System;
using MySql.Data.MySqlClient;

namespace BrewTrack
{
    // PUBLIC CLASS — reusable MySQL connection for the whole app
    public class DBConnection
    {
        private static string server   = "localhost";
        private static string port     = "3307";       
        private static string database = "brewtrackdb";
        private static string user     = "root";
        private static string password = "";      

        private static string ConnectionString =>
            $"Server={server};Port={port};Database={database};" +
            $"Uid={user};Pwd={password};";

        public static MySqlConnection GetConnection()
        {
            var conn = new MySqlConnection(ConnectionString);
            conn.Open();
            return conn;
        }

        public static bool TestConnection()
        {
            try
            {
                using (var conn = GetConnection())
                    return conn.State == System.Data.ConnectionState.Open;
            }
            catch
            {
                return false;
            }
        }
    }
}
