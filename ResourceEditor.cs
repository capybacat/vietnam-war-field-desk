using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

namespace VietnamWarEditor {
public sealed class ResourceSpec {
    public readonly string Id,Group,Label,Target,Field;public readonly bool Integer;
    public readonly decimal Fill,Maximum;
    public ResourceSpec(string id,string group,string label,string target,string field,bool integer,decimal fill,decimal maximum){Id=id;Group=group;Label=label;Target=target;Field=field;Integer=integer;Fill=fill;Maximum=maximum;}
}
public sealed class ResourceState {
    public ResourceSpec Spec;public decimal Value,Limit;public string Error;
    public bool Available {get{return Error==null;}}
}
public sealed class ResourceEditor {
    readonly Engine engine;readonly Memory mem;readonly Dictionary<string,ulong> classes;
    readonly Dictionary<ulong,Dictionary<string,int>> offsets=new Dictionary<ulong,Dictionary<string,int>>();
    readonly Dictionary<ulong,Dictionary<string,int>> types=new Dictionary<ulong,Dictionary<string,int>>();
    public static readonly ResourceSpec[] Specs={
        new ResourceSpec("cash","Money","Cash, $","CareerManager","Dollars",false,1000,10000000),
        new ResourceSpec("bank","Money","Bank balance, $","CareerManager","DollarsInBankACcount",false,1000,10000000),
        new ResourceSpec("rations","Personal supplies","Rations","PlayerMotor","Rations",true,10,99),
        new ResourceSpec("bandages","Personal supplies","Bandages","PlayerMotor","Bandages",true,4,999),
        new ResourceSpec("syringes","Personal supplies","Syringes","PlayerMotor","Syringes",true,2,999),
        new ResourceSpec("glue","Personal supplies","Medical glue","PlayerMotor","SuperGlueSprays",true,10,99),
        new ResourceSpec("materials","Construction","Carried materials","BuildManagerVietnam","SuppliesCarriedPlayer",true,150,150),
        new ResourceSpec("build","Field-base stock","Construction materials","BaseCommandManager","BuildSuppliesStored",false,4000,4000),
        new ResourceSpec("fuel","Field-base stock","Fuel","BaseCommandManager","FuelStored",false,1000000,1000000),
        new ResourceSpec("food","Field-base stock","Food","BaseCommandManager","FoodStored",false,180,180),
        new ResourceSpec("water","Field-base stock","Water","BaseCommandManager","WaterStored",false,5000,5000),
        new ResourceSpec("beer","Field-base stock","Beer","BaseCommandManager","BeerStored",false,100,1000000),
        new ResourceSpec("munitions","Field-base stock","Ammunition crates","BaseCommandManager","MunitionsStored",false,18,18),
        new ResourceSpec("reinforcements","Field-base stock","Reinforcement points","BaseCommandManager","ReinforcementPoints",false,2000,2000)
    };
    public ResourceEditor(Engine e,Dictionary<string,ulong> knownClasses){engine=e;mem=e.Mem;classes=knownClasses;}
    Dictionary<string,int> Fields(ulong cls){if(!offsets.ContainsKey(cls)){offsets[cls]=mem.Fields(cls);types[cls]=mem.FieldTypes(cls);}return offsets[cls];}
    ulong Resolve(string target) {
        ulong cls;if(!classes.TryGetValue(target,out cls))throw new IOException("Unavailable in this scene; reconnect after loading");
        ulong result=mem.Instance(cls);if(result==0)throw new IOException("Unavailable in the current game mode");
        if(target=="BaseCommandManager"){
            var fields=Fields(cls);ulong bases=mem.Ptr(result+(ulong)fields["Bases"]);
            if(bases==0||mem.Int(bases+24)<=0)throw new IOException("Create or load your own field base first");
        }return result;
    }
    bool IsCurrent(ResourceSpec spec,ulong owner,ulong cls){try{return Resolve(spec.Target)==owner&&mem.Ptr(owner)==cls&&mem.Ptr(owner+16)!=0;}catch{return false;}}
    ulong Address(ResourceSpec spec,ulong owner){ulong cls=mem.Ptr(owner);var fields=Fields(cls);int offset;
        if(!fields.TryGetValue(spec.Field,out offset)||offset<24||offset>65536||types[cls][spec.Field]!=(spec.Integer?8:12))throw new IOException("Field type mismatch: "+spec.Field);
        return owner+(ulong)offset;
    }
    bool Exists(ulong owner,string field){ulong obj=mem.Ptr(owner+(ulong)Fields(mem.Ptr(owner))[field]);return obj!=0&&mem.Ptr(obj+16)!=0;}
    decimal Limit(ResourceSpec spec,ulong owner){
        var fields=Fields(mem.Ptr(owner));
        if(spec.Id=="materials"){
            ulong cls;if(classes.TryGetValue("WeaponManager",out cls)){ulong weapons=mem.Instance(cls);if(weapons!=0&&mem.Read(weapons+(ulong)Fields(cls)["Backpack"],1)[0]!=0)return 150;}return 100;
        }
        if(spec.Id=="bandages")return Math.Max(0,Math.Min(spec.Maximum,mem.Int(owner+(ulong)fields["BandagesMax"])));
        if(spec.Id=="syringes")return Math.Max(0,Math.Min(spec.Maximum,mem.Int(owner+(ulong)fields["SyringesMax"])));
        if(spec.Target!="BaseCommandManager")return spec.Maximum;
        bool warehouse=Exists(owner,"StorageWarehouse");
        switch(spec.Id){
            case "build":return warehouse?4000:1000;
            case "food":return warehouse?180:30;
            case "munitions":return Exists(owner,"MunitionsStorage")?18:4;
            case "fuel":if(!warehouse)return 9;ulong oils=mem.Ptr(owner+(ulong)fields["OilStorages"]);int count=mem.Int(oils+24);if(count<0||count>1000)throw new IOException("Invalid fuel-storage list");return 72+count*72;
            case "water":
                if(Exists(owner,"WaterTower")){
                    ulong tower=mem.Ptr(owner+(ulong)fields["WaterTower"]);var towerFields=Fields(mem.Ptr(tower));
                    if(!towerFields.ContainsKey("HP"))throw new IOException("Could not check the water tower's condition");
                    if(mem.Float(tower+(ulong)towerFields["HP"])>1)return 5000;
                }return warehouse?800:200;
            default:return spec.Maximum;
        }
    }
    decimal ReadValue(ResourceSpec spec,ulong address){
        if(spec.Integer)return mem.Int(address);float f=mem.Float(address);
        if(float.IsNaN(f)||float.IsInfinity(f))throw new IOException("Invalid resource value");return (decimal)f;
    }
    public ResourceState Read(string id){var spec=Specs.Single(x=>x.Id==id);var state=new ResourceState{Spec=spec};
        try{ulong owner=Resolve(spec.Target);state.Value=ReadValue(spec,Address(spec,owner));state.Limit=Limit(spec,owner);}
        catch(Exception ex){state.Error=ex.Message;}return state;
    }
    public List<ResourceState> ReadAll(){return Specs.Select(s=>Read(s.Id)).ToList();}
    Edit Make(ResourceSpec spec,decimal value){
        ulong owner=Resolve(spec.Target),cls=mem.Ptr(owner);decimal limit=Limit(spec,owner);
        if(value<0||value>limit)throw new ArgumentException(spec.Label+": allowed range is 0 to "+limit+".");
        if(spec.Integer&&value!=decimal.Truncate(value))throw new ArgumentException("Item quantities must be whole numbers.");
        ulong address=Address(spec,owner);return new Edit{Name="Resource."+spec.Id,Address=address,Before=mem.Read(address,4),After=spec.Integer?BitConverter.GetBytes((int)value):BitConverter.GetBytes((float)value),ValidateOwner=()=>IsCurrent(spec,owner,cls)};
    }
    public void Set(string id,decimal value){engine.Validate();engine.Apply(new List<Edit>{Make(Specs.Single(s=>s.Id==id),value)});}
    public void Add(string id,decimal amount){engine.Validate();var row=Read(id);if(!row.Available)throw new IOException(row.Error);Set(id,row.Value+amount);}
    public void Fill(string group){
        engine.Validate();var selected=Specs.Where(s=>s.Group==group).ToList();if(selected.Count==0||group=="Money")throw new ArgumentException("Select a supply or base-stock group.");
        var edits=new List<Edit>();foreach(var s in selected){ulong owner=Resolve(s.Target);decimal fill=(s.Id=="bandages"||s.Id=="syringes")?Limit(s,owner):Math.Min(s.Fill,Limit(s,owner));decimal current=ReadValue(s,Address(s,owner));if(current<fill)edits.Add(Make(s,fill));}
        if(edits.Count==0)throw new IOException("This group's supplies are already stocked.");engine.Apply(edits);
    }
}
}
