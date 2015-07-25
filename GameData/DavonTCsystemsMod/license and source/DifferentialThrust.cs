//Written by Flip van Toly for KSP community
//License GPL v2.0 (GNU General Public License)
// Namespace Declaration 
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using System.Reflection;


namespace PFlipDifferentialThrustMod
{
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class DifferentialThrustToolbar : MonoBehaviour
    {
        //stock toolbar button
        private static ApplicationLauncherButton toolBarButton;
        private static Texture2D toolBarButtonTexture;

        private double lastUpdateToolBarTime = 0.0f;

        public void Awake()
        {
            RenderingManager.AddToPostDrawQueue(643, OnDraw);
        }

        void onDestroy()
        {
            if (toolBarButton != null) { removeToolbarButton(); }
        }

        private void OnDraw()
        {
            updateToolBar();
        }

        private void updateToolBar()
        {
            
            if (HighLogic.LoadedScene != GameScenes.FLIGHT || (lastUpdateToolBarTime + 2) > Planetarium.GetUniversalTime() ||!ApplicationLauncher.Ready) { return; }

            if (toolBarButton == null && hasDifferentialThrustModule()) { addToolbarButton();}
            if (toolBarButton != null && !hasDifferentialThrustModule()) { removeToolbarButton(); }

            lastUpdateToolBarTime = Planetarium.GetUniversalTime();
        }

        private void addToolbarButton()
        {
            toolBarButtonTexture = GameDatabase.Instance.GetTexture("DavonTCsystemsMod/Textures/TCbutton", false);

            toolBarButton = ApplicationLauncher.Instance.AddModApplication(
                                onToggleOn,
                                onToggleOff,
                                null,
                                null,
                                null,
                                null,
                                ApplicationLauncher.AppScenes.FLIGHT,
                                toolBarButtonTexture);
        }

        private void removeToolbarButton()
        {
            ApplicationLauncher.Instance.RemoveModApplication(toolBarButton);
            toolBarButton = null;
        }

        void onToggleOn()
        {
            if (!toggleOnPrimary())
            {
                makePrimary();
                toggleOnPrimary();
            }
        }

        void onToggleOff()
        {
            toggleOff();
        }

        private bool hasDifferentialThrustModule()
        {
            foreach (Part p in FlightGlobals.ActiveVessel.parts)
            {
                foreach (PartModule pm in p.Modules)
                {
                    if (pm.ClassName == "DifferentialThrust")
                    {
                        return (true);
                    }
                }
            }
            return (false);
        }

        private bool toggleOnPrimary()
        {
            foreach (Part p in FlightGlobals.ActiveVessel.parts)
            {
                foreach (PartModule pm in p.Modules)
                {
                    if (pm.ClassName == "DifferentialThrust")
                    {
                        DifferentialThrust aDifferentialThrust;
                        aDifferentialThrust = p.Modules.OfType<DifferentialThrust>().FirstOrDefault();
                        if (aDifferentialThrust.isPrimary) { aDifferentialThrust.toggleModuleOn(); return true; }
                    }
                }
            }
            return (false);
        }

        private void makePrimary()
        {
            foreach (Part p in FlightGlobals.ActiveVessel.parts)
            {
                foreach (PartModule pm in p.Modules)
                {
                    if (pm.ClassName == "DifferentialThrust")
                    {
                        DifferentialThrust aDifferentialThrust;
                        aDifferentialThrust = p.Modules.OfType<DifferentialThrust>().FirstOrDefault();
                        aDifferentialThrust.isPrimary = true;
                        return;
                    }
                }
            }
        }

        private bool toggleOff()
        {
            foreach (Part p in FlightGlobals.ActiveVessel.parts)
            {
                foreach (PartModule pm in p.Modules)
                {
                    if (pm.ClassName == "DifferentialThrust")
                    {
                        DifferentialThrust aDifferentialThrust;
                        aDifferentialThrust = p.Modules.OfType<DifferentialThrust>().FirstOrDefault();
                        aDifferentialThrust.toggleModuleOff();
                    }
                }
            }
            return (false);
        }
    }

    public class DifferentialThrust : PartModule
    {
        [KSPField(isPersistant = true, guiActive = false)]
        public bool isActive = false;
        [KSPField(isPersistant = true, guiActive = false)]
        public bool isPrimary = false;

        private List<Part> EngineList = new List<Part>();
        private List<DifferentialThrustEngineModule> TCEngineList = new List<DifferentialThrustEngineModule>();

        [KSPField(isPersistant = false, guiActive = false)]
        public bool CenterThrustToggle = false;

        [KSPField(isPersistant = true, guiActive = false)]
        public bool OnlyDesEng = false;

        [KSPField(isPersistant = true, guiActive = false)]
        public int xax = 0;
        [KSPField(isPersistant = true, guiActive = false)]
        public int yay = 2;

        [KSPField(isPersistant = true, guiActive = false)]
        public int CenterThrustDirection = 0;
        [KSPField(isPersistant = true, guiActive = false)]
        public float adjstr = 10f;

        private Vector3 CoM;
        private List<simulatedEngine> simulatedEngineList = new List<simulatedEngine>();

        private float adjustmentSize = 0.1f;

        //simulatedEngine previousAdjustEngine;
        //private float previousAdjust;
        private float previousCoTX;
        private float previousCoTY;

        [KSPField(isPersistant = true, guiActive = false)]
        public string savedEngCon = "0";
        public double lastEngineConfigurationSaveTime = 0;
        public bool loadedEngineSettings = false;

        //auto on variables
        public double plannedReactivationTime = 0.0;

        //GUI variables
        protected Rect windowPos = new Rect(50, 50, 200, 200);
        protected Rect windowPosC = new Rect(150, 150, 200, 120);
        protected Rect windowPosT = new Rect(300, 50, 160, 280);
        protected Rect windowPosD = new Rect(330, 60, 160, 240);
        protected Rect windowPosS = new Rect(340, 70, 220, 400);
        protected Rect windowPosJ = new Rect(250, 300, 300, 200);

        //Profile window (GUI)
        System.IO.DirectoryInfo GamePath;
        List<string> profiles = new List<string>();
        string ProfilesPath = "/GameData/DavonTCsystemsMod/Plugins/Profiles/";
        string StrProfName = "untitled profile";
        Vector2 scrollPosition;

