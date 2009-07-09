 public class Note {

   private String text;
   private String color;
   private int pos_x;
   private int pos_y;

   public Note(String text, String color, int pos_x, int pos_y) {
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
