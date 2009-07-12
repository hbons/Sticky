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
		StickyUI UI = new StickyUI();


		Application.Run();
	}
}

public class StickyUI {

	public StatusIcon status_icon;
	public bool notes_showing;
	public Window background_window;
	public EventBox add_eventbox;
	public Fixed grid;
	public NotesDatabase db;

	public NoteData[] notes;
	public NoteWindow[] note_windows;
	
	public StickyUI() {
		this.SetupWindow();
		this.LoadNotes();
		this.background_window.HideAll(); 			
		this.notes_showing = false;
		this.status_icon = StatusIcon.NewFromIconName("tomboy");
		this.status_icon.Activate += new EventHandler(this.ToggleNotes);
	}

	public void ToggleNotes(object obj, EventArgs args) {
		if(!this.notes_showing) {
			this.notes_showing = true;
			this.background_window.ShowAll(); 
			foreach(NoteWindow x in this.note_windows) {
				x.ShowAll();
			}
		}
		else {
			this.notes_showing = false;
			foreach(NoteWindow x in this.note_windows) {
				x.Hide();
				this.background_window.HideAll();
			} 			
		}
	}

	public void SetupWindow() {
		this.background_window = new Window("Sticky");
		this.background_window.Opacity = 0.75;
        this.background_window.ModifyBg( StateType.Normal, new Gdk.Color (0, 0, 0) );
		this.background_window.Decorated = false;
		this.background_window.Maximize(); // Fullscreen() later
		this.background_window.DeleteEvent += new DeleteEventHandler (Window_Delete);

		this.add_eventbox = new EventBox();
		this.add_eventbox.Add(new Gtk.Image("./note-add.png"));
		this.add_eventbox.VisibleWindow = false;
		this.add_eventbox.ButtonPressEvent += new ButtonPressEventHandler (AddNote);
		this.grid = new Fixed();
		this.grid.Put(add_eventbox, 12, 12);
		this.background_window.Add(this.grid);
	}

	public void LoadNotes() {

		this.db = new NotesDatabase();

		this.notes = db.fetch_notes();
		this.note_windows = new NoteWindow[this.notes.Length];

		int i = 0;
		foreach(NoteData x in this.notes) {
			this.note_windows[i] = new NoteWindow(x,background_window);
			i++;
		}
	}

	public void AddNote(object obj, EventArgs args){
		int last_id;
		NoteData new_data;
		NoteWindow new_window;
		NotesDatabase db;

		db = new NotesDatabase();
		last_id = db.CreateNote();

		new_data = new NoteData("","ffffff",500,500,last_id);
		new_window = new NoteWindow(new_data,this.background_window);
		new_window.ShowAll();
		/*
		this.note_windows[this.note_windows.Length] = new_window;
		new_window.window.ShowAll();
		*/
	}

	static void Window_Delete (object obj, DeleteEventArgs args)
	{
		Application.Quit ();
		args.RetVal = true;
	}

}

public class NoteWindow : Window {

	public NoteData data;
	private Gtk.TextView view;
	private Gtk.TextBuffer buffer;
	public string font_size;
	private Gdk.Pixbuf image;
	public int max_lines;
	public int max_characters;

	public NoteWindow (NoteData note_data, Window parent) : base ("Note") {
		this.data = note_data;
		TransientFor = parent;
		DestroyWithParent = true;
		SetSizeRequest (250, 210);
		Resizable = false;
		Move(this.data.get_pos_x(), this.data.get_pos_y());
		Decorated = false;
		SkipPagerHint = true;
		SkipTaskbarHint = true;
		BorderWidth = 12;
		ConfigureEvent += window_position_changed;
        ModifyBg( StateType.Normal, new Gdk.Color (0xf4, 0xff, 0x51) );

		this.view = new Gtk.TextView ();
        this.view.ModifyBase( StateType.Normal, new Gdk.Color (0xf4, 0xff, 0x51) );
		this.view.ModifyBg( StateType.Normal, new Gdk.Color (0xf4, 0xff, 0x51) );
        this.view.WrapMode = Gtk.WrapMode.WordChar;

		this.buffer = this.view.Buffer;
		this.buffer.Text = this.data.get_text();
		this.buffer.Changed += this.text_change;
		this.font_size = "14";
		this.max_lines = 8;
		this.max_characters = 7;
		this.resize_font();
		image = new Gdk.Pixbuf( "noise.png" );
		Add(view);
	}

