// See https://aka.ms/new-console-template for more information

using InfEngine;
using InfEngine.Engine;

var str = Z.M("str");
var eq = Z.M("Eq");
var list = Z.A1("List");
var strEq = Z.Impl0("strEq", str, eq);
var listEq = Z.Impl1("listEq", t => Z.Impl(list(t), eq, Z.ImplC(t, eq)));
var goal = Z.ImplG(list(list(str)), eq, "goal1");
var solver = new Solver([goal], [strEq, listEq]);
var result = solver.Run();
