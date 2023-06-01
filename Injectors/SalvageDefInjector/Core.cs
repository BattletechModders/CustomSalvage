using ComponentDefInjector;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using FieldAttributes = Mono.Cecil.FieldAttributes;
using OpCodes = Mono.Cecil.Cil.OpCodes;

namespace SalvageDefInjector
{
    internal static class Injector {
        internal static AssemblyDefinition game { get; set; } = null;
        internal static AssemblyDefinition mscore { get; set; } = null;
        public static string AssemblyDirectory
        {
            get
            {
                string codeBase = Assembly.GetExecutingAssembly().CodeBase;
                UriBuilder uri = new UriBuilder(codeBase);
                string path = Uri.UnescapeDataString(uri.Path);
                return Path.GetDirectoryName(path);
            }
        }

        public static void Inject(IAssemblyResolver resolver)    
        {
            Log.BaseDirectory = AssemblyDirectory;
            Log.InitLog();
            Log.Err?.TWL(0, $"SalvageDefInjector initing {Assembly.GetExecutingAssembly().GetName().Version}", true);
            try
            {
                game = resolver.Resolve(new AssemblyNameReference("Assembly-CSharp", null));
                if (game == null)
                {
                    Log.Err?.WL(1, "can't resolve main game assembly", true);
                    return;
                }
                var core = resolver.Resolve(new AssemblyNameReference("mscorlib", null));
                if (core == null)
                {
                    Log.Err?.WL(1, "can't resolve mscorlib assembly", true);
                    return;
                }
                TypeDefinition ComponentTypeEnum = game.MainModule.GetType("BattleTech.ComponentType");
                if (ComponentTypeEnum == null)
                {
                    Log.Err?.WL(1, "can't resolve BattleTech.ComponentType enum", true);
                    return;
                }
                ComponentTypeEnum.Fields.Add(new FieldDefinition("MechFull", FieldAttributes.Public | FieldAttributes.Static | FieldAttributes.Literal | FieldAttributes.HasDefault, ComponentTypeEnum) { Constant = 8 }); ;
                TypeDefinition SalvageDef = game.MainModule.GetType("BattleTech.SalvageDef");
                if (SalvageDef == null)
                {
                    Log.Err?.WL(1, "can't resolve BattleTech.SalvageDef type", true);
                    return;
                }
                TypeDefinition SalvageDef_SalvageType = SalvageDef.NestedTypes.First<TypeDefinition>((a) => { return a.Name == "SalvageType"; });
                if (SalvageDef_SalvageType == null)
                {
                    Log.Err?.WL(1, "can't resolve BattleTech.SalvageDef.SalvageType enum", true);
                    return;
                }
                SalvageDef_SalvageType.Fields.Add(new FieldDefinition("MECH", FieldAttributes.Public | FieldAttributes.Static | FieldAttributes.Literal | FieldAttributes.HasDefault, SalvageDef_SalvageType) { Constant = 4 }); ;
                TypeDefinition MechDef = game.MainModule.GetType("BattleTech.MechDef");
                if (MechDef == null)
                {
                    Log.Err?.WL(1, "can't resolve BattleTech.MechDef type", true);
                    return;
                }
                var mechDefField = new FieldDefinition("mechDef", Mono.Cecil.FieldAttributes.Public, MechDef);
                SalvageDef.Fields.Add(mechDefField);
                var constructor = SalvageDef.GetConstructors().First((a) => { return (a.Parameters.Count == 1) && (a.Parameters[0].ParameterType == SalvageDef); });
                if (constructor == null)
                {
                    Log.Err?.WL(1, "can't resolve BattleTech.SalvageDef.constructor(original) method", true);
                    return;
                }
                {
                    int ti = -1;
                    ILProcessor body = constructor.Body.GetILProcessor();
                    for (var i = 0; i < constructor.Body.Instructions.Count; i++)
                    {
                        var instruction = constructor.Body.Instructions[i];
                        if (instruction.OpCode == OpCodes.Ret) { ti = i; }
                    }
                    if (ti == -1)
                    {
                        Log.Err?.WL(1, "can't find return opcode", true);
                        return;
                    }
                    Instruction ret = constructor.Body.Instructions[ti];
                    var instructions = new List<Instruction>()
                    {
                        body.Create(OpCodes.Ldarg_0),
                        body.Create(OpCodes.Ldarg_1),
                        body.Create(OpCodes.Ldfld, mechDefField),
                        body.Create(OpCodes.Stfld, mechDefField)
                    };
                    instructions.Reverse();
                    foreach (var instruction in instructions) { body.InsertAfter(constructor.Body.Instructions[ti - 1], instruction); }
                }
                var GetSalvageSortVal = SalvageDef.Methods.First((a) => a.Name == "GetSalvageSortVal");
                if (GetSalvageSortVal == null)
                {
                    Log.Err?.WL(1, "can't resolve BattleTech.SalvageDef.GetSalvageSortVal() method", true);
                    return;
                }
                {
                    Log.Err?.WL(1, "BattleTech.SalvageDef.GetSalvageSortVal");
                    var switch_instruction = GetSalvageSortVal.Body.Instructions.First((a) => a.OpCode==OpCodes.Switch);
                    if(switch_instruction == null)
                    {
                        Log.Err?.WL(1, "can't resolve BattleTech.SalvageDef.GetSalvageSortVal() switch opcode", true);
                        return;
                    }
                    List<Instruction> cases = new List<Instruction>((Instruction[])switch_instruction.Operand);
                    cases.Insert(cases.Count - 1, cases[cases.Count - 1]);
                    switch_instruction.Operand = cases.ToArray();
                }
                var SimGameState = game.MainModule.GetType("BattleTech.SimGameState");
                if(SimGameState == null)
                {
                    Log.Err?.WL(1, "can't resolve BattleTech.SimGameState type", true);
                    return;
                }
                TypeDefinition Contract = game.MainModule.GetType("BattleTech.Contract");
                if (Contract == null)
                {
                    Log.Err?.WL(1, "can't resolve BattleTech.Contract type", true);
                    return;
                }
                TypeDefinition BaseDescriptionDef = game.MainModule.GetType("BattleTech.BaseDescriptionDef");
                if (BaseDescriptionDef == null)
                {
                    Log.Err?.WL(1, "can't resolve BattleTech.BaseDescriptionDef type", true);
                    return;
                }
                TypeDefinition ChassisDef = game.MainModule.GetType("BattleTech.ChassisDef");
                if (ChassisDef == null)
                {
                    Log.Err?.WL(1, "can't resolve BattleTech.ChassisDef type", true);
                    return;
                }
                TypeDefinition ComponentType = game.MainModule.GetType("BattleTech.ComponentType");
                if (ComponentType == null)
                {
                    Log.Err?.WL(1, "can't resolve BattleTech.ComponentType type", true);
                    return;
                }
                TypeDefinition DataManager = game.MainModule.GetType("BattleTech.Data.DataManager");
                if (DataManager == null)
                {
                    Log.Err?.WL(1, "can't resolve BattleTech.Data.DataManager type", true);
                    return;
                }
                TypeReference List_SalvageDef = game.MainModule.ImportReference(game.MainModule.ImportReference(typeof(List<>)).MakeGenericInstanceType(SalvageDef));
                if (List_SalvageDef == null)
                {
                    Log.Err?.WL(1, "can't resolve List<SalvageDef> type", true);
                    return;
                }
                TypeReference List_SalvageDef_Enumerator = game.MainModule.ImportReference(game.MainModule.ImportReference(typeof(List<>.Enumerator)).MakeGenericInstanceType(SalvageDef));
                if (List_SalvageDef_Enumerator == null)
                {
                    Log.Err?.WL(1, "can't resolve List<>.Enumerator<SalvageDef> type", true);
                    return;
                }
                else
                {
                    Log.Err?.WL(1, $"{List_SalvageDef_Enumerator.FullName}");
                    foreach(var method in List_SalvageDef_Enumerator.Resolve().Methods)
                    {
                        Log.Err?.WL(2, $"{method.Name}");
                    }
                }
                var ResolveCompleteContract = SimGameState.Methods.First((x) => x.Name == "ResolveCompleteContract");

                MethodReference GetEnumerator = game.MainModule.ImportReference(List_SalvageDef.Resolve().Methods.First((x) => x.Name == "GetEnumerator"));
                MethodReference get_Current = game.MainModule.ImportReference(List_SalvageDef_Enumerator.Resolve().Methods.First((x)=>x.Name == "get_Current"));
                MethodReference MoveNext = game.MainModule.ImportReference(List_SalvageDef_Enumerator.Resolve().Methods.First((x) => x.Name == "MoveNext"));
                MethodReference Dispose = game.MainModule.ImportReference(game.MainModule.ImportReference(typeof(IDisposable)).Resolve().Methods.First((x) => x.Name == "Dispose"));
                MethodReference AppendFormat = game.MainModule.ImportReference(game.MainModule.ImportReference(typeof(StringBuilder)).Resolve().Methods.First((x) => x.Name == "AppendFormat" && x.Parameters.Count == 2));
                MethodReference AppendLine = game.MainModule.ImportReference(game.MainModule.ImportReference(typeof(StringBuilder)).Resolve().Methods.First((x) => x.Name == "AppendLine" && x.Parameters.Count == 0));
                MethodReference ToString = game.MainModule.ImportReference(game.MainModule.ImportReference(typeof(object)).Resolve().Methods.First((x) => x.Name == "ToString" && x.Parameters.Count == 0));
                MethodReference Concat = game.MainModule.ImportReference(game.MainModule.ImportReference(typeof(string)).Resolve().Methods.First((x) => x.Name == "Concat" && x.Parameters.Count == 2 && x.Parameters[0].ParameterType.Name == typeof(string).Name && x.Parameters[1].ParameterType.Name == typeof(string).Name));
                Log.Err?.WL(1,$"{List_SalvageDef.FullName}");
                MethodDefinition SimGameState_ResolveSalvage = new MethodDefinition("ResolveSalvage", ResolveCompleteContract.Attributes, game.MainModule.ImportReference(typeof(void)));
                //SimGameState_ResolveSalvage.Parameters.Add(new ParameterDefinition("salvageResults",Mono.Cecil.ParameterAttributes.None, List_SalvageDef));
                SimGameState_ResolveSalvage.Parameters.Add(new ParameterDefinition("report", Mono.Cecil.ParameterAttributes.None, game.MainModule.ImportReference(typeof(StringBuilder))));
                SimGameState_ResolveSalvage.Body.Variables.Add(new VariableDefinition(List_SalvageDef_Enumerator));
                SimGameState_ResolveSalvage.Body.Variables.Add(new VariableDefinition(SalvageDef));
                SimGameState_ResolveSalvage.Body.Variables.Add(new VariableDefinition(SalvageDef_SalvageType));
                SimGameState_ResolveSalvage.Body.Variables.Add(new VariableDefinition(game.MainModule.ImportReference(typeof(int))));
                SimGameState_ResolveSalvage.Body.Variables.Add(new VariableDefinition(game.MainModule.ImportReference(typeof(int))));
                SimGameState_ResolveSalvage.Body.Variables.Add(new VariableDefinition(game.MainModule.GetType("BattleTech.MechDef")));
                SimGameState_ResolveSalvage.Body.Variables.Add(new VariableDefinition(game.MainModule.GetType("BattleTech.MechDef")));
                var ResolveSalvage_IL = SimGameState_ResolveSalvage.Body.GetILProcessor();
                Log.Err?.WL(1,$"Creating SimGameState.ResolveSalvage method");
                ///*00*/ResolveSalvage_IL.Append(Instruction.Create(OpCodes.Ldarg_0));Log.Err?.WL(2,"00");
                ///*01*/ResolveSalvage_IL.Append(Instruction.Create(OpCodes.Call, SimGameState.Properties.First((x)=>x.Name == "CompletedContract").GetMethod));;Log.Err?.WL(2,"01");
                ///*02*/ResolveSalvage_IL.Append(Instruction.Create(OpCodes.Callvirt, Contract.Properties.First((x) => x.Name == "SalvageResults").GetMethod));;Log.Err?.WL(2,"02");
                ///*03*/ResolveSalvage_IL.Append(Instruction.Create(OpCodes.Callvirt, GetEnumerator));;Log.Err?.WL(2,"03");
                ///*04*/ResolveSalvage_IL.Append(Instruction.Create(OpCodes.Stloc_0));;Log.Err?.WL(2,"04");
                ///*05*/ResolveSalvage_IL.Append(Instruction.Create(OpCodes.Br, SimGameState_ResolveSalvage.Body.Instructions[0]));;Log.Err?.WL(2,"05");
                ///*06*/ResolveSalvage_IL.Append(Instruction.Create(OpCodes.Ldloca_S, SimGameState_ResolveSalvage.Body.Variables[0]));;Log.Err?.WL(2,"06");
                ///*07*/ResolveSalvage_IL.Append(Instruction.Create(OpCodes.Call, get_Current));;Log.Err?.WL(2,"07");
                ///*08*/ResolveSalvage_IL.Append(Instruction.Create(OpCodes.Stloc_1));;Log.Err?.WL(2,"08");
                ///*09*/ResolveSalvage_IL.Append(Instruction.Create(OpCodes.Ldloc_1));;Log.Err?.WL(2,"09");
                ///*10*/ResolveSalvage_IL.Append(Instruction.Create(OpCodes.Ldfld, SalvageDef.Fields.First((x)=>x.Name=="Type")));;Log.Err?.WL(2,"10");
                ///*11*/ResolveSalvage_IL.Append(Instruction.Create(OpCodes.Stloc_2));;Log.Err?.WL(2,"11");
                ///*12*/ResolveSalvage_IL.Append(Instruction.Create(OpCodes.Ldloc_2));;Log.Err?.WL(2,"12");
                ///*13*/ResolveSalvage_IL.Append(Instruction.Create(OpCodes.Ldc_I4_1));;Log.Err?.WL(2,"13");
                ///*14*/ResolveSalvage_IL.Append(Instruction.Create(OpCodes.Sub));;Log.Err?.WL(2,"14");
                ///*15*/ResolveSalvage_IL.Append(Instruction.Create(OpCodes.Switch, new Instruction[] { SimGameState_ResolveSalvage.Body.Instructions[0], SimGameState_ResolveSalvage.Body.Instructions[0], SimGameState_ResolveSalvage.Body.Instructions[0], SimGameState_ResolveSalvage.Body.Instructions[0] }));;Log.Err?.WL(2,"15");
                ///*16*/ResolveSalvage_IL.Append(Instruction.Create(OpCodes.Br, SimGameState_ResolveSalvage.Body.Instructions[0]));;Log.Err?.WL(2,"16");
                ///*17*/ResolveSalvage_IL.Append(Instruction.Create(OpCodes.Ldc_I4_0));;Log.Err?.WL(2,"17");
                ///*18*/ResolveSalvage_IL.Append(Instruction.Create(OpCodes.Stloc_3));;Log.Err?.WL(2,"18");
                ///*19*/ResolveSalvage_IL.Append(Instruction.Create(OpCodes.Br_S, SimGameState_ResolveSalvage.Body.Instructions[0]));;Log.Err?.WL(2,"19");
                ///*20*/ResolveSalvage_IL.Append(Instruction.Create(OpCodes.Ldarg_0));;Log.Err?.WL(2,"20");
                ///*21*/ResolveSalvage_IL.Append(Instruction.Create(OpCodes.Ldloc_1));;Log.Err?.WL(2,"21");
                ///*22*/ResolveSalvage_IL.Append(Instruction.Create(OpCodes.Ldfld, SalvageDef.Fields.First((x)=>x.Name=="Description")));;Log.Err?.WL(2,"22");
                ///*23*/ResolveSalvage_IL.Append(Instruction.Create(OpCodes.Callvirt, BaseDescriptionDef.Properties.First((x) => x.Name == "Id").GetMethod));;Log.Err?.WL(2,"23");
                ///*24*/ResolveSalvage_IL.Append(Instruction.Create(OpCodes.Ldloc_1));;Log.Err?.WL(2,"24");
                ///*25*/ResolveSalvage_IL.Append(Instruction.Create(OpCodes.Ldfld, SalvageDef.Fields.First((x) => x.Name == "ComponentType")));;Log.Err?.WL(2,"25");
                ///*26*/ResolveSalvage_IL.Append(Instruction.Create(OpCodes.Constrained, ComponentType));;Log.Err?.WL(2,"26");
                ///*27*/ResolveSalvage_IL.Append(Instruction.Create(OpCodes.Callvirt, ToString));;Log.Err?.WL(2,"27");
                ///*28*/ResolveSalvage_IL.Append(Instruction.Create(OpCodes.Ldstr, "Def"));;Log.Err?.WL(2,"28");
                ///*29*/ResolveSalvage_IL.Append(Instruction.Create(OpCodes.Call, Concat));;Log.Err?.WL(2,"29");
                ///*30*/ResolveSalvage_IL.Append(Instruction.Create(OpCodes.Ldloc_1));;Log.Err?.WL(2,"30");
                ///*31*/ResolveSalvage_IL.Append(Instruction.Create(OpCodes.Ldfld, SalvageDef.Fields.First((x) => x.Name == "Damaged")));;Log.Err?.WL(2,"31");
                ///*32*/ResolveSalvage_IL.Append(Instruction.Create(OpCodes.Call, SimGameState.Methods.First((x) => x.Name == "AddItemStat" && x.Parameters[1].ParameterType.Name == typeof(string).Name)));;Log.Err?.WL(2,"32");
                ///*33*/ResolveSalvage_IL.Append(Instruction.Create(OpCodes.Ldloc_3));;Log.Err?.WL(2,"33");
                ///*34*/ResolveSalvage_IL.Append(Instruction.Create(OpCodes.Ldc_I4_1));;Log.Err?.WL(2,"34");
                ///*35*/ResolveSalvage_IL.Append(Instruction.Create(OpCodes.Add));;Log.Err?.WL(2,"35");
                ///*36*/ResolveSalvage_IL.Append(Instruction.Create(OpCodes.Stloc_3));;Log.Err?.WL(2,"36");
                ///*37*/ResolveSalvage_IL.Append(Instruction.Create(OpCodes.Ldloc_3));;Log.Err?.WL(2,"37");
                ///*38*/ResolveSalvage_IL.Append(Instruction.Create(OpCodes.Ldloc_1));;Log.Err?.WL(2,"38");
                ///*39*/ResolveSalvage_IL.Append(Instruction.Create(OpCodes.Ldfld, SalvageDef.Fields.First((x) => x.Name == "Count")));;Log.Err?.WL(2,"39");
                ///*40*/ResolveSalvage_IL.Append(Instruction.Create(OpCodes.Blt_S, ResolveSalvage_IL.Body.Instructions[20]));;Log.Err?.WL(2,"40");
                ///*41*/ResolveSalvage_IL.Append(Instruction.Create(OpCodes.Br_S, SimGameState_ResolveSalvage.Body.Instructions[0]));;Log.Err?.WL(2,"41");
                ///*42*/ResolveSalvage_IL.Append(Instruction.Create(OpCodes.Ldc_I4_0));;Log.Err?.WL(2,"42");
                ///*43*/ResolveSalvage_IL.Append(Instruction.Create(OpCodes.Stloc_S, SimGameState_ResolveSalvage.Body.Variables[4]));;Log.Err?.WL(2,"43");
                ///*44*/ResolveSalvage_IL.Append(Instruction.Create(OpCodes.Br_S, SimGameState_ResolveSalvage.Body.Instructions[0]));;Log.Err?.WL(2,"44");
                ///*45*/ResolveSalvage_IL.Append(Instruction.Create(OpCodes.Ldarg_0));;Log.Err?.WL(2,"45");
                ///*46*/ResolveSalvage_IL.Append(Instruction.Create(OpCodes.Ldloc_1));;Log.Err?.WL(2,"46");
                ///*47*/ResolveSalvage_IL.Append(Instruction.Create(OpCodes.Ldfld, SalvageDef.Fields.First((x) => x.Name == "Description")));;Log.Err?.WL(2,"47");
                ///*48*/ResolveSalvage_IL.Append(Instruction.Create(OpCodes.Callvirt, BaseDescriptionDef.Properties.First((x) => x.Name == "Id").GetMethod));;Log.Err?.WL(2,"48");
                ///*49*/ResolveSalvage_IL.Append(Instruction.Create(OpCodes.Call, SimGameState.Methods.First((x) => x.Name == "AddMechPart")));;Log.Err?.WL(2,"49");
                ///*50*/ResolveSalvage_IL.Append(Instruction.Create(OpCodes.Ldloc_S, SimGameState_ResolveSalvage.Body.Variables[4]));;Log.Err?.WL(2,"50");
                ///*51*/ResolveSalvage_IL.Append(Instruction.Create(OpCodes.Ldc_I4_1));;Log.Err?.WL(2,"51");
                ///*52*/ResolveSalvage_IL.Append(Instruction.Create(OpCodes.Add));;Log.Err?.WL(2,"52");
                ///*53*/ResolveSalvage_IL.Append(Instruction.Create(OpCodes.Stloc_S, SimGameState_ResolveSalvage.Body.Variables[4]));;Log.Err?.WL(2,"53");
                ///*54*/ResolveSalvage_IL.Append(Instruction.Create(OpCodes.Ldloc_S, SimGameState_ResolveSalvage.Body.Variables[4]));;Log.Err?.WL(2,"54");
                ///*55*/ResolveSalvage_IL.Append(Instruction.Create(OpCodes.Ldloc_1));;Log.Err?.WL(2,"55");
                ///*56*/ResolveSalvage_IL.Append(Instruction.Create(OpCodes.Ldfld, SalvageDef.Fields.First((x) => x.Name == "Count")));;Log.Err?.WL(2,"56");
                ///*57*/ResolveSalvage_IL.Append(Instruction.Create(OpCodes.Blt_S, ResolveSalvage_IL.Body.Instructions[45]));;Log.Err?.WL(2,"57");
                ///*58*/ResolveSalvage_IL.Append(Instruction.Create(OpCodes.Br_S, SimGameState_ResolveSalvage.Body.Instructions[0]));;Log.Err?.WL(2,"58");
                ///*59*/ResolveSalvage_IL.Append(Instruction.Create(OpCodes.Ldarg_0));;Log.Err?.WL(2,"59");
                ///*60*/ResolveSalvage_IL.Append(Instruction.Create(OpCodes.Call, SimGameState.Properties.First((x) => x.Name == "DataManager").GetMethod));;Log.Err?.WL(2,"60");
                ///*61*/ResolveSalvage_IL.Append(Instruction.Create(OpCodes.Callvirt, DataManager.Properties.First((x) => x.Name == "ChassisDefs").GetMethod));;Log.Err?.WL(2,"61");
                ///*62*/ResolveSalvage_IL.Append(Instruction.Create(OpCodes.Ldloc_1));;Log.Err?.WL(2,"62");
                ///*63*/ResolveSalvage_IL.Append(Instruction.Create(OpCodes.Ldfld, SalvageDef.Fields.First((x) => x.Name == "Description")));;Log.Err?.WL(2,"63");
                ///*64*/ResolveSalvage_IL.Append(Instruction.Create(OpCodes.Callvirt, BaseDescriptionDef.Properties.First((x) => x.Name == "Id").GetMethod));;Log.Err?.WL(2,"64");
                ///*65*/ResolveSalvage_IL.Append(Instruction.Create(OpCodes.Callvirt, DataManager.Properties.First((x) => x.Name == "ChassisDefs").PropertyType.Resolve().Methods.First((x)=> {
                //          Log.Err?.WL(3, $"{x.Name} {x.Parameters.Count}");
                //          return x.Name == "Get" && x.Parameters.Count == 1;
                //})));;Log.Err?.WL(2,"65");
                ///*66*/ResolveSalvage_IL.Append(Instruction.Create(OpCodes.Callvirt, ChassisDef.Properties.First((x) => x.Name == "Description").GetMethod));;Log.Err?.WL(2,"66");
                ///*67*/ResolveSalvage_IL.Append(Instruction.Create(OpCodes.Ldloc_1));;Log.Err?.WL(2,"67");
                ///*68*/ResolveSalvage_IL.Append(Instruction.Create(OpCodes.Ldfld, SalvageDef.Fields.First((x) => x.Name == "Description")));;Log.Err?.WL(2,"68");
                ///*69*/ResolveSalvage_IL.Append(Instruction.Create(OpCodes.Callvirt, BaseDescriptionDef.Properties.First((x) => x.Name == "Id").GetMethod));;Log.Err?.WL(2,"69");
                ///*70*/ResolveSalvage_IL.Append(Instruction.Create(OpCodes.Ldc_I4_0));;Log.Err?.WL(2,"70");
                ///*71*/ResolveSalvage_IL.Append(Instruction.Create(OpCodes.Newarr, game.MainModule.GetType("BattleTech.MechComponentRef")));;Log.Err?.WL(2,"71");
                ///*72*/ResolveSalvage_IL.Append(Instruction.Create(OpCodes.Ldarg_0));;Log.Err?.WL(2,"72");
                ///*73*/ResolveSalvage_IL.Append(Instruction.Create(OpCodes.Call, SimGameState.Properties.First((x) => x.Name == "DataManager").GetMethod));;Log.Err?.WL(2,"73");
                ///*74*/ResolveSalvage_IL.Append(Instruction.Create(OpCodes.Newobj, MechDef.Resolve().Methods.First((x) => x.IsConstructor && x.Parameters.Count == 4 && x.Parameters[0].Name == "description")));;Log.Err?.WL(2,"74");
                ///*75*/ResolveSalvage_IL.Append(Instruction.Create(OpCodes.Stloc_S, SimGameState_ResolveSalvage.Body.Variables[5]));;Log.Err?.WL(2,"75");
                ///*76*/ResolveSalvage_IL.Append(Instruction.Create(OpCodes.Ldarg_0));;Log.Err?.WL(2,"76");
                ///*77*/ResolveSalvage_IL.Append(Instruction.Create(OpCodes.Ldloc_S, SimGameState_ResolveSalvage.Body.Variables[5]));;Log.Err?.WL(2,"77");
                ///*78*/ResolveSalvage_IL.Append(Instruction.Create(OpCodes.Call, SimGameState.Methods.First((x) => x.Name == "CreateMechPlacementPopup")));;Log.Err?.WL(2,"78");
                ///*79*/ResolveSalvage_IL.Append(Instruction.Create(OpCodes.Br_S, SimGameState_ResolveSalvage.Body.Instructions[0]));;Log.Err?.WL(2,"79");
                ///*80*/ResolveSalvage_IL.Append(Instruction.Create(OpCodes.Ldloc_1));;Log.Err?.WL(2,"80");
                ///*81*/ResolveSalvage_IL.Append(Instruction.Create(OpCodes.Ldfld, SalvageDef.Fields.First((x) => x.Name == "mechDef")));;Log.Err?.WL(2,"81");
                ///*82*/ResolveSalvage_IL.Append(Instruction.Create(OpCodes.Ldarg_0));;Log.Err?.WL(2,"82");
                ///*83*/ResolveSalvage_IL.Append(Instruction.Create(OpCodes.Call, SimGameState.Methods.First((x) => x.Name == "GenerateSimGameUID")));;Log.Err?.WL(2,"83");
                ///*84*/ResolveSalvage_IL.Append(Instruction.Create(OpCodes.Ldc_I4_1));;Log.Err?.WL(2,"84");
                ///*85*/ResolveSalvage_IL.Append(Instruction.Create(OpCodes.Newobj, MechDef.Resolve().Methods.First((x) => x.IsConstructor && x.Parameters.Count == 3 && x.Parameters[0].Name == "def" && x.Parameters[1].Name == "newGUID" && x.Parameters[2].Name == "copyInventory")));;Log.Err?.WL(2,"85");
                ///*86*/ResolveSalvage_IL.Append(Instruction.Create(OpCodes.Stloc_S, SimGameState_ResolveSalvage.Body.Variables[6]));;Log.Err?.WL(2,"86");
                ///*87*/ResolveSalvage_IL.Append(Instruction.Create(OpCodes.Ldarg_0));;Log.Err?.WL(2,"87");
                ///*88*/ResolveSalvage_IL.Append(Instruction.Create(OpCodes.Ldloc_S, SimGameState_ResolveSalvage.Body.Variables[6]));;Log.Err?.WL(2,"88");
                ///*89*/ResolveSalvage_IL.Append(Instruction.Create(OpCodes.Call, SimGameState.Methods.First((x) => x.Name == "CreateMechPlacementPopup")));;Log.Err?.WL(2,"89");
                ///*90*/ResolveSalvage_IL.Append(Instruction.Create(OpCodes.Ldarg_1));;Log.Err?.WL(2,"90");
                ///*91*/ResolveSalvage_IL.Append(Instruction.Create(OpCodes.Ldstr, "• {0}"));;Log.Err?.WL(2,"91");
                ///*92*/ResolveSalvage_IL.Append(Instruction.Create(OpCodes.Ldloc_1));;Log.Err?.WL(2,"92");
                ///*93*/ResolveSalvage_IL.Append(Instruction.Create(OpCodes.Ldfld, SalvageDef.Fields.First((x) => x.Name == "Description")));;Log.Err?.WL(2,"93");
                ///*94*/ResolveSalvage_IL.Append(Instruction.Create(OpCodes.Callvirt, BaseDescriptionDef.Properties.First((x) => x.Name == "Name").GetMethod));;Log.Err?.WL(2,"94");
                ///*95*/ResolveSalvage_IL.Append(Instruction.Create(OpCodes.Call, AppendFormat));;Log.Err?.WL(2,"95");
                ///*96*/ResolveSalvage_IL.Append(Instruction.Create(OpCodes.Call, AppendLine));;Log.Err?.WL(2,"96");
                ///*97*/ResolveSalvage_IL.Append(Instruction.Create(OpCodes.Pop));;Log.Err?.WL(2,"97");
                ///*98*/ResolveSalvage_IL.Append(Instruction.Create(OpCodes.Ldloca_S, SimGameState_ResolveSalvage.Body.Variables[0]));;Log.Err?.WL(2,"98");
                ///*99*/ResolveSalvage_IL.Append(Instruction.Create(OpCodes.Call, MoveNext));;Log.Err?.WL(2,"99");
                ///*100*/ResolveSalvage_IL.Append(Instruction.Create(OpCodes.Brtrue, SimGameState_ResolveSalvage.Body.Instructions[6]));;Log.Err?.WL(2,"100");
                ///*101*/ResolveSalvage_IL.Append(Instruction.Create(OpCodes.Leave_S, SimGameState_ResolveSalvage.Body.Instructions[0]));;Log.Err?.WL(2,"101");
                ///*102*/ResolveSalvage_IL.Append(Instruction.Create(OpCodes.Ldloca_S, SimGameState_ResolveSalvage.Body.Variables[0]));;Log.Err?.WL(2,"102");
                ///*103*/ResolveSalvage_IL.Append(Instruction.Create(OpCodes.Constrained, List_SalvageDef_Enumerator));;Log.Err?.WL(2,"103");
                ///*104*/ResolveSalvage_IL.Append(Instruction.Create(OpCodes.Callvirt, Dispose));;Log.Err?.WL(2,"104");
                ///*105*/ResolveSalvage_IL.Append(Instruction.Create(OpCodes.Endfinally));;Log.Err?.WL(2,"105");
                ///*106*/ResolveSalvage_IL.Append(Instruction.Create(OpCodes.Ret));;Log.Err?.WL(2,"106");

                //ResolveSalvage_IL.Replace(SimGameState_ResolveSalvage.Body.Instructions[101], Instruction.Create(OpCodes.Leave_S, SimGameState_ResolveSalvage.Body.Instructions[106]));
                //ResolveSalvage_IL.Replace(SimGameState_ResolveSalvage.Body.Instructions[58], Instruction.Create(OpCodes.Br_S, SimGameState_ResolveSalvage.Body.Instructions[90]));
                //ResolveSalvage_IL.Replace(SimGameState_ResolveSalvage.Body.Instructions[44], Instruction.Create(OpCodes.Br_S, SimGameState_ResolveSalvage.Body.Instructions[54]));
                //ResolveSalvage_IL.Replace(SimGameState_ResolveSalvage.Body.Instructions[41], Instruction.Create(OpCodes.Br_S, SimGameState_ResolveSalvage.Body.Instructions[90]));
                //ResolveSalvage_IL.Replace(SimGameState_ResolveSalvage.Body.Instructions[19], Instruction.Create(OpCodes.Br_S, SimGameState_ResolveSalvage.Body.Instructions[37]));
                //ResolveSalvage_IL.Replace(SimGameState_ResolveSalvage.Body.Instructions[16], Instruction.Create(OpCodes.Br, SimGameState_ResolveSalvage.Body.Instructions[90]));
                //ResolveSalvage_IL.Replace(SimGameState_ResolveSalvage.Body.Instructions[15], Instruction.Create(OpCodes.Switch, new Instruction[] { SimGameState_ResolveSalvage.Body.Instructions[17], SimGameState_ResolveSalvage.Body.Instructions[42], SimGameState_ResolveSalvage.Body.Instructions[59], SimGameState_ResolveSalvage.Body.Instructions[80] }));
                //ResolveSalvage_IL.Replace(SimGameState_ResolveSalvage.Body.Instructions[5], Instruction.Create(OpCodes.Br, SimGameState_ResolveSalvage.Body.Instructions[98]));

                //SimGameState.Methods.Add(SimGameState_ResolveSalvage);

                //var ResolveCompleteContract_IL = ResolveCompleteContract.Body.GetILProcessor();
                //int get_SalvageResults_i = -1;
                //for (int i=0;i < ResolveCompleteContract.Body.Instructions.Count; ++i)
                //{
                //    var instruction = ResolveCompleteContract.Body.Instructions[i];
                //    if (instruction.OpCode != OpCodes.Callvirt) { continue; }
                //    MethodReference method = instruction.Operand as MethodReference;
                //    if (method == null) { continue; }
                //    if (method.Name != "get_SalvageResults") { continue; }
                //    get_SalvageResults_i = i;
                //    break;
                //}
                //if(get_SalvageResults_i <= 0)
                //{
                //    Log.Err?.WL(1, "Can't find get_SalvageResults call");
                //    return;
                //}
                //int switch_i = -1;
                //for(int i = get_SalvageResults_i; i < ResolveCompleteContract.Body.Instructions.Count; ++i)
                //{
                //    var instruction = ResolveCompleteContract.Body.Instructions[i];
                //    if (instruction.OpCode != OpCodes.Switch) { continue; }
                //    switch_i = i;
                //    break;
                //}
                //if (switch_i <= 0)
                //{
                //    Log.Err?.WL(1, "Can't find switch opcode");
                //    return;
                //}
                //int CreateMechPlacementPopup_i = -1;
                //for (int i = get_SalvageResults_i; i < ResolveCompleteContract.Body.Instructions.Count; ++i)
                //{
                //    var instruction = ResolveCompleteContract.Body.Instructions[i];
                //    if (instruction.OpCode != OpCodes.Call) { continue; }
                //    MethodReference method = instruction.Operand as MethodReference;
                //    if (method == null) { continue; }
                //    if (method.Name != "CreateMechPlacementPopup") { continue; }
                //    CreateMechPlacementPopup_i = i;
                //    break;
                //}
                //if (CreateMechPlacementPopup_i <= 0)
                //{
                //    Log.Err?.WL(1, "Can't find CreateMechPlacementPopup call");
                //    return;
                //}
                //Log.Err?.WL(1, $"CreateMechPlacementPopup call found {CreateMechPlacementPopup_i}/{ResolveCompleteContract_IL.Body.Instructions.Count}");

                //var switch_break = ResolveCompleteContract_IL.Body.Instructions[CreateMechPlacementPopup_i + 1].Operand as Instruction;
                //ResolveCompleteContract_IL.InsertAfter(ResolveCompleteContract_IL.Body.Instructions[CreateMechPlacementPopup_i + 1], Instruction.Create(OpCodes.Br_S, switch_break)); Log.Err?.WL(2, $"{CreateMechPlacementPopup_i + 11}");
                //ResolveCompleteContract_IL.InsertAfter(ResolveCompleteContract_IL.Body.Instructions[CreateMechPlacementPopup_i + 1], Instruction.Create(OpCodes.Stloc_S, ResolveCompleteContract.Body.Variables[11])); Log.Err?.WL(2, $"{CreateMechPlacementPopup_i + 1}");
                //ResolveCompleteContract_IL.InsertAfter(ResolveCompleteContract_IL.Body.Instructions[CreateMechPlacementPopup_i + 1], Instruction.Create(OpCodes.Newobj, MechDef.Resolve().Methods.First((x) => x.IsConstructor && x.Parameters.Count == 3 && x.Parameters[0].Name == "def" && x.Parameters[1].Name == "newGUID" && x.Parameters[2].Name == "copyInventory"))); Log.Err?.WL(2, $"{CreateMechPlacementPopup_i + 1}");
                //ResolveCompleteContract_IL.InsertAfter(ResolveCompleteContract_IL.Body.Instructions[CreateMechPlacementPopup_i + 1], Instruction.Create(OpCodes.Ldc_I4_1)); Log.Err?.WL(2, $"{CreateMechPlacementPopup_i + 11}");
                //ResolveCompleteContract_IL.InsertAfter(ResolveCompleteContract_IL.Body.Instructions[CreateMechPlacementPopup_i + 1], Instruction.Create(OpCodes.Call, SimGameState.Methods.First((x) => x.Name == "GenerateSimGameUID"))); Log.Err?.WL(2, $"{CreateMechPlacementPopup_i + 11}");
                //ResolveCompleteContract_IL.InsertAfter(ResolveCompleteContract_IL.Body.Instructions[CreateMechPlacementPopup_i + 1], Instruction.Create(OpCodes.Ldarg_0)); Log.Err?.WL(2, $"{CreateMechPlacementPopup_i + 11}");
                //ResolveCompleteContract_IL.InsertAfter(ResolveCompleteContract_IL.Body.Instructions[CreateMechPlacementPopup_i + 1], Instruction.Create(OpCodes.Ldfld, SalvageDef.Fields.First((x) => x.Name == "mechDef"))); Log.Err?.WL(2, $"{CreateMechPlacementPopup_i + 11}");
                //ResolveCompleteContract_IL.InsertAfter(ResolveCompleteContract_IL.Body.Instructions[CreateMechPlacementPopup_i + 1], Instruction.Create(OpCodes.Ldloc_S, ResolveCompleteContract.Body.Variables[9])); Log.Err?.WL(2, $"{CreateMechPlacementPopup_i + 11}");
                //ResolveCompleteContract_IL.InsertAfter(ResolveCompleteContract_IL.Body.Instructions[CreateMechPlacementPopup_i + 1], Instruction.Create(OpCodes.Ldloc_S, ResolveCompleteContract.Body.Variables[9])); Log.Err?.WL(2, $"{CreateMechPlacementPopup_i + 1}");
                //ResolveCompleteContract_IL.InsertAfter(ResolveCompleteContract_IL.Body.Instructions[CreateMechPlacementPopup_i + 2], Instruction.Create(OpCodes.Ldfld, SalvageDef.Fields.First((x) => x.Name == "mechDef"))); Log.Err?.WL(2, $"{CreateMechPlacementPopup_i + 2}");
                //ResolveCompleteContract_IL.InsertAfter(ResolveCompleteContract_IL.Body.Instructions[CreateMechPlacementPopup_i + 3], Instruction.Create(OpCodes.Ldarg_0)); Log.Err?.WL(2, $"{CreateMechPlacementPopup_i + 3}");
                //ResolveCompleteContract_IL.InsertAfter(ResolveCompleteContract_IL.Body.Instructions[CreateMechPlacementPopup_i + 4], Instruction.Create(OpCodes.Call, SimGameState.Methods.First((x) => x.Name == "GenerateSimGameUID"))); Log.Err?.WL(2, $"{CreateMechPlacementPopup_i + 4}");
                //ResolveCompleteContract_IL.InsertAfter(ResolveCompleteContract_IL.Body.Instructions[CreateMechPlacementPopup_i + 5], Instruction.Create(OpCodes.Ldc_I4_1)); Log.Err?.WL(2, $"{CreateMechPlacementPopup_i + 5}");
                //ResolveCompleteContract_IL.InsertAfter(ResolveCompleteContract_IL.Body.Instructions[CreateMechPlacementPopup_i + 6], Instruction.Create(OpCodes.Newobj, MechDef.Resolve().Methods.First((x) => x.IsConstructor && x.Parameters.Count == 3 && x.Parameters[0].Name == "def" && x.Parameters[1].Name == "newGUID" && x.Parameters[2].Name == "copyInventory"))); Log.Err?.WL(2, $"{CreateMechPlacementPopup_i + 6}");
                //ResolveCompleteContract_IL.InsertAfter(ResolveCompleteContract_IL.Body.Instructions[CreateMechPlacementPopup_i + 7], Instruction.Create(OpCodes.Stloc_S, ResolveCompleteContract.Body.Variables[11])); Log.Err?.WL(2, $"{CreateMechPlacementPopup_i + 7}");
                //ResolveCompleteContract_IL.InsertAfter(ResolveCompleteContract_IL.Body.Instructions[CreateMechPlacementPopup_i + 8], Instruction.Create(OpCodes.Ldarg_0)); Log.Err?.WL(2, $"{CreateMechPlacementPopup_i + 1}"); Log.Err?.WL(2, $"{CreateMechPlacementPopup_i + 8}");
                //ResolveCompleteContract_IL.InsertAfter(ResolveCompleteContract_IL.Body.Instructions[CreateMechPlacementPopup_i + 9], Instruction.Create(OpCodes.Ldloc_S, ResolveCompleteContract.Body.Variables[11])); Log.Err?.WL(2, $"{CreateMechPlacementPopup_i + 9}");
                //ResolveCompleteContract_IL.InsertAfter(ResolveCompleteContract_IL.Body.Instructions[CreateMechPlacementPopup_i + 10], Instruction.Create(OpCodes.Call, SimGameState.Methods.First((x) => x.Name == "CreateMechPlacementPopup"))); Log.Err?.WL(2, $"{CreateMechPlacementPopup_i + 10}");
                //ResolveCompleteContract_IL.InsertAfter(ResolveCompleteContract_IL.Body.Instructions[CreateMechPlacementPopup_i + 11], Instruction.Create(OpCodes.Br_S, switch_break)); Log.Err?.WL(2, $"{CreateMechPlacementPopup_i + 11}");
                //Instruction[] switch_cases = ResolveCompleteContract.Body.Instructions[switch_i].Operand as Instruction[];
                //List<Instruction> switch_cases_add = new List<Instruction>(switch_cases);
                //switch_cases_add.Add(ResolveCompleteContract_IL.Body.Instructions[CreateMechPlacementPopup_i + 2]);
                //ResolveCompleteContract_IL.Replace(ResolveCompleteContract.Body.Instructions[switch_i], Instruction.Create(OpCodes.Switch, switch_cases_add.ToArray()));

                //ResolveCompleteContract_IL.Replace(ResolveCompleteContract.Body.Instructions[get_SalvageResults_i + 1], Instruction.Create(OpCodes.Br, br_end));
                //ResolveCompleteContract_IL.InsertBefore(br_end, Instruction.Create(OpCodes.Ldarg_0));
                //ResolveCompleteContract_IL.InsertAfter(ResolveCompleteContract.Body.Instructions[get_SalvageResults_i + 1], Instruction.Create(OpCodes.Ldarg_0));
                //ResolveCompleteContract_IL.InsertAfter(ResolveCompleteContract.Body.Instructions[get_SalvageResults_i + 2], Instruction.Create(OpCodes.Call, SimGameState.Properties.First((x) => x.Name == "CompletedContract").GetMethod));
                //ResolveCompleteContract_IL.InsertAfter(ResolveCompleteContract.Body.Instructions[get_SalvageResults_i + 3], Instruction.Create(OpCodes.Callvirt, Contract.Properties.First((x) => x.Name == "SalvageResults").GetMethod));
                //ResolveCompleteContract_IL.InsertAfter(ResolveCompleteContract.Body.Instructions[get_SalvageResults_i + 4], Instruction.Create(OpCodes.Brfalse, br_end));
                //ResolveCompleteContract_IL.InsertAfter(ResolveCompleteContract.Body.Instructions[get_SalvageResults_i + 1], Instruction.Create(OpCodes.Ldarg_0));
                //ResolveCompleteContract_IL.InsertAfter(ResolveCompleteContract.Body.Instructions[get_SalvageResults_i + 2], Instruction.Create(OpCodes.Ldloc_1));
                //ResolveCompleteContract_IL.InsertAfter(ResolveCompleteContract.Body.Instructions[get_SalvageResults_i + 3], Instruction.Create(OpCodes.Call, SimGameState_ResolveSalvage));
                //ResolveCompleteContract_IL.InsertAfter(ResolveCompleteContract.Body.Instructions[get_SalvageResults_i + 4], Instruction.Create(OpCodes.Br, br_end));
                //ResolveCompleteContract_IL.InsertAfter(ResolveCompleteContract.Body.Instructions[get_SalvageResults_i + 1], Instruction.Create(OpCodes.Ldarg_0));
                //ResolveCompleteContract_IL.Replace(ResolveCompleteContract.Body.Instructions[get_SalvageResults_i + 6], Instruction.Create(OpCodes.Ldloc_1));
                //ResolveCompleteContract_IL.Replace(ResolveCompleteContract.Body.Instructions[get_SalvageResults_i + 7], Instruction.Create(OpCodes.Call, SimGameState_ResolveSalvage));
                //while(ResolveCompleteContract.Body.Instructions[get_SalvageResults_i + 8] != br_end)
                //{
                //    ResolveCompleteContract_IL.Remove(ResolveCompleteContract.Body.Instructions[get_SalvageResults_i + 8]);
                //}
            }
            catch (Exception e)
            {
                Log.Err?.TWL(0, e.ToString(), true);
            }
        }
    }
}
