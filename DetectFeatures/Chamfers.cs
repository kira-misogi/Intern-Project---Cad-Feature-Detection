using devDept.Eyeshot.Entities;
using devDept.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;

namespace DetectFeatures
{
    public struct ChamferData
    {
        public int index;
        public double angle;
        public double length;
    }
    public class Chamfer
    {
        readonly Brep model;
        Adjacent adjacentobj = new Adjacent();

        List<Surface> allSurfaces = new List<Surface>();
        List<int> surfacesIndexList = new List<int>();
        List<int> planarSurfaces = new List<int>();
        List<int> chamferSurfaces = new List<int>();
        List<int> horizontalChamfer = new List<int>();
        List<int> verticalChamfer = new List<int>();
        
        public List<ChamferData> GroupedChamfers = new List<ChamferData>();
        public List<int> chamferList = new List<int>();
        public Chamfer()
        {

        }
        public Chamfer(Brep brep)
        {
            Clearlists();
            model = brep;
            allSurfaces = adjacentobj.GetSurfaces(model);
            chamferSurfaces = ChamferTypeSurfaces();
            chamferList = RemoveNonchamfers(chamferSurfaces);
            AddChamfers();
        }
        public void Clearlists()
        {
            chamferSurfaces.Clear();
            planarSurfaces.Clear();
            horizontalChamfer.Clear();
            verticalChamfer.Clear();
            chamferList.Clear();
        }

