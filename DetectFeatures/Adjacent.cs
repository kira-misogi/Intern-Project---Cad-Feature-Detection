using devDept.Eyeshot.Entities;
using devDept.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Drawing;
using static devDept.Eyeshot.Entities.Brep;

namespace DetectFeatures
{
    public class Adjacent
    {
        public static Brep solid;
        public static int size;
        public static void Exp(Brep s)
        {
            solid = s;
            size = solid.Faces.Length;
            solid.Rebuild();
        }
        public List<Surface> GetSurfaces(Brep model)
        {
            List<Surface> entitySurfaces = new List<Surface>();
            for (int i = 0; i < model.Faces.Length; i++)
            {
                Brep.Face face = model.Faces[i];
                Surface surface = null;
                if (face.Parametric.Count() >= 1)
                {
                    surface = face.Parametric[0];
                }
                else
                {
                    continue;
                }
                surface.ColorMethod = colorMethodType.byEntity;
                surface.Color = Color.FromArgb(255, 160, 160, 160);
                surface.Regen(0.1);
                surface.EntityData = i;
                entitySurfaces.Add(surface);
            }
            return entitySurfaces;
        }
        /// <summary>
        /// gets a baseface and listoffaces to check if any of faces from listoffaces are adjacent to baseface
        /// if they are adjcaent they're added to adjacentfaces list to be returned
        /// entity_surface holds the surface data.
        /// </summary>
        /// <param name="listofFaces"></param>
        /// <param name="baseface"></param>
        /// <param name="entity_surfaces"></param>
        /// <returns></returns>
        public List<int> GetAdjFaces(List<int> listofFaces, int baseface, List<Surface> entity_surfaces)
        {
            List<int> Adjsofbaseface = new List<int>();
            for (int j = 0; j < listofFaces.Count; j++)
            {   
                if(listofFaces[j] == baseface)
                {
                    continue;
                }
                if (IsTwoFacesAdjacent(entity_surfaces[baseface], entity_surfaces[listofFaces[j]]))
                {
                    Adjsofbaseface.Add(listofFaces[j]);
                }
            }
            return Adjsofbaseface;
        }
        public List<int> GetAdjEdges(int baseEdgeIndex, Brep model)
        {
            List<int> AdjEdges = new List<int>();
            ICurve baseEdge = model.Edges[baseEdgeIndex].Curve;
            for(int i = 0; i < model.Edges.Count(); i++)
            {
                ICurve edge = model.Edges[i].Curve;
                if(baseEdgeIndex != i)
                {
                    if(IsVerticesIntersecting(edge, baseEdge))
                    {
                        AdjEdges.Add(edge.EdgeIndex);
                    }
                }
            }
            return AdjEdges;
        }
        /// <summary>
        /// checks if two surfaces are adjacent or not,
        /// if they have a common edge then they're adjacent
        /// </summary>
        /// <param name="baseFace"></param>
        /// <param name="adjFace"></param>
        /// <returns></returns>
        public bool IsTwoFacesAdjacent(Surface baseFace, Surface adjFace)
        {
            bool isIntersecting = false;
            var baseFaceEdge = baseFace.ExtractEdges();
            var adjFaceEdge = adjFace.ExtractEdges();
            for (int j = 0; j <= (adjFaceEdge.Length - 1); j++)
            {
                for (int k = 0; k <= (baseFaceEdge.Length - 1); k++)
                {
                    isIntersecting = IsEdgesIntersecting(adjFaceEdge[j], baseFaceEdge[k]);
                    if (isIntersecting)
                    {
                        break;
                    }

                }
                if (isIntersecting) { break; }
            }
            return isIntersecting;
        }
        public bool IsVerticesIntersecting(ICurve edge1, ICurve edge2)
        {
            return ((edge1.StartPoint == edge2.StartPoint && edge1.EndPoint != edge2.EndPoint) ||
                (edge1.StartPoint == edge2.EndPoint && edge1.EndPoint != edge2.StartPoint) ||
                (edge1.EndPoint == edge2.StartPoint && edge1.StartPoint != edge2.EndPoint) ||
                (edge1.EndPoint == edge2.EndPoint && edge1.StartPoint != edge2.StartPoint));
        }
        public bool IsEdgesIntersecting(ICurve baseEdge, ICurve currentEdge)
        {
            return (((baseEdge.StartPoint.X == currentEdge.StartPoint.X) && (baseEdge.StartPoint.Y == currentEdge.StartPoint.Y) && (baseEdge.StartPoint.Z == currentEdge.StartPoint.Z)
               && ((baseEdge.EndPoint.X == currentEdge.EndPoint.X) && (baseEdge.EndPoint.Y == currentEdge.EndPoint.Y) && (baseEdge.EndPoint.Z == currentEdge.EndPoint.Z)))
               || ((baseEdge.StartPoint.X == currentEdge.EndPoint.X) && (baseEdge.StartPoint.Y == currentEdge.EndPoint.Y) && (baseEdge.EndPoint.Z == currentEdge.StartPoint.Z))
               && ((baseEdge.EndPoint.X == currentEdge.StartPoint.X) && (baseEdge.EndPoint.Y == currentEdge.StartPoint.Y) && (baseEdge.EndPoint.Z == currentEdge.StartPoint.Z)));
        }
        /// <summary>
        /// Checks if two surface form concave edges, two surface which are inclined at 90 deg or less 
        /// and theres no solid in between them
        /// </summary>
        /// <param name="surface1"></param>
        /// <param name="surface2"></param>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <exception cref="NullReferenceException"></exception>
        public bool Concavity(Surface surface1, Surface surface2, Brep model)
        {
            //double angle = 0;
            Startagain:
            bool isconcave = false;
            //bool isconvex;
            surface1.Regen(0.1);
            Mesh temp1 = surface1.ConvertToMesh();
            AreaProperties ap = new AreaProperties(temp1.Vertices, temp1.Triangles);
            ap.GetResults(ap.Area, ap.Centroid, out double x, out double y, out double z, out double xx, out double yy, out double zz, out double xy, out double zx, out double yz, out MomentOfInertia world, out MomentOfInertia centroid);
            surface2.Regen(0.1);
            Mesh temp2 = surface2.ConvertToMesh();
            AreaProperties ap1 = new AreaProperties(temp2.Vertices, temp2.Triangles);
            ap.GetResults(ap1.Area, ap1.Centroid, out double x1, out double y1, out double z1, out double xx1, out double yy1, out double zz1, out double xy1, out double zx1, out double yz1, out MomentOfInertia world1, out MomentOfInertia centroid1);
            surface1.IsPlanar(0, out Plane surface1equation);
            surface2.IsPlanar(0, out Plane surface2equation);
            Point3D pointSurface1 = ap.Centroid;
            Point3D pointSurface2 = ap1.Centroid;
            if (surface1equation == null && surface2equation == null)
            {
                throw new NullReferenceException();
            }
            if(surface1equation == null)
            {
                Surface temp = surface1;
                surface1 = surface2;
                surface2 = surface1;
                goto Startagain;
            }
            if(!(surface1.Trimming.IsPointInside(ap.Centroid)))
            {
                double distancetopointonsurface = surface1.ClosestPointTo(ap.Centroid, out Point3D pointOnSurface);
                pointSurface1 = pointOnSurface;
                
            }
            if (!(surface2.Trimming.IsPointInside(ap1.Centroid)))
            {
                double distancetopointonsurface = surface2.ClosestPointTo(ap1.Centroid, out Point3D pointOnSurface);
                pointSurface2 = pointOnSurface;
            }
            Point3D pointbtw2Surfaces;
            double pointInsideModel = 0;
            for (double i = 0.1; i < 1; i += 0.2)
            {
                pointbtw2Surfaces = pointSurface1 + i*(pointSurface2 - pointSurface1);
                if((model.IsPointInside(pointbtw2Surfaces)))
                {
                    pointInsideModel++;
                }
            }
            if (pointInsideModel == 0)
            {
                double angle = 180; ;
                if (!(surface2 is PlanarSurface))
                {
                    Vector3D normal1 = surface1.ConvertToMesh().Normals[0];
                    Vector3D radialvector = new Vector3D(ap1.Centroid, pointSurface2);
                    angle = FindAngleVectors(radialvector, normal1);
                }
                else
                {
                    angle = FindAngleSurfaces(surface1, surface2);
                }
                if(angle <= 90 && angle > 0)
                {
                    isconcave = true;
                }
            }
        
            return isconcave;
        }
        
