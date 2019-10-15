using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rhino;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using Microsoft.VisualBasic.FileIO;
using System.Globalization;
using Grasshopper.Kernel;
using System.Drawing;
using Rhino.Collections;

namespace PolyBrick.Params
{
    public class Tensor
    {
        private double magnitude_x;
        private double magnitude_y;
        private double magnitude_z;
        private double rotation_x;
        private double rotation_y;
        private double rotation_z;
        private Point3d location;

        public double Magnitude_X
        {
            get { return magnitude_x; }
            set { magnitude_x = value; }
        }

        public double Magnitude_Y
        {
            get { return magnitude_y; }
            set { magnitude_y = value; }
        }

        public double Magnitude_Z
        {
            get { return magnitude_z; }
            set { magnitude_z = value; }
        }

        public double Rotation_YZ
        {
            //In radians
            get { return rotation_x; }
            set { rotation_x = value; }
        }

        public double Rotation_ZX
        {
            //In radians
            get { return rotation_y; }
            set { rotation_y = value; }
        }

        public double Rotation_XY
        {
            //In radians
            get { return rotation_z; }
            set { rotation_z = value; }
        }

        public Point3d Location
        {
            get { return location; }
            set { location = value; }
        }

        public Tensor()
        {
            magnitude_x = 0;
            magnitude_y = 0;
            magnitude_z = 0;
            rotation_x = 0;
            rotation_y = 0;
            rotation_z = 0;
            location = new Point3d(0, 0, 0);
        }
        public Tensor(double mx, double my, double mz, double rx, double ry, double rz, double x, double y, double z)
        {
            magnitude_x = mx;
            magnitude_y = my;
            magnitude_z = mz;
            rotation_x = rx;
            rotation_y = ry;
            rotation_z = rz;
            location = new Point3d(x, y, z);
        }
    }

    public class TensorField
    {
        public readonly List<Tensor> Tensors;
        public readonly Point3dList Nodes;
        public readonly double MaxStress;
        public readonly double MinStress;

        public TensorField()
        {
            Tensors = new List<Tensor>();
        }

        public TensorField(string path)
        {
            Tensors = new List<Tensor>();
            MaxStress = Double.MinValue;
            MinStress = Double.MaxValue;
            List<Point3d> tensorLocations = new List<Point3d>();
            TextFieldParser parser = new TextFieldParser(path);
            parser.TextFieldType = FieldType.Delimited;
            parser.SetDelimiters(",");
            parser.ReadLine(); //Skip the header line
            while (!parser.EndOfData)
            {
                string[] fields = parser.ReadFields();
                Tensor tensor = new Tensor();
                tensor.Location = new Point3d(Double.Parse(fields[3], NumberStyles.Float), Double.Parse(fields[1], NumberStyles.Float), Double.Parse(fields[2], NumberStyles.Float));
                tensorLocations.Add(tensor.Location);
                tensor.Magnitude_X = Double.Parse(fields[4], NumberStyles.Float);
                MaxStress = Math.Max(MaxStress, Math.Abs(tensor.Magnitude_X));
                MinStress = Math.Min(MinStress, Math.Abs(tensor.Magnitude_X));
                tensor.Magnitude_Y = Double.Parse(fields[5], NumberStyles.Float);
                MaxStress = Math.Max(MaxStress, Math.Abs(tensor.Magnitude_Y));
                MinStress = Math.Min(MinStress, Math.Abs(tensor.Magnitude_Y));
                tensor.Magnitude_Z = Double.Parse(fields[6], NumberStyles.Float);
                MaxStress = Math.Max(MaxStress, Math.Abs(tensor.Magnitude_Z));
                MinStress = Math.Min(MinStress, Math.Abs(tensor.Magnitude_Z));
                tensor.Rotation_XY = Double.Parse(fields[7], NumberStyles.Float) / 180 * Math.PI;
                tensor.Rotation_YZ = Double.Parse(fields[8], NumberStyles.Float) / 180 * Math.PI;
                tensor.Rotation_ZX = Double.Parse(fields[9], NumberStyles.Float) / 180 * Math.PI;
                this.Tensors.Add(tensor);
            }
            Nodes = new Point3dList(tensorLocations);
        }

        public TensorField(TensorField tensorField)
        {
            Tensors = new List<Tensor>(tensorField.Tensors);
            Nodes = new Point3dList(tensorField.Nodes);
            MaxStress = tensorField.MaxStress;
            MinStress = tensorField.MinStress;
        }

