using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Printing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using ZXing;
using ZXing.Common;
using ZXing.QrCode;
using ZXing.QrCode.Internal;

namespace BarcodeStudio;
public sealed class MainForm : Form {
  readonly ComboBox format = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 220 };
  readonly TextBox value = new() { Width = 600, Height = 75, Multiline = true, ScrollBars = ScrollBars.Vertical };
  readonly NumericUpDown w = new() { Minimum = 160, Maximum = 2400, Value = 650, Increment = 50, Width = 100 };
  readonly NumericUpDown h = new() { Minimum = 80, Maximum = 1600, Value = 220, Increment = 20, Width = 100 };
  readonly ComboBox level = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 90 };
  readonly CheckBox readable = new() { Text = "Show readable text", AutoSize = true, Checked = true };
  readonly PictureBox preview = new() { Dock = DockStyle.Fill, SizeMode = PictureBoxSizeMode.Zoom, BackColor = Color.White };
  readonly Label status = new() { AutoSize = true, ForeColor = Color.DimGray };
  readonly System.Windows.Forms.Timer timer = new() { Interval = 250 };
  Bitmap? image;
  readonly (string Name, BarcodeFormat Kind)[] types = { ("QR Code", BarcodeFormat.QR_CODE), ("Code 128", BarcodeFormat.CODE_128), ("Code 39", BarcodeFormat.CODE_39), ("EAN-13", BarcodeFormat.EAN_13), ("UPC-A", BarcodeFormat.UPC_A), ("EAN-8", BarcodeFormat.EAN_8), ("ITF", BarcodeFormat.ITF) };
  public MainForm() {
    Text = "Barcode Studio"; Size = new Size(950, 760); MinimumSize = new Size(720, 600);
    StartPosition = FormStartPosition.CenterScreen; Font = new Font("Segoe UI", 10);
    BackColor = Color.FromArgb(243,246,250);
    var title = new Label { Text = "BARCODE STUDIO  |  QR + 1D", Dock = DockStyle.Top, Height = 70, BackColor = Color.FromArgb(25,46,77), ForeColor = Color.White, Font = new Font("Segoe UI", 19, FontStyle.Bold), TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(22,0,0,0) };
    Controls.Add(title);
    var layout = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 4, ColumnCount = 1, Padding = new Padding(20) };
    layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 170)); layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));
    layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 65));
    Controls.Add(layout); layout.BringToFront(); title.BringToFront();
    var form = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoScroll = true };
    var row = new FlowLayoutPanel { Width = 860, Height = 65, WrapContents = false };
    format.Items.AddRange(types.Select(t => (object)t.Name).ToArray()); format.SelectedIndex = 0;
    level.Items.AddRange(new object[] { "L","M","Q","H" }); level.SelectedIndex = 1;
    row.Controls.Add(Field("Type",format,230)); row.Controls.Add(Field("Width",w,115)); row.Controls.Add(Field("Height",h,115)); row.Controls.Add(Field("QR correction",level,130));
    form.Controls.Add(row); form.Controls.Add(Field("Value / URL",value,650)); layout.Controls.Add(form,0,0);
    var opts = new FlowLayoutPanel { Dock = DockStyle.Fill }; opts.Controls.Add(readable);
    opts.Controls.Add(new Label { Text = "EAN/UPC need valid numeric lengths; QR supports URLs and text.", AutoSize = true, Padding = new Padding(20,4,0,0) });
    layout.Controls.Add(opts,0,1);
    var frame = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20), BackColor = Color.White };
    frame.Controls.Add(preview); layout.Controls.Add(frame,0,2);
    var actions = new FlowLayoutPanel { Dock = DockStyle.Fill };
    actions.Controls.Add(Btn("Save PNG",Save)); actions.Controls.Add(Btn("Copy",Copy)); actions.Controls.Add(Btn("Print",Print));
    actions.Controls.Add(Btn("Generate",(_,_)=>Generate())); actions.Controls.Add(status); layout.Controls.Add(actions,0,3);
    value.Text = "https://example.com";
    timer.Tick += (_,_) => { timer.Stop(); Generate(); };
    foreach (var control in new Control[] { format, value, w, h, level, readable }) {
      if (control is ComboBox cb) cb.SelectedIndexChanged += (_,_) => Queue();
      else if (control is TextBox tb) tb.TextChanged += (_,_) => Queue();
      else if (control is NumericUpDown n) n.ValueChanged += (_,_) => Queue();
      else if (control is CheckBox c) c.CheckedChanged += (_,_) => Queue();
    }
    format.SelectedIndexChanged += (_,_) => { level.Enabled = format.SelectedIndex == 0; readable.Enabled = format.SelectedIndex != 0; };
    readable.Enabled = false; Program.Log("MainForm constructor finished. Initial generation deferred.");
  }
  static Control Field(string caption, Control field, int width) {
    var panel = new Panel { Width = width, Height = field is TextBox ? 105 : 60 };
    panel.Controls.Add(new Label { Text = caption, AutoSize = true, Location = new Point(0,0) });
    field.Location = new Point(0,24); field.Width = width-10; panel.Controls.Add(field); return panel;
  }
  static Button Btn(string name, EventHandler handler) {
    var b = new Button { Text = name, Width = 118, Height = 40, Margin = new Padding(0,0,10,0), BackColor = Color.FromArgb(34,83,149), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
    b.FlatAppearance.BorderSize = 0; b.Click += handler; return b;
  }
  protected override void OnShown(EventArgs e) {
    base.OnShown(e);
    Program.Log("MainForm OnShown fired.");
    WindowState = FormWindowState.Normal;
    ShowInTaskbar = true;
    Activate();
    BeginInvoke(new Action(() => {
      Program.Log("Deferred initial barcode generation started.");
      Generate();
      Program.Log("Deferred initial barcode generation finished.");
    }));
  }
  void Queue() { timer.Stop(); timer.Start(); }
  void Generate() {
    try {
      Program.Log("Generate entered.");
      string content = value.Text.Trim();
      if (content.Length == 0) throw new ArgumentException("Enter a barcode value.");
      var kind = types[format.SelectedIndex].Kind;
      EncodingOptions options = kind == BarcodeFormat.QR_CODE
        ? new QrCodeEncodingOptions { Width=(int)w.Value, Height=(int)h.Value, Margin=4, CharacterSet="UTF-8", ErrorCorrection=level.Text switch { "L"=>ErrorCorrectionLevel.L, "Q"=>ErrorCorrectionLevel.Q, "H"=>ErrorCorrectionLevel.H, _=>ErrorCorrectionLevel.M } }
        : new EncodingOptions { Width=(int)w.Value, Height=(int)h.Value, Margin=12, PureBarcode=true };
      var pixels = new BarcodeWriterPixelData { Format=kind, Options=options }.Write(content);
      var bmp = new Bitmap(pixels.Width,pixels.Height,PixelFormat.Format32bppArgb);
      var rect = new Rectangle(0,0,bmp.Width,bmp.Height);
      var bits = bmp.LockBits(rect,ImageLockMode.WriteOnly,PixelFormat.Format32bppArgb);
      try { Marshal.Copy(pixels.Pixels,0,bits.Scan0,pixels.Pixels.Length); }
      finally { bmp.UnlockBits(bits); }
      if (kind != BarcodeFormat.QR_CODE && readable.Checked) {
        var labeled = new Bitmap(bmp.Width,bmp.Height+44);
        using (var g = Graphics.FromImage(labeled)) {
          g.Clear(Color.White); g.DrawImageUnscaled(bmp,0,0);
          using var font = new Font("Consolas",15,FontStyle.Regular,GraphicsUnit.Pixel);
          using var brush = new SolidBrush(Color.Black);
          using var align = new StringFormat { Alignment = StringAlignment.Center };
          g.DrawString(content,font,brush,new RectangleF(0,bmp.Height+5,bmp.Width,35),align);
        }
        bmp.Dispose(); bmp = labeled;
      }
      var old = image; image = bmp; preview.Image = bmp; old?.Dispose(); status.Text = "Ready"; Program.Log("Generate completed.");
    } catch(Exception ex) { preview.Image=null; image?.Dispose(); image=null; status.Text=ex.Message; Program.Log("Generate error: " + ex); }
  }
  void Save(object? s,EventArgs e) {
    if(image==null) return;
    using var dialog=new SaveFileDialog { Filter="PNG image|*.png",FileName="barcode.png",DefaultExt="png" };
    if(dialog.ShowDialog(this)==DialogResult.OK) { try { image.Save(dialog.FileName,ImageFormat.Png); status.Text="Saved"; } catch(Exception ex) { MessageBox.Show(ex.Message); } }
  }
  void Copy(object? s,EventArgs e) { if(image!=null) { try { Clipboard.SetImage(new Bitmap(image)); status.Text="Copied"; } catch(Exception ex) { MessageBox.Show(ex.Message); } } }
  void Print(object? s,EventArgs e) {
    if(image==null) return;
    using var doc=new PrintDocument();
    doc.PrintPage += (_,args) => {
      if(image==null || args.Graphics==null) return;
      var area=args.MarginBounds;
      float scale=Math.Min(1f,Math.Min((float)area.Width/image.Width,(float)area.Height/image.Height));
      args.Graphics.DrawImage(image,area.Left,area.Top,(int)(image.Width*scale),(int)(image.Height*scale));
      args.HasMorePages=false;
    };
    using var dialog=new PrintDialog { Document=doc,UseEXDialog=true };
    try { if(dialog.ShowDialog(this)==DialogResult.OK) doc.Print(); } catch(Exception ex) { MessageBox.Show(ex.Message); }
  }
  protected override void OnFormClosed(FormClosedEventArgs e) { timer.Dispose(); image?.Dispose(); base.OnFormClosed(e); }
}