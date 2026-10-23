using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Xml.Serialization;

public class Reading {
    public DateTime Date;
    public double Gravity;
    public string Temperature = "";
    public string OtherSide = "";
    public string Note = "";
}
public class Batch {
    public string Name = "New batch", Drink = "Ginger beer", Litres = "", Yeast = "";
    public string Recipe = "", Notes = "", StartGravity = "", StartTemperature = "";
    public string FermentTemperature = "", Calibration = "15.56", OtherScale = "Not identified (record only)";
    public string StartOtherSide = "", TargetGravity = "1.000", PlanningMin = "7", PlanningMax = "21";
    public string Finish = "Fizzy", Sweetness = "Not decided";
    public bool LaterAdditions;
    public DateTime Started = DateTime.Today;
    public List<Reading> Readings = new List<Reading>();
    public override string ToString() { return Name; }
}
public class Logbook { public List<Batch> Batches = new List<Batch>(); }

public static class BrewMath {
    public static bool Number(string text, out double n) {
        return double.TryParse(text.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out n)
            && !double.IsNaN(n) && !double.IsInfinity(n);
    }
    public static double Gravity(string text) {
        double n;
        if (!Number(text, out n)) throw new ArgumentException("Enter gravity as 62 or 1.062. Use a dot for decimals.");
        if (n < .980 || n > 1.170) n = 1 + n / 1000;
        if (n < .980 || n > 1.170) throw new ArgumentException("Gravity must be between 0.980 and 1.170 (-20 to 170 on the short scale).");
        return n;
    }
    public static double Temperature(string text) {
        double n;
        if (!Number(text, out n) || n < 0 || n > 40) throw new ArgumentException("Use a sample temperature between 0 and 40 Celsius, or leave it blank if unknown.");
        return n;
    }
    static double Density(double c) {
        double f = c * 1.8 + 32;
        return 1.00130346 - .000134722124*f + .00000204052596*f*f - .00000000232820948*f*f*f;
    }
    public static double Correct(double sg, double sampleC, double calibrationC) { return sg * Density(sampleC) / Density(calibrationC); }
    public static double Abv(double original, double current) { return (original - current) * 131.25; }
    public static void Save(string path, Logbook book) {
        StringBuilder text = new StringBuilder();
        using (StringWriter w = new StringWriter(text, CultureInfo.InvariantCulture))
        using (var writer = System.Xml.XmlWriter.Create(w, new System.Xml.XmlWriterSettings { Indent = true, NewLineHandling = System.Xml.NewLineHandling.Entitize })) { new XmlSerializer(typeof(Logbook)).Serialize(writer, book); }
        string value = text.ToString().Replace("encoding=\"utf-16\"", "encoding=\"utf-8\"");
        // Only these two existing text files need write permission in Program Files.
        if (File.Exists(path) && new FileInfo(path).Length > 0) File.WriteAllText(path + ".bak", File.ReadAllText(path), new UTF8Encoding(false));
        File.WriteAllText(path, value, new UTF8Encoding(false));
    }
    public static Logbook Load(string path) {
        if (!File.Exists(path) || new FileInfo(path).Length == 0) return new Logbook();
        using (StreamReader r = new StreamReader(path, Encoding.UTF8)) {
            var settings = new System.Xml.XmlReaderSettings { DtdProcessing = System.Xml.DtdProcessing.Prohibit, XmlResolver = null };
            using (var reader = System.Xml.XmlReader.Create(r, settings)) return (Logbook)new XmlSerializer(typeof(Logbook)).Deserialize(reader);
        }
    }
}

