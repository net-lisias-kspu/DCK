/*Copywrite © 2018, DoctorDavinci
 * 
 * 
 * All code used from the Cloaking Device mod has been absorbed into this code via
 * one-way compatibility from CC BY-SA 4.0 to GPLv3 and is released as such
 * <https://creativecommons.org/2015/10/08/cc-by-sa-4-0-now-one-way-compatible-with-gplv3/>
 * 

 Attribution and previous license.....
--------------------------------------------------------------------------------------------------
 * Copyright © 2016, wasml
 Licensed under the Attribution-ShareAlike 4.0 (CC BY-SA 4.0)
 creative commons license. See <https://creativecommons.org/licenses/by-nc-sa/4.0/>
 for full details.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE
FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL
DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR
SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY,
OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
--------------------------------------------------------------------------------------------------
*/
using System;
using System.Collections.Generic;
using System.Linq;
using BDArmory;
using BDArmory.Radar;
using BDArmory.Parts;
using UnityEngine;

namespace DCKinc
{
    public class ModuleDCKACS : PartModule
    {
        private static float UNCLOAKED = 1.0f;
        private static float RENDER_THRESHOLD = 0.25f;
        private static string modTag = "[ModuleDCKACS]";

        private float fadePerTime = 0.5f;
        private bool currentShadowState = true;

        [KSPField(isPersistant = true)]
        public bool resourceAvailable;

