// See https://aka.ms/new-console-template for more information

using InfEngine;
using InfEngine.Engine;

var str = Z.M("str");
var eq = Z.M("Eq");
var list = Z.A1("List");
var strEq = Z.Impl0("strEq", str, eq);
var dictEq = Z.Impl1("listEq", (t1) => Z.Impl(list(t1), eq, Z.ImplC(list(list(t1)), eq)));
var goal1 = Z.ImplG(list(str), eq, "goal1");
var solver = new Solver([goal1], [strEq, dictEq]);
var result = solver.Run();

// infinite recursion
Console.WriteLine(result);

Console.WriteLine("Hello, World!");