        //center thrust window (GUI)
        [KSPField(isPersistant = true, guiActive = false)]
        public int selEngGridInt = 1;
        [KSPField(isPersistant = true, guiActive = false)]
        public string Stradjstr = "10";
        [KSPField(isPersistant = true, guiActive = false)]
        public int selOnOffGridInt = 0;


        //Direction (GUI)
        [KSPField(isPersistant = true, guiActive = false)]
        int selDirGridInt;

        //Throttles (GUI)
        float Throttle1 = 0.0f;
        [KSPField(isPersistant = true, guiActive = false)]
        float Throttleslider1 = 0.0f;
        float Throttle2 = 0.0f;
        [KSPField(isPersistant = true, guiActive = false)]
        float Throttleslider2 = 0.0f;
        float Throttle3 = 0.0f;
        [KSPField(isPersistant = true, guiActive = false)]
        float Throttleslider3 = 0.0f;
        float Throttle4 = 0.0f;
        [KSPField(isPersistant = true, guiActive = false)]
        float Throttleslider4 = 0.0f;

        //Controls
        [KSPField(isPersistant = true, guiActive = false)]
        public string Throttle1ControlDes = "";
        [KSPField(isPersistant = true, guiActive = false)]
        public string Throttle2ControlDes = "";
        [KSPField(isPersistant = true, guiActive = false)]
        public string Throttle3ControlDes = "";
        [KSPField(isPersistant = true, guiActive = false)]
        public string Throttle4ControlDes = "";

        public int selJoyGridInt = 0;
        public string StrKeyThrottleUp = "";
        public string StrKeyThrottleDown = "";
        public string StrJoystick = "";
        public string StrAxis = "";
        public bool BoolInvert = false;
        public bool BoolCentral = false;

        /// <summary>
        /// General functions
        /// </summary>
        /// 

        public override void OnStart(StartState state)
        {
            if (isActive)
            {
                plannedReactivationTime = Planetarium.GetUniversalTime() + 3;
            }
        }

        public override void OnFixedUpdate()
        {
            checkCommandPodPresent();
            checkEngineLoss();

            if (CenterThrustToggle == true) 
            { 
                //adjust();

                centerThrustInSim();
            }


            if (lastEngineConfigurationSaveTime < Planetarium.GetUniversalTime() - 10 && loadedEngineSettings == true)
            {
                SaveEngineSettings();
                lastEngineConfigurationSaveTime = Planetarium.GetUniversalTime();
            }

            if (plannedReactivationTime != 0 && plannedReactivationTime < Planetarium.GetUniversalTime())
            {
                //print("hi");
                Activate();
                plannedReactivationTime = 0.0;
            }
        }

        public override void OnUpdate()
        {
            ThrottleExecute();
        }

        //Scan vessel for engines and add partmodules to control engines
        public void Activate()
        {
            //Activate part
            part.force_activate(); //activate(vessel.currentStage, vessel );

            //deactivate any other modules
            foreach (Part p in FlightGlobals.ActiveVessel.parts)
            {
                foreach (PartModule pm in p.Modules)
                {
                    if (pm.ClassName == "DifferentialThrust" && p.flightID != part.flightID)
                    {
                        DifferentialThrust aDifferentialThrust;
                        aDifferentialThrust = p.Modules.OfType<DifferentialThrust>().FirstOrDefault();
                        aDifferentialThrust.isPrimary = false;
                        aDifferentialThrust.Deactivate();
                        aDifferentialThrust.Events["Moduleconfig"].guiName = "TC systems";
                    }
                }
            }

            //make list of parts with engines
            EngineList.Clear();
            foreach (Part p in vessel.parts)
            {
                bool added = false;
                foreach (PartModule pm in p.Modules)
                {
                    if (added == false && (pm.ClassName == "ModuleEngines" || pm.ClassName == "MultiModeEngine" || pm.ClassName == "ModuleEnginesFX") )
                    {
                        EngineList.Add(p);
                        added = true;
                    }
                }
            }

            TCEngineList.Clear();
            //add temp module to every engine
            foreach (Part p in EngineList)
            {
                bool dontadd = false;
                bool couldadd = false;
                foreach (PartModule pm in p.Modules)
                {
                    //check if already added
                    if (pm.ClassName == "DifferentialThrustEngineModule")
                    {
                        dontadd = true;
                        TCEngineList.Add((DifferentialThrustEngineModule)pm);
                    }

                    //check if SRB
                    if (pm.ClassName == "ModuleEngines")
                    {
                        ModuleEngines cModuleEngines;
                        cModuleEngines = p.Modules.OfType<ModuleEngines>().FirstOrDefault();
                        if (cModuleEngines.throttleLocked == false) { couldadd = true; };
                    }
                    //check if SRB
                    if (pm.ClassName == "ModuleEnginesFX")
                    {
                        ModuleEnginesFX cModuleEnginesFX;
                        cModuleEnginesFX = p.Modules.OfType<ModuleEnginesFX>().FirstOrDefault();
                        if (cModuleEnginesFX.throttleLocked == false) { couldadd = true; };
                    }
                }

                if (dontadd == false && couldadd == true)
                {
                    p.AddModule("DifferentialThrustEngineModule");
                    //and add to list
                    foreach (PartModule pmt in p.Modules)
                    {
                        //check if already added
                        if (pmt.ClassName == "DifferentialThrustEngineModule")
                        {
                            TCEngineList.Add((DifferentialThrustEngineModule)pmt);
                        }
                    }
                }
            }

            if (loadedEngineSettings == false)
            {
                LoadEngineSettings();
                CenterThrustToggle = (selOnOffGridInt == 1);
                UpdateCenterThrust();
                loadedEngineSettings = true;
            }

            Events["Moduleconfig"].guiName = "TC systems - ACTIVE";

            isPrimary = true;
            isActive = true;
        }