        public double FindAngleSurfaces(Surface surface1, Surface surface2)
        {
            PlanarSurface planarSurface1 = surface1 as PlanarSurface;
            var normal1 = surface1.ConvertToMesh().Normals[0];

            PlanarSurface planarSurface2 = surface2 as PlanarSurface;
            var normal2 = surface2.ConvertToMesh().Normals[0];
            //angle between normals
            
            double numerator = ((normal1.X * normal2.X) + (normal1.Y * normal2.Y) + (normal1.Z * normal2.Z));
            double denomin = Math.Sqrt(Math.Pow(normal1.X, 2) + Math.Pow(normal1.Y, 2) + Math.Pow(normal1.Z, 2)) * Math.Sqrt(Math.Pow(normal2.X, 2) + Math.Pow(normal2.Y, 2) + Math.Pow(normal2.Z, 2));
            double angle = Math.Round(Math.Acos(numerator / denomin) * (180 / Math.PI), 5);
            if(numerator > 0 && angle > 90)
            {
                angle = 180 - angle;
            }
            if(numerator < 0 && angle < 90)
            {
                angle = 180 - angle;
            }
            angle = 180 - angle;
            return angle;
        }
        
        public double FindAngleVectors(Vector3D vector1, Vector3D vector2)
        {
            double numerator1 = (vector1.X * vector2.X) + (vector1.Y * vector2.Y) + (vector1.Z * vector2.Z);
            double numerator = Math.Abs((vector1.X * vector2.X) + (vector1.Y * vector2.Y) + (vector1.Z * vector2.Z));
            double denomin = Math.Sqrt(Math.Pow(vector1.X, 2) + Math.Pow(vector1.Y, 2) + Math.Pow(vector1.Z, 2)) * Math.Sqrt(Math.Pow(vector2.X, 2) + Math.Pow(vector2.Y, 2) + Math.Pow(vector2.Z, 2));
            double angle = Math.Round(Math.Acos(numerator / denomin) * (180 / Math.PI), 5);

            return angle;
        }
        /// <summary>
        /// checks if a base surface and a selected surface form depression or portrusion types feature
        /// </summary>
        /// <param name="surface1"></param>
        /// <param name="surface2"></param>
        /// <returns> true if it form depression type feature, its potrusion if its false </returns>
        public bool CheckForDepression(Surface surface1, Surface surface2)
        {
            bool isdepression = false;
            surface1.Regen(0.1);
            Mesh temp1 = surface1.ConvertToMesh();
            AreaProperties surface1AreaProp = new AreaProperties(temp1.Vertices, temp1.Triangles);
            surface1AreaProp.GetResults(surface1AreaProp.Area, surface1AreaProp.Centroid, out double x, out double y, out double z, out double xx, out double yy, out double zz, out double xy, out double zx, out double yz, out MomentOfInertia world, out MomentOfInertia centroid);
            surface2.Regen(0.1);
            Mesh temp2 = surface2.ConvertToMesh();
            AreaProperties surface2AreaProp = new AreaProperties(temp2.Vertices, temp2.Triangles);
            surface2AreaProp.GetResults(surface2AreaProp.Area, surface1AreaProp.Centroid, out double x1, out double y1, out double z1, out double xx1, out double yy1, out double zz1, out double xy1, out double zx1, out double yz1, out MomentOfInertia world1, out MomentOfInertia centroid1);
            surface1.IsPlanar(0, out Plane surface1equation);
            surface2.IsPlanar(0, out Plane surface2equation);
            if (Math.Round(surface1equation.Equation.X) == 1 || Math.Round(surface1equation.Equation.X) == -1)
            {
                if (Math.Round(surface1equation.Equation.X) == -1)
                {
                    if (surface1AreaProp.Centroid.X - surface2AreaProp.Centroid.X < 0)
                    {
                        isdepression = true;
                        goto Next;
                    }
                }
                if (surface1AreaProp.Centroid.X - surface2AreaProp.Centroid.X > 0)
                {
                    isdepression = true;
                    goto Next;
                }
            }
            if (Math.Round(surface1equation.Equation.Y) == 1 || Math.Round(surface1equation.Equation.Y) == -1)
            {
                if (Math.Round(surface1equation.Equation.Y) == -1)
                {
                    if (surface1AreaProp.Centroid.Y - surface2AreaProp.Centroid.Y < 0)
                    {
                        isdepression = true;
                        goto Next;
                    }
                }
                if (surface1AreaProp.Centroid.Y - surface2AreaProp.Centroid.Y > 0)
                {
                    isdepression = true;
                    goto Next;
                }
            }
            if (Math.Round(surface1equation.Equation.Z) == 1 || Math.Round(surface1equation.Equation.Z) == -1)
            {
                if (Math.Round(surface1equation.Equation.Z) == -1)
                {
                    if (surface1AreaProp.Centroid.Z - surface2AreaProp.Centroid.Z < 0)
                    {
                        isdepression = true;
                        goto Next;
                    }
                }
                if (surface1AreaProp.Centroid.Z - surface2AreaProp.Centroid.Z > 0)
                {
                    isdepression = true;
                    goto Next;
                }
            }
        Next:;
            return isdepression;
        }
    }
}
