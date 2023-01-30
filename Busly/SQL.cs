using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Busly
{
    public class SQL
    {

        static void Main(string[] args) 
        {
           

        }

        public static SQLiteConnection CreateConnection()
        {

            SQLiteConnection sqlite_conn;
            // Create a new database connection:
            sqlite_conn = new SQLiteConnection($"Data Source=database.db;Version=3;New=True;Compress=True;");

             // Open the connection:
             try
                {
                    sqlite_conn.Open();
                }
                catch (Exception ex)
                {

                }

             return sqlite_conn;
         }

        public static void ExecuteCommand(String command, ref SQLiteDataReader result)
        {

            SQLiteConnection sqlite_conn;
            sqlite_conn = CreateConnection();

            SQLiteDataReader sqlite_datareader;
            SQLiteCommand sqlite_cmd;
            sqlite_cmd = sqlite_conn.CreateCommand();
            sqlite_cmd.CommandText = command;

            sqlite_datareader = sqlite_cmd.ExecuteReader();
            result = sqlite_datareader;

            sqlite_conn.Close();
        }



    }
}
