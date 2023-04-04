using devDept.Eyeshot.Entities;
using devDept.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;

namespace DetectFeatures
{
    public struct HoleData
    {
        public int faceIndex;
        public double holeDepth;
        public Vector3D axisofHole;
        public Point3D centerofHole;
        public int holeOnFace;
        public double angle;
        public double radius1;
        public double radius2;
    }
    public class Hole
    {
        readonly Brep model;
        readonly Adjacent adjacentobj = new Adjacent();

        List<Surface> allSurfaces = new List<Surface>();
        List<int> surfaceIndexs = new List<int>();
        List<int> holeSurfaces = new List<int>();
        List<int> nonholes = new List<int>();
        
        public List<HoleData> GroupedHoles = new List<HoleData>();
        public List<int> holeList = new List<int>();
        public Hole()
        {

        }
        public Hole(Brep brep)
        {
            Clearlists();
            model = brep;
            allSurfaces = adjacentobj.GetSurfaces(model);
            holeSurfaces = HoleTypeSurfaces();
            RemoveNonHoles(holeSurfaces);
            //GroupingHoles(holeList);
        }
        public void Clearlists()
        {
            allSurfaces.Clear();
            holeSurfaces.Clear();
            nonholes.Clear();
            GroupedHoles.Clear();
            holeList.Clear();
        }
        public List<int> HoleTypeSurfaces()
        {
            for(int i = 0; i < allSurfaces.Count; i++)
            {
                surfaceIndexs.Add(i);
                if(allSurfaces[i] is CylindricalSurface && !(allSurfaces[i] is ConicalSurface))
                {
                    holeSurfaces.Add(i);
                }
                if (allSurfaces[i] is ConicalSurface)
                {
                    holeSurfaces.Add(i);
                }
                //if(entitySurfaces[i] is TabulatedSurface)
                //{
                //    holeslist.Add(i);
                //}
            }
            return holeSurfaces;
        }
        /// <summary>
        /// only Cylinder surface which have circlular areas 
        /// with more than 225 degrees are considered as holes (some holes form pie)
        /// and those which are holes are added list of Holedata where depth, radius are of a hole are stored
        /// </summary>
        /// <param name="Holes"></param>
        public void RemoveNonHoles(List<int> Holes)
        {
            for(int i = 0; i < Holes.Count; i++)
            {
                HoleData hole1 = new HoleData
                {
                    faceIndex = Holes[i] //hole data
                };
                ICurve[] edges = allSurfaces[Holes[i]].ExtractEdges();
                Vector3D AxisofHole =new Vector3D();
                int circleCount = 0;
                int arcCount = 0;
                List<double> radiusofholes = new List<double>();
                double totalAngle = 0;
                foreach (var edge in edges)
                {
                    bool isline = edge is Line;
                    if (edge is Circle circle1 && !(edge is Arc))
                    {
                        Circle circle = circle1;
                        radiusofholes.Add(Math.Round(circle.Radius));
                        circleCount++;
                        totalAngle += 360;
                    }
                    if (edge is Arc c)
                    {
                        radiusofholes.Add(Math.Round(c.Radius));
                        double angle = c.AngleInDegrees;
                        totalAngle += c.AngleInDegrees;
                        if (angle >= 180)
                        {
                            arcCount++;
                        }
                    }
                    if (edge is Line line1)
                    {
                        hole1.holeDepth = line1.Length();//hole data
                    }
                    if (edge is EllipticalArc arc)
                    {
                        totalAngle += arc.AngleInDegrees;
                    }
                    if (edge is Curve)
                    {
                        if(edge.StartPoint == edge.EndPoint)
                        {
                            totalAngle += 360;
                        }
                    }
                }
                radiusofholes = radiusofholes.Distinct().ToList();
                hole1.angle = totalAngle / 2; //hole data
                if (circleCount == 2 || arcCount == 2 || totalAngle > 450)
                {
                    //they are considered holes
                }
                else
                {
                    nonholes.Add(Holes[i]);
                }
                // defining hole data for each hole type surface
                if (allSurfaces[Holes[i]] is CylindricalSurface cylindricalSurface1)
                {
                    AxisofHole = cylindricalSurface1.Axis;
                    hole1.axisofHole = AxisofHole;
                    cylindricalSurface1.Regen(0.1);
                    Mesh temp1 = cylindricalSurface1.ConvertToMesh();
                    AreaProperties ap = new AreaProperties(temp1.Vertices, temp1.Triangles);
                    ap.GetResults(ap.Area, ap.Centroid, out double x, out double y, out double z, out double xx, out double yy, out double zz, out double xy, out double zx, out double yz, out MomentOfInertia world, out MomentOfInertia centroid);
                    hole1.centerofHole = ap.Centroid;
                    hole1.centerofHole.X = Math.Round(hole1.centerofHole.X, 5); // hole Centeroid
                    hole1.centerofHole.Y = Math.Round(hole1.centerofHole.Y, 5);
                    hole1.centerofHole.Z = Math.Round(hole1.centerofHole.Z, 5);
                    if (radiusofholes.Count == 1)
                    {
                        hole1.radius1 = radiusofholes[0];
                    } // hole radius
                    else
                    {
                        hole1.radius1 = cylindricalSurface1.Radius;
                    }
                }
                if (allSurfaces[Holes[i]] is ConicalSurface conicallSurface1)
                {
                    AxisofHole = conicallSurface1.Axis;
                    hole1.axisofHole = AxisofHole;
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
                }
                List<int> adjFacesOfHoles = adjacentobj.GetAdjFaces(surfaceIndexs, hole1.faceIndex, allSurfaces);
                foreach(var faces in adjFacesOfHoles)
                {
                    if (model.Faces[faces].Loops.Length > 1)
                    {
                        Vector3D normal = allSurfaces[faces].ConvertToMesh().Normals[0];
                        double angle = adjacentobj.FindAngleVectors(hole1.axisofHole, normal);
                        if (angle == 0 || angle == 180)
                        {
                            hole1.holeOnFace = faces;
                        }
                    }
                }
                GroupedHoles.Add(hole1);
                if (CheckifSurfaceisOuter(AxisofHole, hole1.centerofHole, hole1.radius1, model))
                {
                    nonholes.Add(Holes[i]);
                }
            }
            //conical Holes which are not considered holes are added to chamfer and removed from holes
            for (int i = 0; i < GroupedHoles.Count; i++)
            {
                if (allSurfaces[GroupedHoles[i].faceIndex] is ConicalSurface)
                {
                    nonholes.Add(GroupedHoles[i].faceIndex);                   
                }
            }
            // removing non holes if present in holeslist
            for (int i = 0; i < GroupedHoles.Count; i++)
            {
                if (nonholes.Contains(GroupedHoles[i].faceIndex))
                {
                    GroupedHoles.Remove(GroupedHoles[i]);
                    i--;
                }
            }
            foreach (var hole in GroupedHoles)
            {
                holeList.Add(hole.faceIndex);
            }
        }

