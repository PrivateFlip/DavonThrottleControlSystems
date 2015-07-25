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
    public class DifferentialThrustEngineModule : PartModule
    {
        private bool booted = false;
        public int enginemoduletype = 0;

        public ModuleEngines PartmoduleModuleEngines;
        public MultiModeEngine PartmoduleMultiModeEngine;
        public ModuleEnginesFX PartmoduleModuleEnginesFX;

        [KSPField(guiName = "Level Thrust", isPersistant = false, guiActive = true, guiActiveEditor = true)]
        [UI_FloatRange(stepIncrement = 1f, maxValue = 100f, minValue = 0f)]
        public float levelThrust = 100;

        [KSPField(guiName = "Throttle", isPersistant = false, guiActive = true, guiActiveEditor = true)]
        [UI_FloatRange(stepIncrement = 1f, maxValue = 4f, minValue = 0f)]
        public float throttleFloatSelect;
        public int throttleSelect;
        public float THRAIM = 0;

        public float throttleSvalue;

        //[KSPField(guiName = "Center Thrust", isPersistant = false, guiActive = true, guiActiveEditor = true)]
        public string CenterThrustMode = "available";
        public bool CenterThrust = false;

        [KSPField(isPersistant = false, guiActive = false, guiName = "CoT calc", guiUnits = "")]
        [UI_Toggle(disabledText = "Include", enabledText = "Exclude")]
        public bool CoTcalc = true;


        [KSPField(isPersistant = false, guiActive = false, guiName = "Aim", guiUnits = "")]
        [UI_FloatRange(stepIncrement = 0.001f, maxValue = 100f, minValue = 0f)]
        public float aim = 100;

        [KSPField(isPersistant = false, guiActive = true, guiName = "net", guiUnits = "")]
        [UI_Toggle(disabledText = "Connected", enabledText = "Isolated")]
        public bool isolated = false;

        public bool StoredOuseEngineResponseTime;
        public float StoredOengineAccelerationSpeed;
        public float StoredOengineDecelerationSpeed;

        [KSPEvent(name = "transferToAllEngineOfType", isDefault = false, guiActive = true, guiName = "Center thrust: available")]
        public void CycleCenterThrustMode()
        {
            if (CenterThrustMode == "available")
                CenterThrustMode = "designated";
            else if (CenterThrustMode == "designated")
                CenterThrustMode = "ignore";
            else if (CenterThrustMode == "ignore")
                CenterThrustMode = "available";
            else
                CenterThrustMode = "available";

            Events["CycleCenterThrustMode"].guiName = "Center thrust: " + CenterThrustMode;
        }

        //Adjust engines every cycle. Purposfull OnUpdate instead of OnFixedUpdate.
        public override void OnUpdate()
        {
            if (booted == false)
            {
                boot();
                return;
            }
            
            if (enginemoduletype == 0)
            {
                if (PartmoduleModuleEngines.throttleLocked == true)
                {
                    return;
                }
            }
            else
            {
                if (PartmoduleModuleEnginesFX.throttleLocked == true)
                {
                    return;
                }
            }

            if (enginemoduletype == 2)
            {
                if (PartmoduleModuleEnginesFX.engineID != PartmoduleMultiModeEngine.mode)
                {
                    PartmoduleModuleEnginesFX.useEngineResponseTime = StoredOuseEngineResponseTime;
                    PartmoduleModuleEnginesFX.engineAccelerationSpeed = StoredOengineAccelerationSpeed;
                    PartmoduleModuleEnginesFX.engineDecelerationSpeed = StoredOengineDecelerationSpeed;
                    booted = false;
                    return;
                }
            }

            if (enginemoduletype == 0)
            {
                if (!PartmoduleModuleEngines.EngineIgnited || PartmoduleModuleEngines.engineShutdown)
                {
                    PartmoduleModuleEngines.currentThrottle = 0;
                    return;
                }
            }
            else
            {
                if (!PartmoduleModuleEnginesFX.EngineIgnited || PartmoduleModuleEnginesFX.engineShutdown)
                {
                    PartmoduleModuleEnginesFX.currentThrottle = 0;
                    return;
                }
            }




            //set to correct throttle
            throttleSelect = (int)Math.Round(throttleFloatSelect, 0);


            //retrieve correct throttle value based on selected throttle
            if (throttleSelect == 0)
            {
                throttleSvalue = vessel.ctrlState.mainThrottle;
            }
            else
            {
                throttleSvalue = THRAIM / 100;
            }

            //if center thrust is enabled for this engine, set it to the desired aimpoint
            if (CenterThrust == true)
            {
                if (enginemoduletype == 0)
                {
                    PartmoduleModuleEngines.thrustPercentage = aim;
                }
                else
                {
                    PartmoduleModuleEnginesFX.thrustPercentage = aim;
                }
                Fields["aim"].guiActive = true;

                levelThrust = 100f;
                Fields["levelThrust"].guiActive = false;
            }
            else
            {
                Fields["aim"].guiActive = false;

                Fields["levelThrust"].guiActive = true;
            }


            
            float thrustperc = 100;
            if (enginemoduletype == 0)
            {
                thrustperc = PartmoduleModuleEngines.thrustPercentage;
            }
            else
            {
                thrustperc = PartmoduleModuleEnginesFX.thrustPercentage;
            }

            if ((levelThrust / 100) / (throttleSvalue * (thrustperc / 100)) < 1)
            {
                setThrottle(levelThrust / 100);
            }
            else
            {
                setThrottle(throttleSvalue * (thrustperc / 100));
            }
        }


        private void setThrottle(float Throttle)
        {
            //PartmoduleModuleEngines.currentThrottle = Throttle;


            if (enginemoduletype == 0)
            {
                //With thanks to ZRM, maker of Kerbcom Avionics, and the help of the code of the Throttle Steering mod made by ruffus.
                if (StoredOuseEngineResponseTime && !CenterThrust)
                {
                    if (PartmoduleModuleEngines.currentThrottle > Throttle)
                        PartmoduleModuleEngines.currentThrottle = Mathf.Lerp(PartmoduleModuleEngines.currentThrottle, Throttle, StoredOengineDecelerationSpeed * Time.deltaTime);
                    else
                        PartmoduleModuleEngines.currentThrottle = Mathf.Lerp(PartmoduleModuleEngines.currentThrottle, Throttle, StoredOengineAccelerationSpeed * Time.deltaTime);
                }
                else
                {
                    PartmoduleModuleEngines.currentThrottle = Throttle;
                }
            }
            else
            {
                //With thanks to ZRM, maker of Kerbcom Avionics, and the help of the code of the Throttle Steering mod made by ruffus.
                if (StoredOuseEngineResponseTime && !CenterThrust)
                {
                    if (PartmoduleModuleEnginesFX.currentThrottle > Throttle)
                        PartmoduleModuleEnginesFX.currentThrottle = Mathf.Lerp(PartmoduleModuleEnginesFX.currentThrottle, Throttle, StoredOengineDecelerationSpeed * Time.deltaTime);
                    else
                        PartmoduleModuleEnginesFX.currentThrottle = Mathf.Lerp(PartmoduleModuleEnginesFX.currentThrottle, Throttle, StoredOengineAccelerationSpeed * Time.deltaTime);
                }
                else
                {
                    PartmoduleModuleEnginesFX.currentThrottle = Throttle;
                }
            }
        }

        //first startup boot sequence
        private void boot()
        {
            //print("booting");

            //Euid = (int)part.uid;
            enginemoduletype = 0;
            foreach (PartModule pm in part.Modules)
            {
                if (pm.ClassName == "MultiModeEngine")
                {
                    enginemoduletype = 2;
                    PartmoduleMultiModeEngine = (MultiModeEngine)pm;
                    ChooseMultiModeEngine();

                    //store original values before engine control takeover
                    StoredOuseEngineResponseTime = PartmoduleModuleEnginesFX.useEngineResponseTime;
                    StoredOengineAccelerationSpeed = PartmoduleModuleEnginesFX.engineAccelerationSpeed;
                    StoredOengineDecelerationSpeed = PartmoduleModuleEnginesFX.engineDecelerationSpeed;

                    //This settings must be set to true to be able to control engines with currentThrottle. 
                    //Found this with the help of the code of the Throttle Steering mod made by ruffus. 
                    PartmoduleModuleEnginesFX.useEngineResponseTime = true;

                    //This eliminates the influence of the main throttle on engines
                    PartmoduleModuleEnginesFX.engineAccelerationSpeed = 0.0f;
                    PartmoduleModuleEnginesFX.engineDecelerationSpeed = 0.0f;

                    //set aim to chosen limit thrust
                    aim = PartmoduleModuleEnginesFX.thrustPercentage;
                }
            }
            if (enginemoduletype != 2)
            {
                foreach (PartModule pm in part.Modules)
                {
                    if (pm.ClassName == "ModuleEngines")
                    {
                        enginemoduletype = 0;
                        PartmoduleModuleEngines = (ModuleEngines)pm;

                        //store original values before engine control takeover
                        StoredOuseEngineResponseTime = PartmoduleModuleEngines.useEngineResponseTime;
                        StoredOengineAccelerationSpeed = PartmoduleModuleEngines.engineAccelerationSpeed;
                        StoredOengineDecelerationSpeed = PartmoduleModuleEngines.engineDecelerationSpeed;

                        //This settings must be set to true to be able to control engines with currentThrottle. 
                        //Found this with the help of the code of the Throttle Steering mod made by ruffus. 
                        PartmoduleModuleEngines.useEngineResponseTime = true;

                        //This eliminates the influence of the main throttle on engines
                        PartmoduleModuleEngines.engineAccelerationSpeed = 0.0f;
                        PartmoduleModuleEngines.engineDecelerationSpeed = 0.0f;

                        //set aim to chosen limit thrust
                        aim = PartmoduleModuleEngines.thrustPercentage;

                    }
                    if (pm.ClassName == "ModuleEnginesFX")
                    {
                        enginemoduletype = 1;
                        PartmoduleModuleEnginesFX = (ModuleEnginesFX)pm;

                        //store original values before engine control takeover
                        StoredOuseEngineResponseTime = PartmoduleModuleEnginesFX.useEngineResponseTime;
                        StoredOengineAccelerationSpeed = PartmoduleModuleEnginesFX.engineAccelerationSpeed;
                        StoredOengineDecelerationSpeed = PartmoduleModuleEnginesFX.engineDecelerationSpeed;

                        //This settings must be set to true to be able to control engines with currentThrottle. 
                        //Found this with the help of the code of the Throttle Steering mod made by ruffus. 
                        PartmoduleModuleEnginesFX.useEngineResponseTime = true;

                        //This eliminates the influence of the main throttle on engines
                        PartmoduleModuleEnginesFX.engineAccelerationSpeed = 0.0f;
                        PartmoduleModuleEnginesFX.engineDecelerationSpeed = 0.0f;

                        //set aim to chosen limit thrust
                        aim = PartmoduleModuleEnginesFX.thrustPercentage;
                    }
                }
            }

            Events["transferToAllEngineOfType"].guiName = "Sync all " + part.partInfo.name;

            booted = true;//boot completed
        }

        private void ChooseMultiModeEngine()
        {
            foreach (PartModule pm in part.Modules)
            {
                if (pm.ClassName == "ModuleEnginesFX")
                {
                    ModuleEnginesFX cModuleEnginesFX = (ModuleEnginesFX)pm;
                    if (cModuleEnginesFX.engineID == PartmoduleMultiModeEngine.mode)
                    {
                        PartmoduleModuleEnginesFX = (ModuleEnginesFX)pm;
                    }
                }
            }
        }

        [KSPEvent(name = "transferToAllEngineOfType", isDefault = false, guiActive = true, guiName = "Sync all enginetype")]
        public void transferToAllEngineOfType()
        {
            foreach (Part p in vessel.parts)
            {

                if (p.partInfo.name == part.partInfo.name)
                {
                    foreach (PartModule pm in p.Modules)
                    {
                        if (pm.ClassName == "DifferentialThrustEngineModule")
                        {
                            DifferentialThrustEngineModule aDifferentialThrustEngineModule;
                            aDifferentialThrustEngineModule = p.Modules.OfType<DifferentialThrustEngineModule>().FirstOrDefault();

                            if (aDifferentialThrustEngineModule.isolated == false)
                            {
                                aDifferentialThrustEngineModule.levelThrust = levelThrust;
                                aDifferentialThrustEngineModule.throttleFloatSelect = throttleFloatSelect;
                                aDifferentialThrustEngineModule.CenterThrustMode = CenterThrustMode;
                                aDifferentialThrustEngineModule.Events["CycleCenterThrustMode"].guiName = "Center thrust: " + aDifferentialThrustEngineModule.CenterThrustMode;
                                aDifferentialThrustEngineModule.aim = aim;
                                aDifferentialThrustEngineModule.isolated = isolated;

                                foreach (PartModule pmt in p.Modules)
                                {
                                    if (pmt.ClassName == "ModuleEngines")
                                    {
                                        ModuleEngines aModuleEngines;
                                        aModuleEngines = (ModuleEngines)pmt;

                                        aModuleEngines.thrustPercentage = PartmoduleModuleEngines.thrustPercentage;
                                    }
                                    if (pmt.ClassName == "ModuleEnginesFX")
                                    {
                                        ModuleEnginesFX aModuleEnginesFX;
                                        aModuleEnginesFX = (ModuleEnginesFX)pmt;

                                        aModuleEnginesFX.thrustPercentage = PartmoduleModuleEnginesFX.thrustPercentage;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}


