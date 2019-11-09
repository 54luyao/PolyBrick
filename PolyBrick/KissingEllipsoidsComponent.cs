using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;
using Rhino.Collections;

namespace PolyBrick.EllipsoidPacking
{
    public class Kissing_Ellipsoids : GH_Component
    {
        public override GH_Exposure Exposure => GH_Exposure.secondary;

        /// <summary>
        /// Initializes a new instance of the Kissing_Ellipsoids class.
        /// </summary>
        public Kissing_Ellipsoids()
          : base("Kissing Ellipsoids", "Kissing Ellipsoids",
              "Connect the centers of kissing ellipsoids.",
              "PolyBrick", "Packing")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddParameter(new EllipsoidParameter(), "Ellipsoid", "Ellipsoid", "Ellipsoids.", GH_ParamAccess.list);
            pManager.AddNumberParameter("Minimum", "Min", "Minimum of tolerance.", GH_ParamAccess.item, 0.9);
            pManager.AddNumberParameter("Maximum", "Max", "Maximum of tolerance.", GH_ParamAccess.item, 1.2);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.Register_PointParam("Centroid", "Centroid", "Centroids of ellipsoids.", GH_ParamAccess.list);
            pManager.Register_LineParam("Lattice", "Lattice", "Lattice of kissing ellipsoids.", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<Point3d> centroids = new List<Point3d>();
            List<Line> lattice = new List<Line>();
            List<EllipsoidGoo> ellipsoidGoos = new List<EllipsoidGoo>();
            if (!DA.GetDataList(0, ellipsoidGoos)) { AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid ellipsoid list."); return; }
            List<Ellipsoid> ellipsoids = new List<Ellipsoid>();
            double maxR = Double.MinValue;
            for (int i = 0; i < ellipsoidGoos.Count; i++)
            {
                Ellipsoid e = ellipsoidGoos[i].Value;
                ellipsoids.Add(e);
                maxR = Math.Max(Math.Max(Math.Max(maxR, e.radiusC), e.radiusB), e.radiusA);
                centroids.Add(new Point3d(e.position));
            }
            double min_tolerance = 0;
            DA.GetData(1, ref min_tolerance);
            double max_tolerance = 0;
            DA.GetData(2, ref max_tolerance);

            BoundingBox bBox = new BoundingBox(centroids);
            Point3d min_corner = bBox.Min;
            Point3d max_corner = bBox.Max;

            double cell_size = maxR * 2 * 1.01;
            int xCount = (int)Math.Ceiling((max_corner.X - min_corner.X) / cell_size);
            int yCount = (int)Math.Ceiling((max_corner.Y - min_corner.Y) / cell_size);
            int zCount = (int)Math.Ceiling((max_corner.Z - min_corner.Z) / cell_size);

            Grid grid = new Grid(xCount, yCount, zCount, maxR);

            for (int i = 0; i < ellipsoids.Count; i++)
            {
                grid.Allocate(ellipsoids[i]);
            }

            bool kissing;
            for (int x = 0; x < grid.x_count; x++)
            {
                for (int y = 0; y < grid.y_count; y++)
                {
                    for (int z = 0; z < grid.z_count; z++)
                    {
                        LinkedList<Ellipsoid> cell = grid.cells[x, y, z];
                        if (cell.Count != 0)
                        {
                            Ellipsoid[] cell_array = new Ellipsoid[cell.Count];
                            cell.CopyTo(cell_array, 0);
                            List<Ellipsoid> neighbors = grid.GetNeighborCellEllipsoids(x, y, z);
                            for (int i = 0; i < cell.Count; i++)
                            {
                                Ellipsoid ellipsoid_i = cell_array[i];
                                double d;
                                double rimDistance;
                                for (int j = i + 1; j < cell.Count; j++)
                                {
                                    Ellipsoid ellipsoid_j = cell_array[j];
                                    d = new Point3d(ellipsoid_i.position).DistanceTo(new Point3d(ellipsoid_j.position));
                                    rimDistance = ellipsoid_i.GetRimDistance(ellipsoid_j);
                                    kissing = d <= rimDistance * max_tolerance && d >= rimDistance * min_tolerance;
                                    if (kissing)
                                    {
                                        lattice.Add(new Line(new Point3d(ellipsoid_i.position), new Point3d(ellipsoid_j.position)));
                                    }

                                }
                                for (int k = 0; k < neighbors.Count; k++)
                                {
                                    Ellipsoid ellipsoid_k = neighbors[k];
                                    if (ellipsoid_i.moved || ellipsoid_k.moved)
                                    {
                                        d = new Point3d(ellipsoid_i.position).DistanceTo(new Point3d(ellipsoid_k.position));
                                        rimDistance = ellipsoid_i.GetRimDistance(ellipsoid_k);
                                        kissing = d <= rimDistance * max_tolerance && d >= rimDistance * min_tolerance;
                                        if (kissing)
                                        {
                                            lattice.Add(new Line(new Point3d(ellipsoid_i.position), new Point3d(ellipsoid_k.position)));
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            DA.SetDataList(0, centroids);
            DA.SetDataList(1, lattice);
        }
        
        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                //You can add image files to your project resources and access them like this:
                // return Resources.IconForThisComponent;
                return Resource1.PolyBrickIcons_44;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("d7b1c734-66f9-47d7-b2e8-805472b9376a"); }
        }
    }
}