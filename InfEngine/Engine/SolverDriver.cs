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
                else if (solvers is ImplsDriverFrame idf)
                {
                    MoveNext:
                    if (idf.Solvers.MoveNext())
                    {
                        if (idf.Solvers.Current._infRec)
                        {
                            goto MoveNext;
                        }

                        var newFrame = idf.Solvers.Current.InternalRun();
                        if (newFrame == null)
                        {
                            continue;
                        }
                        Debug.Assert(newFrame is not RootFrame);
                        if (newFrame is SuccessFrame)
                        {
                            return idf.Solvers.Current;
                        }

                        this._solvers.Add(newFrame);
                    }
                    else
                    {
                        this._solvers.RemoveAt(this._solvers.Count - 1);
                        if (this._solvers.Count == 1)
                        {
                            // we returned to the root, so we're done
                            return null;
                        }
                    }
                }
                else if (solvers is NormsDriverFrame ndf)
                {
                    if (ndf.Solvers.MoveNext())
                    {
                        if (ndf.Solvers.Current._infRec)
                        {
                            return ndf.Solvers.Current;
                        }
                        
                        var newFrame = ndf.Solvers.Current.InternalRun();
                        if (newFrame == null)
                        {
                            continue;
                        }
                        Debug.Assert(newFrame is not RootFrame);
                        if (newFrame is SuccessFrame)
                        {
                            return ndf.Solvers.Current;
                        }
                        this._solvers.Add(newFrame);
                    }
                    else
                    {
                        this._solvers.RemoveAt(this._solvers.Count - 1);
                        if (this._solvers.Count == 1)
                        {
                            // we returned to the root, so we're done
                            return null;
                        }
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
    private record ImplsDriverFrame(IEnumerator<Solver> Solvers) : SolverDriverFrame;
    private record NormsDriverFrame(IEnumerator<Solver> Solvers) : SolverDriverFrame;
}