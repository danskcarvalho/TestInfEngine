namespace InfEngine.Engine;

public partial class Solver
{
    public class IterationCount
    {
        private int _iterations;

        public void Increment()
        {
            this._iterations++;
        }

        public bool Overflown()
        {
            return this._iterations > MaxIterations;
        }
        
        public int Count => this._iterations;
    }
}