namespace DelphAi.Core
{
    // Minimal stub for M1.1. Will grow with personality / vitals / behavior in
    // later slices. LLM-coupled fields (memory_summary / emotion / relationships
    // / divine_awareness) are intentionally deferred to Phase 2.
    public class Citizen
    {
        public string Name { get; }

        public Citizen(string name)
        {
            Name = name;
        }
    }
}
