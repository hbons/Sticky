//
//
//
//

 using System;
 using Gtk;
// using Mono.Data.SqliteClient;

 
 public class Sticky {

  
   public static void Main() {

     Application.Init();
 
     //Create the Window
     StatusIcon status_icon = StatusIcon.NewFromIconName("tomboy");
     status_icon.Activate += new EventHandler(ShowNotes);

     Application.Run();

   }

   public static void ShowNotes(object obj, EventArgs args) {

     Window background_window = new Window("Sticky");
     background_window.Maximize(); // Fullscreen() later
     background_window.ShowAll(); 



     NoteWindow[] notes = new NoteWindow[4];
     notes[0] = new NoteWindow( new NoteData ("Have you been high today?", "#00ff00", 470, 256), background_window);
     notes[1] = new NoteWindow( new NoteData ("I see the nuns are gay!", "#00ff00", 100, 152), background_window);
     notes[2] = new NoteWindow( new NoteData ("Now poop on them Oliver!", "#00ff00", 600, 234), background_window);
     notes[3] = new NoteWindow( new NoteData ("test?", "#00ff00", 360, 55), background_window);




   }



 }

 public class NoteWindow {

   public NoteData data;
   private Window window;
   private Gtk.TextView view;
   private Gtk.TextBuffer buffer;

   public NoteWindow (NoteData note_data, Window parent) {
     this.data = note_data;
     this.window = new Window("test_note");
     this.window.TransientFor = parent;
     this.window.DestroyWithParent = true;
     this.window.Resize (200, 200);
     this.window.Move(this.data.get_pos_x(), this.data.get_pos_y());
     this.window.HasFrame = true;
     this.window.SetFrameDimensions(12, 12, 12, 12);

     this.view = new Gtk.TextView ();
     this.buffer = this.view.Buffer;
     this.buffer.Text = this.data.get_text();

     this.window.Add(view);
     this.window.ShowAll();      
   }

 }


 public class NoteData {

   private String text;
   private String color;
   private int pos_x;
   private int pos_y;

   public NoteData(String text, String color, int pos_x, int pos_y) {
     this.text  = text;
     this.color = color;
     this.pos_x = pos_x;
     this.pos_y = pos_y;
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
