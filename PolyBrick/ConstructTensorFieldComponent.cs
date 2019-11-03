using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;
using PolyBrick.Params;
using System.Linq;
namespace PolyBrick.EllipsoidPacking
{
    public class ConstructTensorFieldComponent : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the ConstructTensorFieldComponent class.
        /// </summary>
        public ConstructTensorFieldComponent()
          : base("Construct Tensor Field", "ConstructTF",
              "Construct Tensor Field from planes.",
              "PolyBrick", "Tensor Field")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddPlaneParameter("Plane", "P", "Input planes.", GH_ParamAccess.list);
            pManager.AddNumberParameter("XStrength", "XS", "Strengths along the X axes of the input planes. List length should equal to the length of plane list. If only one number is provided then use it for all planes", GH_ParamAccess.list,1);
            pManager.AddNumberParameter("YStrength", "YS", "Strengths along the Y axes of the input planes. List length should equal to the length of plane list. If only one number is provided then use it for all planes", GH_ParamAccess.list,1);
            pManager.AddNumberParameter("ZStrength", "ZS", "Strengths along the Z axes of the input planes. List length should equal to the length of plane list. If only one number is provided then use it for all planes", GH_ParamAccess.list,1);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.RegisterParam(new TensorFieldParameter(), "Tensor Field", "TF", "Output Tensor Field.");
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<Plane> planes = new List<Plane>();
            DA.GetDataList(0, planes);
            List<double> xs = new List<double>();
            DA.GetDataList(1, xs);
            List<double> ys = new List<double>();
            DA.GetDataList(2, ys);
            List<double> zs = new List<double>();
            DA.GetDataList(3, zs);
            if (xs.Count ==1) { xs = Enumerable.Repeat(xs[0], planes.Count).ToList(); }
            else if (xs.Count != planes.Count) { AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "X lengths list is not equal to plane list."); }
            if (ys.Count == 1) { ys = Enumerable.Repeat(ys[0], planes.Count).ToList(); }
            else if (ys.Count != planes.Count) { AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Y lengths list is not equal to plane list."); }
            if (zs.Count == 1) { zs = Enumerable.Repeat(zs[0], planes.Count).ToList(); }
            else if (zs.Count != planes.Count) { AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Z lengths list is not equal to plane list."); }
            TensorField tensorField = new TensorField(planes,xs,ys,zs);
            DA.SetData(0, new TensorFieldGoo(tensorField));
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
                return Resource1.PolyBrickIcons_52;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("21a95373-2df0-48d9-8367-9fe952179e55"); }
        }
    }
}