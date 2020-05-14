using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;
using PolyBrick.Params;

namespace PolyBrick
{
    public class MoveTensorField : GH_Component
    {
        public override GH_Exposure Exposure => GH_Exposure.secondary;

        /// <summary>
        /// Initializes a new instance of the MoveTensorField class.
        /// </summary>
        public MoveTensorField()
          : base("Move Tensor Field", "Move TF",
              "Move a Tensor Field.",
              "PolyBrick", "Tensor Field")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddParameter(new TensorFieldParameter(), "Tensor Field", "TF", "Tensor Field to move", GH_ParamAccess.item);
            pManager.AddVectorParameter("Motion", "T", "Translation vector", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddParameter(new TensorFieldParameter(), "Tensor Field", "TF", "Moved Tensor Field", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            TensorFieldGoo TFGoo = new TensorFieldGoo();
            DA.GetData(0, ref TFGoo);
            Vector3d v = new Vector3d();
            DA.GetData(1, ref v);

            TensorField tf = new TensorField(TFGoo.Value);
            for (int i =0;i< tf.Tensors.Count; i++)
            {
                tf.Tensors[i].plane.Origin += v;
            }
            tf.Nodes.Clear();
            tf.RTreeNodes.Clear();
            for (int i = 0; i < tf.Tensors.Count; i++)
            {
                tf.Nodes.Add(tf.Tensors[i].plane.Origin);
                tf.RTreeNodes.Insert(tf.Tensors[i].plane.Origin, i);
            }
            DA.SetData(0, new TensorFieldGoo(tf));
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
                return Resource1.PolyBrickIcons_55;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("b0a0b01a-e8df-4d81-aaf8-355c94fc18d4"); }
        }
    }
}