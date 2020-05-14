using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;
using Sawapansolversnet;
using MillipedeShared;
using SawapanStatRhino;
using SawapanStatica;

namespace PolyBrick.FEA
{
    public class TestFEA : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the TestFEA class.
        /// </summary>
        public TestFEA()
          : base("Test FEA", "Test FEA",
              "Test Millipede FEA engine",
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
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.Register_DoubleParam("Stresses", "S", "Max stress in each strut.", GH_ParamAccess.list);
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

            //instantiate the new truss system
            TrussSystem structure = new TrussSystem();

            //convert each line to a truss element and register it in the truss system
            //this will create nodes and truss elements connecting them
            //it will also automatically fuse coincident nodes
            foreach (Line l in trussLines)
            {
                structure.AddEdge(l.From, l.To);
            }

            //set the boundary conditions for points that came from the fixedPoints input
            foreach (Point3d pp in fixedPoints)
            {
                structure.FixPoint(pp, true, true, true);
            }

            //add a force along the YAxis to each of the nodes
            foreach (Node ni in structure.Nodes)
            {
                ni.f += Vector3d.YAxis * (-1.5);
            }

            //.............................Start solution

            double[,] k = new double[6, 6]; // local stiffness matrix will be filled by each truss element

            //instantiate the sparse linear solver
            SparseSolver solver = new SparseSolver();
            //initialize the system of equations. We have 3xnodeCount degrees of freedom or unknowns in the system
            //since each node can move in the X,Y and Z directions (3 DOFS)
            solver.Initialize(structure.Nodes.Count * 3);

            //material properties for all edges
            double elasticModulo = 1.0;
            double crossSectionArea = 1.0;

            //build the global stiffness matrix
            uint j0 = 0;
            uint i0 = 0;
            foreach (Edge ei in structure.Edges)
            {
                ei.calculateLocalStiffnessMatrix(elasticModulo, crossSectionArea, k); //get local stiffness matrix for one truss element


                //remap local degrees of freedom for edge nodes to global degrees in the stiffness matrix
                for (uint j = 0; j < 6; ++j)
                {
                    if (j < 3) j0 = ei.n0.id * 3 + j;
                    else j0 = ei.n1.id * 3 + j - 3;

                    for (uint i = j; i < 6; ++i)
                    {
                        if (i < 3) i0 = ei.n0.id * 3 + i;
                        else i0 = ei.n1.id * 3 + i - 3;

                        //add an entry into the global matrix at global [j0,i0]
                        solver.AddValue(j0, i0, k[j, i]);
                    }
                }
            }

            //add loads (this is the right hand side vector of the system of equations)
            foreach (Node ni in structure.Nodes)
            {
                solver.B[ni.id * 3] = ni.f.X;
                solver.B[ni.id * 3 + 1] = ni.f.Y;
                solver.B[ni.id * 3 + 2] = ni.f.Z;
            }
            //solver.CommitB();

            //add essential boundary conditions (this will actually knock out rows and columns of the global stiffness matrix
            //because when we say that we fix the node not to move it means that the three aunkowns (the displacements of the node)
            //are known and equal to zero
            foreach (Node ni in structure.Nodes)
            {

                if (ni.fixX)
                {
                    solver.LockVariableValue(ni.id * 3, 0.0);
                }
                if (ni.fixY)
                {
                    solver.LockVariableValue(ni.id * 3 + 1, 0.0);
                }
                if (ni.fixZ)
                {
                    solver.LockVariableValue(ni.id * 3 + 2, 0.0);
                }

            }

            //call the solver to solve the system of equations
            //this solver is based on intel's MKL which has two different solvers,
            //ConjugateGradient and PARDISO
            //The solver will solve the system  K.X=B
            //K is the global stiffness matrix
            //B is the loads vector (known since we applied the forces)
            //X are the unknown displacements
            //This is basically Hook's law    Stiffness*Displacement = Force
            //but in matrix form
            solver.BuildSparseMatrix();
            solver.CallPardisoSolver(false);

            //ouput the solution vector X as a list of displacement vectors one for each node
            double maxDeflection = 0.0;
            List<Vector3d> displacements = new List<Vector3d>();
            foreach (Node ni in structure.Nodes)
            {
                double dx = solver.X[ni.id * 3];
                double dy = solver.X[ni.id * 3 + 1];
                double dz = solver.X[ni.id * 3 + 2];

                ni.u = new Vector3d(dx, dy, dz);
                double deflection = ni.u.Length;
                if (maxDeflection < deflection) maxDeflection = deflection;

                displacements.Add(ni.u);
            }
            //////////MAXDEFLECTION = maxDeflection;
            //////////DISPLACEMENTS = displacements;


            //gather some data for visualization

            //output the locations of the nodes
            List<Point3d> np = new List<Point3d>();
            foreach (Node ni in structure.Nodes)
            {
                np.Add(ni.p);
            }

            //////////POINTS = np;

            //output the edge connectivity as two list of node indices so we know how nodes connect
            List<uint> sp = new List<uint>();
            List<uint> ep = new List<uint>();
            foreach (Edge ei in structure.Edges)
            {
                sp.Add(ei.n0.id);
                ep.Add(ei.n1.id);
            }

            //////////EDGE_NODE_0 = sp;
            //////////EDGE_NODE_1 = ep;
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
                return null;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("12b5c828-84b2-4628-9b28-762817762dbc"); }
        }
    }

    class TrussSystem
    {
        public List<Node> Nodes = new List<Node>();
        public List<Edge> Edges = new List<Edge>();

        public void Clear()
        {
            Nodes.Clear();
            Edges.Clear();
        }

        public Edge AddEdge(Point3d p0, Point3d p1)
        {
            Edge ei = new Edge(AddPoint(p0, 0.0001), AddPoint(p1, 0.0001));
            Edges.Add(ei);
            return ei;
        }

        public void AddLoad(Point3d p, Vector3d f)
        {
            Node nd = AddPoint(p, 0.001);
            nd.f += f;
        }

        public void FixPoint(Point3d p, bool fixx, bool fixy, bool fixz)
        {
            Node nd = AddPoint(p, 0.001);
            nd.fixX = fixx;
            nd.fixY = fixy;
            nd.fixZ = fixz;
        }

        public Node AddPoint(Point3d p, double tolerance)
        {
            Node ni = FindNode(p, tolerance);
            if (ni != null) return ni;
            ni = new Node();
            ni.p = p;

            ni.id = (uint)Nodes.Count;
            Nodes.Add(ni);

            return ni;
        }

        public Node FindNode(Point3d p, double tolerance)
        {
            double t2 = tolerance * tolerance;

            foreach (Node ni in Nodes)
            {
                Vector3d dv = ni.p - p;
                if (dv.SquareLength <= t2) return ni;
            }
            return null;
        }


    }

    class Node
    {
        public Point3d p = Point3d.Origin; //position
        public Vector3d f = Vector3d.Zero; //applied force
        public uint id = 0;                //index (just a unique serial id for each node)

        public bool fixX = false;          //boundary conditions (DOFs)
        public bool fixY = false;
        public bool fixZ = false;

        public Vector3d u = Vector3d.Zero; //displacement vector (will be computed by solver)
    }

    class Edge
    {
        public Edge(Node _n0, Node _n1)
        {
            n0 = _n0;
            n1 = _n1;
        }

        public Node n0;
        public Node n1;

        public void calculateLocalStiffnessMatrix(double E, double A, double[,] K)
        {
            Vector3d dv = n1.p - n0.p;
            double L = dv.Length;
            double EAL = E * A / L;

            double l = dv.X / L;
            double m = dv.Y / L;
            double n = dv.Z / L;

            K[0, 0] = l * l;
            K[1, 1] = m * m;
            K[2, 2] = n * n;

            K[3, 3] = l * l;
            K[4, 4] = m * m;
            K[5, 5] = n * n;

            K[0, 1] = K[1, 0] = m * l;
            K[0, 2] = K[2, 0] = n * l;
            K[0, 3] = K[3, 0] = -l * l;
            K[0, 4] = K[4, 0] = -m * l;
            K[0, 5] = K[5, 0] = -n * l;

            K[1, 2] = K[2, 1] = m * n;
            K[1, 3] = K[3, 1] = -m * l;
            K[1, 4] = K[4, 1] = -m * m;
            K[1, 5] = K[5, 1] = -m * n;

            K[2, 3] = K[3, 2] = -n * l;
            K[2, 4] = K[4, 2] = -m * n;
            K[2, 5] = K[5, 2] = -n * n;

            K[3, 4] = K[4, 3] = m * l;
            K[3, 5] = K[5, 3] = n * l;

            K[4, 5] = K[5, 4] = m * n;
        }
    }
}