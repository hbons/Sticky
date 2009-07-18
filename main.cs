// License: GNU GPLv3
// (c) 2009, Kalle Persson, Hylke Bons, Jakub Steiner

using System;
using System.Data;
using GLib;
using Gtk;
using Mono.Data.SqliteClient;

public class Sticky {

	public static void Main(String[] args) {

		Application.Init();
		StickyUI UI = new StickyUI();

		if(args.Length > 0 && args[0] == "-h")
			UI.HideNotes();
		else
			UI.ShowNotes();

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
	GLib.List note_windows;
	
	public StickyUI() {
		this.SetupWindow();
		this.LoadNotes();
		this.status_icon = StatusIcon.NewFromIconName("gnome-sticky-notes-applet");
		this.status_icon.Activate += new EventHandler(this.ToggleNotes);	
		this.notes_showing = true;
	}

	public void ToggleNotes(object obj, EventArgs args) {
		if(!this.notes_showing) {
			this.ShowNotes();
		}
		else {
			this.HideNotes();
		} 			
	}

	public void ShowNotes() {
		this.background_window.ShowAll(); 
		foreach(NoteWindow note_window in this.note_windows) {
			note_window.ShowAll();
		}
		this.notes_showing = true;
		//WORKAROUND: Must be set every time we show notes to stay opaque.
		this.background_window.Opacity = 0.75;
	}

	public void HideNotes() {
		foreach(NoteWindow note_window in this.note_windows) {
			note_window.Hide();
		}
		this.background_window.HideAll();
		this.notes_showing = false;
	}

	public void SetupWindow() {
		this.background_window = new Window("Sticky");

        this.background_window.ModifyBg( StateType.Normal, new Gdk.Color (0, 0, 0) );
		this.background_window.Decorated = false;
		this.background_window.Opacity = 0.6;
		this.background_window.Maximize(); // Fullscreen() later
		this.background_window.DeleteEvent += new DeleteEventHandler (Window_Delete);
		this.background_window.KeyReleaseEvent += new KeyReleaseEventHandler(check_shortcuts);

		this.note_windows = new GLib.List (typeof (NoteWindow));

		this.add_eventbox = new EventBox();
		this.add_eventbox.Add(new Gtk.Image("./note-add.png"));
		this.add_eventbox.VisibleWindow = false;
		this.add_eventbox.ButtonPressEvent += new ButtonPressEventHandler (AddNote);
		//this.add_eventbox.EnterNotifyEvent += new EnterNotifyEventHandler (AddNote);
		this.grid = new Fixed();
		this.grid.Put(add_eventbox, 10, 10);
		this.background_window.Add(this.grid);

	}

	public void check_shortcuts(object sender, Gtk.KeyReleaseEventArgs args) {
        Gdk.Key key = args.Event.Key;
		if(key == Gdk.Key.Escape && this.notes_showing) {
			this.HideNotes();
		}
		else if(key == Gdk.Key.q) {
			Application.Quit ();
		}
	}

	public void LoadNotes() {
		this.db = new NotesDatabase();
		this.notes = db.fetch_notes();
		foreach(NoteData x in this.notes) {
			this.note_windows.Append(new NoteWindow(x,background_window));
		}
	}

	public void AddNote(object obj, EventArgs args){

		NotesDatabase database = new NotesDatabase();
		database.QueryNoResults("INSERT INTO notes (text, color, pos_x, pos_y) VALUES ('', '#ffffff', 100, 100)");

		NoteData new_note_data = new NoteData("", RandomColor(), 450, 450, database.get_last_id());
		NoteWindow new_window = new NoteWindow(new_note_data, this.background_window);
		new_window.ShowAll();
		this.note_windows.Append(new_window);

	}

	public static string RandomColor() {
		GLib.List colors = new GLib.List (typeof (string));
		colors.Append("#f4ff51");
		colors.Append("#88dcd5");
		colors.Append("#b3f75f");
		colors.Append("#f75f77");
		colors.Append("#dc5ff7");
		Random random = new Random();
		return (string)colors[random.Next(colors.Count)];
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
//	private Gdk.Pixbuf image;
	public int max_lines;
	public int max_characters;
	private bool marked_for_deletion;

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

		Gdk.Color note_color = new Gdk.Color();
		Gdk.Color.Parse(note_data.get_color(), ref note_color);

        ModifyBg(StateType.Normal,note_color);

		this.view = new Gtk.TextView ();
        this.view.WrapMode = Gtk.WrapMode.WordChar;
        this.view.ModifyBase( StateType.Normal,note_color);

		this.view.KeyReleaseEvent += new KeyReleaseEventHandler(this.check_deletion);

		this.buffer = this.view.Buffer;
		this.buffer.Text = this.data.get_text();
		this.buffer.Changed += this.text_change;
		this.font_size = "14";
		this.max_lines = 8;
		this.max_characters = 7;
		this.resize_font();

		//image = new Gdk.Pixbuf( "noise.png" );
		Add(view);
	}

	public void check_deletion(object sender, Gtk.KeyReleaseEventArgs args) {
        Gdk.Key key = args.Event.Key;
		if(key == Gdk.Key.BackSpace) {

			if (this.buffer.Text == "") {
				if (this.marked_for_deletion)
					this.remove(); // Remove the window
				else
					this.marked_for_deletion = true;
			} else {
				if (this.marked_for_deletion)
					this.marked_for_deletion = false;
			}
		}
		else if(key != Gdk.Key.BackSpace && this.marked_for_deletion) {
			this.marked_for_deletion = false;
		}
	}

	public void resize_font() {
			this.font_size = "14";
		if (this.buffer.LineCount >= 8) {
			this.font_size = "12";
			this.max_lines = 9;
		}
		if (this.buffer.LineCount >= 9){
			this.font_size = "11";
			this.max_lines = 10;
		}
		if (this.buffer.LineCount >= 10){
			this.font_size = "10";
			this.max_lines = 11;
		}
		if (this.buffer.LineCount >= 11){
			this.font_size = "9";
			this.max_lines = 12;
		}
		if (this.buffer.LineCount >= 12){
			this.font_size = "8";
			this.max_lines = 12;
		}
		this.view.ModifyFont(Pango.FontDescription.FromString("Rufscript " + this.font_size));
	}

	public void text_change(object sender, System.EventArgs args) {
		if (this.buffer.LineCount > this.max_lines) {
			this.buffer.Text = this.buffer.Text.TrimEnd();
		}
		this.resize_font();
		this.data.set_text (this.buffer.Text);
		this.data.save();
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
		this.data.save();
	}

	public void remove() {
		this.data.remove();
		Destroy(); // destroy the window. TODO: NoteData is still in memory until next launch, needs to be removed.
	}
}


public class NoteData {

