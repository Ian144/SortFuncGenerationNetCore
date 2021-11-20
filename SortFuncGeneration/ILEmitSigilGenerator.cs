using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
//using Sigil;


//namespace SortFuncGeneration
//{
//    public static class ILEmitSigilGenerator
//    {
//        private static readonly MethodInfo _strCompareOrdinal = typeof(string).GetMethod("CompareOrdinal", new[] { typeof(string), typeof(string) });
//        private static readonly MethodInfo _intCompareTo = typeof(int).GetMethod("CompareTo", new[] { typeof(int) });

//        [System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S125:Sections of code should not be commented out", Justification = "<Pending>")]
//        public static Func<T, T, int> EmitSortFunc<T>(List<SortDescriptor> sortBys)
//        {
//            var xs = sortBys.Select(sd => (typeof(T).GetMethod($"get_{sd.PropName}"), sd.Ascending)).ToList();

//            if(xs.Any(t2 => t2.Item1== null))
//                throw new ApplicationException($"unknown property on: {typeof(T)}");

//            var em = Emit<Func<T, T, int>>.NewDynamicMethod("SigilEmitSortFunc");


//            using Local tmpLocal = em.DeclareLocal(typeof(int));
//            var label = em.DefineLabel("label1");

//            for(int ctr = 0; ctr < xs.Count; ++ctr)
//            {
//                (MethodInfo propGet, bool ascending) = xs[ctr];

//                if (propGet.ReturnType == typeof(int))
//                {
//                    if (ascending)
//                    {
//                        em.LoadArgument(0);
//                        em.CallVirtual(propGet);
//                        em.StoreArgument(0);        // why does this need to be done for an int (a value-type) and not for a string (reference type)?
//                        em.LoadLocalAddress(tmpLocal);
//                        em.LoadArgument(1);
//                        em.CallVirtual(propGet);
//                        em.CallVirtual(_intCompareTo);

//                        //il.Emit(OpCodes.Ldarg_0);
//                        //il.Emit(OpCodes.Call, propGet);
//                        //il.Emit(OpCodes.Stloc_0);
//                        //il.Emit(OpCodes.Ldloca, 0);
//                        //il.Emit(OpCodes.Ldarg_1);
//                        //il.Emit(OpCodes.Call, propGet);
//                        //il.Emit(OpCodes.Call, _intCompareTo);
//                    }
//                    else
//                    {
//                        // LoadArgument calls are the other way around compared to ascending
//                        //em.LoadArgument(1);
//                        //em.Call(propGet);
//                        //em.StoreArgument(0);
//                        //em.LoadLocalAddress(tmpLocal);
//                        //em.LoadArgument(0);
//                        //em.Call(propGet);
//                        //em.Call(_intCompareTo);

//                        em.LoadArgument(1);
//                        em.CallVirtual(propGet);
//                        em.LoadArgument(0);
//                        em.CallVirtual(propGet);
//                        em.BranchIfLessOrEqual(label);

//                        //il.Emit(OpCodes.Ldarg_1);
//                        //il.Emit(OpCodes.Call, propGet);
//                        //il.Emit(OpCodes.Stloc_0);
//                        //il.Emit(OpCodes.Ldloca, 0);
//                        //il.Emit(OpCodes.Ldarg_0);
//                        //il.Emit(OpCodes.Call, propGet);
//                        //il.Emit(OpCodes.Call, _intCompareTo);
//                    }
//                }
//                else if (propGet.ReturnType == typeof(string))
//                {
//                    if (ascending)
//                    {
//                        em.LoadArgument(0);
//                        em.Call(propGet);
//                        em.LoadArgument(1);
//                        em.Call(propGet);
//                        em.Call(_strCompareOrdinal);

//                        //il.Emit(OpCodes.Ldarg_0);
//                        //il.Emit(OpCodes.Call, propGet);
//                        //il.Emit(OpCodes.Ldarg_1);
//                        //il.Emit(OpCodes.Call, propGet);
//                        //il.Emit(OpCodes.Call, _strCompareOrdinal);
//                    }
//                    else
//                    {
//                        em.LoadArgument(1);
//                        em.Call(propGet);
//                        em.LoadArgument(0);
//                        em.Call(propGet);
//                        em.Call(_strCompareOrdinal);

//                        //il.Emit(OpCodes.Ldarg_1);
//                        //il.Emit(OpCodes.Call, propGet);
//                        //il.Emit(OpCodes.Ldarg_0);
//                        //il.Emit(OpCodes.Call, propGet);
//                        //il.Emit(OpCodes.Call, _strCompareOrdinal);
//                    }
//                }
//                else
//                {
//#pragma warning disable S112 // General exceptions should never be thrown
//                    throw new ApplicationException($"unsupported property type: {propGet.ReturnType}");
//#pragma warning restore S112 // General exceptions should never be thrown
//                }

//                bool isLast = ctr == xs.Count - 1;

//                if (!isLast)
//                {
//                    em.Duplicate();
//                    em.BranchIfTrue(label);
//                    em.Pop();
//                    //il.Emit(OpCodes.Dup); // Brtrue_S will pop the last value on the stack after testing
//                    //il.Emit(OpCodes.Brtrue_S, label1);
//                    //il.Emit(OpCodes.Pop);
//                }
//                else
//                {
//                    em.MarkLabel(label);
//                    em.Return();
//                    //il.MarkLabel(label1);
//                    //il.Emit(OpCodes.Ret);
//                }
//            }


//            return em.CreateDelegate();
//        }
//    }
//}
