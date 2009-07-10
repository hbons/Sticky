using System;
using System.Data;
using Mono.Data.SqliteClient;

public class Sticky {

	public static void Main(string[] args)
	{
		NotesDatabase db = new NotesDatabase();
		db.fetch_notes();
	}

    public class NotesDatabase {

		public IDbConnection dbcon;
		public IDbCommand dbcmd;

		public NotesDatabase() {
			string connection_string = "URI=file:notes.db,version=3";
			this.dbcon = (IDbConnection) new SqliteConnection(connection_string);
			this.dbcon.Open();
			this.dbcmd = dbcon.CreateCommand();
		}

		public void fetch_notes() {
			string sql_count = "SELECT COUNT(*) FROM notes";
			string sql = "SELECT * FROM notes";
			dbcmd.CommandText = sql;
			IDataReader reader = dbcmd.ExecuteReader();
			//NoteData[] arr = new NoteData[];
			while(reader.Read()) {
			    string Content = reader.GetString (0);
			    Console.WriteLine("Content: " + Content);
			}
			// clean up
			reader.Close();
			reader = null;
			dbcmd.Dispose();
			dbcmd = null;
			this.dbcon.Close();
			this.dbcon = null;
		}
	}
}