	private int id;
	private String text;
	private String color;
	private int pos_x;
	private int pos_y;

	public NoteData(String text, String color, int pos_x, int pos_y, int id) {
		this.id = id;
		this.text = text;
		this.color = color;
		this.pos_x = pos_x;
		this.pos_y = pos_y;
	}

	public int get_id () {
		return this.id;
	}

	public String get_text () {
		return this.text;
	}

	public String get_color () {
		return this.color;
	}

	public int get_pos_x () {
		return this.pos_x;
	}

	public int get_pos_y () {
		return this.pos_y;
	}

	public void set_id (int id) {
		this.id = id;
	}

	public void set_text (String text) {
		text = text.Replace('"', '\"');
		text = text.Replace("'", "`"); // Dirty hack
		this.text = text;
	}

	public void set_color (String color) {
		this.color = color;
	}

	public void set_pos_x (int pos_x) {
		this.pos_x = pos_x;
	}

	public void set_pos_y (int pos_y) {
		this.pos_y = pos_y;
	}

	public void save () {
		NotesDatabase database = new NotesDatabase();
		database.QueryNoResults("UPDATE notes SET text = '" + this.get_text() + "', " + 
					    		  "color = '" + this.get_color() + "'" +  
								  ", pos_x = " + this.get_pos_x() + " " + 
								  ", pos_y = " + this.get_pos_y() + " " + 
							   	  "WHERE id = " + this.get_id()
								 );
	}

	public void remove () {
		NotesDatabase database = new NotesDatabase();
		database.QueryNoResults("DELETE FROM notes WHERE id = " + this.get_id());		
	}

}

public class NotesDatabase {

	private IDbConnection dbcon;
	private IDbCommand dbcmd;

	public NotesDatabase() {

		// Create a database if none exists.
		try {
			QueryNoResults("SELECT * FROM notes");
		}
		catch (SqliteSyntaxException) {

			QueryNoResults("CREATE TABLE notes ( " +
							"text TEXT, color TEXT, " + 
							"pos_x INTEGER, pos_y INTEGER, " +
							"id INTEGER PRIMARY KEY AUTOINCREMENT )");

			QueryNoResults("INSERT INTO notes " + 
							 "(text, color, pos_x, pos_y) VALUES " + 
							 "('Welcome to Sticky! This \nis your first note. Just \nclick on it to edit it.\n" + 
							 "Do not worry about \nsaving, it is all done \nautomatically.', '#f4ff51', 100, 100)");

			QueryNoResults("INSERT INTO notes " + 
							 "(text, color, pos_x, pos_y) VALUES " + 
							 "('Remove all text in a note to delete it.', '#f4ff51', 250, 250)");

		}

	}

	public void OpenConnection() {
		string connection_string = "URI=file:notes.db,version=3";
		this.dbcon = (IDbConnection) new SqliteConnection(connection_string);
		this.dbcon.Open();
		this.dbcmd = dbcon.CreateCommand();		 
	}

	public void CloseConnection() {
		dbcmd.Dispose();
		dbcmd = null;
		this.dbcon.Close();
		this.dbcon = null;		 
	}

	public NoteData[] fetch_notes() {
		this.OpenConnection();
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
		this.CloseConnection();
		return arr;
	}
	
	public void QueryNoResults (String query) {
		this.OpenConnection ();
		this.dbcmd.CommandText = query;
		this.dbcmd.ExecuteNonQuery();
		this.CloseConnection();
	}

	public int get_last_id () {
		this.OpenConnection ();
		this.dbcmd.CommandText = "SELECT id FROM notes ORDER BY id DESC LIMIT 1";
		IDataReader reader = this.dbcmd.ExecuteReader();
		reader.Read();
		this.CloseConnection();
		return int.Parse(reader.GetString (0));
	}

}
