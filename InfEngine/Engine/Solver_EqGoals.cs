using InfEngine.Engine.Terms;

namespace InfEngine.Engine;

public partial class Solver
{
    private bool ProcessEquationGoals()
    {
        while (this._eqGoals.Count != 0)
        {
            CreateNormalizationGoals(this._normGoals, this._eqGoals, null, 0);

            var eqGoal = this._eqGoals[^1];
            this._eqGoals.RemoveAt(this._eqGoals.Count - 1);

            var match = Term.TryMatch(eqGoal.Left.Substitute(this._match), eqGoal.Right.Substitute(this._match));
            if (match == null)
            {
                return false;
            }

            this._match = this._match.Merge(match);
            this._eqGoals.AddRange(this._match.LateGoals);
            this._match = this._match.PurgeGoals();
        }

        return true;
    }

    private void ApplySubstitutionsToGoals()
    {
        if (!this._match.IsEmpty)
        {
            for (int i = 0; i < this._implGoals.Count; i++)
            {
                this._implGoals[i] = new RecImplGoalChain(
                    this._implGoals[i].Goal.Substitute(this._match),
                    this._implGoals[i].Chain,
                    this._implGoals[i].RecursionDepth);
            }

            for (int i = 0; i < this._normGoals.Count; i++)
            {
                this._normGoals[i] = new RecNormGoalChain(
                    this._normGoals[i].Goal.Substitute(this._match),
                    this._normGoals[i].Chain,
                    this._normGoals[i].RecursionDepth);
            }

            this._provenImplGoals = this._provenImplGoals.ToDictionary(
                x => x.Key,
                x => x.Value.Select(y => y.Substitute(this._match)).ToList());

            foreach (var instName in this._instatiations.Keys.ToList())
            {
                this._instatiations[instName] = this._instatiations[instName].Substitute(this._match);
            }
        }
    }
}