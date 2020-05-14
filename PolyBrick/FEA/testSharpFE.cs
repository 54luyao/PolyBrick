using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpFE;

namespace PolyBrick.FEA
{
    class testSharpFE
    {
        public void Calculate3DTrussOf3BarsAnd12Dof()
        {
            FiniteElementModel model = new FiniteElementModel(ModelType.Truss3D);

            FiniteElementNode node1 = model.NodeFactory.Create(72, 0, 0);
            model.ConstrainNode(node1, DegreeOfFreedom.Y);

            FiniteElementNode node2 = model.NodeFactory.Create(0, 36, 0);
            model.ConstrainNode(node2, DegreeOfFreedom.X);
            model.ConstrainNode(node2, DegreeOfFreedom.Y);
            model.ConstrainNode(node2, DegreeOfFreedom.Z);

            FiniteElementNode node3 = model.NodeFactory.Create(0, 36, 72);
            model.ConstrainNode(node3, DegreeOfFreedom.X);
            model.ConstrainNode(node3, DegreeOfFreedom.Y);
            model.ConstrainNode(node3, DegreeOfFreedom.Z);

            FiniteElementNode node4 = model.NodeFactory.Create(0, 0, -48);
            model.ConstrainNode(node4, DegreeOfFreedom.X);
            model.ConstrainNode(node4, DegreeOfFreedom.Y);
            model.ConstrainNode(node4, DegreeOfFreedom.Z);

            IMaterial material = new GenericElasticMaterial(0, 1200000, 0, 0);
            ICrossSection section1 = new SolidRectangle(1, 0.302);
            ICrossSection section2 = new SolidRectangle(1, 0.729); ///NOTE example also refers to this as A1.  Assume errata in book
			ICrossSection section3 = new SolidRectangle(1, 0.187); ///NOTE example also refers to this as A1.  Assume errata in book

            model.ElementFactory.CreateLinearTruss(node1, node2, material, section1);
            model.ElementFactory.CreateLinearTruss(node1, node3, material, section2);
            model.ElementFactory.CreateLinearTruss(node1, node4, material, section3);

            ForceVector externalForce = model.ForceFactory.Create(0, 0, -1000, 0, 0, 0);
            model.ApplyForceToNode(externalForce, node1);

            IFiniteElementSolver solver = new MatrixInversionLinearSolver(model);
            FiniteElementResults results = solver.Solve();

            ReactionVector reactionAtNode1 = results.GetReaction(node1);
            //Assert.AreEqual(0, reactionAtNode1.X, 1);
            //Assert.AreEqual(-223.1632, reactionAtNode1.Y, 1);
            //Assert.AreEqual(0, reactionAtNode1.Z, 1);

            ReactionVector reactionAtNode2 = results.GetReaction(node2);
            //Assert.AreEqual(256.1226, reactionAtNode2.X, 1);
            //Assert.AreEqual(-128.0613, reactionAtNode2.Y, 1);
            //Assert.AreEqual(0, reactionAtNode2.Z, 1);

            ReactionVector reactionAtNode3 = results.GetReaction(node3);
            //Assert.AreEqual(-702.4491, reactionAtNode3.X, 1);
            //Assert.AreEqual(351.2245, reactionAtNode3.Y, 1);
            //Assert.AreEqual(702.4491, reactionAtNode3.Z, 1);

            ReactionVector reactionAtNode4 = results.GetReaction(node4);
            //Assert.AreEqual(446.3264, reactionAtNode4.X, 1);
            //Assert.AreEqual(0, reactionAtNode4.Y, 1);
            //Assert.AreEqual(297.5509, reactionAtNode4.Z, 1);

            DisplacementVector displacementAtNode1 = results.GetDisplacement(node1);
            //Assert.AreEqual(-0.0711, displacementAtNode1.X, 0.0001);
            //Assert.AreEqual(0, displacementAtNode1.Y, 0.0001);
            //Assert.AreEqual(-0.2662, displacementAtNode1.Z, 0.0001);
        }
    }
}