        /// <summary>
        /// gets planar surfaces from all surfaces
        /// </summary>
        /// <param name="entitySurfaces"></param>
        public List<int> ChamferTypeSurfaces()
        {
            for (int i = 0; i < allSurfaces.Count; i++)
            {
                if (allSurfaces[i] is PlanarSurface)
                {
                    chamferSurfaces.Add(i);
                    planarSurfaces.Add(i);
                }                
            }
            return chamferSurfaces;
        }
        /// <summary>
        /// Removing all surface which do not satisfy conditions of a chamfer
        /// </summary>
        /// <param name="Chamfers"></param>
        public List<int> RemoveNonchamfers(List<int> Chamfers)
        {
            //removing planar surfaces which are inclined at 90 to XY, YZ && XZ planes
            for (int i = 0; i < Chamfers.Count; i++)
            {
                double inclinedangle = FindInclination(allSurfaces[Chamfers[i]]);
                if (inclinedangle % 90 == 0)
                {
                    Chamfers.Remove(Chamfers[i]);
                    i--;
                    continue;
                }
            }
            for (int i = 0; i < Chamfers.Count; i++)
            {
                List<int> adjfaces = new List<int>();
                adjfaces = adjacentobj.GetAdjFaces(planarSurfaces, Chamfers[i], allSurfaces);
                List<double> adjfaceangles = new List<double>();
                List<int> facesmakingchamfer = new List<int>();
                foreach (var surface in adjfaces)
                {
                    double angle = adjacentobj.FindAngleSurfaces(allSurfaces[Chamfers[i]], allSurfaces[surface]);
                    adjfaceangles.Add(angle);
                    
                }
                if (adjfaceangles.Count(s => s <= 110) > 2)
                {
                    Chamfers.Remove(Chamfers[i]);
                    i--;
                }
                
            }
            //grouping chamfer
            return Chamfers;           
        }
        /// <summary>
        /// chamfer made on concial hole ends and elliptical holes end are found and 
        /// added to chamfer list
        /// </summary>
        public void AddChamfers()
        {
            Hole holeobj = new Hole();
            for (int i = 0; i < allSurfaces.Count; i++)
            {
                if (allSurfaces[i] is Surface && !(allSurfaces[i] is CylindricalSurface) && !(allSurfaces[i] is ToroidalSurface) && 
                    !(allSurfaces[i] is PlanarSurface) && !(allSurfaces[i] is SphericalSurface) && !(allSurfaces[i] is TabulatedSurface))
                {
                    List<int> adjfacesofFillet = adjacentobj.GetAdjFaces(surfacesIndexList, i, allSurfaces);
                    ICurve[] edges = allSurfaces[i].ExtractEdges();
                    bool iselliptical = false;
                    for (int j = 0; j < adjfacesofFillet.Count; j++)
                    {
                        if (allSurfaces[adjfacesofFillet[j]] is TabulatedSurface)
                        {
                            iselliptical = true;
                        }
                    }
                    if (iselliptical)
                    {
                        foreach (var curve in edges)
                        {
                            if (curve is Line)
                            {
                                chamferList.Add(i);
                            }
                        }
                    }
                }
                if (allSurfaces[i] is ConicalSurface conicallSurface1)
                {
                    HoleData hole1 = new HoleData();
                    hole1.faceIndex = i; //conical hole center

                    Vector3D AxisofHole = conicallSurface1.Axis;
                    List<double> radiusofholes = new List<double>();
                    ICurve[] edges = allSurfaces[i].ExtractEdges();
                    foreach (var curve in edges)
                    {
                        if (curve is Line line1)
                        {
                            hole1.holeDepth = line1.Length();//conical hole data
                        }
                    }
                    double breplength = model.BoxMax.Z - model.BoxMin.Z;

                    conicallSurface1.Regen(0.1);
                    Mesh temp1 = conicallSurface1.ConvertToMesh();
                    AreaProperties ap = new AreaProperties(temp1.Vertices, temp1.Triangles);
                    ap.GetResults(ap.Area, ap.Centroid, out double x, out double y, out double z, out double xx, out double yy, out double zz, out double xy, out double zx, out double yz, out MomentOfInertia world, out MomentOfInertia centroid);

                    hole1.centerofHole = ap.Centroid;
                    hole1.centerofHole.X = Math.Round(hole1.centerofHole.X, 5); //conical hole center
                    hole1.centerofHole.Y = Math.Round(hole1.centerofHole.Y, 5);
                    hole1.centerofHole.Z = Math.Round(hole1.centerofHole.Z, 5);
                    if (radiusofholes.Count == 2)
                    {
                        hole1.radius1 = radiusofholes[0];
                        hole1.radius2 = radiusofholes[1];
                    } // conical hole radius
                    else
                    {
                        hole1.radius1 = conicallSurface1.Radius;
                    }
                    if (!(holeobj.CheckifSurfaceisOuter(AxisofHole, hole1.centerofHole, hole1.radius1, model)))
                    {
                        if (hole1.holeDepth < breplength / 5)
                        {
                            chamferList.Add(i);
                        }
                    }
                }
            }
        }
        /// <summary>
        /// FInds plane which are inclined at a degree to XY, YZ or XZ plane but not perpendicular
        /// </summary>
        /// <param name="surf1"></param>
        /// <returns> inclination angle in degrees </returns>
        public double FindInclination(Surface surface)
        {
            PlanarSurface planarSurface = surface as PlanarSurface;
            Plane plane1 = planarSurface.Plane;
            PlaneEquation planeEquation1 = plane1.Equation;

            PlaneEquation planeEquation2;
            if (planeEquation1.Z == 0 && planeEquation1.Y != 0)
            {
                Plane plane2 = Plane.XZ;
                planeEquation2 = plane2.Equation;
            }
            else
            {
                Plane plane2 = Plane.XY;
                planeEquation2 = plane2.Equation;
            }

            double numerator = Math.Abs((planeEquation1.X * planeEquation2.X) + (planeEquation1.Y * planeEquation2.Y) + (planeEquation1.Z * planeEquation2.Z));
            double denomin = Math.Sqrt(Math.Pow(planeEquation1.X, 2) + Math.Pow(planeEquation1.Y, 2) + Math.Pow(planeEquation1.Z, 2)) * Math.Sqrt(Math.Pow(planeEquation2.X, 2) + Math.Pow(planeEquation2.Y, 2) + Math.Pow(planeEquation2.Z, 2));
            double inclinationangle = Math.Round(Math.Acos(numerator / denomin) * (180 / Math.PI), 5);
            return inclinationangle;
        }
        /// <summary>
        /// Finds angle between two planar surfaces
        /// </summary>
        /// <param name="surf1"></param>
        /// <param name="surf2"></param>
        /// <returns> angle in degrees </returns>
        
    }
}
