﻿using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;

namespace PolyBrick.Params
{
    public class TempEvaluateTensorField : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the TempEvaluateTensorField class.
        /// </summary>
        public TempEvaluateTensorField()
          : base("Evaluate Tensor Field", "EvaTF",
              "Evaluate the tensor value.",
              "PolyBrick", "Analysis")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddParameter(new TensorFieldParameter(), "TensorField", "TF", "Base tensor field.", GH_ParamAccess.item);
            pManager.AddPointParameter("Point", "P", "Point to evaluate.",GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.Register_VectorParam("Orienvation", "T", "Orientation of the major axis.", GH_ParamAccess.item);
            pManager.Register_DoubleParam("MajorFactor", "MaxF", "Size factor of the major axis.", GH_ParamAccess.item);
            pManager.Register_DoubleParam("MinorFactor", "MinF", "Size factor of the minor axis.", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            TensorFieldGoo tensorFieldGoo = new TensorFieldGoo();
            Point3d point = new Point3d();
            if (!DA.GetData(0, ref tensorFieldGoo)) return;
            if (!DA.GetData(1, ref point)) return;
            Vector3d orientation = new Vector3d(0,0,0);
            double majorFactor = 0;
            double minorFactor = 0;
            tensorFieldGoo.Value.GetOrientation(point, ref orientation, ref majorFactor, ref minorFactor);
            DA.SetData(0, orientation);
            DA.SetData(1, majorFactor);
            DA.SetData(2, minorFactor);
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
                return Resource1.PolyBrickIcons_43;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("6c92af96-d843-4ec9-b4d9-3b0ae2984da3"); }
        }
    }
}