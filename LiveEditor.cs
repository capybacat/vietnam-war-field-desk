using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace VietnamWarEditor {
public sealed class Memory : IDisposable {
    [StructLayout(LayoutKind.Sequential)] public struct Region {
        public ulong BaseAddress, AllocationBase; public uint AllocationProtect;
        public ushort PartitionId; public ushort Padding;
        public ulong RegionSize; public uint State, Protect, Type;
    }
    [DllImport("kernel32.dll", SetLastError=true)] static extern IntPtr OpenProcess(uint access, bool inherit, int pid);
    [DllImport("kernel32.dll", SetLastError=true)] static extern bool ReadProcessMemory(IntPtr h, IntPtr address, byte[] buffer, UIntPtr size, out UIntPtr read);
    [DllImport("kernel32.dll", SetLastError=true)] static extern bool WriteProcessMemory(IntPtr h, IntPtr address, byte[] buffer, UIntPtr size, out UIntPtr written);
    [DllImport("kernel32.dll")] static extern UIntPtr VirtualQueryEx(IntPtr h, IntPtr address, out Region info, UIntPtr length);
    [DllImport("kernel32.dll")] static extern bool CloseHandle(IntPtr h);
    [DllImport("psapi.dll", CharSet=CharSet.Unicode)] static extern uint GetMappedFileName(IntPtr h, IntPtr address, StringBuilder path, int size);
    readonly IntPtr handle;
    public readonly Process Process;
    public Memory(Process process) {
        Process=process; handle=OpenProcess(0x400|0x10|0x20|0x8,false,process.Id);
        if(handle==IntPtr.Zero) throw new Win32Exception(Marshal.GetLastWin32Error(),"Cannot access game memory. Run the editor with the same privileges as the game.");
    }
    public byte[] Read(ulong address,int size) {
        if(address<0x10000 || size<0 || size>32*1024*1024) throw new IOException("Invalid memory address or read size.");
        byte[] result=new byte[size]; UIntPtr count;
        if(!ReadProcessMemory(handle,new IntPtr((long)address),result,(UIntPtr)(uint)size,out count) || count.ToUInt64()!=(ulong)size) throw new IOException("Memory is unavailable. The scene may have changed; reconnect.");
        return result;
    }
    public byte[] TryRead(ulong address,int size) { try{return Read(address,size);}catch(IOException){return null;} }
    public ulong Ptr(ulong address) { return BitConverter.ToUInt64(Read(address,8),0); }
    public int Int(ulong address) { return BitConverter.ToInt32(Read(address,4),0); }
    public float Float(ulong address) { return BitConverter.ToSingle(Read(address,4),0); }
    public string Ascii(ulong address,int limit) { var b=Read(address,limit);int n=Array.IndexOf(b,(byte)0);return n<0?null:Encoding.ASCII.GetString(b,0,n); }
    public string String(ulong address) { if(address==0)return "";int n=Int(address+16);if(n<0||n>512)throw new IOException("Invalid game string.");return Encoding.Unicode.GetString(Read(address+20,n*2)); }
    public void Write(ulong address,byte[] data) {
        UIntPtr written;
        if(!WriteProcessMemory(handle,new IntPtr((long)address),data,(UIntPtr)(uint)data.Length,out written) || written.ToUInt64()!=(ulong)data.Length) throw new Win32Exception(Marshal.GetLastWin32Error(),"Could not write the value.");
        if(!Read(address,data.Length).SequenceEqual(data))throw new IOException("The game immediately changed the value; the write could not be verified.");
    }
    public List<Region> Regions() {
        var all=new List<Region>();ulong address=0;
        while(address<0x7FFFFFFF0000) {
            Region r;if(VirtualQueryEx(handle,new IntPtr((long)address),out r,(UIntPtr)(uint)Marshal.SizeOf(typeof(Region)))==UIntPtr.Zero)break;
            if(r.RegionSize==0||r.BaseAddress+r.RegionSize<=address)break;
            if(r.State==0x1000&&(r.Protect&0x100)==0&&(r.Protect&0xFF)!=1&&r.Protect!=0)all.Add(r);
            address=r.BaseAddress+r.RegionSize;
        } return all;
    }
    public string MappedPath(ulong address) { var p=new StringBuilder(1024);return GetMappedFileName(handle,new IntPtr((long)address),p,p.Capacity)>0?p.ToString():""; }
    public unsafe Dictionary<string,ulong> FindClasses(string[] names,Action<string> progress,bool allowMissing=false) {
        var regions=Regions();var patterns=names.ToDictionary(n=>n,n=>Encoding.ASCII.GetBytes(n+"\0"));
        var namePointers=new Dictionary<ulong,string>();var checkedBases=new HashSet<ulong>();
        foreach(var r in regions.Where(x=>x.Type==0x40000)) {
            if(!checkedBases.Add(r.AllocationBase))continue;
            if(!MappedPath(r.BaseAddress).EndsWith("global-metadata.dat",StringComparison.OrdinalIgnoreCase))continue;
            foreach(var m in regions.Where(x=>x.AllocationBase==r.AllocationBase)) {
                for(ulong off=0;off<m.RegionSize;off+=1024*1024) {
                    int size=(int)Math.Min(1024*1024+256,m.RegionSize-off);var b=TryRead(m.BaseAddress+off,size);if(b==null)continue;
                    foreach(var pair in patterns) for(int i=0;i<=b.Length-pair.Value.Length;i++) {
                        if(b[i]!=pair.Value[0])continue;bool match=true;
                        for(int j=1;j<pair.Value.Length;j++)if(b[i+j]!=pair.Value[j]){match=false;break;}
                        if(match)namePointers[m.BaseAddress+off+(ulong)i]=pair.Key;
                    }
                }
            }
        }
        if(namePointers.Count==0)throw new IOException("IL2CPP metadata was not found. Wait for the game's main menu to load.");
        var found=new Dictionary<string,ulong>();long scanned=0;long reported=-1;
        foreach(var r in regions.Where(x=>x.Type==0x20000&&(x.Protect&0xFF)!=0x10)) {
            for(ulong off=0;off<r.RegionSize;off+=1024*1024) {
                var b=TryRead(r.BaseAddress+off,(int)Math.Min(1024*1024,r.RegionSize-off));if(b==null)continue;
                fixed(byte* start=b) for(int i=0;i+8<=b.Length;i+=8) {
                    string name;if(!namePointers.TryGetValue(*(ulong*)(start+i),out name)||found.ContainsKey(name))continue;
                    ulong klass=r.BaseAddress+off+(ulong)i-0x10;
                    if(ValidateClass(klass,name))found[name]=klass;
                }
                if(found.Count==names.Length)return found;
                scanned+=b.Length;
                long bucket=scanned/(64*1024*1024);if(bucket!=reported&&progress!=null){reported=bucket;progress("Scanning structures: "+(scanned/1024/1024)+" MB");}
            }
        }
        if(found.Count!=names.Length&&!allowMissing)throw new IOException("Progress structures are not ready. Load your character and reconnect.");
        return found;
    }
    bool ValidateClass(ulong klass,string name) {
        try { if(Ascii(Ptr(klass+0x10),64)!=name)return false;int count=BitConverter.ToUInt16(Read(klass+0x120,2),0);
            if(count<2||count>512)return false;ulong fields=Ptr(klass+0x80);
            return Ptr(fields+16)==klass&&Ascii(Ptr(fields),64)=="Instance";
        }catch{return false;}
    }
    public Dictionary<string,int> Fields(ulong klass) {
        int n=BitConverter.ToUInt16(Read(klass+0x120,2),0);ulong fields=Ptr(klass+0x80);
        var result=new Dictionary<string,int>();
        for(int i=0;i<n;i++){ulong f=fields+(ulong)(i*32);if(Ptr(f+16)!=klass)throw new IOException("IL2CPP field validation failed.");result.Add(Ascii(Ptr(f),128),Int(f+24));}
        return result;
    }
    public Dictionary<string,int> FieldTypes(ulong klass) {
        int n=BitConverter.ToUInt16(Read(klass+0x120,2),0);ulong fields=Ptr(klass+0x80);
        var result=new Dictionary<string,int>();
        for(int i=0;i<n;i++){ulong f=fields+(ulong)(i*32);if(Ptr(f+16)!=klass)throw new IOException("IL2CPP type validation failed.");result.Add(Ascii(Ptr(f),128),Read(Ptr(f+8)+10,1)[0]);}return result;
    }
    public ulong Instance(ulong klass) { ulong statics=Ptr(klass+0xB8);if(statics==0)return 0;ulong o=Ptr(statics);return o!=0&&Ptr(o)==klass&&Ptr(o+16)!=0?o:0; }
    public void Dispose(){CloseHandle(handle);Process.Dispose();}
}

public sealed class FieldDef {
    public string Name,Type;public int Offset;
    public bool IsFloat {get{return Type=="System.Single";}}
    public bool IsInt {get{return Type=="System.Int32";}}
    public bool IsList {get{return Type=="System.Collections.Generic.List`1<System.Single>";}}
    public bool IsSkill {get{return IsFloat&&!NonSkills.Contains(Name);}}
    static readonly HashSet<string> NonSkills=new HashSet<string>(("MaxSkillCap SkillGainFraction SkillGain Reputation Reputation5MinsAgo PoliticalReputationUSA Patriotism Stimulant Smoking WarCrimes CollateralDamage AgentOExposure AceOfSpades timeSinceSixthSense PoliceXP CriminalXp Xp XpTotal").Split(' '));
    public static List<FieldDef> Load() {
        using(var s=Assembly.GetExecutingAssembly().GetManifestResourceStream("fields.tsv"))
        using(var reader=new StreamReader(s))return reader.ReadToEnd().Split('\n').Where(l=>l.Trim().Length>0).Select(l=>{var p=l.Trim().Split('\t');return new FieldDef{Name=p[0],Type=p[1],Offset=int.Parse(p[2])};}).ToList();
    }
}
public sealed class Edit {public ulong Address;public byte[] Before,After;public string Name;public Func<bool> ValidateOwner;}
public sealed class Engine : IDisposable {
    public const string ExpectedHash="716DE1B00DAA869BA46E01FD26FDE7AB70A16E7BAE4A607E44495ED324BECE5E";
    public const string DefaultGamePath=@"C:\Program Files (x86)\Steam\steamapps\common\VietnamWar";
    public Memory Mem; public ulong SkillClass,RankClass,Skill,Rank;
    public List<FieldDef> Defs=FieldDef.Load();public Dictionary<string,int> RankFields;
    public ResourceEditor Resources;
    readonly Stack<List<Edit>> history=new Stack<List<Edit>>();
    public int UndoCount {get{return history.Count;}}
    public static string Hash(string path){using(var sha=SHA256.Create())using(var s=File.OpenRead(path))return BitConverter.ToString(sha.ComputeHash(s)).Replace("-","");}
    public void Connect(Action<string> progress) {
        var processes=Process.GetProcessesByName("Vietnam War");
        if(processes.Length!=1)throw new IOException(processes.Length==0?"Launch Vietnam War and load your character.":"Multiple game instances are running. Leave only one open.");
        var p=processes[0];string dll=Path.Combine(Path.GetDirectoryName(p.MainModule.FileName),"GameAssembly.dll");
        if(Hash(dll)!=ExpectedHash)throw new IOException("This game version does not match supported build 26541584. Editing is disabled; an updated profile is required.");
        Mem=new Memory(p);
        var classes=Mem.FindClasses(new[]{"SkillManager","RankManager","CareerManager","PlayerMotor","BaseCommandManager","BuildManagerVietnam","WeaponManager"},progress,true);
        if(!classes.ContainsKey("SkillManager")||!classes.ContainsKey("RankManager"))throw new IOException("Load your character and reconnect.");
        SkillClass=classes["SkillManager"];RankClass=classes["RankManager"];
        var actual=Mem.Fields(SkillClass);
        foreach(var d in Defs)if(!actual.ContainsKey(d.Name)||actual[d.Name]!=d.Offset)throw new IOException("Game field offsets do not match: "+d.Name);
        RankFields=Mem.Fields(RankClass);Validate();Resources=new ResourceEditor(this,classes);
    }
    public void Validate() {
        if(Mem==null||Mem.Process.HasExited)throw new IOException("The game has closed. Launch it and reconnect.");
        ulong s=Mem.Instance(SkillClass),r=Mem.Instance(RankClass);
        if(s==0||r==0)throw new IOException("Load your character. Progress is unavailable in the main menu.");
        if(Skill!=0&&(s!=Skill||r!=Rank)) {history.Clear();Skill=s;Rank=r;throw new IOException("The character or scene changed. Wait for values to refresh and try again.");}
        Skill=s;Rank=r;
        float cap=Read("MaxSkillCap");if(float.IsNaN(cap)||cap<=0||cap>100000000)throw new IOException("Skill object validation failed.");
    }
    public FieldDef Def(string name){return Defs.First(x=>x.Name==name);}
    public float Read(string name){return Mem.Float(Skill+(ulong)Def(name).Offset);}
    public int PlayerRank {get{return Mem.Int(Rank+(ulong)RankFields["PlayerRank"]);}}
    public string RankTitle {get{return Mem.String(Mem.Ptr(Rank+(ulong)RankFields["PlayerRankTitle"]));}}
    public List<float> ReadList(ulong list) {
        int n=Mem.Int(list+24);ulong a=Mem.Ptr(list+16);if(n<0||n>4096||a==0)throw new IOException("Invalid game list.");
        if(Mem.Ptr(a+24)<(ulong)n)throw new IOException("The array size changed.");
        byte[] bytes=Mem.Read(a+32,n*4);var values=new List<float>();for(int i=0;i<n;i++)values.Add(BitConverter.ToSingle(bytes,i*4));return values;
    }
    public List<float> Thresholds(){return ReadList(Mem.Ptr(Rank+(ulong)RankFields["XP_requirementsPlayer"]));}
    public List<string> Titles(string service) {
        ulong l=Mem.Ptr(Rank+(ulong)RankFields[service]);int n=Mem.Int(l+24);ulong a=Mem.Ptr(l+16);
        if(n<0||n>128||Mem.Ptr(a+24)<(ulong)n)throw new IOException("Invalid rank list.");
        var result=new List<string>();for(int i=0;i<n;i++)result.Add(Mem.String(Mem.Ptr(a+32+(ulong)i*8)));return result;
    }
    public List<string> CurrentTitles() {
        int rank=PlayerRank;string title=RankTitle;
        foreach(string service in new[]{"RankTitlesArmy","RankTitlesMarines","RankTitlesAirForce"}){var titles=Titles(service);if(rank>=0&&rank<titles.Count&&titles[rank]==title)return titles;}
        return Titles("RankTitlesArmy");
    }
    public float MaximumXP() {
        var limits=Thresholds();int titleCount=CurrentTitles().Count;
        if(titleCount<1||limits.Count<=titleCount)throw new IOException("A safe XP limit could not be determined for this rank list.");
        float boundary=limits[titleCount];if(float.IsNaN(boundary)||float.IsInfinity(boundary)||boundary<=0)throw new IOException("Invalid XP limit.");
        return Math.Min(1000000000,boundary-1);
    }
    Edit Make(string name,float value) {
        if(float.IsNaN(value)||float.IsInfinity(value)||value<0||value>1000000000)throw new ArgumentException("Enter a number from 0 to 1,000,000,000.");
        var d=Def(name);if(!d.IsFloat)throw new IOException("The field is not a Single-precision number.");
        ulong address=Skill+(ulong)d.Offset;return new Edit{Name=name,Address=address,Before=Mem.Read(address,4),After=BitConverter.GetBytes(value)};
    }
    public void SetXP(float value) {
        Validate();float maximum=MaximumXP();if(value>maximum)throw new ArgumentException("Maximum XP for this build: "+maximum.ToString("N0")+". Higher values would select a nonexistent rank.");float old=Read("Xp"),total=Read("XpTotal");
        Apply(new List<Edit>{Make("Xp",value),Make("XpTotal",Math.Max(0,total+value-old))});
    }
    public void SetField(string name,float value) {
        Validate();var d=Def(name);if(!d.IsSkill && name!="MaxSkillCap" && name!="SkillGainFraction")throw new IOException("This field is not editable.");
        if(d.IsSkill&&value>Read("MaxSkillCap"))throw new ArgumentException("The value exceeds the skill cap ("+Read("MaxSkillCap")+"). Increase the cap first.");
        if(name=="MaxSkillCap"&&value<=0)throw new ArgumentException("The cap must be greater than zero.");
        if(name=="SkillGainFraction"&&value<=0)throw new ArgumentException("The coefficient must be greater than zero.");
        Apply(new List<Edit>{Make(name,value)});
    }
    public void MaxSkills() {
        Validate();float cap=Read("MaxSkillCap");var edits=Defs.Where(d=>d.IsSkill).Select(d=>Make(d.Name,cap)).ToList();
        foreach(var d in Defs.Where(d=>d.IsList)) {
            ulong list=Mem.Ptr(Skill+(ulong)d.Offset);var values=ReadList(list);ulong a=Mem.Ptr(list+16);
            for(int i=0;i<values.Count;i++){ulong p=a+32+(ulong)i*4;edits.Add(new Edit{Name=d.Name+"["+i+"]",Address=p,Before=Mem.Read(p,4),After=BitConverter.GetBytes(cap)});}
        }Apply(edits);
    }
    public void SetWeapon(string listName,int index,float value) {
        Validate();var d=Def(listName);if(!d.IsList)throw new IOException("This is not a skill list.");if(value<0||value>Read("MaxSkillCap")||float.IsNaN(value))throw new ArgumentException("The value is outside the skill limit.");
        ulong l=Mem.Ptr(Skill+(ulong)d.Offset);var values=ReadList(l);if(index<0||index>=values.Count)throw new IOException("The index is outside the list.");
        ulong p=Mem.Ptr(l+16)+32+(ulong)index*4;Apply(new List<Edit>{new Edit{Name=listName+"["+index+"]",Address=p,Before=Mem.Read(p,4),After=BitConverter.GetBytes(value)}});
    }
    internal void Apply(List<Edit> edits) {
        Validate();var written=new List<Edit>();
        foreach(var e in edits)CheckOwner(e);
        try {foreach(var e in edits){CheckOwner(e);if(!Mem.Read(e.Address,e.Before.Length).SequenceEqual(e.Before))throw new IOException("The value changed during the write. Try again.");written.Add(e);Mem.Write(e.Address,e.After);}if(edits.Count>0)history.Push(edits);}
        catch {foreach(var e in written.AsEnumerable().Reverse())try{if(e.ValidateOwner==null||e.ValidateOwner())Mem.Write(e.Address,e.Before);}catch{}throw;}
    }
    void CheckOwner(Edit e){if(e.ValidateOwner!=null&&!e.ValidateOwner()){history.Clear();throw new IOException("The character or base changed. Undo history was cleared; refresh the values.");}}
    public void Undo() {
        Validate();if(history.Count==0)return;var edits=history.Peek();
        foreach(var e in edits){CheckOwner(e);if(!Mem.Read(e.Address,e.After.Length).SequenceEqual(e.After))throw new IOException("The game changed these values after the edit. Undo was blocked to preserve newer progress.");}
        var reverted=new List<Edit>();
        try{foreach(var e in edits.AsEnumerable().Reverse()){CheckOwner(e);reverted.Add(e);Mem.Write(e.Address,e.Before);}history.Pop();}
        catch{foreach(var e in reverted.AsEnumerable().Reverse())try{if(e.ValidateOwner==null||e.ValidateOwner())Mem.Write(e.Address,e.After);}catch{}throw;}
    }
    public void Export(string path) {
        Validate();var b=new StringBuilder("Field\tValue\r\n");foreach(var d in Defs.Where(d=>d.IsFloat||d.IsInt))b.Append(d.Name).Append('\t').Append(d.IsFloat?Mem.Float(Skill+(ulong)d.Offset).ToString("R",CultureInfo.InvariantCulture):Mem.Int(Skill+(ulong)d.Offset).ToString(CultureInfo.InvariantCulture)).Append("\r\n");
        b.Append("PlayerRank\t").Append(PlayerRank).Append("\r\nPlayerRankTitle\t").Append(RankTitle).Append("\r\n");
        if(Resources!=null)foreach(var row in Resources.ReadAll())b.Append("Resource.").Append(row.Spec.Id).Append('\t').Append(row.Available?row.Value.ToString(CultureInfo.InvariantCulture):row.Error).Append("\r\n");File.WriteAllText(path,b.ToString(),Encoding.UTF8);
    }
    public void Dispose(){if(Mem!=null)Mem.Dispose();}
}

static class Program {
    [STAThread] static int Main(string[] args) {
        System.Threading.Thread.CurrentThread.CurrentCulture=CultureInfo.GetCultureInfo("en-US");
        System.Threading.Thread.CurrentThread.CurrentUICulture=CultureInfo.GetCultureInfo("en-US");
        if(args.Length>0) {
            try {using(var engine=new Engine()) {
                engine.Connect(s=>Console.WriteLine(s));Console.WriteLine("PID="+engine.Mem.Process.Id+" SkillClass=0x"+engine.SkillClass.ToString("X")+" Skill=0x"+engine.Skill.ToString("X")+" Rank=0x"+engine.Rank.ToString("X"));
                Console.WriteLine("Xp="+engine.Read("Xp")+" XpTotal="+engine.Read("XpTotal")+" Cap="+engine.Read("MaxSkillCap")+" PlayerRank="+engine.PlayerRank+" Title="+engine.RankTitle);
                Console.WriteLine("THRESHOLDS="+string.Join(",",engine.Thresholds()));Console.WriteLine("TITLES="+string.Join("|",engine.Titles("RankTitlesArmy")));
                if(args[0]=="--probe"){
                    float old=engine.Read("Aim");engine.SetField("Aim",Math.Min(engine.Read("MaxSkillCap"),old+0.125f));engine.Undo();if(engine.Read("Aim")!=old)throw new Exception("Aim restore failed");Console.WriteLine("PASS: live Aim write/read/undo; restored="+old);
                    float oldXP=engine.Read("Xp"),oldTotal=engine.Read("XpTotal");int oldRank=engine.PlayerRank;var limits=engine.Thresholds();float next=limits.FirstOrDefault(v=>v>oldXP);
                    if(next>oldXP+1){engine.SetXP(oldXP+0.125f);engine.Undo();if(engine.Read("Xp")!=oldXP||engine.Read("XpTotal")!=oldTotal||engine.PlayerRank!=oldRank)throw new Exception("XP restore failed");Console.WriteLine("PASS: live XP + total XP transaction/undo; restored="+oldXP+"/"+oldTotal);}
                    string weapon="MainWeapSkills";ulong l=engine.Mem.Ptr(engine.Skill+(ulong)engine.Def(weapon).Offset);var values=engine.ReadList(l);
                    if(values.Count>0){float before=values[0];engine.SetWeapon(weapon,0,Math.Min(engine.Read("MaxSkillCap"),before+0.125f));engine.Undo();if(engine.ReadList(l)[0]!=before)throw new Exception("Weapon restore failed");Console.WriteLine("PASS: weapon-list write/read/undo; restored="+before);}
                    bool refused=false;try{engine.SetXP(engine.MaximumXP()+1);}catch(ArgumentException){refused=true;}if(!refused)throw new Exception("Invalid rank boundary accepted");Console.WriteLine("PASS: invalid rank XP blocked; maximum="+engine.MaximumXP());
                    refused=false;try{engine.SetField("Aim",float.NaN);}catch(ArgumentException){refused=true;}if(!refused)throw new Exception("NaN accepted");Console.WriteLine("PASS: NaN blocked before memory write");
                }
                if(args[0]=="--export"&&args.Length>1)engine.Export(args[1]);
            }return 0;}catch(Exception e){Console.Error.WriteLine(e.ToString());return 1;}
        }
        Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);Application.Run(new EditorForm());return 0;
    }
}
}