        public void Deactivate()
        {
            //make list of parts with temp module
            List<Part> ModDifList = new List<Part>();
            foreach (Part p in EngineList)
            {
                foreach (PartModule pm in p.Modules)
                {
                    if (pm.ClassName == "DifferentialThrustEngineModule")
                    {
                        ModDifList.Add(p);
                    }
                }
            }


            foreach (Part p in ModDifList)
            {
                DifferentialThrustEngineModule rDifferentialThrustEngineModule;
                rDifferentialThrustEngineModule = p.Modules.OfType<DifferentialThrustEngineModule>().FirstOrDefault();

                //reset engines
                if (rDifferentialThrustEngineModule.enginemoduletype == 0)
                {
                    //ModuleEngines rModuleEngines;
                    //rModuleEngines = p.Modules.OfType<ModuleEngines>().FirstOrDefault();

                    rDifferentialThrustEngineModule.PartmoduleModuleEngines.useEngineResponseTime = rDifferentialThrustEngineModule.StoredOuseEngineResponseTime;
                    rDifferentialThrustEngineModule.PartmoduleModuleEngines.engineAccelerationSpeed = rDifferentialThrustEngineModule.StoredOengineAccelerationSpeed;
                    rDifferentialThrustEngineModule.PartmoduleModuleEngines.engineDecelerationSpeed = rDifferentialThrustEngineModule.StoredOengineDecelerationSpeed;
                }
                else
                {
                    rDifferentialThrustEngineModule.PartmoduleModuleEnginesFX.useEngineResponseTime = rDifferentialThrustEngineModule.StoredOuseEngineResponseTime;
                    rDifferentialThrustEngineModule.PartmoduleModuleEnginesFX.engineAccelerationSpeed = rDifferentialThrustEngineModule.StoredOengineAccelerationSpeed;
                    rDifferentialThrustEngineModule.PartmoduleModuleEnginesFX.engineDecelerationSpeed = rDifferentialThrustEngineModule.StoredOengineDecelerationSpeed;
                }

                //remove temp module from every engine
                p.RemoveModule(rDifferentialThrustEngineModule);

            }

            EngineList.Clear();
            TCEngineList.Clear();
            simulatedEngineList.Clear();

            //set the loaded engine setting variable to false so engines will be reloaded on next activation
            loadedEngineSettings = false;

            closeAllGUIs();

            Events["Moduleconfig"].guiName = "TC systems - INACTIVE";

            isActive = false;
        }

        private void closeAllGUIs()
        {
            closeGUIMconfig();
            closeGUITconfig();
            closeGUICconfig();
            closeGUIDconfig();
            closeGUISconfig();
            closeGUIJconfig();
        }

        ////Save and load
        /*Since the modules added to the engines are temporary, they are not saved, when switching vessel, saving, etcetra
        These functions saves configuration settings of of all engines to a string in this central module so they 
        are saved by the games save system and can be pushed to the engines again after reactivation of the central module.
        Saving is done based on location of the part, since no persistent identifier of parts was known to me. part.uid changes on saves
        Pushing these function to OnSave and OnLoad was unsuccesfull for reasons unknown.*/

        public void SaveEngineSettings()
        {
            savedEngCon = "1";
            foreach (DifferentialThrustEngineModule dtm in TCEngineList)
            {
                        //Create SaveId based on position. This allows the central module to save and load settings back to this engine module.
                        string SaveID = dtm.part.orgPos.ToString();

                        savedEngCon = savedEngCon + ":" + SaveID + ">" + dtm.levelThrust + ">" + dtm.throttleFloatSelect + ">" + dtm.CenterThrustMode + ">" + dtm.aim + ">" + dtm.isolated;
            }
            //print(savedEngCon);
        }

        public void LoadEngineSettings()
        {
            if ("1" == savedEngCon.Substring(0, 1))
            {
                string[] arrShips = savedEngCon.Split(':');

                foreach (DifferentialThrustEngineModule dtm in TCEngineList)
                {
                    foreach (string sti in arrShips)
                    {
                        int SaveIDLen = (dtm.part.orgPos.ToString()).Length;
                        if (SaveIDLen <= sti.Length)
                        {
                            if (dtm.part.orgPos.ToString() == sti.Substring(0, SaveIDLen))
                            {
                                string[] arrData = sti.Split('>');
                                print(sti);

                                dtm.levelThrust = (float)Convert.ToDouble(arrData[1]);
                                dtm.throttleFloatSelect = (float)Convert.ToDouble(arrData[2]);
                                dtm.CenterThrustMode = arrData[3];
                                dtm.Events["CycleCenterThrustMode"].guiName = "Center thrust: " + dtm.CenterThrustMode;
                                dtm.aim = (float)Convert.ToDouble(arrData[4]);
                                dtm.isolated = (arrData[5] == "True");
                            }
                        }
                    }
                }
            }
        }

        public void checkCommandPodPresent()
        {
            bool present = false;
            foreach (Part p in vessel.parts)
            {
                foreach (PartModule pm in p.Modules)
                {
                    if (pm.ClassName == "ModuleCommand")
                    {
                        present = true;
                    }
                }
            }

            if (present == false)
            {
                Deactivate();
            }
        }

        public void checkEngineLoss()
        {
            foreach (Part p in EngineList)
            {
                //check for engine loss
                if (p.vessel == null || p.vessel != part.vessel)
                {
                    Activate();
                    makeSimulatedEngineList();
                    print("engine loss");
                    return;
                }
            }    //update engine date for each physics cycle
        }


        public void toggleModuleOn()
        {
            Moduleconfig();
        }

        public void toggleModuleOff()
        {
            closeAllGUIs();
        }


        /// <summary>
        /// Module config window
        /// </summary>

        private void WindowGUIMconfig(int windowID)
        {
            GUI.DragWindow(new Rect(0, 0, 200, 25));
            GUILayout.BeginVertical();

            if (GUILayout.Button("All Normal"))
            {
                foreach (DifferentialThrustEngineModule dtm in TCEngineList)
                {
                    if (!dtm.isolated)
                    {
                        dtm.levelThrust = 100f;
                        dtm.throttleFloatSelect = 0f;
                        dtm.CenterThrust = false;

                        if (dtm.enginemoduletype == 0)
                        {
                            dtm.PartmoduleModuleEngines.thrustPercentage = 100f;
                        }
                        else if (dtm.enginemoduletype == 1)
                        {
                            dtm.PartmoduleModuleEnginesFX.thrustPercentage = 100f;
                        }
                    }
                }
                selOnOffGridInt = 0;
                CenterThrustToggle = false;
            }

            //GUILayout.Label(" ");
            if (GUILayout.Button("Center thrust mode"))
            {
                CenterThrust();
            }
            //GUILayout.Label("Extra Throttles");
            if (GUILayout.Button("Throttles"))
            {
                Throttleconfig();
            }


            /*GUILayout.Label("Engine configuration save");
            if (GUILayout.Button("Save Engine Configuration"))
            {
                SaveEngineSettings();
            }
            if (GUILayout.Button("Load Engine Configuration"))
            {
                LoadEngineSettings();
            }*/
            if (GUILayout.Button("Profiles"))
            {
                ProfileWindow();
            }

            //GUILayout.Label(" ");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Deactivate module"))
            {
                Deactivate();
            }
            if (GUILayout.Button("X", GUILayout.Width(40)))
            {
                closeGUIMconfig();
            }
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();

        }


