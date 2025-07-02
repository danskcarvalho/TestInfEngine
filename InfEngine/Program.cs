// See https://aka.ms/new-console-template for more information

using InfEngine;
using InfEngine.Engine;

var a = Z.M("B");
var num = Z.M("Num");
var list = Z.M("List");
var implClause = Z.Impl0("listTrait", a, list).Assoc("Item", num);
var goal = Z.NormG(a.Proj(list, "Item"), Z.Fv("a"));
var solver = new Solver([goal], [implClause]);
var result = solver.Run();

Console.WriteLine("Hello World!");
