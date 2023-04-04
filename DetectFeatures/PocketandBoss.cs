using devDept.Eyeshot.Entities;
using System.Collections.Generic;
using System;
using System.Linq;
using System.Windows;
using devDept.Geometry;
using static devDept.Eyeshot.Entities.Brep;

namespace DetectFeatures
{
    public struct PocketData
    {
        public int totalFaces;
        public int baseFaceIndex;
        public int pocketOnFaceIndex;
        public double pocketDepth;
        public List<int> pocketFacesIndex;
    }
    public struct BossData
    {
        public int totalFaces;
        public int baseFaceIndex;
        public int bossOnFaceIndex;
        public double bossHeight;
        public List<int> bossFacesIndex;
    }
    public struct FacewithIndex
    {
        public int Index;
        public Surface surface;
        public Brep.Face face;

    }
    public struct surfaceAndSegements
    {
        public FacewithIndex face;
        public OrientedEdge[] seg;
    }
    public class PocketandBoss
    {
        readonly Brep model;
        Adjacent adjacentobj = new Adjacent();

        List<Surface> allSurfaces = new List<Surface>();
        List<int> surfaceIndexs = new List<int>();
        List<FacewithIndex> surfacewithfaces = new List<FacewithIndex>();
        List<surfaceAndSegements> surfacewithInnerloops = new List<surfaceAndSegements>();

        public List<PocketData> GroupedPockets = new List<PocketData>();
        public List<BossData> GroupedBosses = new List<BossData>();
        public List<int> pocketslist = new List<int>();
        public List<int> bosslist = new List<int>();
        public PocketandBoss()
        {

        }
        public PocketandBoss(Brep brep)
        {
            Clearlists();
            model = brep;
            GetTypesofSurfaces(model);
            PocketBossTypeSurfaces(surfacewithfaces);
            GroupingPocketBoss();

        }
        public void Clearlists()
        {
            allSurfaces.Clear();
            surfaceIndexs.Clear();
            surfacewithfaces.Clear();
            surfacewithInnerloops.Clear();
            pocketslist.Clear();
            bosslist.Clear();
            GroupedPockets.Clear();
            GroupedBosses.Clear();
        }