public class BrewForm : Form {
    Logbook book;
    Batch current;
    string dataPath;
    bool loading, brokenStore;
    ComboBox batches = new ComboBox(), drink = new ComboBox(), otherScale = new ComboBox(), finish = new ComboBox(), sweetness = new ComboBox();
    Dictionary<string, TextBox> fields = new Dictionary<string, TextBox>();
    DateTimePicker started = new DateTimePicker(), readingDate = new DateTimePicker();
    CheckBox additions = new CheckBox();
    DataGridView history = new DataGridView();
    TextBox report = new TextBox();
    TextBox readingSg = new TextBox(), readingTemp = new TextBox(), readingOther = new TextBox(), readingNote = new TextBox();
    public BrewForm(string file) {
        dataPath = file; AppBranding.SetIcon(this);
        Text = "Brew Log & Calculator"; Font = new Font("Segoe UI", 10); BackColor = Color.White;
        Size = new Size(1060, 850); MinimumSize = new Size(860, 650); StartPosition = FormStartPosition.CenterScreen;
        AutoScaleMode = AutoScaleMode.Dpi;
        var menu = new MenuStrip(); var helpMenu = new ToolStripMenuItem("Help");
        helpMenu.DropDownItems.Add("Using Brew Log", null, delegate { Help(); });
        helpMenu.DropDownItems.Add("About Brew Log", null, delegate { AppBranding.About(this); });
        helpMenu.DropDownItems.Add("Contact", null, delegate { AppBranding.Contact(this, false); });
        helpMenu.DropDownItems.Add("Donations", null, delegate { AppBranding.Contact(this, true); });
        menu.Items.Add(helpMenu); MainMenuStrip = menu;
        try { book = BrewMath.Load(dataPath); }
        catch (Exception e) { book = new Logbook(); brokenStore = true; MessageBox.Show("The text file could not be read. It has NOT been overwritten.\n\nKeep a copy of it and its .bak file.\n\n" + e.Message, "Saved data needs attention"); }
        if (book.Batches.Count == 0) book.Batches.Add(new Batch());
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 3, ColumnCount = 1, Padding = new Padding(12) };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 44)); root.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); root.RowStyles.Add(new RowStyle(SizeType.Absolute, 28)); Controls.Add(root); Controls.Add(menu);
        var bar = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false };
        batches.DropDownStyle = ComboBoxStyle.DropDownList; batches.Width = 235;
        bar.Controls.Add(new Label { Text = "Batch", AutoSize = true, Padding = new Padding(0,7,4,0) }); bar.Controls.Add(batches);
        bar.Controls.Add(Button("New", delegate { NewBatch(false); })); bar.Controls.Add(Button("Copy recipe", delegate { NewBatch(true); }));
        bar.Controls.Add(Button("Save", delegate { SaveCurrent(true); })); bar.Controls.Add(Button("Open TXT", delegate { OpenData(); }));
        bar.Controls.Add(Button("Help", delegate { Help(); })); root.Controls.Add(bar,0,0);
        var tabs = new TabControl { Dock = DockStyle.Fill };
        var recipe = new TabPage("Batch & recipe"); var readings = new TabPage("Readings & estimate"); tabs.TabPages.Add(recipe); tabs.TabPages.Add(readings); root.Controls.Add(tabs,0,1);
        var grid = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 18, AutoScroll = true, Padding = new Padding(12) };
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 265)); grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100)); recipe.Controls.Add(grid);
        int row = 0;
        Field(grid, ref row, "Batch name", "Name");
        SetupCombo(drink, new [] {"Ginger beer","Cider","Beer","Wine","Mead","Other"}); Add(grid, ref row, "Drink type (or type your own)", drink);
        Field(grid, ref row, "Batch size (litres)", "Litres"); Field(grid, ref row, "Yeast / starter", "Yeast");
        started.Format = DateTimePickerFormat.Custom; started.CustomFormat = "dd MMM yyyy  HH:mm"; Add(grid, ref row, "Fermentation started", started);
        Field(grid, ref row, "Starting gravity (98 or 1.098)", "StartGravity");
        Field(grid, ref row, "Starting sample temperature (°C)", "StartTemperature");
        Field(grid, ref row, "Hydrometer calibration (°C)", "Calibration");
        SetupCombo(otherScale,new [] {"Not identified (record only)","Brix / Balling (record only)","Potential alcohol (record only)","Other (record only)"}); Add(grid, ref row,"Other side: scale name",otherScale);
        Field(grid,ref row,"Starting other-side reading (optional)","StartOtherSide");
        Field(grid,ref row,"Current fermentation temperature (°C)","FermentTemperature");
        Field(grid,ref row,"Recipe target gravity (planning only)","TargetGravity");
        var days = new FlowLayoutPanel { Dock=DockStyle.Fill, WrapContents=false };
        fields["PlanningMin"]=new TextBox {Width=65}; fields["PlanningMax"]=new TextBox {Width=65}; days.Controls.Add(fields["PlanningMin"]); days.Controls.Add(new Label {Text="to",AutoSize=true,Padding=new Padding(3,5,3,0)}); days.Controls.Add(fields["PlanningMax"]); days.Controls.Add(new Label {Text="days total (editable allowance)",AutoSize=true,Padding=new Padding(4,5,0,0)}); Add(grid,ref row,"Fermentation planning window",days);
        var style = new FlowLayoutPanel { Dock=DockStyle.Fill, WrapContents=false }; SetupCombo(finish,new [] {"Fizzy","Still"}); finish.Width=110; SetupCombo(sweetness,new [] {"Not decided","Dry","Medium","Sweet"}); sweetness.Width=150; style.Controls.Add(finish);style.Controls.Add(sweetness); Add(grid,ref row,"Desired finish / sweetness",style);
        additions.Text="Sugar, honey, water or juice added AFTER the starting reading"; additions.AutoSize=true; Add(grid,ref row,"Later additions?",additions);
        Field(grid,ref row,"Recipe: ingredients AND quantities","Recipe",true);
        Field(grid,ref row,"Notes / later additions: amount and date","Notes",true);
        var layout = new TableLayoutPanel {Dock=DockStyle.Fill,ColumnCount=1,RowCount=5,Padding=new Padding(10)};
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute,24));layout.RowStyles.Add(new RowStyle(SizeType.Absolute,85));layout.RowStyles.Add(new RowStyle(SizeType.Percent,45));layout.RowStyles.Add(new RowStyle(SizeType.Absolute,40));layout.RowStyles.Add(new RowStyle(SizeType.Percent,55));readings.Controls.Add(layout);
        layout.Controls.Add(new Label {Text="Read the bottom of the meniscus. Degas the sample; float clear of the bottom and sides.",Dock=DockStyle.Fill},0,0);
        var entry = new TableLayoutPanel {Dock=DockStyle.Fill,ColumnCount=4,RowCount=2}; foreach(float width in new [] {28f,23f,23f,26f})entry.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,width));
        readingDate.Format=DateTimePickerFormat.Custom;readingDate.CustomFormat="dd MMM yyyy HH:mm";
        AddEntry(entry,0,"Reading date / time",readingDate);AddEntry(entry,1,"Gravity: 62 or 1.062",readingSg);AddEntry(entry,2,"Sample °C (blank = unknown)",readingTemp);AddEntry(entry,3,"Other side (optional, record only)",readingOther);layout.Controls.Add(entry,0,1);
        history.Dock=DockStyle.Fill;history.ReadOnly=true;history.AllowUserToAddRows=false;history.AllowUserToDeleteRows=false;history.SelectionMode=DataGridViewSelectionMode.FullRowSelect;history.MultiSelect=false;history.AutoSizeColumnsMode=DataGridViewAutoSizeColumnsMode.Fill;history.RowHeadersVisible=false;history.BackgroundColor=Color.White;
        history.Columns.Add("Date","Date / time"); history.Columns.Add("SG","Measured SG");history.Columns.Add("Temp","Sample °C");history.Columns.Add("Corrected","Corrected SG");history.Columns.Add("Other","Other side");history.Columns.Add("Note","Note");layout.Controls.Add(history,0,2);
        var actions=new FlowLayoutPanel {Dock=DockStyle.Fill,WrapContents=false};readingNote.Width=270;actions.Controls.Add(new Label {Text="Note",AutoSize=true,Padding=new Padding(0,7,0,0)});actions.Controls.Add(readingNote);actions.Controls.Add(Button("Add reading",delegate{AddReading();}));actions.Controls.Add(Button("Remove selected",delegate{RemoveReading();}));actions.Controls.Add(Button("Recalculate",delegate{ReadDetails();RefreshResult();}));layout.Controls.Add(actions,0,3);
        report.Dock=DockStyle.Fill;report.Multiline=true;report.ReadOnly=true;report.ScrollBars=ScrollBars.Vertical;report.BackColor=Color.FromArgb(246,249,244);report.Font=new Font("Segoe UI",10);layout.Controls.Add(report,0,4);
        root.Controls.Add(new Label {Dock=DockStyle.Fill,Text="Saved beside the app: BrewLog.txt   •   Approximate alcohol; no automatic bottling approval",ForeColor=Color.DimGray},0,2);
        batches.SelectedIndexChanged += delegate { if(loading)return; if(current!=null)SaveCurrent(false);current=(Batch)batches.SelectedItem;LoadDetails(); };
        FormClosing += delegate(object sender,FormClosingEventArgs e){if(!SaveCurrent(false)&&!brokenStore)e.Cancel=true;};
        PopulateBatches(book.Batches[0]);
    }
    static Button Button(string text,Action action) {var b=new Button {Text=text,AutoSize=true,Height=30,Margin=new Padding(5,0,0,0)};b.Click+=delegate{action();};return b;}
    static void SetupCombo(ComboBox c,string[] items){c.Items.AddRange(items);c.Dock=DockStyle.Fill;}
    void Field(TableLayoutPanel g,ref int row,string label,string key,bool multiline=false){var t=new TextBox{Dock=DockStyle.Fill,Multiline=multiline,ScrollBars=multiline?ScrollBars.Vertical:ScrollBars.None};fields[key]=t;Add(g,ref row,label,t,multiline?90:32);}
    static void Add(TableLayoutPanel g,ref int row,string label,Control control,int height=32){g.RowStyles.Add(new RowStyle(SizeType.Absolute,height));g.Controls.Add(new Label{Text=label,Dock=DockStyle.Fill,TextAlign=ContentAlignment.MiddleLeft},0,row);control.Dock=DockStyle.Fill;g.Controls.Add(control,1,row++);}
    static void AddEntry(TableLayoutPanel g,int col,string text,Control c){g.Controls.Add(new Label{Text=text,Dock=DockStyle.Fill},col,0);c.Dock=DockStyle.Fill;g.Controls.Add(c,col,1);}
    void PopulateBatches(Batch selected){loading=true;batches.Items.Clear();foreach(var b in book.Batches)batches.Items.Add(b);batches.SelectedItem=selected;loading=false;current=selected;LoadDetails();}
    void ReadDetails(){if(current==null)return;foreach(var p in fields){var f=typeof(Batch).GetField(p.Key);f.SetValue(current,p.Value.Text);}current.Drink=drink.Text;current.OtherScale=otherScale.Text;current.Started=started.Value;current.LaterAdditions=additions.Checked;current.Finish=finish.Text;current.Sweetness=sweetness.Text;}
    void LoadDetails(){if(current==null)return;foreach(var p in fields)p.Value.Text=(string)typeof(Batch).GetField(p.Key).GetValue(current);drink.Text=current.Drink;otherScale.Text=current.OtherScale;started.Value=current.Started;additions.Checked=current.LaterAdditions;finish.Text=current.Finish;sweetness.Text=current.Sweetness;RefreshResult();}
    bool SaveCurrent(bool notify){try{if(brokenStore){if(notify)MessageBox.Show("Saving is disabled to protect the unreadable original. Keep a copy and repair/restore it first.");return false;}ReadDetails();BrewMath.Save(dataPath,book);if(notify){PopulateBatches(current);MessageBox.Show("Saved to:\n"+dataPath,"Batch saved");}return true;}catch(Exception e){MessageBox.Show("Could not save. Your data has NOT been moved elsewhere.\n\n"+e.Message,"Save failed");return false;}}
    void NewBatch(bool copy){if(!SaveCurrent(false))return;Batch b=new Batch();if(copy){var s=new XmlSerializer(typeof(Batch));using(var m=new MemoryStream()){s.Serialize(m,current);m.Position=0;b=(Batch)s.Deserialize(m);}b.Name=current.Name+" — new batch";b.Readings.Clear();b.Started=DateTime.Now;b.StartGravity="";b.StartOtherSide="";b.LaterAdditions=false;b.Notes="";}book.Batches.Add(b);PopulateBatches(b);SaveCurrent(false);}
    double Calibration(){return BrewMath.Temperature(current.Calibration);}
    double Corrected(Reading r){return string.IsNullOrWhiteSpace(r.Temperature)?r.Gravity:BrewMath.Correct(r.Gravity,BrewMath.Temperature(r.Temperature),Calibration());}
    void AddReading(){try{ReadDetails();double sg=BrewMath.Gravity(readingSg.Text);if(!string.IsNullOrWhiteSpace(readingTemp.Text))BrewMath.Temperature(readingTemp.Text);Calibration();if(readingDate.Value<current.Started)throw new ArgumentException("This reading is before the batch start date.");if(readingDate.Value>DateTime.Now.AddMinutes(5))throw new ArgumentException("Do not enter a future reading.");current.Readings.Add(new Reading{Date=readingDate.Value,Gravity=sg,Temperature=readingTemp.Text.Trim(),OtherSide=readingOther.Text.Trim(),Note=readingNote.Text.Trim()});if(!SaveCurrent(false))return;RefreshResult();readingSg.Clear();readingOther.Clear();readingNote.Clear();}catch(Exception e){MessageBox.Show(e.Message,"Check this reading");}}
    void RemoveReading(){if(history.SelectedRows.Count==0)return;var r=(Reading)history.SelectedRows[0].Tag;if(MessageBox.Show("Remove the selected reading? The previous saved text is retained in BrewLog.txt.bak.","Remove reading",MessageBoxButtons.YesNo)!=DialogResult.Yes)return;current.Readings.Remove(r);SaveCurrent(false);RefreshResult();}
    void RefreshResult(){history.Rows.Clear();try{Calibration();foreach(var r in current.Readings.OrderBy(x=>x.Date)){int i=history.Rows.Add(r.Date.ToString("dd MMM yyyy HH:mm"),r.Gravity.ToString("F3"),r.Temperature,Corrected(r).ToString("F4"),r.OtherSide,r.Note);history.Rows[i].Tag=r;}report.Text=Analyse();}catch(Exception e){report.Text="Check the batch settings: "+e.Message;}}
    string Analyse(){var b=current;var s=new StringBuilder();s.AppendLine(b.Name+" — "+b.Drink);s.AppendLine("Age: "+Math.Max(0,(int)(DateTime.Now-b.Started).TotalDays)+" days. Fermentation temperature: "+(string.IsNullOrWhiteSpace(b.FermentTemperature)?"not recorded":b.FermentTemperature+" °C")+".");
        double min,max;if(BrewMath.Number(b.PlanningMin,out min)&&BrewMath.Number(b.PlanningMax,out max)&&min>=0&&max>=min&&max<=730){s.AppendLine("Your planning allowance: "+min+"–"+max+" days total ("+b.Started.AddDays(min).ToString("dd MMM")+"–"+b.Started.AddDays(max).ToString("dd MMM")+"). This is an editable allowance, not a fermentation prediction.");}
        else s.AppendLine("Enter a sensible planning window in the batch tab (or use your recipe / yeast instructions).");
        s.AppendLine(b.Finish=="Fizzy"?"Bottle conditioning is EXTRA time after fermentation; allow roughly another 1–3 weeks as a planning guide, sometimes longer. No deadline is guaranteed.":"Still brew: clearing and ageing may take additional time; timing is recipe dependent.");
        if(string.IsNullOrWhiteSpace(b.StartGravity)){s.AppendLine("\r\nEnter the starting gravity taken AFTER the initial ingredients were mixed, BEFORE fermentation.");return s.ToString();}
        double raw=BrewMath.Gravity(b.StartGravity);bool startKnown=!string.IsNullOrWhiteSpace(b.StartTemperature);double og=startKnown?BrewMath.Correct(raw,BrewMath.Temperature(b.StartTemperature),Calibration()):raw;s.AppendLine("\r\nStarting gravity: "+og.ToString("F4")+(startKnown?" (temperature corrected).":" (temperature unknown; uncorrected)."));
        var rs=b.Readings.OrderBy(x=>x.Date).ToList();if(rs.Count==0){s.AppendLine("Add a current reading to estimate alcohol and progress.");return s.ToString();}var last=rs.Last();double fg=Corrected(last);s.AppendLine("Latest gravity: "+fg.ToString("F4")+" on "+last.Date.ToString("dd MMM HH:mm")+".");
        if(b.LaterAdditions)s.AppendLine("ALCOHOL: cannot estimate reliably from this baseline because ingredients/water were added later. Keep the dates/amounts in Notes. Do not treat an unadjusted calculation as the bottle's ABV.");
        else if(fg>og+.0005)s.AppendLine("ALCOHOL: no valid estimate — gravity rose above the starting reading. Check scale, mixing, bubbles, temperature and later additions.");
        else {s.AppendLine("Approximate alcohol so far: "+Math.Max(0,BrewMath.Abv(og,fg)).ToString("F1")+"% ABV. Simple density-drop estimate, not a laboratory measurement.");if(!startKnown||string.IsNullOrWhiteSpace(last.Temperature))s.AppendLine("Temperature missing for one or both readings: ABV remains provisional.");}
        s.AppendLine("The other side of the meter is recorded separately, not averaged or used as alcohol. Its scale must be identified first.");
        if(rs.Count>=1){var previous=rs.Count>=2?rs[rs.Count-2]:new Reading{Date=b.Started,Gravity=raw,Temperature=b.StartTemperature};double days=(last.Date-previous.Date).TotalDays;double drop=Corrected(previous)-fg;if(days>=1){if(drop>.001){s.AppendLine("\r\nGravity is falling: fermentation is progressing. Do not seal an actively fermenting brew.");double target;if(BrewMath.Number(b.TargetGravity,out target)){target=BrewMath.Gravity(b.TargetGravity);if(target<fg&&og>=fg&&!b.LaterAdditions){double remaining=(fg-target)/(drop/days);if(remaining>0&&remaining<365)s.AppendLine("Straight-line trend to your target "+target.ToString("F3")+": about "+Math.Ceiling(remaining)+" days. This is NOT a finish date: fermentation often slows and can stall.");}}}else if(drop<-.001)s.AppendLine("\r\nGravity increased. Recheck readings and additions before interpreting progress.");else s.AppendLine("\r\nLittle change between the last two readings. This alone does not establish completion.");}else s.AppendLine("\r\nTrend: leave at least 24 hours between readings for a useful comparison.");}
        var recent=rs.Where(r=>r.Date>=last.Date.AddDays(-3)).ToList();if(recent.Count>=3&&(last.Date-recent.First().Date).TotalDays>=2&&recent.Max(r=>Corrected(r))-recent.Min(r=>Corrected(r))<=.001)s.AppendLine("Several readings look stable over 2+ days. Check against the recipe target; high/stalled gravity can also be stable. This app does NOT certify safe bottling.");
        s.AppendLine("\r\nBefore bottling: fermentation finished, appropriate stable readings and expected final gravity, then measured priming sugar and pressure-rated bottles. Sweetness from fermentable sugar can restart fermentation.");return s.ToString();}
    void OpenData(){if(!SaveCurrent(false))return;System.Diagnostics.Process.Start("notepad.exe","\""+dataPath+"\"");}
    void Help(){MessageBox.Show("1. Create a batch and enter the recipe, yeast and start date.\n2. Enter gravity as 98 or 1.098. Both mean 1.098; 104 means 1.104, not 1.004.\n3. Record BOTH sample temperatures if known. Calibration is the temperature printed on the meter, not a thermometer reading. Your pictured meter says 60°F = 15.56°C.\n4. Log the other-side value separately. Its name is not established; it is never used automatically.\n5. Add dated readings. ABV ≈ (corrected starting SG − corrected current SG) × 131.25.\n6. Later honey/sugar/water changes the baseline: tick Later additions and record the details.\n\nThe 7–21 day default is a changeable planning allowance, not a model of your recipe or yeast. Sample temperature is not the full fermentation temperature history. A date never proves bottling readiness.\n\nData: readable XML text in BrewLog.txt beside the app; previous save in BrewLog.txt.bak. Copy these elsewhere for backups. No account, internet or AI required.\n\nReferences: brewersfriend.com/abv-calculator/; vinolab.hr/calculator/hydrometer-temperature-correction-en31; diybeer.com/pages/faq.","How to use Brew Log");}
}

