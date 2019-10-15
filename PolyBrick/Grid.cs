using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rhino;
using Rhino.Geometry;

namespace PolyBrick.EllipsoidPacking
{
    class Grid
    {
        public int x_count;
            public int y_count;
            public int z_count;
            public double cell_size;
            public List<Ellipsoid>[,,] cells;
            //public Stress[,,] stresses; //TODO
            public Dictionary<Ellipsoid, int> ellipsoid_index;

            public Grid(int x, int y, int z, double size)
            {
                x_count = x;
                y_count = y;
                z_count = z;
                cell_size = size;
                cells = new List<Ellipsoid>[x, y, z];
                ellipsoid_index = new Dictionary<Ellipsoid, int>();
                for (int i = 0; i < x_count; i++)
                {
                    for (int j = 0; j < y_count; j++)
                    {
                        for (int k = 0; k < z_count; k++)
                        {
                            cells[i, j, k] = new List<Ellipsoid>();
                        }
                    }
                }
            }

            //public List<Circle> GetCircles(int x,int y,int z)
            //{
            //    return cells[x, y, z];
            //}

            public void Allocate(Ellipsoid circle)
            {
                //int x = (int)Math.Floor((circle.position.X -Globals.BOUND_X_MIN)/ cell_size);
                //int y = (int)Math.Floor((circle.position.Y - Globals.BOUND_Y_MIN) / cell_size);
                //int z = (int)Math.Floor((circle.position.Z - Globals.BOUND_Z_MIN) / cell_size);
                int pos_x = (int)Math.Floor((circle.position.X - EGlobals.BOUND_X_MIN) / cell_size);
                if (pos_x >= x_count) pos_x = x_count - 1;
                if (pos_x < 0) pos_x = 0;
                int pos_y = (int)Math.Floor((circle.position.Y - EGlobals.BOUND_Y_MIN) / cell_size);
                if (pos_y >= x_count) pos_y = y_count - 1;
                if (pos_y < 0) pos_y = 0;
                int pos_z = (int)Math.Floor((circle.position.Z - EGlobals.BOUND_Z_MIN) / cell_size);
                if (pos_z >= z_count) pos_z = z_count - 1;
                if (pos_z < 0) pos_z = 0;
                cells[pos_x, pos_y, pos_z].Add(circle);
                
                
                //circle_index.Add(circle, i);

            }

            public List<Ellipsoid> GetNeighborCellEllipsoids(int x, int y, int z)
            {
                List<Ellipsoid> c1 = GetOneCell(x - 1, y - 1, z - 1);
                List<Ellipsoid> c2 = GetOneCell(x, y - 1, z - 1);
                List<Ellipsoid> c3 = GetOneCell(x - 1, y - 1, z);
                List<Ellipsoid> c4 = GetOneCell(x, y - 1, z);
                List<Ellipsoid> c5 = GetOneCell(x - 1, y - 1, z + 1);
                List<Ellipsoid> c6 = GetOneCell(x, y - 1, z + 1);
                List<Ellipsoid> c7 = GetOneCell(x + 1, y - 1, z - 1);
                List<Ellipsoid> c8 = GetOneCell(x + 1, y - 1, z);
                List<Ellipsoid> c9 = GetOneCell(x + 1, y - 1, z + 1);
                List<Ellipsoid> c10 = GetOneCell(x - 1, y, z - 1);
                List<Ellipsoid> c11 = GetOneCell(x - 1, y, z);
                List<Ellipsoid> c12 = GetOneCell(x - 1, y, z + 1);
                List<Ellipsoid> c13 = GetOneCell(x, y, z + 1);

                List<Ellipsoid> neighbors = new List<Ellipsoid>();
                if (c1 != null) neighbors.AddRange(c1);
                if (c2 != null) neighbors.AddRange(c2);
                if (c3 != null) neighbors.AddRange(c3);
                if (c4 != null) neighbors.AddRange(c4);
                if (c5 != null) neighbors.AddRange(c5);
                if (c6 != null) neighbors.AddRange(c6);
                if (c7 != null) neighbors.AddRange(c7);
                if (c8 != null) neighbors.AddRange(c8);
                if (c9 != null) neighbors.AddRange(c9);
                if (c10 != null) neighbors.AddRange(c10);
                if (c11 != null) neighbors.AddRange(c11);
                if (c12 != null) neighbors.AddRange(c12);
                if (c13 != null) neighbors.AddRange(c13);

                return neighbors;
            }

            public List<Ellipsoid> GetOneCell(int x, int y, int z)
            {
                if (x < 0 || y < 0 || z < 0 || x > x_count - 1 || y > y_count - 1 || z > z_count - 1)
                {
                    return null;
                }
                return cells[x, y, z];
            }

            public List<Ellipsoid> GetOneCell(Ellipsoid ellipsoid)
            {
                int pos_x = (int)Math.Floor((ellipsoid.position.X - EGlobals.BOUND_X_MIN) / cell_size);
                if (pos_x >= x_count) pos_x = x_count - 1;
                if (pos_x < 0) pos_x = 0;
                int pos_y = (int)Math.Floor((ellipsoid.position.Y - EGlobals.BOUND_Y_MIN) / cell_size);
                if (pos_y >= x_count) pos_y = y_count - 1;
                if (pos_y < 0) pos_y = 0;
                int pos_z = (int)Math.Floor((ellipsoid.position.Z - EGlobals.BOUND_Z_MIN) / cell_size);
                if (pos_z >= z_count) pos_z = z_count - 1;
                if (pos_z < 0) pos_z = 0;
                return cells[pos_x, pos_y, pos_z];
            }
    }
}
