using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using static System.String;



namespace SortFuncGeneration;

public static class ILEmitGenerator
{
    private static readonly MethodInfo _strCompareOrdinal = typeof(string).GetMethod("CompareOrdinal", new[] { typeof(string), typeof(string) });
    private static readonly MethodInfo _intCompareTo = typeof(int).GetMethod("CompareTo", new[] { typeof(int) });
        
    public static Func<T, T, int> EmitSortFunc<T>(List<SortDescriptor> sortBys)
    {
        var xs = sortBys.Select(sd => (typeof(T).GetMethod($"get_{sd.PropName}"), sd.Ascending)).ToList();

        if(xs.Any(t2 => t2.Item1== null))
            throw new ApplicationException($"unknown property on: {typeof(T)}");

        Type returnType = typeof(int);
        Type[] methodParamTypes = { typeof(T), typeof(T) };

        var dynamicMethod = new DynamicMethod(
            name: Empty,
            returnType: returnType,
            parameterTypes: methodParamTypes,
            owner: typeof(ILEmitGenerator),
            skipVisibility: true);

        var il = dynamicMethod.GetILGenerator();
        il.DeclareLocal(typeof(int));
        var label1 = il.DefineLabel();

        for(int ctr = 0; ctr < xs.Count; ++ctr)
        {
            (MethodInfo propGet, bool ascending) = xs[ctr];

            if (propGet.ReturnType == typeof(int))
            {
                if (ascending)
                {
                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Call, propGet);
                    il.Emit(OpCodes.Stloc_0); // why does this need to be done for an int (a value-type) and not for a string (reference type)?
                    il.Emit(OpCodes.Ldloca, 0); // load location address, unlike for string, i suspect this is because the first arg to int.CompareTo must be an address
                    il.Emit(OpCodes.Ldarg_1);
                    il.Emit(OpCodes.Call, propGet);
                    il.Emit(OpCodes.Call, _intCompareTo);
                }
                else
                {
                    // Ldarg_ are the other way around compared to ascending
                    il.Emit(OpCodes.Ldarg_1);
                    il.Emit(OpCodes.Call, propGet);
                    il.Emit(OpCodes.Stloc_0);
                    il.Emit(OpCodes.Ldloca, 0);
                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Call, propGet);
                    il.Emit(OpCodes.Call, _intCompareTo);
                }
            }
            else if (propGet.ReturnType == typeof(string))
            {
                if (ascending)
                {
                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Call, propGet);
                    il.Emit(OpCodes.Ldarg_1);
                    il.Emit(OpCodes.Call, propGet);
                    il.Emit(OpCodes.Call, _strCompareOrdinal);
                }
                else
                {
                    il.Emit(OpCodes.Ldarg_1);
                    il.Emit(OpCodes.Call, propGet);
                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Call, propGet);
                    il.Emit(OpCodes.Call, _strCompareOrdinal);
                }
            }
            else
            {
                throw new ApplicationException($"unsupported property type: {propGet.ReturnType}");
            }

            bool isLast = ctr == xs.Count - 1;

            if (!isLast)
            {
                il.Emit(OpCodes.Dup); // Brtrue_S will pop the last value on the stack after testing
                il.Emit(OpCodes.Brtrue_S, label1);
                il.Emit(OpCodes.Pop);
            }
            else
            {
                il.MarkLabel(label1);
                il.Emit(OpCodes.Ret);
            }
        }

        return (Func<T, T, int>)dynamicMethod.CreateDelegate(typeof(Func<T, T, int>));
    }
}