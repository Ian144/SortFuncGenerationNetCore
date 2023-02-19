using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using static System.String;



namespace SortFuncGeneration;

// ReSharper disable once InconsistentNaming
public static class ILEmitGenerator
{
    private static readonly MethodInfo _strCompareOrdinal = typeof(string).GetMethod("CompareOrdinal", new[] { typeof(string), typeof(string) });
    private static readonly MethodInfo _intCompareTo = typeof(int).GetMethod("CompareTo", new[] { typeof(int) });
        
    public static Func<T, T, int> EmitSortFunc<T>(List<SortDescriptor> sortBys)
    {
        List<(MethodInfo, bool Ascending)> xs = sortBys.Select(sd => (typeof(T).GetMethod($"get_{sd.PropName}"), sd.Ascending)).ToList();

        if(xs.Any(t2 => t2.Item1== null))
            throw new ApplicationException($"unknown property on: {typeof(T)}");

        var dynamicMethod = new DynamicMethod(
            name: Empty,
            returnType: typeof(int),
            parameterTypes: new[] { typeof(T), typeof(T) },
            owner: typeof(ILEmitGenerator),
            skipVisibility: true);

        var generator = dynamicMethod.GetILGenerator();
        
        // needed to add the 'tmp' variable, and loc.0, 
        generator.DeclareLocal(typeof(int));
        
        void Emitx(OpCode opCode)
        {
            generator.Emit(opCode);
            Debug.WriteLine(opCode);
        }

        void Emitb(OpCode opCode, Label lbl, string lblName)
        {
            generator.Emit(opCode, lbl);
            Debug.WriteLine($"{opCode}\t{lblName}");
        }        
        
        void Emitm(OpCode opCode, MethodInfo mi)
        {
            generator.Emit(opCode, mi);
            Debug.WriteLine($"{opCode}\t{mi}");
        }

        void MarkLabel(Label lbl, string lblName)
        {
            generator.MarkLabel(lbl);
            Debug.WriteLine($"LABEL: {lblName}");
        }

        for(int ctr = 0; ctr < xs.Count; ++ctr)
        {
            (MethodInfo propGet, bool ascending) = xs[ctr];

            if (propGet.ReturnType == typeof(int))
            {
                if (ascending)
                {
                    Debug.WriteLine("\n\nint ascending");
                    Debug.WriteLine("-------------");
                    var labelx = generator.DefineLabel();
                    var labely = generator.DefineLabel();

                    Emitx(OpCodes.Ldarg_0);
                    Emitm(OpCodes.Callvirt, propGet);
                    Emitx(OpCodes.Ldarg_1);
                    Emitm(OpCodes.Callvirt, propGet);
                    Emitb(OpCodes.Bge_S, labelx, nameof(labelx));
                    Emitx(OpCodes.Ldc_I4_M1); // return -1
                    Emitx(OpCodes.Ret);
                    MarkLabel(labelx, nameof(labelx));

                    Emitx(OpCodes.Ldarg_0);
                    Emitm(OpCodes.Callvirt, propGet);
                    Emitx(OpCodes.Ldarg_1);
                    Emitm(OpCodes.Callvirt, propGet);
                    Emitb(OpCodes.Ble_S, labely, nameof(labely));
                    Emitx(OpCodes.Ldc_I4_1); // return 1
                    Emitx(OpCodes.Ret);
                    MarkLabel(labely, nameof(labely));
                    
                    Debug.WriteLine("-------------");
                }
                else
                {
                    Debug.WriteLine("\n\nint descending");
                    Debug.WriteLine("-------------");
                    var labelp = generator.DefineLabel();
                    var labelq = generator.DefineLabel();

                    Emitx(OpCodes.Ldarg_0);
                    Emitm(OpCodes.Callvirt, propGet);
                    Emitx(OpCodes.Ldarg_1);
                    Emitm(OpCodes.Callvirt, propGet);
                    Emitb(OpCodes.Ble_S, labelp, nameof(labelp));
                    Emitx(OpCodes.Ldc_I4_1); // return 1
                    Emitx(OpCodes.Ret); 
                    MarkLabel(labelp, nameof(labelp));

                    Emitx(OpCodes.Ldarg_0);
                    Emitm(OpCodes.Callvirt, propGet);
                    Emitx(OpCodes.Ldarg_1);
                    Emitm(OpCodes.Callvirt, propGet);
                    Emitb(OpCodes.Bge_S, labelq, nameof(labelq));
                    Emitx(OpCodes.Ldc_I4_M1); // return -1
                    Emitx(OpCodes.Ret);
                    MarkLabel(labelq, nameof(labelq));
                    Debug.WriteLine("-------------");
                }
            }
            else if (propGet.ReturnType == typeof(string))
            {

                if (ctr == xs.Count - 1)
                {
                    if (ascending)
                    {
                        Debug.WriteLine("\n\nstring ascending last");
                        Debug.WriteLine("----------------");
                        Emitx(OpCodes.Ldarg_0);
                        Emitm(OpCodes.Callvirt, propGet);
                        Emitx(OpCodes.Ldarg_1);
                        Emitm(OpCodes.Callvirt, propGet);
                        Emitm(OpCodes.Call, _strCompareOrdinal);
                        Emitx(OpCodes.Ret);

                    }
                    else
                    {
                        Debug.WriteLine("\n\nstring descending last");
                        Debug.WriteLine("----------------");
                        Emitx(OpCodes.Ldarg_1); // swaps the order of Ldarg_1 and Ldarg_0 compared to ascending
                        Emitm(OpCodes.Callvirt, propGet);
                        Emitx(OpCodes.Ldarg_0);
                        Emitm(OpCodes.Callvirt, propGet);
                        Emitm(OpCodes.Call, _strCompareOrdinal);
                        Emitx(OpCodes.Ret);
                        Debug.WriteLine("----------------");
                    }
                }
                else
                {
                    var labelf = generator.DefineLabel();

                    if (ascending)
                    {
                        Debug.WriteLine("\n\nstring ascending");
                        Debug.WriteLine("----------------");
                        Emitx(OpCodes.Ldarg_0);
                        Emitm(OpCodes.Callvirt, propGet);
                        Emitx(OpCodes.Ldarg_1);
                        Emitm(OpCodes.Callvirt, propGet);
                        Emitm(OpCodes.Call, _strCompareOrdinal);
                        Emitx(OpCodes.Stloc_0); // tmp
                        Emitx(OpCodes.Ldloc_0); // tmp
                        Emitb(OpCodes.Brfalse_S, labelf, nameof(labelf));
                        Emitx(OpCodes.Ldloc_0); // tmp
                        Emitx(OpCodes.Ret);
                        MarkLabel(labelf, nameof(labelf));
                        Debug.WriteLine("----------------");

                    }
                    else
                    {
                        Debug.WriteLine("\n\nstring descending");
                        Debug.WriteLine("----------------");
                        Emitx(OpCodes.Ldarg_1); // swaps the order of Ldarg_1 and Ldarg_0 compared to ascending
                        Emitm(OpCodes.Callvirt, propGet);
                        Emitx(OpCodes.Ldarg_0);
                        Emitm(OpCodes.Callvirt, propGet);
                        Emitm(OpCodes.Call, _strCompareOrdinal);
                        Emitx(OpCodes.Stloc_0);
                        Emitx(OpCodes.Ldloc_0);
                        Emitb(OpCodes.Brfalse_S, labelf, nameof(labelf));
                        Emitx(OpCodes.Ldloc_0);
                        Emitx(OpCodes.Ret);
                        MarkLabel(labelf, nameof(labelf));
                        Debug.WriteLine("----------------");
                    }
                }
            }
            else
            {
                throw new ApplicationException($"unsupported property type: {propGet.ReturnType}");
            }

            // tmp variable only used in non 'last' string comparison, WHAT WOULD A List<SortDescriptor> COMPILER LOOK LIKE
            
            // the end pattern, is this correct for last cases 
            // branchx to label
            // ensure the return value is at the top of the stack, Ldc_I4_M1, Ldc_I4_1 or the return value of String.CompareOrdinal (String, String) will have put it there
            // 
        
        }

        return (Func<T, T, int>)dynamicMethod.CreateDelegate(typeof(Func<T, T, int>));
    }
}