using devDept.Eyeshot.Entities;
using devDept.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using Line = devDept.Eyeshot.Entities.Line;

namespace DetectFeatures
{
    public struct FilletData
    {
        public int index;
        public double radius;
        public ICurve arcShape;
        public double filletlength;
    }

    public class Fillet
    {
        readonly Brep model;
        Adjacent adjacentobj = new Adjacent();
        Chamfer chamferobj;

        List<Surface> allSurfaces = new List<Surface>();
        List<int> surfacesIndexList = new List<int>();
        List<int> filletSurfaces = new List<int>();
        List<int> horizontalFillets = new List<int>();
        List<int> verticalFillets = new List<int>();
        public List<FilletData> GroupedFillets = new List<FilletData>();
        public List<int> filletList = new List<int>();
        public Fillet()
        {

        }
        public Fillet(Brep brep)
        {
            Clearlists();
            model = brep;
            chamferobj = new Chamfer(model);
            allSurfaces = adjacentobj.GetSurfaces(model);
            filletSurfaces = FilletTypeSurfaces();
            filletList = RemoveNonfillets(filletSurfaces);
            GroupingFillets(filletList);
        }
        public void Clearlists()
        {
            allSurfaces.Clear();
            surfacesIndexList.Clear();
            filletSurfaces.Clear();
            horizontalFillets.Clear();
            verticalFillets.Clear();
            filletList.Clear();
        }

        /// <summary>
        /// classfying possible surfaces types which can be fillets to a list
        /// </summary>
        /// <param name="entitySurfaces"></param>
        public List<int> FilletTypeSurfaces()
        {
            for (int i = 0; i < allSurfaces.Count; i++)
            {
                surfacesIndexList.Add(i);
                if (allSurfaces[i] is PlanarSurface || allSurfaces[i] is ConicalSurface || allSurfaces[i] is TabulatedSurface)
                {
                    continue;
                }
                else
                {
                    filletSurfaces.Add(i);
                }
            }
            return filletSurfaces;
        }


        /// <summary>
        /// Removing surfaces which do not satisfy fillet conditions 
        /// all fillets have 2 line type edges or no line type edges. 
        /// </summary>
        /// <param name="Fillets"></param>
        /// <returns></returns>
        public List<int> RemoveNonfillets(List<int> Fillets)
        {
            //Removing all non-fillets using edge data
            List<List<FilletData>> groupingfillets = new List<List<FilletData>>();
            for (int i = 0; i < Fillets.Count; i++)
            {
                ICurve[] edges = allSurfaces[Fillets[i]].ExtractEdges();
                int linecount = 0;
                foreach (var e in edges)
                {
                    bool isline = e is Line;
                    if (isline)
                    {
                        linecount++;
                    }
                }
                if (linecount == 0 || linecount == 2)
                {
                    continue;
                }
                else
                {
                    Fillets.Remove(Fillets[i]);
                    i--;
                }
            }
            //Some cylindrical Surface do not satisy fillet's condition these are removed
            for (int i = 0; i < Fillets.Count; i++)
            {
                if (allSurfaces[Fillets[i]] is CylindricalSurface)
                {
                    ICurve[] edges = allSurfaces[Fillets[i]].ExtractEdges();
                    int circlecount = 0;
                    int arccount = 0;
                    foreach (var e in edges)
                    {
                        bool iscircle = e is Circle;
                        bool isarc = e is Arc;
                        bool isline = e is Line;
                        if (iscircle && !isarc)
                        {
                            circlecount++;
                        }
                        if (isarc)
                        {
                            Arc c = (Arc)e;
                            double angle = c.AngleInDegrees;
                            if (angle > 90)
                            {
                                arccount++;
                            }
                        }
                    }
                    // if cylindrical surface have arc or circle with more than 90 degrees than they're not fillet
                    if (circlecount >= 2 || arccount >= 4 || (circlecount >= 1 && arccount >= 2))
                    {
                        Fillets.Remove(Fillets[i]);
                        i--;
                        goto Next;
                    }
                }
                // Some elliptical Surface are hard to different as elliptical chamfer or fillet
                // here elliptical Chamfer are removed
                if (allSurfaces[Fillets[i]] is Surface && !(allSurfaces[Fillets[i]] is CylindricalSurface) && !(allSurfaces[Fillets[i]] is ToroidalSurface))
                {
                    List<int> adjfacesofFillet = adjacentobj.GetAdjFaces(surfacesIndexList, Fillets[i], allSurfaces);
                    ICurve[] edges = allSurfaces[Fillets[i]].ExtractEdges();
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
                                Fillets.Remove(Fillets[i]);
                                i--;
                                goto Next;
                            }
                        }
                    }

                }
            Next:;
            }
            return Fillets;
        }
        /// <summary>
        /// Grouping fillets into types (horizontal, vertical)
        /// </summary>
        /// <param name="Fillets"></param>
        public void GroupingFillets(List<int> Fillets)
        {
            Vector3D zAxis = Vector3D.AxisZ;
            Vector3D filletAxis = new Vector3D();
            foreach (var i in Fillets)
            {
                if (allSurfaces[i] is CylindricalSurface)
                {
                    CylindricalSurface Surface1 = (CylindricalSurface)allSurfaces[i];
                    filletAxis = Surface1.Axis;

                }
                if (allSurfaces[i] is ConicalSurface)
                {
                    ConicalSurface Surface1 = (ConicalSurface)allSurfaces[i];
                    filletAxis = Surface1.Axis;
                }
                double angle = Math.Abs(adjacentobj.FindAngleVectors(zAxis, filletAxis));
                if (Math.Round(angle) == 90)
                {
                    horizontalFillets.Add(i);
                }
            }
            foreach (var i in Fillets)
            {
                if (allSurfaces[i] is CylindricalSurface)
                {
                    CylindricalSurface Surface1 = (CylindricalSurface)allSurfaces[i];
                    filletAxis = Surface1.Axis;

                }
                if (allSurfaces[i] is ConicalSurface)
                {
                    ConicalSurface Surface1 = (ConicalSurface)allSurfaces[i];
                    filletAxis = Surface1.Axis;
                }
                double angle = adjacentobj.FindAngleVectors(zAxis, filletAxis);
                if (angle == 0 || angle == 180)
                {
                    verticalFillets.Add(i);
                }
            }
            //filletList.AddRange(Fillets);
            foreach (var fillet in filletList)
            {
                FilletData filletdata = new FilletData();
                filletdata.index = fillet; // fillet data
                ICurve[] edges = allSurfaces[fillet].ExtractEdges();
                List<double> arcRadius = new List<double>();
                List<double> arcLength = new List<double>();
                List<double> length = new List<double>();
                foreach (var edge in edges)
                {
                    if (edge is Arc arc)
                    {
                        arcRadius.Add(arc.Radius);
                        arcLength.Add(arc.Length());
                    }
                    if (edge is Line line)
                    {
                        length.Add(line.Length());
                    }
                    if (edge is Curve curve)
                    {
                        length.Add(curve.Length());
                    }
                }
                foreach(var edge in edges)
                {
                    if(edge is Arc arc)
                    {
                        if (arc.Length() == arcLength.Min())
                        {
                            filletdata.arcShape = arc;
                        }
                    }
                }
                if(arcRadius.Count > 0)
                {
                    filletdata.radius = arcRadius.Min();
                }
                if(length.Count > 0)
                {
                    filletdata.filletlength = length.Max();
                }
                GroupedFillets.Add(filletdata);
            }

            //return groupingfillets;
        }

    }
}
