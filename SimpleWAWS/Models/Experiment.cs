namespace SimpleWAWS.Models
{
    public class Experiment
    {
        public string Name { get; private set; }
        public int Weight { get; private set; }

        public Experiment(string name, int weight = 100)
        {
            this.Name = name;
            this.Weight = weight;
        }
    }
}