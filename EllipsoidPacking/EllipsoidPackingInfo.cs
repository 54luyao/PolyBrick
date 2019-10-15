using System;
using System.Drawing;
using Grasshopper.Kernel;

namespace EllipsoidPacking
{
    public class EllipsoidPackingInfo : GH_AssemblyInfo
    {
        public override string Name
        {
            get
            {
                return "EllipsoidPacking";
            }
        }
        public override Bitmap Icon
        {
            get
            {
                //Return a 24x24 pixel bitmap to represent this GHA library.
                return null;
            }
        }
        public override string Description
        {
            get
            {
                //Return a short string describing the purpose of this GHA library.
                return "";
            }
        }
        public override Guid Id
        {
            get
            {
                return new Guid("c1771d7a-bed7-4c91-8026-8ee095f44688");
            }
        }

        public override string AuthorName
        {
            get
            {
                //Return a string identifying you or your company.
                return "";
            }
        }
        public override string AuthorContact
        {
            get
            {
                //Return a string representing your preferred contact details.
                return "";
            }
        }
    }
}
