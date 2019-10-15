using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rhino.Geometry;
using Rhino;

namespace PolyBrick
{
    class GlobalRandom
    {
        public static Random rand = new System.Random();

        public static Vector3d RandomVector()
        {

            double x = rand.NextDouble() * 2 - 1;
            double y = rand.NextDouble() * 2 - 1;
            double z = rand.NextDouble() * 2 - 1;
            Vector3d result = new Vector3d(x, y, z);
            result.Unitize();
            return result;
        }
    }
}
