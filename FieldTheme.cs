using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace VietnamWarEditor {
public static class FieldTheme {
    public static readonly Color Background=Color.FromArgb(22,27,20),Panel=Color.FromArgb(34,41,29),Raised=Color.FromArgb(43,51,36),Input=Color.FromArgb(20,27,21),Border=Color.FromArgb(82,91,61),Text=Color.FromArgb(233,226,205),Muted=Color.FromArgb(165,175,145),Amber=Color.FromArgb(222,172,91),Green=Color.FromArgb(166,193,119),Error=Color.FromArgb(230,150,113),Selected=Color.FromArgb(69,86,47);
    public static readonly Font Body=new Font("Segoe UI",10f),Mono=new Font("Consolas",10f),SmallMono=new Font("Consolas",9f),Heading=new Font("Bahnschrift",18f,FontStyle.Bold);
    public static void Number(NumericUpDown n,int width,decimal max,int decimals){n.Font=Mono;n.Width=width;n.Maximum=max;n.Minimum=0;n.DecimalPlaces=decimals;n.ThousandsSeparator=true;n.BackColor=Input;n.ForeColor=Amber;n.BorderStyle=BorderStyle.FixedSingle;n.Margin=new Padding(0,3,10,0);n.TextAlign=HorizontalAlignment.Right;}
    public static void Combo(ComboBox c,int width){c.Font=Body;c.Width=width;c.ItemHeight=22;c.DropDownStyle=ComboBoxStyle.DropDownList;c.FlatStyle=FlatStyle.Flat;c.BackColor=Input;c.ForeColor=Text;c.IntegralHeight=false;c.DropDownHeight=290;c.Margin=new Padding(0,3,10,0);}
    public static void Grid(DataGridView g){
        g.Dock=DockStyle.Fill;g.Font=Body;g.BackgroundColor=Input;g.BorderStyle=BorderStyle.None;g.AllowUserToAddRows=false;g.AllowUserToDeleteRows=false;g.AllowUserToResizeRows=false;g.ReadOnly=true;g.MultiSelect=false;g.RowHeadersVisible=false;g.SelectionMode=DataGridViewSelectionMode.FullRowSelect;g.AutoSizeColumnsMode=DataGridViewAutoSizeColumnsMode.Fill;g.EnableHeadersVisualStyles=false;
        g.ColumnHeadersDefaultCellStyle.BackColor=Raised;g.ColumnHeadersDefaultCellStyle.ForeColor=Muted;g.ColumnHeadersDefaultCellStyle.Font=SmallMono;g.ColumnHeadersDefaultCellStyle.Padding=new Padding(8,0,8,0);g.ColumnHeadersBorderStyle=DataGridViewHeaderBorderStyle.Single;
        g.DefaultCellStyle.BackColor=Input;g.DefaultCellStyle.ForeColor=Text;g.DefaultCellStyle.SelectionBackColor=Selected;g.DefaultCellStyle.SelectionForeColor=Text;g.DefaultCellStyle.Padding=new Padding(8,0,8,0);g.AlternatingRowsDefaultCellStyle.BackColor=Color.FromArgb(26,33,24);g.GridColor=Color.FromArgb(47,58,38);g.CellBorderStyle=DataGridViewCellBorderStyle.SingleHorizontal;g.RowTemplate.Height=30;g.ColumnHeadersHeight=34;g.ColumnHeadersHeightSizeMode=DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
        g.HandleCreated+=(s,e)=>{DarkScrollbars(g);};
    }
    public static Label Caption(string text){return new Label{Text=text,Font=SmallMono,ForeColor=Muted,AutoSize=true,Margin=new Padding(0,8,0,0)};}
    [DllImport("dwmapi.dll")]static extern int DwmSetWindowAttribute(IntPtr hwnd,int attribute,ref int value,int size);
    [DllImport("uxtheme.dll",CharSet=CharSet.Unicode)]static extern int SetWindowTheme(IntPtr hwnd,string theme,string classes);
    static void DarkScrollbars(Control grid){try{SetWindowTheme(grid.Handle,"DarkMode_Explorer",null);foreach(Control child in grid.Controls)if(child is ScrollBar)SetWindowTheme(child.Handle,"DarkMode_Explorer",null);}catch{}}
    public static void TitleBar(Form form){try{int dark=1;DwmSetWindowAttribute(form.Handle,20,ref dark,4);int caption=ColorTranslator.ToWin32(Background);DwmSetWindowAttribute(form.Handle,35,ref caption,4);int text=ColorTranslator.ToWin32(Text);DwmSetWindowAttribute(form.Handle,36,ref text,4);}catch{}}
}
public static class FieldAssets {
    public static readonly Image Emblem=LoadImage();
    static Image LoadImage(){using(var s=Assembly.GetExecutingAssembly().GetManifestResourceStream("fielddesk.emblem"))using(var i=Image.FromStream(s))return new Bitmap(i);}
    public static Icon Icon(){using(var s=Assembly.GetExecutingAssembly().GetManifestResourceStream("fielddesk.icon"))using(var icon=new Icon(s))return (Icon)icon.Clone();}
}
public class FieldButton : Button {
    bool hover,pressed;public bool Primary {get;set;}
    public FieldButton(){SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.ResizeRedraw,true);FlatStyle=FlatStyle.Flat;FlatAppearance.BorderSize=0;BackColor=FieldTheme.Raised;ForeColor=FieldTheme.Text;Font=new Font("Segoe UI",9.5f,FontStyle.Bold);Height=34;Cursor=Cursors.Hand;Margin=new Padding(0,2,10,0);}
    protected override void OnMouseEnter(EventArgs e){hover=true;Invalidate();base.OnMouseEnter(e);}protected override void OnMouseLeave(EventArgs e){hover=false;pressed=false;Invalidate();base.OnMouseLeave(e);}protected override void OnMouseDown(MouseEventArgs e){pressed=true;Invalidate();base.OnMouseDown(e);}protected override void OnMouseUp(MouseEventArgs e){pressed=false;Invalidate();base.OnMouseUp(e);}protected override void OnEnabledChanged(EventArgs e){Invalidate();base.OnEnabledChanged(e);}
    protected override void OnPaint(PaintEventArgs e){
        Color fill=!Enabled?FieldTheme.Panel:Primary?(hover?Color.FromArgb(239,191,112):FieldTheme.Amber):hover?FieldTheme.Selected:FieldTheme.Raised;if(pressed&&Enabled)fill=FieldTheme.Border;
        using(var b=new SolidBrush(fill))e.Graphics.FillRectangle(b,ClientRectangle);using(var p=new Pen(!Enabled?Color.FromArgb(62,70,48):Primary?FieldTheme.Amber:FieldTheme.Border))e.Graphics.DrawRectangle(p,0,0,Width-1,Height-1);
        var color=!Enabled?Color.FromArgb(112,121,96):Primary?FieldTheme.Background:FieldTheme.Text;TextRenderer.DrawText(e.Graphics,Text,Font,ClientRectangle,color,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis|TextFormatFlags.SingleLine);
        if(Focused&&ShowFocusCues)ControlPaint.DrawFocusRectangle(e.Graphics,new Rectangle(4,4,Width-8,Height-8),color,fill);
    }
}
public class FieldNumber : NumericUpDown {
    public FieldNumber(){AutoScaleMode=AutoScaleMode.None;}
    SpinnerPaint spinner;
    protected override void OnHandleCreated(EventArgs e){base.OnHandleCreated(e);if(spinner!=null)spinner.ReleaseHandle();foreach(Control child in Controls)if(!(child is TextBoxBase)){spinner=new SpinnerPaint(child);break;}}
    protected override void Dispose(bool disposing){if(disposing&&spinner!=null)spinner.ReleaseHandle();base.Dispose(disposing);}
    sealed class SpinnerPaint:NativeWindow {
        readonly Control control;public SpinnerPaint(Control c){control=c;AssignHandle(c.Handle);}
        protected override void WndProc(ref Message m){base.WndProc(ref m);if(m.Msg==0xF){using(var g=Graphics.FromHwnd(Handle))using(var b=new SolidBrush(FieldTheme.Raised))using(var p=new Pen(FieldTheme.Border))using(var a=new SolidBrush(control.Enabled?FieldTheme.Amber:FieldTheme.Border)){
            int w=control.Width,h=control.Height,mid=h/2,x=w/2;g.FillRectangle(b,0,0,w,h);g.DrawLine(p,0,0,0,h);g.DrawLine(p,0,mid,w,mid);g.FillPolygon(a,new[]{new Point(x,mid/2-2),new Point(x-3,mid/2+2),new Point(x+3,mid/2+2)});g.FillPolygon(a,new[]{new Point(x,mid+mid/2+2),new Point(x-3,mid+mid/2-2),new Point(x+3,mid+mid/2-2)});
        }}}
    }
}
public class FieldCombo : ComboBox {
    public FieldCombo(){DrawMode=DrawMode.OwnerDrawFixed;FlatStyle=FlatStyle.Flat;BackColor=FieldTheme.Input;ForeColor=FieldTheme.Text;DropDownStyle=ComboBoxStyle.DropDownList;}
    protected override void OnDrawItem(DrawItemEventArgs e){bool active=(e.State&DrawItemState.Selected)!=0;using(var b=new SolidBrush(active?FieldTheme.Selected:FieldTheme.Input))e.Graphics.FillRectangle(b,e.Bounds);string text=e.Index>=0?GetItemText(Items[e.Index]):Text;var r=e.Bounds;r.Inflate(-8,0);TextRenderer.DrawText(e.Graphics,text,Font,r,FieldTheme.Text,TextFormatFlags.VerticalCenter|TextFormatFlags.SingleLine|TextFormatFlags.EndEllipsis);}
    protected override void WndProc(ref Message m){base.WndProc(ref m);if(m.Msg==0xF){using(var g=Graphics.FromHwnd(Handle))using(var b=new SolidBrush(FieldTheme.Raised))using(var p=new Pen(FieldTheme.Border))using(var a=new SolidBrush(Enabled?FieldTheme.Amber:FieldTheme.Muted)){int w=Math.Max(18,SystemInformation.VerticalScrollBarWidth+2),x=Width-w,cx=x+w/2,cy=Height/2;g.FillRectangle(b,x,1,w-1,Height-2);g.DrawRectangle(p,0,0,Width-1,Height-1);g.DrawLine(p,x,1,x,Height-2);g.FillPolygon(a,new[]{new Point(cx-4,cy-2),new Point(cx+4,cy-2),new Point(cx,cy+2)});}}}
}
public sealed class FieldHeader:Control {
    public FieldHeader(){DoubleBuffered=true;Dock=DockStyle.Fill;BackColor=FieldTheme.Panel;}
    protected override void OnPaint(PaintEventArgs e){
        var g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;float s=Height/102f;g.Clear(FieldTheme.Panel);
        using(var grid=new Pen(Color.FromArgb(42,FieldTheme.Muted)))for(float x=Width*.58f;x<Width;x+=36*s)g.DrawLine(grid,x,0,x,Height);
        using(var grid=new Pen(Color.FromArgb(28,FieldTheme.Muted)))for(float y=12*s;y<Height;y+=27*s)g.DrawLine(grid,Width*.58f,y,Width,y);
        using(var contour=new Pen(Color.FromArgb(48,FieldTheme.Green),1))for(int k=0;k<8;k++){var points=new PointF[35];for(int i=0;i<points.Length;i++){float x=Width*.58f+i*(Width*.42f/34);float y=(k*19-25)*s+(float)(Math.Sin(i*.23+k*.57)*14+Math.Cos(i*.46+k)*5)*s;points[i]=new PointF(x,y);}g.DrawCurve(contour,points);}
        g.DrawImage(FieldAssets.Emblem,new Rectangle((int)(8*s),(int)(3*s),(int)(94*s),(int)(94*s)));
        using(var title=new Font("Impact",32*s,FontStyle.Regular,GraphicsUnit.Pixel))using(var b=new SolidBrush(FieldTheme.Text))g.DrawString("VIETNAM WAR",title,b,112*s,28*s);
        using(var mono=new Font("Consolas",10*s,FontStyle.Bold,GraphicsUnit.Pixel))using(var amber=new SolidBrush(FieldTheme.Amber))g.DrawString("FIELD DESK  /  LIVE EDITOR",mono,amber,114*s,13*s);
        using(var body=new Font("Segoe UI",12*s,FontStyle.Regular,GraphicsUnit.Pixel))using(var b=new SolidBrush(FieldTheme.Muted))g.DrawString("Field operations desk",body,b,114*s,65*s);
        if(Width>710*s){using(var mono=new Font("Consolas",10*s,FontStyle.Regular,GraphicsUnit.Pixel))using(var b=new SolidBrush(FieldTheme.Muted)){g.DrawString("THEATER / VIETNAM",mono,b,Width-202*s,23*s);g.DrawString("PERSONNEL · ORDNANCE · SUPPLY",mono,b,Width-202*s,42*s);}using(var b=new SolidBrush(FieldTheme.Amber))g.FillRectangle(b,Width-202*s,66*s,21*s,3*s);}
        using(var p=new Pen(FieldTheme.Border))g.DrawRectangle(p,0,0,Width-1,Height-1);
        foreach(int x in new[]{5,Width-7})foreach(int y in new[]{5,Height-7})using(var b=new SolidBrush(FieldTheme.Border)){g.FillEllipse(b,x,y,3,3);}
    }
}
public sealed class FieldNavButton:Button {
    public string Number,Subtitle;public bool Active;
    public FieldNavButton(string number,string title,string subtitle){Number=number;Text=title;Subtitle=subtitle;Dock=DockStyle.Top;Height=72;Cursor=Cursors.Hand;SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer,true);Margin=Padding.Empty;}
    protected override void OnPaint(PaintEventArgs e){var g=e.Graphics;g.Clear(Active?FieldTheme.Selected:FieldTheme.Panel);if(Active)using(var b=new SolidBrush(FieldTheme.Amber))g.FillRectangle(b,0,0,4,Height);using(var p=new Pen(FieldTheme.Border))g.DrawLine(p,12,Height-1,Width-12,Height-1);
        using(var f=new Font("Consolas",9,FontStyle.Bold))using(var b=new SolidBrush(Active?FieldTheme.Amber:FieldTheme.Muted))g.DrawString(Number,f,b,16,10);
        using(var f=new Font("Segoe UI",10,FontStyle.Bold))using(var b=new SolidBrush(FieldTheme.Text))g.DrawString(Text,f,b,16,27);
        using(var f=new Font("Consolas",7.5f))using(var b=new SolidBrush(FieldTheme.Muted))g.DrawString(Subtitle,f,b,16,49);
        if(Focused&&ShowFocusCues)ControlPaint.DrawFocusRectangle(g,new Rectangle(6,6,Width-12,Height-12),FieldTheme.Text,FieldTheme.Panel);
    }
}
public sealed class FieldCard:Panel{
    public FieldCard(){DoubleBuffered=true;BackColor=FieldTheme.Panel;}
    protected override void OnPaint(PaintEventArgs e){base.OnPaint(e);using(var p=new Pen(FieldTheme.Border))e.Graphics.DrawRectangle(p,0,0,Width-1,Height-1);}
}
}
