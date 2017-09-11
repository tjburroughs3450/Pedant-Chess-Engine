using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessEnginePort
{
    public class CuncurrentTaskManager
    {
        private List<WorkItem> actions;

        private struct WorkItem
        {
            public string name;
            public Func<object> task;

            public WorkItem(string name, Func<object> task)
            {
                this.name = name;
                this.task = task;
            }
        }

        public CuncurrentTaskManager()
        {
            actions = new List<WorkItem>();
        }

        public void addTask(string name, Func<object> task)
        {
            actions.Add(new WorkItem(name, task));
        }

        public Dictionary<string, object> process()
        {
            Dictionary<string, object> results = new Dictionary<string, object>();

            Parallel.ForEach(this.actions, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount }, (f) =>
            {
                object result = f.task();

                lock (results)
                {
                    results.Add(f.name, result);
                }
            });

            return results;
        }
    }
}
