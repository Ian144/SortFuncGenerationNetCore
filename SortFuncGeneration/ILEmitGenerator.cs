using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using static System.String;

namespace SortFuncGeneration;

// ReSharper disable once InconsistentNaming
public static class ILEmitGenerator
{
    private static readonly MethodInfo _strCompareOrdinal = typeof(string).GetMethod("CompareOrdinal", new[] { typeof(string), typeof(string) });
        
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
        
        generator.DeclareLocal(typeof(int));

        for(int ctr = 0; ctr < xs.Count; ++ctr)
        {
            bool isLastComparison = ctr == xs.Count - 1;
            (MethodInfo propGet, bool ascending) = xs[ctr];

            if (propGet.ReturnType == typeof(int))
            {
                if (isLastComparison)
                {
                    throw new NotImplementedException();
                }

                var labelx = generator.DefineLabel();
                var labely = generator.DefineLabel();

                if (ascending)
                {
                    generator.Emit(OpCodes.Ldarg_0);
                    generator.Emit(OpCodes.Callvirt, propGet);
                    generator.Emit(OpCodes.Ldarg_1);
                    generator.Emit(OpCodes.Callvirt, propGet);
                    generator.Emit(OpCodes.Bge_S, labelx);
                    generator.Emit(OpCodes.Ldc_I4_M1); // return -1
                    generator.Emit(OpCodes.Ret);
                    generator.MarkLabel(labelx);

                    generator.Emit(OpCodes.Ldarg_0);
                    generator.Emit(OpCodes.Callvirt, propGet);
                    generator.Emit(OpCodes.Ldarg_1);
                    generator.Emit(OpCodes.Callvirt, propGet);
                    generator.Emit(OpCodes.Ble_S, labely);
                    generator.Emit(OpCodes.Ldc_I4_1); // return 1
                    generator.Emit(OpCodes.Ret);
                    generator.MarkLabel(labely);
                }
                else
                {
                    generator.Emit(OpCodes.Ldarg_0);
                    generator.Emit(OpCodes.Callvirt, propGet);
                    generator.Emit(OpCodes.Ldarg_1);
                    generator.Emit(OpCodes.Callvirt, propGet);
                    generator.Emit(OpCodes.Ble_S, labelx); // Bge_S if ascending
                    generator.Emit(OpCodes.Ldc_I4_1); // return 1, would return -1 if ascending
                    generator.Emit(OpCodes.Ret); 
                    generator.MarkLabel(labelx);

                    generator.Emit(OpCodes.Ldarg_0);
                    generator.Emit(OpCodes.Callvirt, propGet);
                    generator.Emit(OpCodes.Ldarg_1);
                    generator.Emit(OpCodes.Callvirt, propGet);
                    generator.Emit(OpCodes.Bge_S, labely);
                    generator.Emit(OpCodes.Ldc_I4_M1); // return -1
                    generator.Emit(OpCodes.Ret);
                    generator.MarkLabel(labely);
                }
            }
            else if (propGet.ReturnType == typeof(string))
            {
                if (ascending)
                {
                    generator.Emit(OpCodes.Ldarg_0);
                    generator.Emit(OpCodes.Callvirt, propGet);
                    generator.Emit(OpCodes.Ldarg_1);
                    generator.Emit(OpCodes.Callvirt, propGet);
                }
                else
                {
                    // flips the order of Ldarg_1 and Ldarg_0 compared to ascending
                    generator.Emit(OpCodes.Ldarg_1);
                    generator.Emit(OpCodes.Callvirt, propGet);
                    generator.Emit(OpCodes.Ldarg_0);
                    generator.Emit(OpCodes.Callvirt, propGet);
                }

                generator.Emit(OpCodes.Call, _strCompareOrdinal);

                if (isLastComparison)
                {
                    generator.Emit(OpCodes.Ret);
                }
                else
                {
                    var labelf = generator.DefineLabel();
                    generator.Emit(OpCodes.Stloc_0);
                    generator.Emit(OpCodes.Ldloc_0);
                    generator.Emit(OpCodes.Brfalse_S, labelf);
                    generator.Emit(OpCodes.Ldloc_0);
                    generator.Emit(OpCodes.Ret);
                    generator.MarkLabel(labelf);
                }
            }
            else
            {
                throw new ApplicationException($"unsupported property type: {propGet.ReturnType}");
            }
        }

        return (Func<T, T, int>)dynamicMethod.CreateDelegate(typeof(Func<T, T, int>));
    }
}