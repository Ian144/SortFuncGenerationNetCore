﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace SortFuncGeneration;

public class RoslynGenerator
{
    private readonly List<MetadataReference> _references = [];

    public RoslynGenerator()
    {
        ReferenceAssemblyContainingType<object>();
        ReferenceAssembly(typeof(Enumerable).Assembly);

        // reference the assembly containing type SortFuncGeneration.Target, i.e. this assembly
        ReferenceAssembly(Assembly.GetExecutingAssembly());
    }

    private void ReferenceAssembly(Assembly assembly) =>
        _references.Add(MetadataReference.CreateFromFile(assembly.Location));

    private void ReferenceAssemblyContainingType<T>() => ReferenceAssembly(typeof(T).Assembly);

    public IComparer<Target> GenComparer()
    {
        const string code = """
                            using SortFuncGeneration;
                                                namespace RosGen
                                                {
                                                     public class RoslynComparer : System.Collections.Generic.IComparer<Target>
                                                     {
                                                        // calling int.CompareTo
                                                        //int System.Collections.Generic.IComparer<Target>.Compare(Target xx, Target yy)
                                                        //{
                                                        //    int tmp = xx.IntProp1.CompareTo(yy.IntProp1);
                                                        //    if (tmp != 0)
                                                        //        return tmp;
                            
                                                        //    tmp = string.CompareOrdinal(xx.StrProp1, yy.StrProp1);
                                                        //    if (tmp != 0)
                                                        //        return tmp;
                            
                                                        //    tmp = xx.IntProp2.CompareTo(yy.IntProp2);
                                                        //    if (tmp != 0)
                                                        //        return tmp;
                            
                                                        //    return string.CompareOrdinal(xx.StrProp2, yy.StrProp2);
                                                        //}
                            
                                                        // using fundamental operators instead of int.CompareTo, final ternary
                                                        //int System.Collections.Generic.IComparer<Target>.Compare(Target xx, Target yy)
                                                        //{
                                                        //    if (xx.IntProp1 < yy.IntProp1) return -1;
                                                        //    if (xx.IntProp1 > yy.IntProp1) return 1;
                                                 
                                                        //    int tmp = string.CompareOrdinal(xx.StrProp1, yy.StrProp1);
                                                        //    if (tmp != 0)
                                                        //        return tmp;
                                                 
                                                        //    if (xx.IntProp2 < yy.IntProp2) return -1;
                                                        //    return xx.IntProp2 > yy.IntProp2
                                                        //        ? 1
                                                        //        : string.CompareOrdinal(xx.StrProp2, yy.StrProp2);
                                                        //}
                            
                                                        // using fundamental operators instead of int.CompareTo no final ternary
                                                         int System.Collections.Generic.IComparer<Target>.Compare(Target xx, Target yy)
                                                         {
                                                             if (xx.IntProp1 < yy.IntProp1) return -1;
                                                             if (xx.IntProp1 > yy.IntProp1) return 1;
                            
                                                             int tmp = string.CompareOrdinal(xx.StrProp1, yy.StrProp1);
                                                             if (tmp != 0)
                                                                 return tmp;
                            
                            	                             if (xx.IntProp2 < yy.IntProp2) return -1;
                            	                             if (xx.IntProp2 > yy.IntProp2) return 1;
                            
                            	                             return string.CompareOrdinal(xx.StrProp2, yy.StrProp2);
                                                         }
                            
                                                        // local variables vs propery access (aka function calls)
                                                        // this does require extra stloc. and ldloc. instructions
                                                        // int System.Collections.Generic.IComparer<Target>.Compare(Target xx, Target yy)
                                                        // {
                                                        //    int xx1 = xx.IntProp1;
                                                        //    int yy1 = yy.IntProp1;
                                                        //    if (xx1 < yy1) return -1;
                                                        //    if (xx1 > yy1) return 1;
                                                 
                                                        //    int tmp = string.CompareOrdinal(xx.StrProp1, yy.StrProp1);
                                                        //    if (tmp != 0)
                                                         //        return tmp;
                                                 
                                                         //    int xx2 = xx.IntProp2;
                                                         //    int yy2 = yy.IntProp2;
                                                         //    if (xx2 < yy2) return -1;
                                                         //    if (xx2 > yy2) return 1;
                            	                         //    return string.CompareOrdinal(xx.StrProp2, yy.StrProp2);
                                                         // }
                                                     }
                                            }
                            """;

        var assemblyName = Path.GetRandomFileName();
        var syntaxTree = CSharpSyntaxTree.ParseText(code);

        var compilation = CSharpCompilation.Create(
            assemblyName,
            [syntaxTree],
            _references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary,
                optimizationLevel: OptimizationLevel.Release));

        using var stream = new MemoryStream();
        var result = compilation.Emit(stream);

        if (!result.Success)
        {
#pragma warning disable CA1305
            IEnumerable<string> messages = result.Diagnostics.Select(x => $"{x.Id}: {x.GetMessage()}");
#pragma warning restore CA1305
            throw new InvalidOperationException(string.Join('\n', messages));
        }

        stream.Seek(0, SeekOrigin.Begin);
        var assembly = Assembly.Load(stream.ToArray());
        var type = assembly.GetType("RosGen.RoslynComparer");
        return (IComparer<Target>)Activator.CreateInstance(type!);
    }
}