        /// <summary>
        /// To check if the area inside the surface is empty or to remove outer surfaces.
        /// this checks a point from center at distance less than radius of hole 
        /// if the point if not inside model then its a hole
        /// </summary>
        /// <param name="AxisofHole"></param>
        /// <param name="centerofHole"></param>
        /// <param name="radius1"></param>
        /// <returns> true if surface is outer surface </returns>
        public bool CheckifSurfaceisOuter(Vector3D AxisofHole, Point3D centerofHole, double radius1, Brep model)
        {
            Point3D pointfromCenter = new Point3D();
            if (AxisofHole == Vector3D.AxisZ)
            {
                pointfromCenter.X = centerofHole.X + (0.95 * radius1);
                pointfromCenter.Y = centerofHole.Y;
                pointfromCenter.Z = centerofHole.Z;
            }
            if (AxisofHole == Vector3D.AxisY)
            {
                pointfromCenter.X = centerofHole.X;
                pointfromCenter.Z = centerofHole.Z + (0.95 * radius1);
                pointfromCenter.Y = centerofHole.Y;
            }
            if (AxisofHole == Vector3D.AxisX)
            {
                pointfromCenter.Z = centerofHole.Z;
                pointfromCenter.Y = centerofHole.Y + (0.95 * radius1);
                pointfromCenter.X = centerofHole.X;

            }
            if(model.IsPointInside(pointfromCenter))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
