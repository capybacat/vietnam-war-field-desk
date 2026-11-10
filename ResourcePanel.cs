using System;
using System.Linq;
using System.Drawing;
using System.Windows.Forms;

namespace VietnamWarEditor {
public sealed class ResourcePanel : UserControl {
    readonly Func<Engine> getEngine;readonly Action<string,bool> notify;readonly Action changed;
    readonly DataGridView grid=new DataGridView();readonly FieldNumber amount=new FieldNumber();
    readonly FieldCombo group=new FieldCombo();readonly Label details=new Label();
    readonly FieldButton set=new FieldButton(),add=new FieldButton(),fill=new FieldButton();bool rebuilding;string selectionId;Engine lastEngine;
    readonly Color bg=FieldTheme.Input,panel=FieldTheme.Panel,fg=FieldTheme.Text,muted=FieldTheme.Muted;
    public ResourcePanel(Func<Engine> engine,Action<string,bool> message,Action onChanged){
        getEngine=engine;notify=message;changed=onChanged;Dock=DockStyle.Fill;BackColor=panel;ForeColor=fg;
        var layout=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,RowCount=4,Padding=new Padding(10,8,10,0)};
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute,44));layout.RowStyles.Add(new RowStyle(SizeType.Percent,100));layout.RowStyles.Add(new RowStyle(SizeType.Absolute,58));layout.RowStyles.Add(new RowStyle(SizeType.Absolute,43));Controls.Add(layout);
        var toolbar=new FlowLayoutPanel{Dock=DockStyle.Fill,WrapContents=false};FieldTheme.Combo(group,200);group.Items.AddRange(new object[]{"All groups","Money","Personal supplies","Construction","Field-base stock"});group.SelectedIndexChanged+=(s,e)=>Rebuild();toolbar.Controls.Add(group);
        toolbar.Controls.Add(NewButton("+1 000 $",135,(s,e)=>Run(()=>Require().Resources.Add("cash",1000),"Added $1,000 cash.")));
        toolbar.Controls.Add(NewButton("+10 000 $",145,(s,e)=>Run(()=>Require().Resources.Add("cash",10000),"Added $10,000 cash.")));layout.Controls.Add(toolbar,0,0);
        FieldTheme.Grid(grid);
        grid.Columns.Add("group","Group");grid.Columns.Add("resource","Resource");grid.Columns.Add("value","Current");grid.Columns.Add("limit","Limit");grid.Columns[0].FillWeight=22;grid.Columns[1].FillWeight=38;grid.Columns[2].FillWeight=20;grid.Columns[3].FillWeight=20;grid.Columns[2].DefaultCellStyle.Font=FieldTheme.Mono;grid.Columns[2].DefaultCellStyle.ForeColor=FieldTheme.Amber;grid.Columns[3].DefaultCellStyle.Font=FieldTheme.Mono;grid.SelectionChanged+=(s,e)=>SelectValue();layout.Controls.Add(grid,0,1);
        var edit=new FlowLayoutPanel{Dock=DockStyle.Fill,Padding=new Padding(0,9,0,0),WrapContents=false};FieldTheme.Number(amount,145,10000000,2);amount.AccessibleName="Resource quantity";amount.Margin=new Padding(0,2,10,0);edit.Controls.Add(amount);
        SetupButton(set,"Set value",110);set.Click+=(s,e)=>Run(()=>Require().Resources.Set(SelectedId(),amount.Value),"Quantity updated.");edit.Controls.Add(set);
        SetupButton(add,"Add value",130);add.Click+=(s,e)=>Run(()=>Require().Resources.Add(SelectedId(),amount.Value),"Quantity added.");edit.Controls.Add(add);
        SetupButton(fill,"Restock group",205);fill.Click+=(s,e)=>Run(()=>{var spec=ResourceEditor.Specs.Single(x=>x.Id==SelectedId());Require().Resources.Fill(spec.Group);},"Selected supply group restocked.");edit.Controls.Add(fill);layout.Controls.Add(edit,0,2);
        details.Dock=DockStyle.Fill;details.ForeColor=muted;details.Font=new Font("Segoe UI",9);details.Text="Select a resource. Restock group replenishes the selected supply group.";layout.Controls.Add(details,0,3);group.SelectedIndex=0;
    }
    void SetupButton(Button b,string caption,int width){b.Text=caption;b.Width=width;b.Height=34;b.Margin=new Padding(8,1,0,0);}
    Button NewButton(string caption,int width,EventHandler handler){var b=new FieldButton();SetupButton(b,caption,width);b.Click+=handler;return b;}
    Engine Require(){var e=getEngine();if(e==null||e.Resources==null)throw new Exception("Connect to the game first.");e.Validate();return e;}
    string SelectedId(){if(grid.SelectedRows.Count!=1)throw new Exception("Select a resource in the table.");return (string)grid.SelectedRows[0].Tag;}
    void Rebuild(){rebuilding=true;try{grid.Rows.Clear();string selected=Convert.ToString(group.SelectedItem);foreach(var s in ResourceEditor.Specs.Where(x=>selected=="All groups"||x.Group==selected)){int n=grid.Rows.Add(s.Group,s.Label,"—","—");grid.Rows[n].Tag=s.Id;}if(grid.Rows.Count>0){grid.CurrentCell=grid.Rows[0].Cells[1];grid.Rows[0].Selected=true;}}finally{rebuilding=false;}RefreshValues();SelectValue();}
    public void RefreshValues(){var e=getEngine();if(lastEngine!=e){selectionId=null;lastEngine=e;}foreach(DataGridViewRow row in grid.Rows){ResourceState state=e!=null&&e.Resources!=null?e.Resources.Read((string)row.Tag):null;bool available=state!=null&&state.Available;row.Cells["value"].Value=available?state.Value.ToString(state.Spec.Integer?"N0":"N2"):"Unavailable";row.Cells["limit"].Value=available?state.Limit.ToString("N0"):"—";row.DefaultCellStyle.ForeColor=available?fg:muted;row.Cells["value"].ToolTipText=state==null?"Connect to the game":state.Error??"Current value";}UpdateSelection(false);}
    void SelectValue(){if(!rebuilding)UpdateSelection(true);}
    void UpdateSelection(bool updateAmount){
        set.Enabled=add.Enabled=fill.Enabled=false;if(grid.SelectedRows.Count!=1)return;
        var e=getEngine();if(e==null||e.Resources==null)return;var state=e.Resources.Read(SelectedId());
        if(!state.Available){details.Text=state.Error;return;}
        amount.DecimalPlaces=state.Spec.Integer?0:2;amount.Maximum=state.Limit;
        if(updateAmount||selectionId!=state.Spec.Id)amount.Value=Math.Max(0,Math.Min(state.Limit,state.Value));selectionId=state.Spec.Id;
        set.Enabled=add.Enabled=true;fill.Enabled=state.Spec.Group!="Money";
        details.Text=state.Spec.Group=="Money"?"Balances change immediately. Reopen the game's menu if it still shows the old amount.":state.Spec.Group=="Field-base stock"?"Capacity is calculated from your storage buildings and water tower.":state.Spec.Group=="Construction"?"Carried materials: 100 without a backpack, 150 with one. Reopen the build menu after editing.":"Restock targets: 10 rations/glue; bandages and syringes up to their current caps.";
    }
    void Run(Action action,string success){try{action();RefreshValues();changed();notify(success,false);}catch(Exception ex){notify(ex.Message,true);}}
}
}
