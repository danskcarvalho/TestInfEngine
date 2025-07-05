using System.Diagnostics;
using InfEngine.Engine.Goals;

namespace InfEngine.Engine;

public partial class Solver
{
    private class SolverDriver
    {
        private readonly List<SolverDriverFrame> _solvers = new List<SolverDriverFrame>();

        public SolverDriver(Solver solver)
        {
            _solvers.Add(new RootFrame(solver));
        }

        public Solver? Run()
        {
            while (true)
            {
                if (this._solvers.Count == 0)
                    return null;

                var solvers = this._solvers[^1];

                if (solvers is RootFrame rs)
                {
                    var newFrame = rs.Solver.InternalRun();
                    if (newFrame == null)
                    {
                        return null;
                    }
                    Debug.Assert(newFrame is not RootFrame);
                    if (newFrame is SuccessFrame)
                    {
                        return rs.Solver;
                    }
                    this._solvers.Add(newFrame);
                }
                else if (solvers is ImplsOrNormsDriverFrame idf)
                {
                    if (idf.Solvers.Count == 1)
                    {
                        if (idf.Solvers[0]._infRec)
                        {
                            return idf.Solvers[0];
                        }

                        var newFrame = idf.Solvers[0].InternalRun();
                        if (newFrame == null)
                        {
                            return null;
                        }
                        
                        Debug.Assert(newFrame is not RootFrame);
                        if (newFrame is SuccessFrame)
                        {
                            return idf.Solvers[0];
                        }

                        this._solvers.Add(newFrame);
                    }
                    else
                    {
                        // error: we're done
                        return null;
                    }
                }
                else
                    return null;
            }
        }
    }

    private record SolverDriverFrame;

    private record SuccessFrame : SolverDriverFrame;
    private record RootFrame(Solver Solver) : SolverDriverFrame;

    private record ImplsOrNormsDriverFrame(List<Solver> Solvers) : SolverDriverFrame;
}