        /// <summary>
        /// Getting surfaces from brep and surface data of each surface
        /// </summary>
        /// <param name="brep"></param>
        public void GetTypesofSurfaces(Brep model)
        {
            try
            {
                for (int i = 0; i < model.Faces.Length; i++)
                {
                    Brep.Face face = model.Faces[i];
                    Surface surface = face.Parametric[0];
                    FacewithIndex f = new FacewithIndex();
                    f.face = face;
                    f.surface = surface;
                    f.Index = i;
                    surface.EntityData = i;
                    allSurfaces.Add(surface);
                    surfaceIndexs.Add(i);
                    surfacewithfaces.Add(f);
                }
            }
            catch
            {
                MessageBox.Show("Unexpected error occured");
            }
        }
        /// <summary>
        /// Getting surfaces which have inner loop and searching for pockets and boss
        /// if boss or pocket types are present, they're grouped together and added to list
        /// </summary>
        /// <param name="surfacewithfaces"></param>
        public void PocketBossTypeSurfaces(List<FacewithIndex> surfacewithfaces)
        {
            PocketData pocketdata = new PocketData(); ;
            BossData bossdata = new BossData();
            //getting surfaces which have innerloops
            for (int i = surfacewithfaces.Count; i > 0; i--)
            {
                if (surfacewithfaces[i - 1].face.Loops.Length > 1)
                {
                    pocketdata = new PocketData();
                    bossdata = new BossData();

                    for (int j = 1; j < surfacewithfaces[i - 1].face.Loops.Length; j++)
                    {
                        OrientedEdge[] segments = model.Faces[surfacewithfaces[i - 1].Index].Loops[j].Segments;
                        surfaceAndSegements surfaceSegments = new surfaceAndSegements();
                        surfaceSegments.face = surfacewithfaces[i - 1];
                        surfaceSegments.seg = segments;
                        surfacewithInnerloops.Add(surfaceSegments);
                    }
                }
            }
            for (int i = 0; i < surfacewithInnerloops.Count; i++)
            {
                pocketdata.pocketOnFaceIndex = surfacewithInnerloops[i].face.Index;
                bossdata.bossOnFaceIndex = surfacewithInnerloops[i].face.Index;
                for (int j = 0; j < surfacewithInnerloops[i].seg.Length; j++)//.seg.
                {
                    pocketslist.Clear();
                    bosslist.Clear();
                    var ad = model.Edges[surfacewithInnerloops[i].seg[j].CurveIndex].Parents;
                    foreach (int index in ad)
                    {
                        if (index != surfacewithInnerloops[i].face.Index)
                        {
                            if (allSurfaces[index] is Surface)
                            {
                                bool ispocket = adjacentobj.CheckForDepression(surfacewithInnerloops[i].face.surface, model.Faces[index].Parametric[0]);
                                if (ispocket)
                                {
                                    pocketslist.Add(index);

                                }
                                if (!ispocket)
                                {
                                    bosslist.Add(index);
                                }
                            }
                        }
                    }
                }

                //getting pockets and grouping them
                if (pocketslist.Count > 0)
                {
                    for (int l = 0; l < pocketslist.Count; l++)
                    {
                        List<int> adjacentfaces = adjacentobj.GetAdjFaces(surfaceIndexs, pocketslist[l], allSurfaces);
                        for (int m = 0; m < adjacentfaces.Count; m++)
                        {
                            for (int n = 0; n < surfacewithInnerloops.Count; n++)
                            {
                                if (adjacentfaces.Contains(surfacewithInnerloops[n].face.Index))
                                {
                                    adjacentfaces.Remove(surfacewithInnerloops[n].face.Index);
                                    n--;
                                }
                            }
                            if (pocketslist.Contains(adjacentfaces[m]))
                            {
                                continue;
                            }
                            pocketslist.Add(adjacentfaces[m]);
                        }
                    }
                    pocketslist = pocketslist.Distinct().ToList();
                    for (int p = 0; p < pocketslist.Count; p++)
                    {
                        if (pocketslist[p] != pocketdata.pocketOnFaceIndex)
                        {
                            double angle = adjacentobj.FindAngleSurfaces(allSurfaces[pocketdata.pocketOnFaceIndex], allSurfaces[pocketslist[p]]);
                            if (angle == 0 || angle == 180)
                            {
                                pocketdata.baseFaceIndex = pocketslist[p];
                            }
                        }
                    }
                    Mesh temp1 = allSurfaces[pocketdata.pocketOnFaceIndex].ConvertToMesh();
                    AreaProperties ap = new AreaProperties(temp1.Vertices, temp1.Triangles);
                    ap.GetResults(ap.Area, ap.Centroid, out double x, out double y, out double z, out double xx, out double yy, out double zz, out double xy, out double zx, out double yz, out MomentOfInertia world, out MomentOfInertia centroid);
                    Point3D point1 = ap.Centroid;
                    allSurfaces[pocketdata.baseFaceIndex].ClosestPointTo(point1, out Point3D point2);
                    pocketdata.pocketDepth = Math.Round(Point3D.Distance(point1, point2), 5);
                    pocketdata.totalFaces = pocketslist.Count();
                    pocketdata.pocketFacesIndex = new List<int>(pocketslist);
                    GroupedPockets.Add(pocketdata);
                }
                // getting boss and grouping them
                if (bosslist.Count > 0)
                {
                    for (int l = 0; l < bosslist.Count; l++)
                    {
                        List<int> adjacentfaces = adjacentobj.GetAdjFaces(surfaceIndexs, bosslist[l], allSurfaces);
                        for (int m = 0; m < adjacentfaces.Count; m++)
                        {
                            for (int n = 0; n < surfacewithInnerloops.Count; n++)
                            {
                                if (adjacentfaces.Contains(surfacewithInnerloops[n].face.Index))
                                {
                                    adjacentfaces.Remove(surfacewithInnerloops[n].face.Index);
                                    n--;
                                }
                            }
                            if (bosslist.Contains(adjacentfaces[m]))
                            {
                                continue;
                            }
                            bosslist.Add(adjacentfaces[m]);
                        }
                    }
                    bosslist = bosslist.Distinct().ToList();
                    for (int p = 0; p < bosslist.Count; p++)
                    {
                        if (bosslist[p] != bossdata.bossOnFaceIndex)
                        {
                            double angle = adjacentobj.FindAngleSurfaces(allSurfaces[bossdata.bossOnFaceIndex], allSurfaces[bosslist[p]]);
                            if (angle == 0 || angle == 180)
                            {
                                bossdata.baseFaceIndex = bosslist[p];
                            }
                        }
                    }
                    Mesh temp2 = allSurfaces[bossdata.bossOnFaceIndex].ConvertToMesh();
                    AreaProperties ap2 = new AreaProperties(temp2.Vertices, temp2.Triangles);
                    ap2.GetResults(ap2.Area, ap2.Centroid, out double x, out double y, out double z, out double xx, out double yy, out double zz, out double xy, out double zx, out double yz, out MomentOfInertia world, out MomentOfInertia centroid);
                    Point3D point1 = ap2.Centroid;
                    allSurfaces[bossdata.baseFaceIndex].ClosestPointTo(point1, out Point3D point2);
                    bossdata.bossHeight = Math.Round(Point3D.Distance(point1, point2));
                    bossdata.totalFaces = bosslist.Count();
                    bossdata.bossFacesIndex = new List<int>(bosslist);
                    GroupedBosses.Add(bossdata);
                }

            }
        }
        public void GroupingPocketBoss()
        {
            pocketslist.Clear();
            bosslist.Clear();
            for (int i = 0; i < GroupedPockets.Count; i++)
            {
                pocketslist.AddRange(GroupedPockets[i].pocketFacesIndex);
            }
            for (int i = 0; i < GroupedBosses.Count; i++)
            {
                bosslist.AddRange(GroupedBosses[i].bossFacesIndex);
            }
        }
    }

}