public static class Program {
    [STAThread] public static int Main(string[] args){Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);
        if(args.Length>0&&args[0]=="--self-test")return SelfTest(args.Length>1?args[1]:Path.GetTempPath());
        if(args.Length>1&&args[0]=="--render-preview"){using(var f=new BrewForm(Path.Combine(args[1],"preview-data.txt"))){f.ShowInTaskbar=false;f.StartPosition=FormStartPosition.Manual;f.Location=new Point(-3000,-3000);f.Show();Application.DoEvents();using(var image=new Bitmap(f.Width,f.Height)){f.DrawToBitmap(image,new Rectangle(Point.Empty,f.Size));image.Save(Path.Combine(args[1],"preview.png"));}var tab=f.Controls.OfType<TableLayoutPanel>().First().Controls.OfType<TabControl>().First();tab.SelectedIndex=1;Application.DoEvents();using(var image=new Bitmap(f.Width,f.Height)){f.DrawToBitmap(image,new Rectangle(Point.Empty,f.Size));image.Save(Path.Combine(args[1],"preview-readings.png"));}f.Close();}return 0;}
        using(var form=new BrewForm(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"BrewLog.txt")))Application.Run(form);return 0;
    }
    static int SelfTest(string dir){try{Directory.CreateDirectory(dir);Action<bool,string> check=delegate(bool ok,string msg){if(!ok)throw new Exception(msg);};
        check(Math.Abs(BrewMath.Gravity("98")-1.098)<1e-10,"Short gravity");check(Math.Abs(BrewMath.Gravity("104")-1.104)<1e-10,"104 must NOT become 1.004");check(Math.Abs(BrewMath.Gravity("1.004")-1.004)<1e-10,"Full gravity");check(Math.Abs(BrewMath.Abv(1.098,1.062)-4.725)<1e-9,"ABV reference");
        check(Math.Abs(BrewMath.Correct(1.050,15.56,15.56)-1.050)<1e-10,"Calibration identity");check(BrewMath.Correct(1.050,25,15.56)>1.050,"Warm correction direction");
        bool invalid=false;try{BrewMath.Gravity("NaN");}catch{invalid=true;}check(invalid,"Reject NaN");
        var b=new Logbook();b.Batches.Add(new Batch{Name="A & B — cider",Recipe="Honey: 50 g\r\nApple juice: 2 L",LaterAdditions=true});b.Batches[0].Readings.Add(new Reading{Date=DateTime.Now,Gravity=1.062,Temperature="25",OtherSide="15",Note="A < B"});string p=Path.Combine(dir,"test-log.txt");BrewMath.Save(p,b);var loaded=BrewMath.Load(p);check(loaded.Batches[0].Recipe==b.Batches[0].Recipe,"Recipe round trip");check(loaded.Batches[0].Readings[0].Note=="A < B","Escaping round trip");check(loaded.Batches[0].LaterAdditions,"Addition flag");BrewMath.Save(p,loaded);check(File.Exists(p+".bak"),"Backup");
        b.Batches[0].StartGravity="98";b.Batches[0].StartTemperature="15.56";b.Batches[0].Started=DateTime.Today.AddDays(-12);b.Batches[0].Readings[0].Temperature="15.56";b.Batches[0].LaterAdditions=false;
        Func<string> analysis=delegate{BrewMath.Save(p,b);using(var f=new BrewForm(p)){return (string)typeof(BrewForm).GetMethod("Analyse",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(f,null);}};
        string result=analysis();check(result.Contains("4.7% ABV"),"Visible alcohol result");check(result.Contains("Straight-line trend"),"Trend from starting and current reading");
        b.Batches[0].LaterAdditions=true;result=analysis();check(result.Contains("cannot estimate reliably")&&!result.Contains("Approximate alcohol so far:"),"Withhold misleading later-addition ABV");
        b.Batches[0].LaterAdditions=false;b.Batches[0].Readings[0].Gravity=1.104;check(analysis().Contains("gravity rose above"),"Flag rising gravity");
        File.WriteAllText(Path.Combine(dir,"self-test.txt"),"PASS: gravity formats; 104 ambiguity; 4.725% ABV; temperature identity/direction; nonfinite rejection; Unicode/multiline persistence; backup; visible ABV; trend; later-addition ABV withheld; rising gravity flagged.\r\n");return 0;
    }catch(Exception e){File.WriteAllText(Path.Combine(dir,"self-test.txt"),"FAIL: "+e);return 1;}}
}