        private void drawGUIMconfig()
        {
            GUI.skin = HighLogic.Skin;
            windowPos = GUILayout.Window(14132, windowPos, WindowGUIMconfig, "Module");
        }


        [KSPEvent(name = "Moduleconfig", isDefault = false, guiActive = true, guiName = "TC systems")]
        public void Moduleconfig()
        {
            if (!isActive) { Activate(); }
            RenderingManager.RemoveFromPostDrawQueue(45115, new Callback(drawGUIMconfig));
            RenderingManager.AddToPostDrawQueue(45115, new Callback(drawGUIMconfig));
        }

        public void closeGUIMconfig()
        {
            RenderingManager.RemoveFromPostDrawQueue(45115, new Callback(drawGUIMconfig));
        }


        /// <summary>
        /// Module throttle
        /// </summary>

        private void WindowGUITconfig(int windowID)
        {

            GUI.DragWindow(new Rect(0, 0, 200, 25));
            GUILayout.BeginHorizontal();


            //vars,here,enginecontrol
            GUILayout.Label("1");
            Throttleslider1 = GUILayout.VerticalSlider(Throttleslider1, 100.0f, 0.0f);

            GUILayout.Label("2");
            Throttleslider2 = GUILayout.VerticalSlider(Throttleslider2, 100.0f, 0.0f);

            GUILayout.Label("3");
            Throttleslider3 = GUILayout.VerticalSlider(Throttleslider3, 100.0f, 0.0f);

            GUILayout.Label("4");
            Throttleslider4 = GUILayout.VerticalSlider(Throttleslider4, 100.0f, 0.0f);


            GUILayout.BeginVertical();
            if (GUILayout.Button("X", GUILayout.Width(40)))
            {
                closeGUITconfig();
            }
            if (GUILayout.Button("C\no\nn\nt\nr\no\nl\ns", GUILayout.Width(40), GUILayout.Height(200)))
            {
                joystickconfig();
            }
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();

        }


        private void drawGUITconfig()
        {
            GUI.skin = HighLogic.Skin;
            windowPosT = GUILayout.Window(4130, windowPosT, WindowGUITconfig, "Throttle 1-4");
        }

        public void Throttleconfig()
        {
            RenderingManager.AddToPostDrawQueue(5110, new Callback(drawGUITconfig));
        }

        public void closeGUITconfig()
        {
            RenderingManager.RemoveFromPostDrawQueue(5110, new Callback(drawGUITconfig));
        }


        /// <summary>
        /// Module throttle controls
        /// </summary>


        private void WindowGUIJconfig(int windowID)
        {
            GUI.DragWindow(new Rect(0, 0, 200, 25));
            GUILayout.BeginVertical();

            string[] selStrings = new string[4];
            selStrings[0] = "Throttle 1";
            selStrings[1] = "Throttle 2";
            selStrings[2] = "Throttle 3";
            selStrings[3] = "Throttle 4";
            selJoyGridInt = GUILayout.SelectionGrid(selJoyGridInt, selStrings, 2);

            switch (selJoyGridInt)
            {
                case 0:
                    GUILayout.Label("Current settings " + selStrings[selJoyGridInt] + ": " + Throttle1ControlDes);
                    break;
                case 1:
                    GUILayout.Label("Current settings " + selStrings[selJoyGridInt] + ": " + Throttle2ControlDes);
                    break;
                case 2:
                    GUILayout.Label("Current settings " + selStrings[selJoyGridInt] + ": " + Throttle3ControlDes);
                    break;
                case 3:
                    GUILayout.Label("Current settings " + selStrings[selJoyGridInt] + ": " + Throttle4ControlDes);
                    break;
            }

            GUILayout.Label("");

            GUILayout.BeginHorizontal();
            StrKeyThrottleUp = GUILayout.TextField(StrKeyThrottleUp, 20, GUILayout.Width(40));
            GUILayout.Label("Throttle up key");
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            StrKeyThrottleDown = GUILayout.TextField(StrKeyThrottleDown, 20, GUILayout.Width(40));
            GUILayout.Label("Throttle down key");
            GUILayout.EndHorizontal();

            GUILayout.Label("");

            GUILayout.BeginHorizontal();
            StrJoystick = GUILayout.TextField(StrJoystick, 5, GUILayout.Width(40));
            GUILayout.Label("Joystick number");
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            StrAxis = GUILayout.TextField(StrAxis, 5, GUILayout.Width(40));
            GUILayout.Label("Axis number");
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            BoolInvert = GUILayout.Toggle(BoolInvert, "Invert axis");
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            BoolCentral = GUILayout.Toggle(BoolCentral, "Zero at center");
            GUILayout.EndHorizontal();


            string ControlDes = "";

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Set", GUILayout.Width(60)))
            {
                if (!(StrKeyThrottleDown == "") || !(StrKeyThrottleUp == ""))
                {
                    ControlDes = "keys>" + StrKeyThrottleUp.Trim() + ">" + StrKeyThrottleDown.Trim();
                }
                else if (!(StrJoystick == "") || !(StrAxis == ""))
                {
                    ControlDes = "joys>joy" + StrJoystick.Trim() + "." + StrAxis.Trim() + ">" + BoolInvert + ">" + BoolCentral;
                }
                else
                {
                    ControlDes = "";
                }
                switch (selJoyGridInt)
                {
                    case 0:
                        Throttle1ControlDes = ControlDes;
                        break;
                    case 1:
                        Throttle2ControlDes = ControlDes;
                        break;
                    case 2:
                        Throttle3ControlDes = ControlDes;
                        break;
                    case 3:
                        Throttle4ControlDes = ControlDes;
                        break;
                }
            }
            if (GUILayout.Button("Clear", GUILayout.Width(60)))
            {
                StrKeyThrottleUp = "";
                StrKeyThrottleDown = "";
                StrJoystick = "";
                StrAxis = "";
                BoolInvert = false;
                BoolCentral = false;

                switch (selJoyGridInt)
                {
                    case 0:
                        Throttle1ControlDes = "";
                        break;
                    case 1:
                        Throttle2ControlDes = "";
                        break;
                    case 2:
                        Throttle3ControlDes = "";
                        break;
                    case 3:
                        Throttle4ControlDes = "";
                        break;
                }
            }
            if (GUILayout.Button("X", GUILayout.Width(40)))
            {
                closeGUIJconfig();
            }
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
        }

