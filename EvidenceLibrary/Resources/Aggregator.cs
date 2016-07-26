using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rage;

namespace EvidenceLibrary.Resources
{
    internal class Aggregator<T> where T : struct
    {
        private static readonly Dictionary<T, SpawnPoint> _spawns = new Dictionary<T, SpawnPoint>();

        public SpawnPoint GetClosestSpawn(Vector3 pos)
        {
            return _spawns.Values.OrderBy(s => Vector3.Distance(pos, s.Position)).First();
        }

        //public 
    }
}
