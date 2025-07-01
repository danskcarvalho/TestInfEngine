// See https://aka.ms/new-console-template for more information

using InfEngine;
using InfEngine.Engine;

var str = Z.M("str");
var trait = Z.M("List");
var vec = Z.A1("Vec");
var aliasClause = Z.Alias1("Item", t => Z.Alias(vec(t), trait, t));
var implClause = Z.Impl1("lisTrait", t => Z.Impl(vec(t), trait));
var goal = Z.NormG(vec(str).Proj(trait, "Item"), Z.Fv("a"));
var solver = new Solver([goal], [aliasClause, implClause]);
var result = solver.Run();

Console.WriteLine("Hello World!");