        private void drawGUIJconfig()
        {
            GUI.skin = HighLogic.Skin;
            windowPosJ = GUILayout.Window(23132, windowPosJ, WindowGUIJconfig, "Controls");
        }

        public void joystickconfig()
        {
            RenderingManager.AddToPostDrawQueue(23115, new Callback(drawGUIJconfig));
        }

        public void closeGUIJconfig()
        {
            RenderingManager.RemoveFromPostDrawQueue(23115, new Callback(drawGUIJconfig));
        }




        /// <summary>
        /// Profile Window
        /// </summary>

        private void WindowGUISconfig(int windowID)
        {
            GUI.DragWindow(new Rect(0, 0, 220, 25));

            GUILayout.BeginVertical();
            scrollPosition = GUILayout.BeginScrollView(scrollPosition, GUILayout.Width(240), GUILayout.Height(350));

            foreach (string profile in profiles)
            {
                if (GUILayout.Button(profile))
                {
                    StrProfName = profile;
                }
            }

            GUILayout.EndScrollView();

            GUILayout.BeginHorizontal();
            GUILayout.Label("name");
            StrProfName = GUILayout.TextField(StrProfName, 200, GUILayout.Width(200));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Save Profile", GUILayout.Width(80)))
            {
                saveprofile(StrProfName);
                updateProfilesList();
                closeGUISconfig();
            }

            if (GUILayout.Button("Load Profile", GUILayout.Width(80)))
            {
                loadprofile(StrProfName);
                updateProfilesList();
                closeGUISconfig();
            }
            if (GUILayout.Button("del", GUILayout.Width(40)))
            {
                System.IO.File.Delete(GamePath + ProfilesPath + StrProfName);
                updateProfilesList();
            }
            if (GUILayout.Button("X", GUILayout.Width(40)))
            {
                closeGUISconfig();
            }
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
        }

        public void saveprofile(string profile)
        {
            string[] data = new string[9];

            SaveEngineSettings();
            data[0] = savedEngCon;

            data[1] = CenterThrustDirection.ToString();

            data[2] = selEngGridInt.ToString();
            data[3] = Stradjstr;
            data[4] = selOnOffGridInt.ToString();
            data[5] = Throttle1ControlDes;
            data[6] = Throttle2ControlDes;
            data[7] = Throttle3ControlDes;
            data[8] = Throttle4ControlDes;


            System.IO.File.WriteAllLines(GamePath + ProfilesPath + profile, data);
        }

        public void loadprofile(string profile)
        {
            string[] data = System.IO.File.ReadAllLines(GamePath + ProfilesPath + profile);

            savedEngCon = data[0];
            LoadEngineSettings();

            CenterThrustDirection = Convert.ToInt32(data[1]);
            setdirection(CenterThrustDirection);
            selDirGridInt = CenterThrustDirection;

            selEngGridInt = Convert.ToInt32(data[2]);
            Stradjstr = data[3];
            selOnOffGridInt = Convert.ToInt32(data[4]);
            CenterThrustToggle = (selOnOffGridInt == 1);
            UpdateCenterThrust();

            Throttle1ControlDes = data[5];
            Throttle2ControlDes = data[6];
            Throttle3ControlDes = data[7];
            Throttle4ControlDes = data[8];

        }

        private void updateProfilesList()
        {
            string[] profilePaths = System.IO.Directory.GetFiles(GamePath + ProfilesPath);
            profiles.Clear();
            foreach (string file in profilePaths)
            {
                profiles.Add(System.IO.Path.GetFileName(file));
            }
        }

        private void drawGUISconfig()
        {
            GUI.skin = HighLogic.Skin;
            windowPosS = GUILayout.Window(14231, windowPosS, WindowGUISconfig, "Profile");
        }

        public void ProfileWindow()
        {
            GamePath = System.IO.Directory.GetParent(Application.dataPath);
            updateProfilesList();
            RenderingManager.AddToPostDrawQueue(45214, new Callback(drawGUISconfig));
        }

        public void closeGUISconfig()
        {
            RenderingManager.RemoveFromPostDrawQueue(45214, new Callback(drawGUISconfig));
        }

        /// <summary>
        /// Center thrust config window
        /// </summary>


        private void WindowGUICconfig(int windowID)
        {
            GUI.DragWindow(new Rect(0, 0, 200, 25));
            GUILayout.BeginVertical();

            string[] selStrings = new string[2];
            selStrings[0] = "Designated";
            selStrings[1] = "All";
            selEngGridInt = GUILayout.SelectionGrid(selEngGridInt, selStrings, 2);

            string[] selOnOffStrings = new string[2];
            selOnOffStrings[0] = "Off";
            selOnOffStrings[1] = "On";

            if (GUILayout.SelectionGrid(selOnOffGridInt, selOnOffStrings, 2) == 0)
            {
                selOnOffGridInt = 0;
                if (CenterThrustToggle == true)
                {
                    CenterThrustToggle = false;
                    UpdateCenterThrust();

                }
            }
            else
            {
                selOnOffGridInt = 1;
                if (CenterThrustToggle == false)
                {
                    CenterThrustToggle = true;
                    UpdateCenterThrust();
                }
            }


            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Direction", GUILayout.Width(100)))
            {
                Directionwindow();
            }
            if (GUILayout.Button("X", GUILayout.Width(40)))
            {
                closeGUICconfig();
            }
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
        }

        private void drawGUICconfig()
        {
            GUI.skin = HighLogic.Skin;
            windowPosC = GUILayout.Window(14131, windowPosC, WindowGUICconfig, "Center Thrust");
        }

