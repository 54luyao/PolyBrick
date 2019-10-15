using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;

namespace PolyBrick
{
    public class KissingSpheresComponent : GH_Component
    {
        public override GH_Exposure Exposure => GH_Exposure.secondary;

        /// <summary>
        /// Initializes a new instance of the KissingSpheres class.
        /// </summary>
        public KissingSpheresComponent()
          : base("Kissing Spheres", "Kissing Spheres",
              "Connect the centroids of kissing spheres.",
              "PolyBrick", "Packing")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddPointParameter("Centroid", "Centroid", "Centroids of spheres.", GH_ParamAccess.list);
            pManager.AddNumberParameter("Radius", "Radius", "Radius of spheres.", GH_ParamAccess.list);
            pManager.AddNumberParameter("Minimum", "Min", "Minimum of tolerance.", GH_ParamAccess.item);
            pManager.AddNumberParameter("Maximum", "Max", "Maximum of tolerance.", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddCurveParameter("Lattice", "Lattice", "Lattice from kissing spheres.", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<Point3d> centroid = new List<Point3d>();
            if (!DA.GetDataList(0, centroid)) { AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid point list."); return; }
            List<double> radius = new List<double>();
            if (!DA.GetDataList(1, radius)) { AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid radius list."); return; }
            double min_tolerance = 0;
            if (!DA.GetData(2, ref min_tolerance)) { AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid minimum tolerance."); return; }
            double max_tolerance = 0;
            if (!DA.GetData(3, ref max_tolerance)) { AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid maximum tolerance."); return; }
            List <Line> lattice = new List<Line>();
            int length = radius.Count;
            for (int i = 0; i < length; i++) {
                for (int j=i+1; j < length; j++)
                {
                    Point3d p1 = centroid[i];
                    Point3d p2 = centroid[j];
                    double dist = p1.DistanceTo(p2);
                    double total_r = radius[i] + radius[j];
                    bool kissing = dist <= total_r * max_tolerance && dist >= total_r * min_tolerance;
                    if (kissing) {
                        lattice.Add(new Line(p1, p2));
                    }
                }
            }
            DA.SetDataList(0, lattice);
        }

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                //You can add image files to your project resources and access them like this:
                return Resource1.PolyBrickIcons_45;
                //return null;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("560e5cf9-1eb9-41b3-bcc0-be5bdc0f896d"); }
        }
    }
}