using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;
using SawapanStatica;
using SawapanStatRhino;

namespace PolyBrick.FEA
{
    public class TestPanFEAComponent : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the TestPanFEAComponent class.
        /// </summary>
        public TestPanFEAComponent()
          : base("Dynamic Thickening WIP", "Dynamic Thickening WIP",
              "Description",
              "PolyBrick", "FEA")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddBooleanParameter("Reset", "R", "Reset the Finite Element model.", GH_ParamAccess.item);
            pManager.AddCurveParameter("Curve", "C", "Curves of a lattice.", GH_ParamAccess.list);
            pManager.AddBrepParameter("Load Region", "L", "Region to apply load.", GH_ParamAccess.item);
            pManager.AddVectorParameter("Load", "L", "Load to apply.", GH_ParamAccess.item);
            pManager.AddBrepParameter("Support Region", "S", "Support Region.", GH_ParamAccess.item);
            pManager.AddNumberParameter("Target Stress", "TS", "Target Stress of each beam", GH_ParamAccess.item);
            pManager.AddNumberParameter("Minimum Radius", "MinR", "Minimum radius of each strut.", GH_ParamAccess.item);
            pManager.AddNumberParameter("Maximum Radius", "MaxR", "Maximum radius of each strut.", GH_ParamAccess.item);
            pManager.AddBooleanParameter("Next", "N", "Next iteration", GH_ParamAccess.item);
            pManager.AddBrepParameter("Fix Region", "F", "Fix Region.", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.Register_DoubleParam("Stress", "S", "Stresses of the struts", GH_ParamAccess.list);
            pManager.Register_LineParam("Line", "L", "Strut lines", GH_ParamAccess.list);
            pManager.Register_LineParam("DeformedLine", "DL", "Deformed strut lines", GH_ParamAccess.list);
            pManager.Register_DoubleParam("Radius", "R", "Radius of strut", GH_ParamAccess.list);
        }

        RStatSystem FEM = new RStatSystem();
        List<double> stress = new List<double>();
        List<Line> lines = new List<Line>();
        List<Line> deformedLines = new List<Line>();
        List<double> radius = new List<double>();
        int iteration = 0;
        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            bool reset = true;
            DA.GetData(0, ref reset);
            List<Curve> curves = new List<Curve>();
            DA.GetDataList(1, curves);
            Brep loadRegion = new Brep();
            DA.GetData(2, ref loadRegion);
            Vector3d load = new Vector3d();
            DA.GetData(3, ref load);
            Brep supportRegion = new Brep();
            DA.GetData(4, ref supportRegion);
            double targetStress = 0;
            DA.GetData(5, ref targetStress);
            double minR = 0;
            DA.GetData(6, ref minR);
            double maxR = 0;
            DA.GetData(7, ref maxR);
            bool next = true;
            DA.GetData(8, ref next);
            Brep fixRegion = new Brep();
            DA.GetData(9, ref fixRegion);


            StatMaterial m = FEM.AddMaterial(MATERIALTYPES.GENERIC, "clay");

            m.Em = 250000000000;
            m.Poisson = 0.3;
            m.YieldStress = 220000000;
            m.Density = 2000;
            
            //RStatCrossSection s = FEM.AddSection(m, "sec") as RStatCrossSection;
            //set the cros section shape to solid circular, and set radii
            //s.CircSolid(1, 6);
            if (iteration == 0)
            {
                for (int i = 0; i < curves.Count; i++)
                {
                    RStatCrossSection s = FEM.AddSection(m, "sec" + i.ToString()) as RStatCrossSection;
                    s.CircSolid(1, 6);
                    Curve curve = curves[i];
                    FEM.AddBeam(curve.PointAtStart, curve.PointAtEnd, s);
                }
                //FEM.SplitAndAddCurvesAsBeams(curves, s, 0.001, 0.1);
                FEM.AddLoadOnPointsWithin(loadRegion, load);
                FEM.AddSupportOnPointsWithin(supportRegion, BOUNDARYCONDITIONS.Z);
                FEM.AddSupportOnPointsWithin(fixRegion, BOUNDARYCONDITIONS.ALL);
                FEM.DeadLoadFactor = 0.0;
                FEM.SolveSystem();
                foreach (StatBeam b in FEM.Beams)
                {
                    stress.Add(b.MaxStress);
                    lines.Add(new Line(new Point3d(b.P0.x, b.P0.y, b.P0.z), new Point3d(b.P1.x, b.P1.y, b.P1.z)));
                    deformedLines.Add(new Line(new Point3d(b.P0.x + b.Node0.u.x, b.P0.y + b.Node0.u.y, b.P0.z + b.Node0.u.z), new Point3d(b.P1.x + b.Node1.u.x, b.P1.y + b.Node1.u.y, b.P1.z + b.Node1.u.z)));
                    radius.Add(Math.Sqrt(b.CrossSection.Area / Math.PI));
                }
            }
            

            if (next && iteration!= 0)
            {
                stress=new List<double>();
                lines=new List<Line>();
                deformedLines = new List<Line>();
                radius =new List<double>();
                foreach (StatBeam b in FEM.Beams)
                {
                    if (Math.Abs(b.MaxStress) > targetStress)
                    {
                        double new_r = Math.Min(Math.Sqrt(b.CrossSection.Area * Math.Abs(b.MaxStress) / (targetStress*0.95) / Math.PI), maxR);
                        b.CrossSection.CircSolid(new_r, 6); 
                    }

                    if (Math.Abs(b.MaxStress) < 0.8*targetStress)
                    {
                        double new_r = Math.Max(Math.Sqrt(b.CrossSection.Area * Math.Abs(b.MaxStress) / (targetStress * 0.85) / Math.PI),Math.Max(minR,0.0001));
                        b.CrossSection.CircSolid(new_r, 6);
                    }

                    stress.Add(b.MaxStress);
                    lines.Add(new Line(new Point3d(b.P0.x, b.P0.y, b.P0.z), new Point3d(b.P1.x, b.P1.y, b.P1.z)));
                    deformedLines.Add(new Line(new Point3d(b.P0.x + b.Node0.u.x, b.P0.y + b.Node0.u.y, b.P0.z + b.Node0.u.z), new Point3d(b.P1.x + b.Node1.u.x, b.P1.y + b.Node1.u.y, b.P1.z + b.Node1.u.z)));
                    radius.Add(Math.Sqrt(b.CrossSection.Area / Math.PI));
                }
                FEM.SolveSystem();
            }

            iteration++;

            if (reset)
            {
                stress = new List<double>();
                lines = new List<Line>();
                deformedLines = new List<Line>();
                radius = new List<double>();
                FEM = new RStatSystem();
                iteration = 0;
            }
            DA.SetDataList(0,stress);
            DA.SetDataList(1,lines);
            DA.SetDataList(2, deformedLines);
            DA.SetDataList(3,radius);
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
            get { return new Guid("bcade5cc-1b3a-4c7e-a6bd-94f3d2055853"); }
        }
    }
}