using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;

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
            pManager.AddParameter(new EllipsoidParameter(),"Ellipsoid", "Ellipsoid", "Ellipsoids.", GH_ParamAccess.list);
            pManager.AddNumberParameter("Minimum", "Min", "Minimum of tolerance.", GH_ParamAccess.item,0.9);
            pManager.AddNumberParameter("Maximum", "Max", "Maximum of tolerance.", GH_ParamAccess.item,1.2);
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
            List<EllipsoidGoo> ellipsoidGoos = new List<EllipsoidGoo>();
            if (!DA.GetDataList(0, ellipsoidGoos)) { AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid ellipsoid list."); return; }
            List<Ellipsoid> ellipsoids = new List<Ellipsoid>();
            foreach (EllipsoidGoo eg in ellipsoidGoos)
            {
                ellipsoids.Add(eg.Value);
            }
            double min_tolerance = 0;
            DA.GetData(1, ref min_tolerance);
            double max_tolerance = 0;
            DA.GetData(2, ref max_tolerance);
            List<Point3d> centroids = new List<Point3d>();
            List<Line> lattice = new List<Line>();
            for (int i = 0; i < ellipsoids.Count; i++)
            {
                Ellipsoid e_i = ellipsoids[i];
                for (int j = i + 1; j < ellipsoids.Count; j++)
                {
                    Ellipsoid e_j = ellipsoids[j];
                    var rim_dist = e_i.GetRimDistance(e_j);
                    Point3d center1 = new Point3d(e_i.position);
                    Point3d center2 = new Point3d(e_j.position);
                    var center_dist = center1.DistanceTo(center2);
                    bool kissing = center_dist <= rim_dist * max_tolerance && center_dist >= rim_dist * min_tolerance;
                    if (kissing)
                    {
                        lattice.Add(new Line(center1, center2));
                    }
                }
                centroids.Add(new Point3d(e_i.position));
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