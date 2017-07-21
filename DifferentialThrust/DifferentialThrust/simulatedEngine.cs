//Written by Flip van Toly for KSP community
//License GPL v2.0 (GNU General Public License)
// Namespace Declaration 
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using System.Reflection;

namespace DifferentialThrustMod
{
    //this object functions mostly as an interface for easy (performance) engine access to nessecary variables.
    public class simulatedEngine
    {
        public Part enginepart;

        public float measuredThrust;
        public float currentThrottle;
        public float thrustPercentage;
        public bool engineactive;
        public bool throttleLocked;

        public bool hasEngineModule = false;
        public DifferentialThrustEngineModule DifMod;

        public int enginemoduletype = 0;
        public ModuleEngines ModEng;
        public MultiModeEngine MultiMod;
        public ModuleEnginesFX ModEngFX;

        public float aimlowest = 0;
        public float aimhighest = 100;

        public float distanceX;
        public float distanceY;


        //update simulated engine with new values for this physics cycle
        public void update(int xax, bool xaxi, int yay, bool yayi, Vector3 CoM)
        {
            float distance;

            //print("update" + ModEng.part.name);

            if (enginemoduletype == 0)
            {
                //read all nessecary values
                measuredThrust = ModEng.finalThrust;
                currentThrottle = ModEng.currentThrottle;
                thrustPercentage = ModEng.thrustPercentage;
                engineactive = (ModEng.EngineIgnited && !ModEng.engineShutdown);
                throttleLocked = ModEng.throttleLocked;

                //establish the average distance of engine to CoM. This is done each physics cycle to account for shifting CoM and possibly altered engine location
                distance = 0.0f;
                foreach (Transform tr in ModEng.thrustTransforms)
                {
                    distance = distance + (enginepart.vessel.ReferenceTransform.InverseTransformPoint(tr.position)[xax] - CoM[xax]);
                }
                distanceX = (distance / ModEng.thrustTransforms.Count()) * (xaxi ? -1 : 1); 

                distance = 0.0f;
                foreach (Transform tr in ModEng.thrustTransforms)
                {
                    distance = distance + (enginepart.vessel.ReferenceTransform.InverseTransformPoint(tr.position)[yay] - CoM[yay]);
                }
                distanceY = (distance / ModEng.thrustTransforms.Count()) * (yayi ? -1 : 1);
            }
            else
            {
                if (hasEngineModule) { ModEngFX = DifMod.PartmoduleModuleEnginesFX; }

                measuredThrust = ModEngFX.finalThrust;
                currentThrottle = ModEngFX.currentThrottle;
                thrustPercentage = ModEngFX.thrustPercentage;
                engineactive = (ModEngFX.EngineIgnited && !ModEngFX.engineShutdown);
                throttleLocked = ModEngFX.throttleLocked;

                distance = 0.0f;
                foreach (Transform tr in ModEngFX.thrustTransforms)
                {
                    distance = distance + (enginepart.vessel.ReferenceTransform.InverseTransformPoint(tr.position)[xax] - CoM[xax]);
                }
                distanceX = (distance / ModEngFX.thrustTransforms.Count()) * (xaxi ? -1 : 1);

                distance = 0.0f;
                foreach (Transform tr in ModEngFX.thrustTransforms)
                {
                    distance = distance + (enginepart.vessel.ReferenceTransform.InverseTransformPoint(tr.position)[yay] - CoM[yay]);
                }
                distanceY = (distance / ModEngFX.thrustTransforms.Count()) * (yayi ? -1 : 1);
            }
        }
    }
}