        public void CenterThrust()
        {
            RenderingManager.AddToPostDrawQueue(45114, new Callback(drawGUICconfig));
        }

        public void closeGUICconfig()
        {
            RenderingManager.RemoveFromPostDrawQueue(45114, new Callback(drawGUICconfig));
        }


        public void UpdateCenterThrust()
        {
            if (CenterThrustToggle)
            {
                /*float n;
                bool isNumeric;

                isNumeric = float.TryParse(Stradjstr, out n);
                if (String.IsNullOrEmpty(Stradjstr) || isNumeric == false)
                {
                    Stradjstr = "error";
                    return;
                }
                else
                {
                    adjstr = Convert.ToSingle(Stradjstr);
                }*/

                CenterThrustToggle = true;

                OnlyDesEng = (selEngGridInt == 0);
                foreach (DifferentialThrustEngineModule dtm in TCEngineList)
                {
                    if (!dtm.isolated || dtm.CenterThrustMode == "designated")
                    {
                        if (OnlyDesEng == true)
                        {
                            if (dtm.CenterThrust == false && dtm.CenterThrustMode == "designated")
                            {
                                dtm.CenterThrust = true;
                            }
                        }
                        else
                        {
                            if (dtm.CenterThrust == false && !(dtm.CenterThrustMode == "ignore"))
                            {
                                dtm.CenterThrust = true;
                            }
                        }
                    }
                }
            }
            else
            {
                foreach (DifferentialThrustEngineModule dtm in TCEngineList)
                {
                    if (dtm.CenterThrust == true)
                    {
                        dtm.CenterThrust = false;
                    }
                }
            }
            makeSimulatedEngineList();
        }

        /// <summary>
        /// Direction config window
        /// </summary>


        private void WindowGUIDconfig(int windowID)
        {
            GUI.DragWindow(new Rect(0, 0, 200, 25));
            GUILayout.BeginVertical();

            GUILayout.Label("Direction engines are facing in relation to the active command module");
            string[] selDirStrings = new string[6];
            selDirStrings[0] = "Normal";
            selDirStrings[1] = "Forward";
            selDirStrings[2] = "Down";
            selDirStrings[3] = "Up";
            selDirStrings[4] = "Left";
            selDirStrings[5] = "Right";
            selDirGridInt = GUILayout.SelectionGrid(selDirGridInt, selDirStrings, 2);


            GUILayout.BeginHorizontal();
            if (CenterThrustDirection != selDirGridInt)
            {
                CenterThrustDirection = selDirGridInt;
                setdirection(CenterThrustDirection);
                closeGUIDconfig();
            }
            if (GUILayout.Button("X", GUILayout.Width(40)))
            {
                closeGUIDconfig();
            }
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
        }

        private void drawGUIDconfig()
        {
            GUI.skin = HighLogic.Skin;
            windowPosD = GUILayout.Window(2131, windowPosD, WindowGUIDconfig, "Direction");
        }

        public void Directionwindow()
        {
            RenderingManager.AddToPostDrawQueue(2114, new Callback(drawGUIDconfig));
        }



        public void closeGUIDconfig()
        {
            RenderingManager.RemoveFromPostDrawQueue(2114, new Callback(drawGUIDconfig));
        }

        public void setdirection(int direction)
        {
            switch (direction)
            {
                case 0:
                    xax = 0;
                    yay = 2;
                    break;
                case 1:
                    xax = 0;
                    yay = 2;

                    break;
                case 2:
                    xax = 0;
                    yay = 1;
                    break;
                case 3:
                    xax = 0;
                    yay = 1;
                    break;
                case 4:
                    xax = 1;
                    yay = 2;
                    break;
                case 5:
                    xax = 1;
                    yay = 2;
                    break;
            }
        }

        ////throttles functions

        public void ThrottleExecute()
        {
            //retrieve Controls input if aplicalbe
            try
            {
                retrieveControlInput(Throttle1ControlDes, 0);

                retrieveControlInput(Throttle2ControlDes, 1);

                retrieveControlInput(Throttle3ControlDes, 2);

                retrieveControlInput(Throttle4ControlDes, 3);
            }
            catch
            {
                print("[Davon TC systems] unknown error: customized controls");
            }

            //set engines
            Throttle1 = Throttleslider1;
            Throttle2 = Throttleslider2;
            Throttle3 = Throttleslider3;
            Throttle4 = Throttleslider4;
            foreach (DifferentialThrustEngineModule dtm in TCEngineList)
            {
                if (dtm.isolated == false)
                {
                    switch (dtm.throttleSelect)
                    {
                        case 1:
                            dtm.THRAIM = Throttle1;
                            break;
                        case 2:
                            dtm.THRAIM = Throttle2;
                            break;
                        case 3:
                            dtm.THRAIM = Throttle3;
                            break;
                        case 4:
                            dtm.THRAIM = Throttle4;
                            break;
                    }
                }
            }
        }

