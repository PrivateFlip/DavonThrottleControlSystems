//Written by Flip van Toly for KSP community
//License GPL v2.0 (GNU General Public License)
// Namespace Declaration 
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using UnityEngine;
using KSP.UI.Screens;

namespace DifferentialThrustMod
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
            //nothing
        }

        void onDestroy()
        {
            if (toolBarButton != null) { removeToolbarButton(); }
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
            //Toolbar button
            updateToolBar();
        }

        private void OnDraw()
        {
            updateToolBar();
        }

        private void updateToolBar()
        {

            if (HighLogic.LoadedScene != GameScenes.FLIGHT || (lastUpdateToolBarTime + 2) > Planetarium.GetUniversalTime() || !ApplicationLauncher.Ready) { return; }

            if (toolBarButton == null && hasDifferentialThrustModule()) { addToolbarButton(); }
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
                    if (pm is DifferentialThrust)
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
                    if (pm is DifferentialThrust)
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
                    if (pm is DifferentialThrust)
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
                    if (pm is DifferentialThrust)
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
}
