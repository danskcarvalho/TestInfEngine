// See https://aka.ms/new-console-template for more information

using InfEngine;
using InfEngine.Engine;

var num = Z.M("Num");
var bol = Z.M("Bool");
var trait = Z.M("Trait");
var eq = Z.M("Eq");
var str = Z.M("Str");
var implClause1 = Z.Impl0("numTrait", num, trait).Assoc("Item", str);
var implClause2 = Z.Impl0("boolTrait", bol, trait).Assoc("Item", str);
var goal = Z.EqG(num.Proj(trait, "Item"), bol.Proj(trait, "Item"));
var solver = new Solver([goal], [implClause1, implClause2]);
var result = solver.Run();

Console.WriteLine("Hello World!");