        private void retrieveControlInput(string ControlDes, int throttlenumber)
        {
            if (!(ControlDes == ""))
            {
                string[] SplitArray = ControlDes.Split('>');
                if (SplitArray[0] == "keys")
                {

                    if (UnityEngine.Input.GetKey(SplitArray[1]))
                    {
                        float newThrottle;
                        switch (throttlenumber)
                        {
                            case 0:
                                newThrottle = (float)(Throttleslider1 + (50 * Time.deltaTime));
                                if (newThrottle < 100)
                                {
                                    Throttleslider1 = newThrottle;
                                }
                                else
                                {
                                    Throttleslider1 = 100;
                                }
                                break;
                            case 1:
                                newThrottle = (float)(Throttleslider2 + (50 * Time.deltaTime));
                                if (newThrottle < 100)
                                {
                                    Throttleslider2 = newThrottle;
                                }
                                else
                                {
                                    Throttleslider2 = 100;
                                }
                                break;
                            case 2:
                                newThrottle = (float)(Throttleslider3 + (50 * Time.deltaTime));
                                if (newThrottle < 100)
                                {
                                    Throttleslider3 = newThrottle;
                                }
                                else
                                {
                                    Throttleslider3 = 100;
                                }
                                break;
                            case 3:
                                newThrottle = (float)(Throttleslider4 + (50 * Time.deltaTime));
                                if (newThrottle < 100)
                                {
                                    Throttleslider4 = newThrottle;
                                }
                                else
                                {
                                    Throttleslider4 = 100;
                                }
                                break;
                        }
                    }

                    if (UnityEngine.Input.GetKey(SplitArray[2]))
                    {
                        float newThrottle;
                        switch (throttlenumber)
                        {
                            case 0:
                                newThrottle = (float)(Throttleslider1 - (50 * Time.deltaTime));
                                if (newThrottle > 0)
                                {
                                    Throttleslider1 = newThrottle;
                                }
                                else
                                {
                                    Throttleslider1 = 0;
                                }
                                break;
                            case 1:
                                newThrottle = (float)(Throttleslider2 - (50 * Time.deltaTime));
                                if (newThrottle > 0)
                                {
                                    Throttleslider2 = newThrottle;
                                }
                                else
                                {
                                    Throttleslider2 = 0;
                                }
                                break;
                            case 2:
                                newThrottle = (float)(Throttleslider3 - (50 * Time.deltaTime));
                                if (newThrottle > 0)
                                {
                                    Throttleslider3 = newThrottle;
                                }
                                else
                                {
                                    Throttleslider3 = 0;
                                }
                                break;
                            case 3:
                                newThrottle = (float)(Throttleslider4 - (50 * Time.deltaTime));
                                if (newThrottle > 0)
                                {
                                    Throttleslider4 = newThrottle;
                                }
                                else
                                {
                                    Throttleslider4 = 0;
                                }
                                break;
                        }
                    }
                }
                else if (SplitArray[0] == "joys")
                {
                    float axisvalue;

                    int invertor = SplitArray[2] == "False" ? 1 : -1;
                    if (SplitArray[3] == "False")
                    {
                        axisvalue = (UnityEngine.Input.GetAxis(SplitArray[1]) * invertor + 1) / 2 * 100;
                    }
                    else
                    {
                        axisvalue = Math.Max(UnityEngine.Input.GetAxis(SplitArray[1]) * invertor, 0) * 100;
                    }

                    switch (throttlenumber)
                    {
                        case 0:
                            Throttleslider1 = axisvalue;
                            break;
                        case 1:
                            Throttleslider2 = axisvalue;
                            break;
                        case 2:
                            Throttleslider3 = axisvalue;
                            break;
                        case 3:
                            Throttleslider4 = axisvalue;
                            break;
                    }
                }
            }
        }

        ////Center thrust functions
        /*These functions manages the center thrust functionality. Since the code maybe hard 
        to follow this is how it basicly works.
        Establish the direction of angular momentum, draw a line true center of mass (CoM).
        Create a line perpendicular to this line. Let's call this new line the balance line, 
        it divides the plain into two areas, one with overthrust and one with underthrust. 
        It first lookes in the underthrust area for an engine which is has been previously 
        tuned down. If it can't find such an engine it looks for an engine in the overthrust 
        area. It always selects the engine with largest distance to the balance line. 
        A small adjustment is then made to this engine.*/
        
        
        //run each physics cycle
        public void centerThrustInSim()
        {
            CoM = vessel.ReferenceTransform.InverseTransformPoint(vessel.CoM + vessel.rb_velocity * Time.deltaTime + 0.5 * vessel.acceleration * Time.deltaTime * Time.deltaTime);

            updateEngineList();

            for (int i = 0; i < 10; i++)
            {
                adjustInSim();
            }
        }

        private void makeSimulatedEngineList()
        {
            simulatedEngineList.Clear();
            foreach (Part p in EngineList)
            {
                bool partadded = false;
                foreach (PartModule pm in p.Modules)
                {
                    
                    if (partadded == false && (pm.ClassName == "ModuleEngines" || pm.ClassName == "ModuleEnginesFX"))
                    {
                        simulatedEngine engine = new simulatedEngine();
                        engine.enginepart = p;

                        if (pm.ClassName == "ModuleEngines")
                        {
                            engine.enginemoduletype = 0;
                            engine.ModEng = (ModuleEngines)pm;
              
                        }
                        else if (pm.ClassName == "ModuleEnginesFX")
                        {
                            engine.enginemoduletype = 1;
                            engine.ModEngFX = (ModuleEnginesFX)pm;
                        }

                        foreach (PartModule pmt in p.Modules)
                        {
                            if (pmt.ClassName == "DifferentialThrustEngineModule")
                            {
                                engine.hasEngineModule = true;
                                engine.DifMod = (DifferentialThrustEngineModule)pmt;
                                engine.enginemoduletype = engine.DifMod.enginemoduletype;
                                if (engine.enginemoduletype == 2)
                                {
                                    engine.MultiMod = engine.DifMod.PartmoduleMultiModeEngine;
                                }
                            }
                        }
                        simulatedEngineList.Add(engine);
                        partadded = true;
                    }
                }
            }
        }

        private void updateEngineList()
        {
            try
            {
                foreach (simulatedEngine simE in simulatedEngineList)
                {
                    simE.update(xax, yay, CoM);
                }
            }
            catch
            {
                makeSimulatedEngineList();
            }
        }
        private void adjustInSim()
        {
            float CoTX = findCoTInSim(0);
            float CoTY = findCoTInSim(1);

            evaluateLastAdjust(CoTX, CoTY);

            //print("CoTX " + CoTX);
            //print("CoTY " + CoTY);

            simulatedEngine EngSelect;
            bool thrustArea = false; //false is underthrust, true is overthrust
            EngSelect = findengineInSim(CoTX, CoTY, thrustArea);
            if (EngSelect == null)
            {
                thrustArea = true;
                EngSelect = findengineInSim(CoTX, CoTY, thrustArea);
            }

            //set engine
            if (EngSelect != null)
            {
                if (thrustArea)
                {
                    EngSelect.DifMod.aim = EngSelect.DifMod.aim - adjustmentSize;
                    if (EngSelect.DifMod.aim <= EngSelect.aimlowest) { EngSelect.DifMod.aim = EngSelect.aimlowest; } 
                }
                else
                {
                    EngSelect.DifMod.aim = EngSelect.DifMod.aim + adjustmentSize;
                    if (EngSelect.DifMod.aim >= EngSelect.aimhighest) { EngSelect.DifMod.aim = EngSelect.aimhighest; }
                }
                previousCoTX = CoTX;
                previousCoTY = CoTY;
            }
        }

