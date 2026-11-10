using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace VietnamWarEditor {
public sealed class EditorForm : Form {
    Engine engine;bool connecting,updating;
    readonly Label connection=new Label(),summary=new Label(),message=new Label();
    readonly FieldButton attach=new FieldButton(),undo=new FieldButton(),max=new FieldButton();
    readonly FieldNumber xp=new FieldNumber(),value=new FieldNumber(),weaponValue=new FieldNumber();
    readonly FieldCombo ranks=new FieldCombo(),weaponList=new FieldCombo();
    readonly DataGridView grid=new DataGridView(),weapons=new DataGridView();
    readonly TextBox filter=new TextBox();readonly Timer refresh=new Timer();
    readonly List<float> thresholds=new List<float>();
    readonly string[] weaponKeys={"MainWeapSkills","SecWeapSkills","SidearmSkills","MeleeWeaponSkills","GrenadeSkills"};
    readonly Panel pageHost=new Panel();readonly List<Control> pages=new List<Control>();readonly List<FieldNavButton> nav=new List<FieldNavButton>();
    ResourcePanel resourcePanel;
    public EditorForm(){
        SuspendLayout();Text="Vietnam War · Field Desk 0.4 Alpha";Icon=FieldAssets.Icon();Size=new Size(1120,820);MinimumSize=new Size(1010,760);StartPosition=FormStartPosition.CenterScreen;
        AutoScaleDimensions=new SizeF(96,96);AutoScaleMode=AutoScaleMode.Dpi;Font=FieldTheme.Body;BackColor=FieldTheme.Background;ForeColor=FieldTheme.Text;KeyPreview=true;
        var shell=new TableLayoutPanel{Dock=DockStyle.Fill,Padding=new Padding(18,14,18,10),ColumnCount=1,RowCount=4};
        shell.RowStyles.Add(new RowStyle(SizeType.Absolute,110));shell.RowStyles.Add(new RowStyle(SizeType.Absolute,51));shell.RowStyles.Add(new RowStyle(SizeType.Percent,100));shell.RowStyles.Add(new RowStyle(SizeType.Absolute,76));Controls.Add(shell);
        shell.Controls.Add(new FieldHeader{Margin=new Padding(0,0,0,8)},0,0);shell.Controls.Add(ConnectionBar(),0,1);
        var body=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=3,RowCount=1,Margin=Padding.Empty};body.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,182));body.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,14));body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));shell.Controls.Add(body,0,2);
        pageHost.Dock=DockStyle.Fill;pageHost.BackColor=FieldTheme.Background;pageHost.Margin=Padding.Empty;body.Controls.Add(pageHost,2,0);
        pages.Add(PersonnelPage());pages.Add(WeaponsPage());pages.Add(ResourcesPage());foreach(var page in pages){page.Dock=DockStyle.Fill;page.Visible=false;pageHost.Controls.Add(page);}body.Controls.Add(Sidebar(),0,0);shell.Controls.Add(Footer(),0,3);
        ShowPage(0);refresh.Interval=1000;refresh.Tick+=(s,e)=>{if(engine==null||connecting)return;try{UpdateValues();}catch(Exception ex){connection.Text="●  Waiting for character";connection.ForeColor=FieldTheme.Amber;Info(ex.Message,true);}};refresh.Start();
        Shown+=(s,e)=>{FieldTheme.TitleBar(this);var area=Screen.FromControl(this).WorkingArea;if(Width>area.Width-30||Height>area.Height-30){MinimumSize=new Size(Math.Min(MinimumSize.Width,area.Width-30),Math.Min(MinimumSize.Height,area.Height-30));Size=new Size(Math.Min(Width,area.Width-30),Math.Min(Height,area.Height-30));CenterToScreen();}};
        KeyDown+=async(s,e)=>{if(e.Control&&e.KeyCode>=Keys.D1&&e.KeyCode<=Keys.D3){ShowPage((int)e.KeyCode-(int)Keys.D1);e.Handled=true;}else if(e.Control&&e.KeyCode==Keys.R){e.Handled=true;await Connect();}};
        FormClosed+=(s,e)=>{refresh.Stop();if(engine!=null)engine.Dispose();};ResumeLayout(true);
    }
    Control ConnectionBar(){
        var row=new FlowLayoutPanel{Dock=DockStyle.Fill,WrapContents=false,Margin=Padding.Empty,Padding=new Padding(0,3,0,0)};
        Configure(attach,"Connect",155);attach.Primary=true;attach.Click+=async(s,e)=>await Connect();row.Controls.Add(attach);
        row.Controls.Add(Button("Launch game",150,(s,e)=>Run(()=>{System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("steam://rungameid/1837480"){UseShellExecute=true});Info("Load your character in the game, then click Connect.");})));
        var pin=new CheckBox{Text="Always on top",AutoSize=true,FlatStyle=FlatStyle.Flat,ForeColor=FieldTheme.Muted,Margin=new Padding(9,10,20,0)};pin.CheckedChanged+=(s,e)=>TopMost=pin.Checked;row.Controls.Add(pin);
        connection.Text="●  Not connected";connection.Font=FieldTheme.Mono;connection.ForeColor=FieldTheme.Muted;connection.AutoSize=true;connection.Margin=new Padding(0,11,0,0);row.Controls.Add(connection);return row;
    }
    Control Sidebar(){
        var card=new FieldCard{Dock=DockStyle.Fill,Margin=Padding.Empty,Padding=new Padding(1)};
        var layout=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,RowCount=6,Margin=Padding.Empty};layout.RowStyles.Add(new RowStyle(SizeType.Absolute,32));for(int i=0;i<3;i++)layout.RowStyles.Add(new RowStyle(SizeType.Absolute,76));layout.RowStyles.Add(new RowStyle(SizeType.Percent,100));layout.RowStyles.Add(new RowStyle(SizeType.Absolute,136));card.Controls.Add(layout);
        layout.Controls.Add(new Label{Text="FIELD JOURNAL",Font=FieldTheme.SmallMono,ForeColor=FieldTheme.Muted,Dock=DockStyle.Fill,Padding=new Padding(16,10,0,0)},0,0);
        string[] titles={"Personnel","Armory","Supply"},subtitles={"XP / SKILLS / RANKS","TRAINING / WEAPONS","MONEY / RESOURCES"};
        for(int i=0;i<3;i++){int page=i;var button=new FieldNavButton("0"+(i+1),titles[i],subtitles[i]){Dock=DockStyle.Fill,Margin=Padding.Empty};button.Click+=(s,e)=>ShowPage(page);nav.Add(button);layout.Controls.Add(button,0,i+1);}
        layout.Controls.Add(new Label{Text="FIELD NOTES\n\nEdits apply to the\nrunning game.\n\nCtrl + 1 / 2 / 3\nSwitch sections",Font=FieldTheme.SmallMono,ForeColor=FieldTheme.Muted,Dock=DockStyle.Fill,Padding=new Padding(15,12,4,0)},0,5);return card;
    }
    void ShowPage(int index){for(int i=0;i<pages.Count;i++){pages[i].Visible=i==index;if(i<nav.Count){nav[i].Active=i==index;nav[i].Invalidate();}}pages[index].BringToFront();}
    TableLayoutPanel Page(string title,string subtitle,int rows){
        var layout=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,RowCount=rows,Margin=Padding.Empty};layout.RowStyles.Add(new RowStyle(SizeType.Absolute,55));
        var heading=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,RowCount=2,Margin=Padding.Empty};heading.RowStyles.Add(new RowStyle(SizeType.Absolute,30));heading.RowStyles.Add(new RowStyle(SizeType.Percent,100));
        heading.Controls.Add(new Label{Text=title,Font=FieldTheme.Heading,ForeColor=FieldTheme.Text,Dock=DockStyle.Fill,Margin=Padding.Empty},0,0);
        heading.Controls.Add(new Label{Text=subtitle,Font=new Font("Segoe UI",9),ForeColor=FieldTheme.Muted,Dock=DockStyle.Fill,Margin=Padding.Empty},0,1);layout.Controls.Add(heading,0,0);return layout;
    }
    Control PersonnelPage(){
        var page=Page("Personnel","Your character's experience, rank and skills.",5);page.RowStyles.Add(new RowStyle(SizeType.Absolute,128));page.RowStyles.Add(new RowStyle(SizeType.Absolute,40));page.RowStyles.Add(new RowStyle(SizeType.Percent,100));page.RowStyles.Add(new RowStyle(SizeType.Absolute,55));
        var card=new FieldCard{Dock=DockStyle.Fill,Padding=new Padding(12,7,12,6),Margin=new Padding(0,0,0,7)};
        var progress=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,RowCount=3,Margin=Padding.Empty};progress.RowStyles.Add(new RowStyle(SizeType.Absolute,28));progress.RowStyles.Add(new RowStyle(SizeType.Absolute,38));progress.RowStyles.Add(new RowStyle(SizeType.Absolute,38));card.Controls.Add(progress);
        summary.Text="No character data. Load your character and connect to the game.";summary.Font=FieldTheme.Mono;summary.ForeColor=FieldTheme.Muted;summary.Dock=DockStyle.Fill;summary.AutoEllipsis=true;summary.Margin=Padding.Empty;progress.Controls.Add(summary,0,0);
        var xpRow=Row();xpRow.Controls.Add(Label("XP",61,true));FieldTheme.Number(xp,130,1000000000,2);xp.AccessibleName="New XP amount";xpRow.Controls.Add(xp);
        xpRow.Controls.Add(Button("Set XP",105,(s,e)=>Run(()=>{Require();engine.SetXP((float)xp.Value);Info("XP updated. The game handles rank promotion.");UpdateValues();})));
        xpRow.Controls.Add(Button("+1 000 XP",115,(s,e)=>Run(()=>{Require();engine.SetXP(engine.Read("Xp")+1000);Info("Added 1,000 XP.");UpdateValues();})));
        xpRow.Controls.Add(Button("+10 000 XP",125,(s,e)=>Run(()=>{Require();engine.SetXP(engine.Read("Xp")+10000);Info("Added 10,000 XP.");UpdateValues();})));progress.Controls.Add(xpRow,0,1);
        var rankRow=Row();rankRow.Controls.Add(Label("RANK",61,true));FieldTheme.Combo(ranks,310);ranks.AccessibleName="Target rank";rankRow.Controls.Add(ranks);
        rankRow.Controls.Add(Button("Promote",190,(s,e)=>Run(()=>{Require();int i=ranks.SelectedIndex;if(i<0||i>=thresholds.Count)throw new Exception("Select a rank.");if(i<=engine.PlayerRank)throw new Exception("Select a higher rank. Demotion is not supported.");engine.SetXP(Math.Max(engine.Read("Xp"),thresholds[i]));Info("XP set to the promotion threshold. Unpause the game for a few seconds.");UpdateValues();})));progress.Controls.Add(rankRow,0,2);page.Controls.Add(card,0,1);
        var toolbar=Row();toolbar.Controls.Add(Label("Search",60));filter.Width=270;filter.Font=FieldTheme.Body;filter.BackColor=FieldTheme.Input;filter.ForeColor=FieldTheme.Text;filter.BorderStyle=BorderStyle.FixedSingle;filter.Margin=new Padding(0,5,12,0);filter.AccessibleName="Search skills";filter.TextChanged+=(s,e)=>FillSkills();toolbar.Controls.Add(filter);toolbar.Controls.Add(FieldTheme.Caption("SKILLS AND TRAINING"));page.Controls.Add(toolbar,0,2);
        FieldTheme.Grid(grid);grid.Columns.Add("name","Skill / parameter");grid.Columns.Add("current","Current");grid.Columns.Add("code","Field");grid.Columns["code"].Visible=false;grid.Columns[0].FillWeight=72;grid.Columns[1].FillWeight=28;grid.Columns[1].DefaultCellStyle.Font=FieldTheme.Mono;grid.Columns[1].DefaultCellStyle.ForeColor=FieldTheme.Amber;grid.SelectionChanged+=(s,e)=>SelectValue();page.Controls.Add(grid,0,3);
        var edit=Row();edit.Padding=new Padding(0,9,0,0);edit.Controls.Add(Label("Value",87));FieldTheme.Number(value,130,1000000000,3);value.AccessibleName="New skill value";edit.Controls.Add(value);
        edit.Controls.Add(Button("Set skill",166,(s,e)=>Run(()=>{Require();if(grid.SelectedRows.Count!=1)throw new Exception("Select a skill.");string name=Convert.ToString(grid.SelectedRows[0].Cells["code"].Value);engine.SetField(name,(float)value.Value);Info(Display(name)+": value updated.");UpdateValues();})));
        Configure(max,"Max all skills",210);max.Click+=(s,e)=>Run(()=>{Require();engine.MaxSkills();Info("Skills and weapon training set to the current skill cap.");UpdateValues();FillWeapons();});edit.Controls.Add(max);page.Controls.Add(edit,0,4);return page;
    }
    Control WeaponsPage(){
        var page=Page("Weapon training","Select a weapon group and a training entry.",5);page.RowStyles.Add(new RowStyle(SizeType.Absolute,48));page.RowStyles.Add(new RowStyle(SizeType.Percent,100));page.RowStyles.Add(new RowStyle(SizeType.Absolute,58));page.RowStyles.Add(new RowStyle(SizeType.Absolute,36));
        var toolbar=Row();toolbar.Controls.Add(Label("Group",75));FieldTheme.Combo(weaponList,330);weaponList.AccessibleName="Weapon group";weaponList.Items.AddRange(new object[]{"Primary weapons","Secondary weapons","Sidearms","Melee weapons","Grenades"});weaponList.SelectedIndexChanged+=(s,e)=>Run(FillWeapons);toolbar.Controls.Add(weaponList);page.Controls.Add(toolbar,0,1);
        FieldTheme.Grid(weapons);weapons.Columns.Add("index","Entry in group");weapons.Columns.Add("current","Training");weapons.Columns[1].DefaultCellStyle.Font=FieldTheme.Mono;weapons.Columns[1].DefaultCellStyle.ForeColor=FieldTheme.Amber;weapons.SelectionChanged+=(s,e)=>{if(weapons.SelectedRows.Count==1){decimal current;if(decimal.TryParse(Convert.ToString(weapons.SelectedRows[0].Cells[1].Value),out current))weaponValue.Value=Math.Max(0,Math.Min(weaponValue.Maximum,current));}};page.Controls.Add(weapons,0,2);
        var edit=Row();edit.Padding=new Padding(0,10,0,0);edit.Controls.Add(Label("Value",90));FieldTheme.Number(weaponValue,160,1000000000,3);weaponValue.AccessibleName="New weapon training value";edit.Controls.Add(weaponValue);edit.Controls.Add(Button("Set training",220,(s,e)=>Run(()=>{Require();if(weapons.SelectedRows.Count!=1)throw new Exception("Select a weapon training entry.");engine.SetWeapon(weaponKeys[weaponList.SelectedIndex],Convert.ToInt32(weapons.SelectedRows[0].Cells[0].Value),(float)weaponValue.Value);Info("Weapon training updated.");FillWeapons();UpdateValues();})));page.Controls.Add(edit,0,3);
        page.Controls.Add(new Label{Text="Entries follow the game's internal weapon order. Values use your character's skill cap.",Font=new Font("Segoe UI",9),ForeColor=FieldTheme.Muted,Dock=DockStyle.Fill},0,4);return page;
    }
    Control ResourcesPage(){var page=Page("Supply","Finances, personal supplies, construction materials and field-base stocks.",2);page.RowStyles.Add(new RowStyle(SizeType.Percent,100));resourcePanel=new ResourcePanel(()=>engine,(s,error)=>Info(s,error),()=>UpdateValues());page.Controls.Add(resourcePanel,0,1);return page;}
    Control Footer(){
        var footer=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,RowCount=2,Padding=new Padding(0,9,0,0),Margin=Padding.Empty};footer.RowStyles.Add(new RowStyle(SizeType.Absolute,37));footer.RowStyles.Add(new RowStyle(SizeType.Percent,100));
        var row=Row();Configure(undo,"Undo last edit",175);undo.Enabled=false;undo.Click+=(s,e)=>Run(()=>{Require();engine.Undo();Info("Last edit undone. Ranks and rewards already granted by the game remain.");UpdateValues();FillWeapons();});row.Controls.Add(undo);
        row.Controls.Add(Button("Export values",185,(s,e)=>Run(()=>{Require();using(var dialog=new SaveFileDialog{Filter="TSV (*.tsv)|*.tsv",FileName="VietnamWar-progress-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".tsv"})if(dialog.ShowDialog(this)==DialogResult.OK){engine.Export(dialog.FileName);Info("Values exported to TSV.");}})));
        row.Controls.Add(new Label{Text="FIELD DESK / 0.4   ·   Save using the game",Font=FieldTheme.SmallMono,ForeColor=FieldTheme.Muted,AutoSize=true,Margin=new Padding(8,13,0,0)});footer.Controls.Add(row,0,0);
        message.Text="Ready to connect. Supported Steam build: 26541584.";message.Font=new Font("Consolas",8.5f);message.ForeColor=FieldTheme.Muted;message.Dock=DockStyle.Fill;message.Margin=new Padding(0,5,0,0);message.AutoEllipsis=true;footer.Controls.Add(message,0,1);return footer;
    }
    FlowLayoutPanel Row(){return new FlowLayoutPanel{Dock=DockStyle.Fill,WrapContents=false,Margin=Padding.Empty};}
    Label Label(string title,int width,bool mono=false){return new Label{Text=title,Width=width,Height=28,Font=mono?FieldTheme.SmallMono:FieldTheme.Body,ForeColor=FieldTheme.Muted,Margin=new Padding(0,8,5,0)};}
    void Configure(FieldButton b,string title,int width){b.Text=title;b.Width=width;}
    FieldButton Button(string title,int width,EventHandler click){var b=new FieldButton{Text=title,Width=width};b.Click+=click;return b;}
    async Task Connect(){
        if(connecting)return;connecting=true;attach.Enabled=false;connection.Text="●  Connecting...";connection.ForeColor=FieldTheme.Amber;Info("Connecting to the game...");if(engine!=null){engine.Dispose();engine=null;}var candidate=new Engine();
        try{await Task.Run(()=>candidate.Connect(s=>{if(IsHandleCreated&&!IsDisposed)BeginInvoke((Action)(()=>Info("Connecting to the game...")));}));if(IsDisposed){candidate.Dispose();return;}
            engine=candidate;connection.Text="●  Connected to game";connection.ForeColor=FieldTheme.Green;thresholds.Clear();var titles=engine.CurrentTitles();thresholds.AddRange(engine.Thresholds().Take(titles.Count));xp.Maximum=(decimal)engine.MaximumXP();ranks.Items.Clear();for(int i=0;i<thresholds.Count;i++)ranks.Items.Add(i+" · "+titles[i]+" · "+thresholds[i].ToString("N0")+" XP");if(ranks.Items.Count>0)ranks.SelectedIndex=Math.Min(ranks.Items.Count-1,engine.PlayerRank+1);FillSkills();if(weaponList.SelectedIndex<0)weaponList.SelectedIndex=0;else FillWeapons();UpdateValues();xp.Value=Clamp(engine.Read("Xp"),xp.Maximum);Info("Connected. Select a parameter and enter a new value.");
        }catch(Exception ex){candidate.Dispose();if(engine==candidate)engine=null;connection.Text="●  Not connected";connection.ForeColor=FieldTheme.Muted;Info(ex.Message,true);}finally{connecting=false;if(!IsDisposed)attach.Enabled=true;}
    }
    void Require(){if(engine==null)throw new Exception("Connect to the game first.");engine.Validate();}
    decimal Clamp(float f,decimal maximum){if(float.IsNaN(f)||float.IsInfinity(f)||f<0)return 0;return Math.Min(maximum,(decimal)f);}
    string Display(string name){switch(name){case "MaxSkillCap":return "Skill cap";case "SkillGainFraction":return "Skill gain chance (higher = more often)";case "Leadership":return "Leadership";case "Strength":return "Strength";case "Stamina":return "Stamina";case "Courage":return "Courage";case "MentalHealth":return "Mental health";case "Aim":return "Aim";case "Reload":return "Reload";case "Hipfire":return "Hip fire";case "Sharpshooter":return "Sharpshooter";case "Medic":return "Medic";case "Stealth":return "Stealth";case "Perception":return "Perception";case "Flying":return "Flying";case "Driving":return "Driving";case "Tactics":return "Tactics";case "Engineering":return "Engineering";case "Swimming":return "Swimming";case "Survival":return "Survival";default:return System.Text.RegularExpressions.Regex.Replace(name,"(?<=[a-z])(?=[A-Z])"," ");}}
    void FillSkills(){if(engine==null)return;updating=true;try{grid.Rows.Clear();foreach(var d in engine.Defs.Where(d=>d.IsSkill||d.Name=="MaxSkillCap"||d.Name=="SkillGainFraction")){string label=Display(d.Name);if((label+" "+d.Name).IndexOf(filter.Text,StringComparison.OrdinalIgnoreCase)<0)continue;grid.Rows.Add(label,engine.Read(d.Name).ToString("0.###"),d.Name);}}finally{updating=false;}SelectValue();}
    void SelectValue(){if(updating||engine==null||grid.SelectedRows.Count!=1)return;try{value.Value=Clamp(engine.Read(Convert.ToString(grid.SelectedRows[0].Cells["code"].Value)),value.Maximum);}catch{}}
    void UpdateValues(){Require();summary.Text="XP "+engine.Read("Xp").ToString("N1")+"   /   Total "+engine.Read("XpTotal").ToString("N1")+"   ·   "+engine.RankTitle+"   ·   Cap "+engine.Read("MaxSkillCap").ToString("0.###");summary.ForeColor=FieldTheme.Text;foreach(DataGridViewRow row in grid.Rows)row.Cells["current"].Value=engine.Read(Convert.ToString(row.Cells["code"].Value)).ToString("0.###");undo.Enabled=engine.UndoCount>0;connection.Text="●  Connected to game";connection.ForeColor=FieldTheme.Green;if(resourcePanel!=null)resourcePanel.RefreshValues();}
    void FillWeapons(){if(engine==null||weaponList.SelectedIndex<0)return;Require();weapons.Rows.Clear();ulong list=engine.Mem.Ptr(engine.Skill+(ulong)engine.Def(weaponKeys[weaponList.SelectedIndex]).Offset);var values=engine.ReadList(list);for(int i=0;i<values.Count;i++)weapons.Rows.Add(i,values[i].ToString("0.###"));}
    void Info(string text,bool error=false){message.Text=text;message.ForeColor=error?FieldTheme.Error:FieldTheme.Muted;}
    void Run(Action action){try{action();}catch(Exception ex){Info(ex.Message,true);}}
}
}
