// License GNU GPL v3
// (C) Kalle Persson, Hylke Bons, Jakub Steiner
//
//

using System;
using System.Data;
using Mono.Data.SqliteClient;
using Gtk;

 
public class Sticky {

	public static void Main() {
		Application.Init();
		//Create the Window
		StatusIcon status_icon = StatusIcon.NewFromIconName("tomboy");
		status_icon.Activate += new EventHandler(ShowNotes);
		Application.Run();
	}

	public static void AddNote(object obj, EventArgs args){
		Console.WriteLine("Add note!");
		//newNote = new NoteWindow();
	}

	public static void ShowNotes(object obj, EventArgs args) {

		NotesDatabase db;
		EventBox add_eventbox;
		Window background_window;

		db = new NotesDatabase();
		background_window = new Window("Sticky");
		background_window.Opacity = 0.85;
        background_window.ModifyBg( StateType.Normal, new Gdk.Color (0, 0, 0) );
		background_window.Decorated = false;
		background_window.Maximize(); // Fullscreen() later
		background_window.DeleteEvent += new DeleteEventHandler (Window_Delete);

		add_eventbox = new EventBox();
		add_eventbox.Add(new Gtk.Image("./note-add.png"));
		//add_eventbox.Relief = Gtk.ReliefStyle.None;
		add_eventbox.Realize();
		add_eventbox.ButtonPressEvent += new ButtonPressEventHandler (AddNote);
		//add_eventbox.Clicked += new EventHandler(AddNote);
		Fixed grid = new Fixed();
		grid.Put(add_eventbox, 12, 12);
		background_window.Add (grid);

		background_window.ShowAll(); 

		NoteData[] Notes = db.fetch_notes();
		NoteWindow[] notewindows = new NoteWindow[Notes.Length];

		foreach(NoteData x in Notes) {
			notewindows[0] = new NoteWindow(x,background_window);
		}


	}

	static void Window_Delete (object obj, DeleteEventArgs args)
	{
		Application.Quit ();
		args.RetVal = true;
	}

}

public class NoteWindow {

	public NoteData data;
	private Window window;
	private Gtk.TextView view;
	private Gtk.TextBuffer buffer;

	public NoteWindow (NoteData note_data, Window parent) {
		this.data = note_data;
		this.window = new Window("Note");
		this.window.TransientFor = parent;
		this.window.DestroyWithParent = true;
		this.window.Resize (250, 200);
		this.window.Move(this.data.get_pos_x(), this.data.get_pos_y());
		//this.window.HasFrame = true;
		//this.window.SetFrameDimensions(12, 12, 12, 12);
		this.window.Decorated = false;
                
		this.view = new Gtk.TextView ();
		this.buffer = this.view.Buffer;
		this.buffer.Text = this.data.get_text();
		this.buffer.Changed += new System.EventHandler(this.SaveNotes);
		
        this.view.WrapMode = Gtk.WrapMode.WordChar;
        this.view.LeftMargin = 12;
        this.view.RightMargin = 12;  
        this.view.PixelsAboveLines = 12;
        this.view.PixelsBelowLines = 12;

        this.view.ModifyBase( StateType.Normal, new Gdk.Color (0xf4, 0xff, 0x51) );

		this.window.Add(view);
		this.window.ShowAll();	 
	}

	public void SaveNotes(object sender, System.EventArgs args) {
		NotesDatabase db = new NotesDatabase();
		Console.WriteLine (this.buffer.Text);
		//db.SaveNote(this.data);
	}
}


public class NoteData {

	private int id;
	private String text;
	private String color;
	private int pos_x;
	private int pos_y;

	public NoteData(String text, String color, int pos_x, int pos_y,int id) {
		this.id = id;
		this.text = text;
		this.color = color;
		this.pos_x = pos_x;
		this.pos_y = pos_y;
	}

	public int get_id() {
		return id;
	}

	public String get_text() {
		return text;
	}

	public String get_color() {
		return color;
	}

	public int get_pos_x() {
		return pos_x;
	}

	public int get_pos_y() {
		return pos_y;
	}

	public void set_id(int id) {
		this.id = id;
	}

	public void set_text(String text) {
		this.text = text;
	}

	public void set_color(String color) {
		this.color = color;
	}

	public void set_pos_x(int pos_x) {
		this.pos_x = pos_x;
	}

	public void set_pos_y(int pos_y) {
		this.pos_y = pos_y;
	}
}


public class NotesDatabase {

	public IDbConnection dbcon;
	public IDbCommand dbcmd;

	public NotesDatabase() {

	}

	private void open_connection() {
		string connection_string = "URI=file:notes.db,version=3";
		this.dbcon = (IDbConnection) new SqliteConnection(connection_string);
		this.dbcon.Open();
		this.dbcmd = dbcon.CreateCommand();		 
	}

	private void close_connection() {
		dbcmd.Dispose();
		dbcmd = null;
		this.dbcon.Close();
		this.dbcon = null;		 
	}

	public NoteData[] fetch_notes() {
		this.open_connection();

		this.dbcmd.CommandText = "SELECT COUNT(*) FROM notes";
		IDataReader count_reader = dbcmd.ExecuteReader();

		count_reader.Read();
		string count = count_reader.GetString (0);
		Console.WriteLine("Number of rows: " + count);
		count_reader.Close();
		count_reader = null;
		
		NoteData[] arr = new NoteData[int.Parse(count)];

		this.dbcmd.CommandText = "SELECT * FROM notes";
		IDataReader note_reader = dbcmd.ExecuteReader();
		int row = 0;
		while(note_reader.Read()){
			arr[row] = new NoteData(
				note_reader.GetString (0),
				note_reader.GetString (1),
				int.Parse(note_reader.GetString (2)),
				int.Parse(note_reader.GetString (3)),
				int.Parse(note_reader.GetString (4))
			);
			row++;
		}

		note_reader.Close();
		note_reader = null;
		this.close_connection();
		return arr;
	}

	public void SaveNote(NoteData note_data) {
		this.open_connection ();

		this.dbcmd.CommandText = "UPDATE TABLE notes SET text = " + note_data.get_text() + " WHERE id = " + note_data.get_id();
		dbcmd.ExecuteReader();

		this.close_connection();			
	}
}
