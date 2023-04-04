using devDept.Eyeshot.Entities;
using System.Collections.Generic;
using System;
using System.Linq;
using System.Windows;
using devDept.Geometry;
using static devDept.Eyeshot.Entities.Brep;

namespace DetectFeatures
{
    public struct SlotData
    {
        public int baseface;
        public List<int> adjSlotfaces;
    }
    public struct StepData
    {
        public int baseface;
        public List<int> adjStepfaces;
    }
    public class StepandSlots
    {
        readonly Brep model;
        Adjacent adjacentobj = new Adjacent();

        List<Surface> allSurfaces = new List<Surface>();
        List<int> planarSurfaces = new List<int>();
        
        public List<SlotData> GroupedSlots = new List<SlotData>();
        public List<StepData> GroupedSteps = new List<StepData>();
        public List<int> slotlist = new List<int>();
        public List<int> steplist = new List<int>();
        public StepandSlots()
        {

        }
        public StepandSlots(Brep brep)
        {
            Clearlists();
            model = brep;
            allSurfaces = adjacentobj.GetSurfaces(model);
            SlotStepTypeSurfaces();
            GetSlotsAndSteps(planarSurfaces);
            
        }
        public void Clearlists()
        {
            allSurfaces.Clear();
            planarSurfaces.Clear();
            slotlist.Clear();
            steplist.Clear();
            GroupedSlots.Clear();
            GroupedSteps.Clear();

        }        
        /// <summary>
        /// Gets planar surfaces from list of all surfaces
        /// search for steps and slot among these planar surfaces
        /// </summary>
        /// <param name="entitySurfaces"></param>
        /// <param name="brep"></param>
        public void SlotStepTypeSurfaces()
        {
            for (int i = 0; i < allSurfaces.Count; i++)
            {
                if (allSurfaces[i] is PlanarSurface)
                {
                    planarSurfaces.Add(i);
                }
            }           
        }
        /// <summary>
        /// Slots generally have 3 concave edges and steps generally have 1 concave edges
        /// but through slot and side step( step with one side covered) have 2 concave edges
        /// so we check for these criteria and grouped them accordingly into steps and slots
        /// </summary>
        /// <param name="planarSurfaces"></param>
        public void GetSlotsAndSteps(List<int> planarSurfaces)
        {
            for (int i = 0; i < planarSurfaces.Count; i++)
            {
                SlotData slotdata = new SlotData();
                slotdata.baseface = planarSurfaces[i];
                slotdata.adjSlotfaces = new List<int>();

                StepData stepdata = new StepData();
                stepdata.baseface = planarSurfaces[i];
                stepdata.adjStepfaces = new List<int>();

                List<int> adjfaces = adjacentobj.GetAdjFaces(planarSurfaces, planarSurfaces[i], allSurfaces);
                int noof90concaveedges = 0;
                int noofconvexedges = 0;
                List<int> adjfacesofslotorstep = new List<int>();
                for (int j = 0; j < adjfaces.Count; j++)
                {
                    double angle = adjacentobj.FindAngleSurfaces(allSurfaces[planarSurfaces[i]], allSurfaces[adjfaces[j]]);
                    bool isconcave = adjacentobj.Concavity(allSurfaces[planarSurfaces[i]], allSurfaces[adjfaces[j]], model);
                    if (angle == 90 && isconcave)
                    {
                        noof90concaveedges++;
                        slotdata.adjSlotfaces.Add(adjfaces[j]);
                        stepdata.adjStepfaces.Add(adjfaces[j]);
                        adjfacesofslotorstep.Add(adjfaces[j]);
                    }
                    if(!isconcave)
                    {
                        noofconvexedges++;
                    }
                }
                if (noof90concaveedges == 3 && noofconvexedges >= 1)
                {
                    GroupedSlots.Add(slotdata);
                    slotlist.Add(planarSurfaces[i]);
                    slotlist.AddRange(adjfacesofslotorstep);
                }
                if(noof90concaveedges == 2 && noofconvexedges >= 2)
                {
                    double angle = adjacentobj.FindAngleSurfaces(allSurfaces[adjfacesofslotorstep[0]], allSurfaces[adjfacesofslotorstep[1]]);
                    if(angle == 0 || angle == 180)
                    {
                        GroupedSlots.Add(slotdata);
                        slotlist.Add(planarSurfaces[i]);
                        slotlist.AddRange(adjfacesofslotorstep);
                    }
                    if(angle == 90)
                    {
                        GroupedSteps.Add(stepdata);
                        steplist.Add(planarSurfaces[i]);
                        steplist.AddRange(adjfacesofslotorstep);
                    }
                }
                if (noof90concaveedges == 1 && noofconvexedges >= 3)
                {
                    GroupedSteps.Add(stepdata);
                    steplist.Add(planarSurfaces[i]);
                    steplist.AddRange(adjfacesofslotorstep);
                }
            }

            steplist = steplist.Distinct().ToList();
            slotlist = slotlist.Distinct().ToList();
        }
    }
}