        [KSPField(isPersistant = true, guiActive = false, guiActiveEditor = false)]/*,
         UI_Toggle(controlEnabled = true, scene = UI_Scene.All, disabledText = "Off", enabledText = "On")]*/
        public bool fullRenderHide = true;

        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "Surface Area", guiFormat = "F1")]
        private float surfaceAreaToCloak = 0.0f;

        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "EC Used", guiFormat = "F1")]
        private float RequiredEC = 0.0f;

        [KSPField(isPersistant = false)]
        private bool recalcCloak = true;

        [KSPField(isPersistant = true, guiActiveEditor = false, guiActive = false)]/*, guiName = "Cloak"),
         UI_Toggle(controlEnabled = true, scene = UI_Scene.All, disabledText = "Off", enabledText = "On")]*/
        public bool cloakOn = false;

        [KSPField(isPersistant = true)]
        public float visiblilityLevel = UNCLOAKED;

        [KSPField(isPersistant = true, guiActive = false, guiActiveEditor = false)]/*,
         UI_FloatRange(controlEnabled = true, scene = UI_Scene.All, minValue = 0.0f, maxValue = 3.0f, stepIncrement = 0.1f)]*/
        public float areaExponet = 0.5f;
        
        [KSPField(isPersistant = true, guiActive = false, guiActiveEditor = false)]/*,
         UI_FloatRange(controlEnabled = true, scene = UI_Scene.All, minValue = 0.0f, maxValue = 1000.0f)]*/
        public float ECPerSec = 1.0f; // Electric charge per second
        
        [KSPField(isPersistant = true, guiActive = false, guiActiveEditor = false)]/*, guiUnits = "sec")],
         UI_FloatRange(controlEnabled = true, scene = UI_Scene.All, minValue = 0.0f, maxValue = 30.0f)]*/
        public float fadeTime = 1.0f; // In seconds
        
        [KSPField(isPersistant = true, guiActive = false, guiActiveEditor = false)]/*,
         UI_FloatRange(controlEnabled = true, scene = UI_Scene.All, minValue = 0.0f, maxValue = 1.0f, stepIncrement = 0.05f)]*/
        public float maxfade = 0.4f; // invisible:0 to uncloaked:1
        
        [KSPField(isPersistant = true, guiActive = false, guiActiveEditor = false)]/*,
         UI_FloatRange(controlEnabled = true, scene = UI_Scene.All, minValue = 0.0f, maxValue = 1.0f, stepIncrement = 0.1f)]*/
        public float shadowCutoff = 0.0f;

        [KSPField(isPersistant = true, guiActiveEditor = false, guiActive = false)]/*, guiName = "Self Cloak"),
         UI_Toggle(controlEnabled = true, scene = UI_Scene.All, disabledText = "Off", enabledText = "On")]*/
        public bool selfCloak = true;

        //---------------------------------------------------------------------

        [KSPAction("Cloak Toggle")]
        public void actionToggleCloak(KSPActionParam param)
        {
            cloakOn = !cloakOn;
            UpdateCloakField(null, null);
        }

        [KSPAction("Cloak On")]
        public void actionCloakOn(KSPActionParam param)
        {
            cloakOn = true;
            UpdateCloakField(null, null);
        }

        [KSPAction("Cloak Off")]
        public void actionCloakOff(KSPActionParam param)
        {
            cloakOn = false;
            UpdateCloakField(null, null);
        }

        //---------------------------------------------------------------------

        private void ScreenMsg(string msg)
        {
            ScreenMessages.PostScreenMessage(new ScreenMessage(msg, 3, ScreenMessageStyle.UPPER_CENTER));
        }

        private void ScreenMsg2(string msg)
        {
            ScreenMessages.PostScreenMessage(new ScreenMessage(msg, 3, ScreenMessageStyle.LOWER_CENTER));
        }


        public override string GetInfo()
        {
            // Editor "More Info" display
            string st;
            st = "Fade/sec: " + fadePerTime.ToString("F2") + "\n" +
                 "EC = Area * Fade% * " + ECPerSec.ToString("F2") + " ^ " + areaExponet.ToString("F2");
            return st;
        }

        //---------------------------------------------------------------------

        public override void OnStart(StartState state)
        {
            GameEvents.onVesselWasModified.Add(ReconfigureEvent);
            recalcSurfaceArea();
        }

        public override void OnUpdate()
        {
            checkresourceAvailable();
            BDAcJammerCheck();
            if (cloakOn)
            {
                BDAcJammerRCS0();
                drawEC();
                radarOff();
            }
            else
            {
                BDAcJammerRCS1();
            }

            if (IsTransitioning())
            {
                recalcCloak = false;
                calcNewCloakLevel();

                foreach (Part p in vessel.parts)
                    if (selfCloak || (p != part))
                    {
                        p.SetOpacity(visiblilityLevel);
                        SetRenderAndShadowStates(p, visiblilityLevel > shadowCutoff, visiblilityLevel > RENDER_THRESHOLD);
                    }
            }
        }

        public void OnDestroy()
        {
            GameEvents.onVesselWasModified.Remove(ReconfigureEvent);
        }

        /// <summary>
        /// Resources
        /// </summary>
        /// 
        protected void drawEC()
        {
            RequiredEC = Time.deltaTime * (1 - visiblilityLevel) * (float)Math.Pow(surfaceAreaToCloak * ECPerSec, areaExponet);

            float AcquiredEC = part.RequestResource("ElectricCharge", RequiredEC);
            if (AcquiredEC < RequiredEC * 0.8f)
            {
                BDAcJammerDisable();
                ScreenMsg("Not Enough Electrical Charge");
                disengageCloak();
            }

            foreach (var p in vessel.parts)
            {
                double totalAmount = 0;
                double maxAmount = 0;
                PartResource r = p.Resources.Where(n => n.resourceName == "ElectricCharge").FirstOrDefault();
                if (r != null)
                {
                    totalAmount += r.amount;
                    maxAmount += r.maxAmount;
                    if (totalAmount < maxAmount * 0.1)
                    {
                        BDAcJammerDisable();
                        ScreenMsg("Not Enough Electrical Charge");
                        disengageCloak();
                    }
                }
            }
        }

        private void checkresourceAvailable()
        {
            foreach (var p in vessel.parts)
            {
                double totalAmount = 0;
                double maxAmount = 0;
                PartResource r = p.Resources.Where(n => n.resourceName == "ElectricCharge").FirstOrDefault();
                if (r != null)
                {
                    totalAmount += r.amount;
                    maxAmount += r.maxAmount;
                    if (totalAmount < maxAmount * 0.2)
                    {
                        resourceAvailable = false;
                    }
                    else
                    {
                        resourceAvailable = true;
                    }
                }
            }
        }

        /// <summary>
        /// BDAc Integration
        /// </summary>
        /// 
        public void underFireCheck()
        {
            List<MissileFire> wmParts = new List<MissileFire>(200);
            foreach (Part p in vessel.Parts)
            {
                wmParts.AddRange(p.FindModulesImplementing<MissileFire>());
            }
            foreach (MissileFire wmPart in wmParts)
            {
                if (wmPart.isFlaring || wmPart.underFire || wmPart.missileIsIncoming)
                {
                    BDAcJammerEnable();
                }
            }
        }

        public void BDAcJammerCheck()
        {
            List<ModuleECMJammer> jp = new List<ModuleECMJammer>(200);
            foreach (Part p in vessel.Parts)
            {
                jp.AddRange(p.FindModulesImplementing<ModuleECMJammer>());
            }
            foreach (ModuleECMJammer j in jp)
            {
                if (j.jammerEnabled && !cloakOn)
                {
                    engageCloak();
                }

                if (!j.jammerEnabled && cloakOn)
                {
                    disengageCloak();
                }
            }
        }

        public void BDAcJammerEnable()
        {
            List<ModuleECMJammer> wmParts = new List<ModuleECMJammer>(200);
            foreach (Part p in vessel.Parts)
            {
                wmParts.AddRange(p.FindModulesImplementing<ModuleECMJammer>());
            }
            foreach (ModuleECMJammer wmPart in wmParts)
            {
                if (!wmPart.jammerEnabled)
                {
                    wmPart.EnableJammer();
                }
            }
        }

        public void BDAcJammerDisable()
        {
            List<ModuleECMJammer> wmParts = new List<ModuleECMJammer>(200);
            foreach (Part p in vessel.Parts)
            {
                wmParts.AddRange(p.FindModulesImplementing<ModuleECMJammer>());
            }
            foreach (ModuleECMJammer wmPart in wmParts)
            {
                if (wmPart.jammerEnabled)
                {
                    wmPart.DisableJammer();
                }
            }
        }

        public void BDAcJammerRCS0()
        {
            List<ModuleECMJammer> wmParts = new List<ModuleECMJammer>(200);
            foreach (Part p in vessel.Parts)
            {
                wmParts.AddRange(p.FindModulesImplementing<ModuleECMJammer>());
            }
            foreach (ModuleECMJammer wmPart in wmParts)
            {
                wmPart.lockBreaker = true;
                wmPart.rcsReductionFactor = 0;
                wmPart.jammerStrength = 2000;
                wmPart.lockBreakerStrength = 2000;
            }
        }

        public void BDAcJammerRCS1()
        {
            List<ModuleECMJammer> wmParts = new List<ModuleECMJammer>(200);
            foreach (Part p in vessel.Parts)
            {
                wmParts.AddRange(p.FindModulesImplementing<ModuleECMJammer>());
            }
            foreach (ModuleECMJammer wmPart in wmParts)
            {
                wmPart.lockBreaker = false;
                wmPart.rcsReductionFactor = 1;
                wmPart.jammerStrength = 0;
                wmPart.lockBreakerStrength = 0;
            }
        }

        public void radarOn()
        {
            List<ModuleRadar> radarParts = new List<ModuleRadar>(200);
            foreach (Part p in vessel.Parts)
            {
                radarParts.AddRange(p.FindModulesImplementing<ModuleRadar>());
            }
            foreach (ModuleRadar radarPart in radarParts)
            {
                if (!radarPart.radarEnabled)
                {
                    radarPart.EnableRadar();
                }
            }
        }

        public void radarOff()
        {
            List<ModuleRadar> radarParts = new List<ModuleRadar>(200);
            foreach (Part p in vessel.Parts)
            {
                radarParts.AddRange(p.FindModulesImplementing<ModuleRadar>());
            }
            foreach (ModuleRadar radarPart in radarParts)
            {
                if (radarPart.radarEnabled)
                {
                    radarPart.DisableRadar();
                }
            }
        }


        /// <summary>
        /// Cloak code
        /// </summary>
        /// 
        public void engageCloak()
        {
            if (resourceAvailable)
            {
                ScreenMsg("Active Camouflage engaging");
                cloakOn = true;
                UpdateCloakField(null, null);
            }
            else
            {
                ScreenMsg("EC too low ... Active Camouflage unable to engage");
                BDAcJammerDisable();
            }
        }
        
        public void disengageCloak()
        {
            if (cloakOn)
            {
                ScreenMsg("Active Camouflage Disengaging");
                cloakOn = false;
                UpdateCloakField(null, null);
            }
        }

        protected void UpdateSelfCloakField(BaseField field, object oldValueObj)
        {
            if (selfCloak)
            {
                SetRenderAndShadowStates(part, visiblilityLevel > shadowCutoff, visiblilityLevel > RENDER_THRESHOLD);
            }
            else
            {
                SetRenderAndShadowStates(part, true, true);
            }
            recalcCloak = true;
        }

        protected void UpdateCloakField(BaseField field, object oldValueObj)
        {
            // Update in case its been changed
            calcFadeTime();
            recalcSurfaceArea();
            recalcCloak = true;
        }

        private void calcFadeTime()
        {
            // In case fadeTime == 0
            try
            { fadePerTime = (1 - maxfade) / fadeTime; }
            catch (Exception)
            { fadePerTime = 10.0f; }
        }

        private void recalcSurfaceArea()
        {
            Part p;

            if (vessel != null)
            {
                surfaceAreaToCloak = 0.0f;
                for (int i = 0; i < vessel.parts.Count; i++)
                {
                    p = vessel.parts[i];
                    if (p != null)
                        if (selfCloak || (p != part))
                            surfaceAreaToCloak = (float)(surfaceAreaToCloak + p.skinExposedArea);
                }
            }
        }

        private void SetRenderAndShadowStates(Part p, bool shadowsState, bool renderState)
        {
            if (p.gameObject != null)
            {
                int i;

                MeshRenderer[] MRs = p.GetComponentsInChildren<MeshRenderer>();
                for (i = 0; i < MRs.GetLength(0); i++)
                    MRs[i].enabled = renderState;// || !fullRenderHide;

                SkinnedMeshRenderer[] SMRs = p.GetComponentsInChildren<SkinnedMeshRenderer>();
                for (i = 0; i < SMRs.GetLength(0); i++)
                    SMRs[i].enabled = renderState;// || !fullRenderHide;

                if (shadowsState != currentShadowState)
                {
                    for (i = 0; i < MRs.GetLength(0); i++)
                    {
                        if (shadowsState)
                            MRs[i].shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                        else
                            MRs[i].shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    }
                    for (i = 0; i < SMRs.GetLength(0); i++)
                    {
                        if (shadowsState)
                            SMRs[i].shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                        else
                            SMRs[i].shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    }
                    currentShadowState = shadowsState;
                }
            }
        }

        private void ReconfigureEvent(Vessel v)
        {
            if (v == null) { return; }

            if (v == vessel)
            {   // This is the cloaking vessel - recalc EC required based on new configuration (unless this is a dock event)
                recalcCloak = true;
                recalcSurfaceArea();
            }
            else
            {   // This is the added/removed part - reset it to normal
                ModuleDCKACS mc = null;
                foreach (Part p in v.parts)
                    if ((p != null) &&
                        ((p != part) || selfCloak))
                    {
                        //p.setOpacity(UNCLOAKED); // 1.1.3
                        p.SetOpacity(UNCLOAKED); // 1.2.2 and up
                        SetRenderAndShadowStates(p, true, true);
                        Debug.Log(modTag + "Uncloak " + p.name);

                        // If the other vessel has a cloak device let it know it needs to do a refresh
                        mc = p.FindModuleImplementing<ModuleDCKACS>();
                        if (mc != null)
                            mc.recalcCloak = true;
                    }
            }
        }

        protected void calcNewCloakLevel()
        {
            calcFadeTime();
            float delta = Time.deltaTime * fadePerTime;
            if (cloakOn && (visiblilityLevel > maxfade))
                delta = -delta;

            visiblilityLevel = visiblilityLevel + delta;
            visiblilityLevel = Mathf.Clamp(visiblilityLevel, maxfade, UNCLOAKED);
        }

        protected bool IsTransitioning()
        {
            return (cloakOn && (visiblilityLevel > maxfade)) ||     // Cloaking in progress
                   (!cloakOn && (visiblilityLevel < UNCLOAKED)) ||  // Uncloaking in progress
                   recalcCloak;                                     // A forced refresh 
        }
    }
}