	public void resize_font() {

			this.font_size = "14";
		if (this.buffer.LineCount >= 8) {
			this.font_size = "12";
			this.max_lines = 9;}
		if (this.buffer.LineCount >= 9){
			this.font_size = "11";
			this.max_lines = 10;}
		if (this.buffer.LineCount >= 10){
			this.font_size = "10";
			this.max_lines = 10;}
		this.view.ModifyFont(Pango.FontDescription.FromString("Rufscript " + this.font_size));
	}

	public void text_change(object sender, System.EventArgs args) {

	/*
		if(this.buffer.CharCount > this.max_characters) {
			this.buffer.Text = this.buffer.Text.Substring(1);
		}
	*/

		if(this.buffer.LineCount > this.max_lines) {
			this.buffer.Text = this.buffer.Text.TrimEnd();
		}
		

		NotesDatabase db = new NotesDatabase();
		this.data.set_text (this.buffer.Text);
		this.resize_font();
		db.UpdateNoteContent(this.data);
	}

	/*
	protected override bool OnExposeEvent (Gdk.EventExpose evnt)
	{
		
		GdkWindow.DrawPixbuf (Style.WhiteGC, image, 0, 0, 0, 0,
			Allocation.Width, Allocation.Height, Gdk.RgbDither.None, 0, 0);
		return true;
	}
	*/

	[GLib.ConnectBefore]
	public void window_position_changed(object sender, System.EventArgs args) {
		int x;
		int y;
		GetPosition(out x, out y);
		this.data.set_pos_x (x);
		this.data.set_pos_y (y);
		NotesDatabase db = new NotesDatabase();
		db.UpdateNotePosition(this.data);
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
		text = text.Replace('"', '\"');
		text = text.Replace("'", "`"); // Dirty hack
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
		this.SetupDatabase();

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

	private void SetupDatabase() {
		this.open_connection ();
		try {
			this.dbcmd.CommandText = "SELECT * FROM notes";
			this.dbcmd.ExecuteNonQuery();
		}
		catch (SqliteSyntaxException no_table) {
			this.dbcmd.CommandText = "CREATE TABLE notes (text TEXT, color TEXT, pos_x INTEGER, pos_y INTEGER, id INTEGER PRIMARY KEY AUTOINCREMENT)";
			this.dbcmd.ExecuteNonQuery();
			this.dbcmd.CommandText = "INSERT INTO notes(text,color,pos_x,pos_y) VALUES ('Welcome to Sticky! This \nis your first note. Just \nclick on it to edit it. \nDo not worry about \nsaving, it is all done \nautomatically.','ffffff',100,100)";
			this.dbcmd.ExecuteNonQuery();
			this.close_connection();
		}
	}

	public NoteData[] fetch_notes() {
		this.open_connection();
		this.dbcmd.CommandText = "SELECT COUNT(id) FROM notes";
		IDataReader count_reader = dbcmd.ExecuteReader();
		count_reader.Read();
		string count = count_reader.GetString (0);
		count_reader.Close();
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

	public void UpdateNoteContent(NoteData note_data) {
		this.open_connection ();
		this.dbcmd.CommandText = "UPDATE notes SET text = '" + note_data.get_text() + "' WHERE id = " + note_data.get_id();
		this.dbcmd.ExecuteNonQuery();
		this.close_connection();			
	}

	public void UpdateNotePosition(NoteData note_data) {
		this.open_connection ();
		this.dbcmd.CommandText = "UPDATE notes SET pos_x = " + note_data.get_pos_x() + ", pos_y = " + note_data.get_pos_y() + " WHERE id = " + note_data.get_id();
		this.dbcmd.ExecuteNonQuery();
		this.close_connection();			
	}


	public int CreateNote() {
		this.open_connection ();

		this.dbcmd.CommandText = "INSERT INTO notes (text,color,pos_x,pos_y) VALUES ('...','ffffff',100,100)";
		this.dbcmd.ExecuteNonQuery();

		this.dbcmd.CommandText = "SELECT id FROM notes ORDER BY id DESC";
		IDataReader last_id_reader = dbcmd.ExecuteReader();
		last_id_reader.Read();
		int last_id = int.Parse(last_id_reader.GetString (0));

		this.close_connection();
		return last_id;
	}
}
