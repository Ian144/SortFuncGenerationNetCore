using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace SortFuncGeneration;

// ReSharper disable once InconsistentNaming
public static class ILEmitGenerator
{
    public static Func<T, T, int> EmitSortFunc<T>(List<SortDescriptor> sortBys)
    {
        ArgumentNullException.ThrowIfNull(sortBys);

        var dynamicMethod = new DynamicMethod(
            name: "",
            returnType: typeof(int),
            parameterTypes: [typeof(T), typeof(T)],
            owner: typeof(T),
            skipVisibility: true);

        var generator = dynamicMethod.GetILGenerator();

        var strCompareOrdinal = typeof(string).GetMethod("CompareOrdinal", [typeof(string), typeof(string)])
            ?? throw new InvalidOperationException("Could not find String.CompareOrdinal method");


        for (int i = 0; i < sortBys.Count; ++i)
        {
            bool isLastComparison = i == sortBys.Count - 1;
            SortDescriptor sd = sortBys[i];
            MethodInfo propGet = typeof(T).GetProperty(sd.PropName)?.GetGetMethod();

            if (propGet == null)
                throw new InvalidPropertyException($"Unknown property '{sd.PropName}' on type '{typeof(T)}'");

            Type propType = propGet.ReturnType;

            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Callvirt, propGet);
            generator.Emit(OpCodes.Ldarg_1);
            generator.Emit(OpCodes.Callvirt, propGet);

            if (propType == typeof(int))
            {
                if (sd.Ascending)
                {
                    generator.Emit(OpCodes.Sub);
                }
                else
                {
                    generator.Emit(OpCodes.Sub);
                    generator.Emit(OpCodes.Neg);
                }
            }
            else if (propType == typeof(string))
            {
                generator.Emit(OpCodes.Call, strCompareOrdinal);
                if (!sd.Ascending)
                {
                    generator.Emit(OpCodes.Neg);
                }
            }
            else
            {
                throw new InvalidPropertyException($"Unsupported property type: {propType}");
            }

            if (!isLastComparison)
            {
                Label nextComparison = generator.DefineLabel();
                generator.Emit(OpCodes.Dup);
                generator.Emit(OpCodes.Brfalse_S, nextComparison);
                generator.Emit(OpCodes.Ret);
                generator.MarkLabel(nextComparison);
                generator.Emit(OpCodes.Pop);
            }
        }

        generator.Emit(OpCodes.Ret);

        return (Func<T, T, int>)dynamicMethod.CreateDelegate(typeof(Func<T, T, int>));
    }
}