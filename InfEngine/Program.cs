// See https://aka.ms/new-console-template for more information

using InfEngine;
using InfEngine.Engine;

var num = Z.M("Num");
var trait = Z.M("Trait");
var implClause1 = Z.Impl0("numTrait", num, trait).Assoc("Item", num);
var goal = Z.NormG(num.Proj(trait, "Item").Proj(trait, "Item"), Z.Fv("a"));
var solver = new Solver([goal], [implClause1]);
var result = solver.Run();

Console.WriteLine("Hello World!");