        private int GetClosestIndex(Point3d testPoint)
        {
            return Nodes.ClosestIndex(testPoint);
        }

        public void GetOrientation(Point3d testPoint, ref Vector3d orientation, ref double majorFactor, ref double minorFactor)
        {
            //TODO:
            int closestIndex = GetClosestIndex(testPoint);
            Tensor tensor = Tensors[closestIndex];
            double[] stresses = new double[] { Math.Abs(tensor.Magnitude_X), Math.Abs(tensor.Magnitude_Y), Math.Abs(tensor.Magnitude_Z) };
            int[] indices = new int[] { 0, 1, 2 };
            Array.Sort(stresses, indices);
            if (MaxStress == MinStress)
            {
                majorFactor = 1;
                minorFactor = 1;
            }
            else
            {
                majorFactor = (stresses[2] - MinStress) / (MaxStress - MinStress);
                minorFactor = (stresses[1] - MinStress) / (MaxStress - MinStress);
            }
            int orientationIndex = indices[2];
            Plane rotationPlane = new Plane(Plane.WorldXY);
            rotationPlane.Rotate(tensor.Rotation_XY, rotationPlane.XAxis);
            rotationPlane.Rotate(tensor.Rotation_YZ, rotationPlane.YAxis);
            rotationPlane.Rotate(tensor.Rotation_ZX, rotationPlane.ZAxis);
            switch (orientationIndex) //Check if the two corrdinate system is the same.
            {
                case 0:
                    orientation = rotationPlane.YAxis;
                    break;
                case 1:
                    orientation = rotationPlane.ZAxis;
                    break;
                case 2:
                    orientation = rotationPlane.XAxis;
                    break;
                default:
                    throw new Exception("Cannot find orientation.");
            }
        }
    }

    public class TensorFieldGoo : GH_Goo<TensorField>
    {
        public TensorFieldGoo()
        {
            Value = new TensorField();
        }

        public TensorFieldGoo(String path)
        {
            Value = new TensorField(path);
        }

        public TensorFieldGoo(TensorField tensorField)
        {
            Value = new TensorField(tensorField);
        }

        public override bool IsValid
        {
            get { return true; }
        }

        public override IGH_Goo Duplicate()
        {
            return DuplicateTensorField();
        }

        public TensorFieldGoo DuplicateTensorField()
        {
            return new TensorFieldGoo(Value);
        }

        public override string TypeName => "TensorField";

        public override string ToString()
        {
            return "PolyBrick Tensor Field";
        }

        public override string TypeDescription => "Defines a tensor field that each unit contains triplet of vectors.";
    }

    public class TensorFieldParameter : GH_Param<TensorFieldGoo>
    {
        public TensorFieldParameter()
            : base(new GH_InstanceDescription("Tensor Field", "TF", "Contains triples of vectors.", "PolyBrick", "Parameters"))
        {
        }

        protected override Bitmap Icon
        {
            get { return Resource1.PolyBrickIcons_41; }
        }

        public override Guid ComponentGuid
        {
            get { return new Guid("045bf2e4-4f1b-40b8-80f5-729b539f7b48"); }
        }

        //protected override GH_GetterResult Prompt_Plural(ref List<TensorFieldGoo> values)
        //{
        //    return GH_GetterResult.cancel;
        //}

        //protected override GH_GetterResult Prompt_Singular(ref TensorFieldGoo value)
        //{
        //    return GH_GetterResult.cancel;
        //}
    }

    public class TensorFieldComponent : GH_Component
    {
        public TensorFieldComponent()
            : base("Tensor Field From CSV", "TFFromCSV", "Create a tensor field from csv file.", "PolyBrick", "Input") { }

        protected override Bitmap Icon
        {
            get { return Resource1.PolyBrickIcons_40; }
        }

        public override Guid ComponentGuid
        {
            get { return new Guid("21ec0fb1-f776-4db8-bfc0-d0b45c3696a7"); }
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Path", "P", "Path of the CSV file", GH_ParamAccess.item);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.RegisterParam(new TensorFieldParameter(), "Tensor Field", "TF", "Tensor Field created from CSV.", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            string path = "";
            if (!DA.GetData(0, ref path)) { return; }
            TensorFieldGoo tensorField = new TensorFieldGoo(path);
            DA.SetData(0, tensorField);
        }
    }
}
