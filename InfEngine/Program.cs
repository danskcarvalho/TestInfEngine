// See https://aka.ms/new-console-template for more information

using InfEngine;
using InfEngine.Engine;

var str = Z.M("str");
var i32 = Z.M("i32");
var trait = Z.M("Trait");
var aliasClause = Z.Alias0("Proj", str, trait, i32);
var implClause = Z.Impl0("strTrait", str, trait);
var goal = Z.NormG(str.Proj(trait, "Proj"), Z.Fv("a"));
var solver = new Solver([goal], [aliasClause, implClause]);
var result = solver.Run();
