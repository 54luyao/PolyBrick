using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;
using SharpFE;
using PolyBrick;
using PolyBrick.EllipsoidPacking;
using Rhino.Collections;
using Rhino;

namespace PolyBrick.FEA
{
    public class testSharpFEComponent : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the testSharpFEComponent class.
        /// </summary>
        public testSharpFEComponent()
          : base("Test SharpFE Component", "Test SharpFE",
              "Test SharpFE engine",
              "PolyBrick", "FEA")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddLineParameter("Lines", "L", "Lines of truss.", GH_ParamAccess.list);
            pManager.AddPointParameter("Point", "P", "Fixed points.", GH_ParamAccess.list);
            pManager.AddVectorParameter("Load", "L", "External load.", GH_ParamAccess.item);
            pManager.AddBrepParameter("Loading Region", "LR", "The region that load is applied to.", GH_ParamAccess.item);
            pManager.AddBrepParameter("Support Region", "SR", "The region that nodes inside are fixed.", GH_ParamAccess.item);
            //pManager.AddGenericParameter("Material", "M", "Material.", GH_ParamAccess.list);
            //pManager[5].Optional = true;
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.Register_PointParam("Points", "P", "Displaced points.", GH_ParamAccess.list);
            pManager.Register_LineParam("Lines", "L", "Deformed Lines.", GH_ParamAccess.list);
            pManager.Register_DoubleParam("Radius", "R", "Radius for each strut.", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<Line> trussLines = new List<Line>();
            DA.GetDataList(0, trussLines);
            List<Point3d> fixedPoints = new List<Point3d>();
            DA.GetDataList(1, fixedPoints);
            Vector3d load = new Vector3d();
            DA.GetData(2, ref load);
            Brep loadingRegion = new Brep();
            DA.GetData(3, ref loadingRegion);
            Brep supportRegion = new Brep();
            DA.GetData(4, ref supportRegion);
            
            List<Point3d>  nodePoints = new List<Point3d>();
            for (int i = 0; i < trussLines.Count; i++)
            {
                nodePoints.Add(trussLines[i].From);
                nodePoints.Add(trussLines[i].To);
            }
            RemoveDuplicatePoints(nodePoints);

            List<Point3d> loadPoints = new List<Point3d>();
            foreach(Point3d p in nodePoints)
            {
                if (loadingRegion.IsPointInside(p, RhinoMath.SqrtEpsilon,false))
                {
                    loadPoints.Add(p);
                }
            }

            Vector3d loadPerNode = load / loadPoints.Count;

            FiniteElementModel model = new FiniteElementModel(ModelType.Truss3D);
            IMaterial material = new GenericElasticMaterial(0, 1200000, 0, 0);
            ICrossSection section1 = new SolidRectangle(1, 1);
            ForceVector externalForce = model.ForceFactory.Create(loadPerNode.X, loadPerNode.Y, loadPerNode.Z, 0, 0, 0);
            List<FiniteElementNode> FENodes = new List<FiniteElementNode>();
            List<LinearTruss> FETruss = new List<LinearTruss>();
            List<int> elementEndIndices = new List<int>();

            for (int i = 0; i < trussLines.Count; i++)
            {
                Line l = trussLines[i];
                FiniteElementNode n1, n2;
                try
                {
                    n1 = (FiniteElementNode)model.FindNodeNearTo(l.FromX, l.FromY, l.FromZ, 0.001);
                }catch (System.InvalidOperationException) {
                    n1 = model.NodeFactory.Create(l.FromX, l.FromY, l.FromZ);
                    FENodes.Add(n1);
                    if(supportRegion.IsPointInside(l.From, RhinoMath.SqrtEpsilon, false))
                    {
                        model.ConstrainNode(n1, DegreeOfFreedom.X);
                        model.ConstrainNode(n1, DegreeOfFreedom.Y);
                        model.ConstrainNode(n1, DegreeOfFreedom.Z);
                    }
                    else
                    {
                        model.ConstrainNode(n1, DegreeOfFreedom.Y);
                    }
                    if (loadingRegion.IsPointInside(l.From, RhinoMath.SqrtEpsilon, false))
                    {
                        model.ApplyForceToNode(externalForce, n1);
                    }
                }
                try
                {
                    n2 = (FiniteElementNode)model.FindNodeNearTo(l.ToX, l.ToY, l.ToZ, 0.001);
                }catch (System.InvalidOperationException)
                {
                    n2 = model.NodeFactory.Create(l.ToX, l.ToY, l.ToZ);
                    FENodes.Add(n2);
                    if (supportRegion.IsPointInside(l.To, RhinoMath.SqrtEpsilon, false))
                    {
                        model.ConstrainNode(n2, DegreeOfFreedom.X);
                        model.ConstrainNode(n2, DegreeOfFreedom.Y);
                        model.ConstrainNode(n2, DegreeOfFreedom.Z);
                    }
                    else
                    {
                        model.ConstrainNode(n2, DegreeOfFreedom.Y);
                    }
                    if (loadingRegion.IsPointInside(l.To, RhinoMath.SqrtEpsilon, false))
                    {
                        model.ApplyForceToNode(externalForce, n2);
                    }
                }
                FETruss.Add(model.ElementFactory.CreateLinearTruss(n1, n2, material, section1));
                elementEndIndices.Add(FENodes.IndexOf(n1));
                elementEndIndices.Add(FENodes.IndexOf(n2));
            }
            IFiniteElementSolver solver = new MatrixInversionLinearSolver(model);
            FiniteElementResults results = solver.Solve();

            List<Point3d> outPoints = new List<Point3d>();
            foreach(FiniteElementNode node in FENodes)
            {
                DisplacementVector dis = results.GetDisplacement(node);
                outPoints.Add(new Point3d(node.X + dis.X, node.Y + dis.Y, node.Z + dis.Z));
            }

            List<Line> outLines = new List<Line>();
            for (int i = 0; i < FETruss.Count; i++)
            {
                outLines.Add(new Line(outPoints[elementEndIndices[2 * i]], outPoints[elementEndIndices[2 * i + 1]]));
            }
            DA.SetData(0, outPoints);
            DA.SetData(1, outLines);
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
                return Resource1.PolyBrickIcons_53;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("d08e2905-5b5b-4927-af30-8eb1b7af79d0"); }
        }

        private static void RemoveDuplicatePoints(List<Point3d> points)
        {
            List<int> indices = new List<int>();
            for (int i = 0; i < points.Count; i++)
            {
                Point3d pi = points[i];
                for (int j = i + 1; j < points.Count; j++)
                {
                    Point3d pj = points[j];
                    if (pi.DistanceTo(pj) < RhinoMath.SqrtEpsilon)
                    {
                        if(!indices.Contains(j))indices.Add(j);
                    }
                }
            }
            indices.Sort();
            indices.Reverse();
            foreach (int i in indices)
            {
                points.RemoveAt(i);
            }
        }
    }
}