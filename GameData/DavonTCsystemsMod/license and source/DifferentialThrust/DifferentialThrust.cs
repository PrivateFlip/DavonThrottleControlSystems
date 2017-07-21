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
        public int CenterThrustDirection = 0;

        private int xax = 0;
        private int yay = 2;
        private bool xaxi = true;
        private bool yayi = false;

        private Vector3 CoM;
        private List<simulatedEngine> simulatedEngineList = new List<simulatedEngine>();

        private float adjustmentSize = 0.1f;

        //simulatedEngine previousAdjustEngine;
        //private float previousAdjust;
        private float previousCoTX;
        private float previousCoTY;

        [KSPField(isPersistant = false, guiActive = false)]
        public bool ThrottleSteeringToggle = false;
        private float input0 = 0f;
        private float input1 = 0f;
        private float throttleSteeringTorque = 10f;

        [KSPField(isPersistant = true, guiActive = false)]
        public string savedEngCon = "0";
        public double lastEngineConfigurationSaveTime = 0;
        public bool loadedEngineSettings = false;

        //auto on variables
        public double plannedReactivationTime = 0.0;

        //GUI variables
        private bool renderGUIM = false;
        private bool renderGUIC = false;
        private bool renderGUIT = false;
        private bool renderGUID = false;
        private bool renderGUIS = false;
        private bool renderGUIJ = false;

        protected Rect windowPosM = new Rect(50, 50, 200, 200);
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
        public string strAdjStr = "10";
        [KSPField(isPersistant = true, guiActive = false)]
        public int selOnOffGridInt = 0;
        [KSPField(isPersistant = true, guiActive = false)]
        public bool boolThrSte = false;
        [KSPField(isPersistant = true, guiActive = false)]
        public string strTorque = "10";

        //Direction (GUI)
        [KSPField(isPersistant = true, guiActive = false)]
        int selDirGridInt = 0;

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

        private void OnGUI()
        {
            if (Event.current.type == EventType.Repaint || Event.current.isMouse)
            {
                // preDraw code
            }
            drawGUI();
        }

        private void drawGUI()
        {
            // GUI rendering
            if (renderGUIM)
            {
                drawGUIMconfig();
            }
            if (renderGUIC)
            {
                drawGUICconfig();
            }
            if (renderGUIT)
            {
                drawGUITconfig();
            }
            if (renderGUID)
            {
                drawGUIDconfig();
            }
            if (renderGUIS)
            {
                drawGUISconfig();
            }
            if (renderGUIJ)
            {
                drawGUIJconfig();
            }
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
                    if (pm is DifferentialThrust && p.flightID != part.flightID)
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
                    if (added == false && (pm is ModuleEngines || pm is MultiModeEngine || pm is ModuleEnginesFX))
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
                    if (pm is DifferentialThrustEngineModule)
                    {
                        dontadd = true;
                        TCEngineList.Add((DifferentialThrustEngineModule)pm);
                    }

                    //check if SRB
                    if (pm is ModuleEngines)
                    {
                        ModuleEngines cModuleEngines;
                        cModuleEngines = p.Modules.OfType<ModuleEngines>().FirstOrDefault();
                        if (cModuleEngines.throttleLocked == false) { couldadd = true; };
                    }
                    //check if SRB
                    if (pm is ModuleEnginesFX)
                    {
                        ModuleEnginesFX cModuleEnginesFX;
                        cModuleEnginesFX = p.Modules.OfType<ModuleEnginesFX>().FirstOrDefault();
                        if (cModuleEnginesFX.throttleLocked == false) { couldadd = true; };
                    }
                }

                if (dontadd == false && couldadd == true)
                {
                    p.AddModule(typeof(DifferentialThrustEngineModule).Name);
                    //and add to list
                    foreach (PartModule pmt in p.Modules)
                    {
                        //check if already added
                        if (pmt is DifferentialThrustEngineModule)
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
                ThrottleSteeringToggle = boolThrSte;
                float.TryParse(strTorque, out throttleSteeringTorque);
                setDirection(CenterThrustDirection);
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
                    if (pm is DifferentialThrustEngineModule)
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
                                //print(sti);

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
                    if (pm is ModuleCommand)
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
        [KSPEvent(name = "Moduleconfig", isDefault = false, guiActive = true, guiName = "TC systems")]
        public void Moduleconfig()
        {
            if (!isActive) { Activate(); }
            openGUIMconfig();
        }

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
            windowPosM = GUILayout.Window(14132, windowPosM, WindowGUIMconfig, "Module");
        }

        public void openGUIMconfig()
        {
            renderGUIM = true;
        }

        public void closeGUIMconfig()
        {
            renderGUIM = false;
        }


        /// <summary>
        /// Module throttle
        /// </summary>

        public void Throttleconfig()
        {
            openGUITconfig();
        }

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

        public void openGUITconfig()
        {
            renderGUIT = true;
        }

        public void closeGUITconfig()
        {
            renderGUIT = false;
        }


        /// <summary>
        /// Module throttle controls
        /// </summary>
        public void joystickconfig()
        {
            openGUIJconfig();
        }

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

        public void openGUIJconfig()
        {
            renderGUIJ = true;
        }

        public void closeGUIJconfig()
        {
            renderGUIJ = false;
        }




        /// <summary>
        /// Profile Window
        /// </summary>
        public void ProfileWindow()
        {
            GamePath = System.IO.Directory.GetParent(Application.dataPath);
            updateProfilesList();
            openGUISconfig();
        }

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
            if (GUILayout.Button("Save", GUILayout.Width(80)))
            {
                saveprofile(StrProfName);
                updateProfilesList();
                closeGUISconfig();
            }

            if (GUILayout.Button("Load", GUILayout.Width(80)))
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
            string[] data = new string[11];

            SaveEngineSettings();
            data[0] = savedEngCon;

            data[1] = CenterThrustDirection.ToString();

            data[2] = selEngGridInt.ToString();
            data[3] = strAdjStr;
            data[4] = selOnOffGridInt.ToString();
            data[5] = Throttle1ControlDes;
            data[6] = Throttle2ControlDes;
            data[7] = Throttle3ControlDes;
            data[8] = Throttle4ControlDes;
            data[9] = boolThrSte.ToString();
            data[10] = strTorque;

            System.IO.File.WriteAllLines(GamePath + ProfilesPath + profile, data);
        }

        public void loadprofile(string profile)
        {
            string[] data = System.IO.File.ReadAllLines(GamePath + ProfilesPath + profile);

            savedEngCon = data[0];
            LoadEngineSettings();

            CenterThrustDirection = Convert.ToInt32(data[1]);
            setDirection(CenterThrustDirection);
            selDirGridInt = CenterThrustDirection;

            selEngGridInt = Convert.ToInt32(data[2]);
            strAdjStr = data[3];
            selOnOffGridInt = Convert.ToInt32(data[4]);
            CenterThrustToggle = (selOnOffGridInt == 1);
            UpdateCenterThrust();

            Throttle1ControlDes = data[5];
            Throttle2ControlDes = data[6];
            Throttle3ControlDes = data[7];
            Throttle4ControlDes = data[8];

            boolThrSte = (data[9] == "True");
            CenterThrustToggle = boolThrSte;
            strTorque = data[10];
            float.TryParse(strTorque, out throttleSteeringTorque);
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

        public void openGUISconfig()
        {
            renderGUIS = true;
        }

        public void closeGUISconfig()
        {
            renderGUIS = false;
        }

        /// <summary>
        /// Center thrust config window
        /// </summary>
        public void CenterThrust()
        {
            openGUICconfig();
        }

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

            boolThrSte = GUILayout.Toggle(boolThrSte, "Throttle Steering");
            if (boolThrSte != ThrottleSteeringToggle)
            {
                ThrottleSteeringToggle = boolThrSte;
            }

            if (boolThrSte)
            {
                GUILayout.Label("Torque:", GUILayout.Width(55));

                GUILayout.BeginHorizontal();
                strTorque = GUILayout.TextField(strTorque, 5, GUILayout.Width(50));
                GUILayout.Label("kN*m", GUILayout.Width(35));
                if (GUILayout.Button("set", GUILayout.Width(30), GUILayout.Height(22)))
                {
                    float.TryParse(strTorque, out throttleSteeringTorque);
                }
                GUILayout.EndHorizontal();
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

        public void openGUICconfig()
        {
            renderGUIC = true;
        }

        public void closeGUICconfig()
        {
            renderGUIC = false;
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

                setDirection(CenterThrustDirection);

                makeSimulatedEngineList();
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
        }

        /// <summary>
        /// Direction config window
        /// </summary>
        public void Directionwindow()
        {
            openGUIDconfig();
        }

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

        public void openGUIDconfig()
        {
            renderGUID = true;
        }

        public void closeGUIDconfig()
        {
            renderGUID = false;
        }

        public void setDirection(int direction)
        {
            switch (direction)
            {
                case 0:
                    xax = 0;
                    yay = 2;
                    xaxi = false;
                    yayi = true;
                    break;
                case 1:
                    xax = 0;
                    yay = 2;
                    xaxi = true;
                    yayi = false;
                    break;
                case 2:
                    xax = 0;
                    yay = 1;
                    xaxi = false;
                    yayi = true;
                    break;
                case 3:
                    xax = 0;
                    yay = 1;
                    xaxi = true;
                    yayi = false;
                    break;
                case 4:
                    xax = 1;
                    yay = 2;
                    xaxi = true;
                    yayi = false;
                    break;
                case 5:
                    xax = 1;
                    yay = 2;
                    xaxi = false;
                    yayi = true;
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
            CoM = vessel.ReferenceTransform.InverseTransformPoint(vessel.CoM);

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

                    if (partadded == false && (pm is ModuleEngines || pm is ModuleEnginesFX))
                    {
                        simulatedEngine engine = new simulatedEngine();
                        engine.enginepart = p;

                        if (pm is ModuleEngines)
                        {
                            engine.enginemoduletype = 0;
                            engine.ModEng = (ModuleEngines)pm;

                        }
                        else if (pm is ModuleEnginesFX)
                        {
                            engine.enginemoduletype = 1;
                            engine.ModEngFX = (ModuleEnginesFX)pm;
                        }

                        foreach (PartModule pmt in p.Modules)
                        {
                            if (pmt is DifferentialThrustEngineModule)
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
                    //print("c" + xax + " " + yay + " " + xaxi + " " + yayi);
                    simE.update(xax, xaxi, yay, yayi, CoM);
                    //print("name" + simE.enginepart.name);
                    //print("X" + simE.distanceX);
                    //print("Y" + simE.distanceY);
                    //print("a" + xax + " " + yay + " " + xaxi + " " + yayi);
                }
            }
            catch
            {
                makeSimulatedEngineList();
            }
            if (ThrottleSteeringToggle)
            {
                //print("a" + xax + " " + yay  + " " + xaxi + " " + yayi);
                input0 = getVesselInput(yay);
                input1 = getVesselInput(xax);
                //print("b" + xax + " " + yay + " " + xaxi + " " + yayi);
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
            float distance = 0f;
            float input = 0f;
            float simulationfactor;
            float SumMoments = 0f;
            float SumThrusts = 0f;

            foreach (simulatedEngine simE in simulatedEngineList)
            {

                if (direction == 0)
                {
                    distance = simE.distanceX;
                    input = input0;
                }
                else
                {
                    distance = simE.distanceY;
                    input = input1;
                }

                skip = false;
                if (simE.hasEngineModule == true)
                {
                    if (simE.DifMod.CenterThrustMode == "ignore") { skip = true; }


                    if (simE.DifMod.CenterThrust)
                    {
                        simulationfactor = (simE.DifMod.aim / simE.thrustPercentage);
                        //don't divide by zero
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

            if (ThrottleSteeringToggle)
            {
                if (input > 0) 
                { SumMoments = SumMoments + ((1 + 1000) * input * throttleSteeringTorque); }
                if (input < 0) { SumMoments = SumMoments + ((-1 + 1000) * -input * throttleSteeringTorque); }
                SumThrusts = SumThrusts + (Math.Abs(input) * throttleSteeringTorque);
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
                if (adjustmentSize < 0.1) { adjustmentSize = 0.1f; }
                //print("down " + adjustmentSize);
            }
            if (adjustmentSize < 10 && (disBetw < 0.1 * disPrev /*&& disCurr > 0.75 * disPrev*/))
            {
                adjustmentSize = adjustmentSize * 1.5f;
                if (adjustmentSize > 10) { adjustmentSize = 10f; }
                //print("--up " + adjustmentSize); 
            }
        }

        private float getVesselInput(int direction)
        {
            //setDirection
            switch(direction)
            {
                case 0:
                    return (vessel.ctrlState.pitch);
                case 1:
                    return (vessel.ctrlState.roll);
                case 2:
                    return (vessel.ctrlState.yaw);
                default:
                    return (0f);
            }
        }

        //
        //
        //
        //
    }

}