        //find CoT for a particular direction X or Y
        private float findCoTInSim(int direction)
        {
            //Datum lies 1000 meter before Com

            bool skip;
            float distance;
            float simulationfactor;
            float SumMoments = 0f;
            float SumThrusts = 0f;

            foreach (simulatedEngine simE in simulatedEngineList)
            {

                if (direction == 0)
                {
                    distance = simE.distanceX;
                }
                else
                {
                    distance = simE.distanceY;
                }

                skip = false;
                if (simE.hasEngineModule == true)
                {
                    if (simE.DifMod.CenterThrustMode == "ignore") { skip = true; }

                    //don't divide by zero
                    if (simE.DifMod.CenterThrust)
                    {
                        simulationfactor = (simE.DifMod.aim / simE.thrustPercentage);
                        if (simE.thrustPercentage <= 0)
                        {
                            simulationfactor = 1;
                            skip = true;
                        }
                    }
                    else
                    {
                        simulationfactor = 1;
                    }
                }
                else
                {
                    simulationfactor = 1;
                }
                
                if (skip == false)
                {
                    SumMoments = SumMoments + ((distance + 1000) * simE.measuredThrust * simulationfactor);
                    SumThrusts = SumThrusts + (simE.measuredThrust * simulationfactor);
                }
            }
            if (SumThrusts == 0) { return 0; }
            return SumMoments / SumThrusts - 1000;
        }

        private simulatedEngine findengineInSim(float AngX, float AngY, bool thrustArea)
        {
            //COTy/COTx is line through origin (constant aAng)
            float aAng = AngY / AngX;
            //(COTy/COTx)*-1 Balance line (constant aBal)
            float aBal = -AngX / AngY;

            //print("aAng " + aAng);
            //print("aBal " + aBal);

            //AngY being positive significe the area above the line has overthrust
            bool area;
            if (AngY > 0 == thrustArea)
            {
                area = true;
            }
            else
            {
                area = false;
            }

            float DisEngBal = 0;
            simulatedEngine Selected = null;

            //engine selection
            foreach (simulatedEngine simE in simulatedEngineList)
            {
                if (simE.engineactive && simE.hasEngineModule && simE.DifMod.CenterThrust && !simE.throttleLocked)
                {
                    if ((simE.DifMod.aim > simE.aimlowest || thrustArea == false) && (simE.DifMod.aim < simE.aimhighest || thrustArea == true))
                    {

                        //print("cEngX " + cEngX);
                        //print("cEngY " + cEngY);

                        //check if engine lies in overthrust area
                        if ((simE.distanceY > simE.distanceX * aBal) == area)
                        {
                            //print("cEngX " + cEngX);
                            //print("cEngY " + cEngY);						
                            //construct line through engine point perpendicular to balance line. aAng is the constant describing the line from the angular momentum through CoM.
                            //cAeng and cBeng are the constants a and b in: ax + b = y
                            float cAeng = aAng;
                            float cBeng = simE.distanceY - simE.distanceX * aAng;

                            //print("cAeng " + cAeng);
                            //print("cBeng " + cBeng);
                            //calculate intercept between balance line and this line
                            float cPointX = -cBeng / (cAeng - aBal);
                            float cPointY = cPointX * aBal;
                            //print("cPointX " + cPointX);
                            //print("cPointY " + cPointY);						
                            //distance from engine to intercept
                            float cDisEngBal = (float)Math.Sqrt(Math.Pow((simE.distanceX - cPointX), 2.0) + Math.Pow((simE.distanceY - cPointY), 2.0));
                            //print("in " + cDisEngBal);
                            //if distance of checked engine is greater than engine then select this one							
                            if (cDisEngBal > DisEngBal)
                            {
                                DisEngBal = cDisEngBal;
                                Selected = simE;

                                //print("DisEngBal " + DisEngBal);

                            }
                        }
                    }
                }
            }
            if (Selected != null)
            {
                return Selected;
            }
            else
            {
                return null;
            }
        }

        private void evaluateLastAdjust(float currentCoTX, float currentCoTY)
        {
            float disPrev = (float)Math.Sqrt(Math.Pow(previousCoTX, 2.0) + Math.Pow(previousCoTY, 2.0));
            float disCurr = (float)Math.Sqrt(Math.Pow(currentCoTX, 2.0) + Math.Pow(currentCoTY, 2.0));
            float disBetw = (float)Math.Sqrt(Math.Pow(previousCoTX - currentCoTX, 2.0) + Math.Pow(previousCoTY - currentCoTY, 2.0));

                if (adjustmentSize > 0.1 && (disBetw > 0.2 * disPrev /*|| disCurr < 0.75 * disPrev*/))
                {
                    adjustmentSize = adjustmentSize / 2f;
                    if (adjustmentSize < 0.1) {adjustmentSize = 0.1f;}
                    //print("down " + adjustmentSize);
                }
                if (adjustmentSize < 10 && (disBetw < 0.1 * disPrev /*&& disCurr > 0.75 * disPrev*/))
                {
                    adjustmentSize = adjustmentSize * 1.5f;
                    if (adjustmentSize > 10) { adjustmentSize = 10f;}
                    //print("--up " + adjustmentSize); 
                }
        }

        //
        //
        //
        //
    }






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
        public void update(int xax, int yay, Vector3 CoM)
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
                distanceX = distance / ModEng.thrustTransforms.Count();

                //print(distanceX);

                distance = 0.0f;
                foreach (Transform tr in ModEng.thrustTransforms)
                {
                    distance = distance + (enginepart.vessel.ReferenceTransform.InverseTransformPoint(tr.position)[yay] - CoM[yay]);
                }
                distanceY = distance / ModEng.thrustTransforms.Count();

                //print(distanceY);
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
                distanceX = distance / ModEngFX.thrustTransforms.Count();

                distance = 0.0f;
                foreach (Transform tr in ModEngFX.thrustTransforms)
                {
                    distance = distance + (enginepart.vessel.ReferenceTransform.InverseTransformPoint(tr.position)[yay] - CoM[yay]);
                }
                distanceY = distance / ModEngFX.thrustTransforms.Count();
            }
        }
    }




















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


