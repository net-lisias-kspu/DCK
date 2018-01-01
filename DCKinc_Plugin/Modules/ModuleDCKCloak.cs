/*Copyright © 2016, wasml
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
*/
using System;
using UnityEngine;

namespace DCKinc
{
    // TODO: correct self cloak state when switched while cloaked
    //  Add option to disable renderer disabling

    public class ModuleDCKCloak : PartModule
    {
        private static float UNCLOAKED = 1.0f;
        private static float RENDER_THRESHOLD = 0.8f;
        private static string modTag = "[ModuleDCKCloak] ";

        private float fadePerTime = 0.5f;
        private bool currentShadowState = true;

        // If this is set true in the cfg file ECPerSec, areaExponet, maxfade, shadowCutoff and
        // fadeTime are tweakable in the editor and in flight
        [KSPField(isPersistant = true)]
        public bool sandboxMode = false;

        [KSPField(isPersistant = true)]
        public bool debugMode = false;

        [KSPField(isPersistant = true, guiActiveEditor = false),
         UI_Toggle(controlEnabled = true, scene = UI_Scene.All, disabledText = "Off", enabledText = "On")]
        public bool hideParticleEffects = false;

        [KSPField(isPersistant = true, guiActive = false, guiActiveEditor = false),
         UI_Toggle(controlEnabled = true, scene = UI_Scene.All, disabledText = "Off", enabledText = "On")]
        public bool fullRenderHide = true;

        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "Surface Area", guiFormat = "F1")]
        private float surfaceAreaToCloak = 0.0f;

        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "EC Used", guiFormat = "F1")]
        private float ECRequired = 0.0f;

        [KSPField(isPersistant = false)]
        private bool recalcCloak = true;

        [KSPField(isPersistant = true, guiActiveEditor = true, guiActive = true, guiName = "Cloak"),
         UI_Toggle(controlEnabled = true, scene = UI_Scene.All, disabledText = "Off", enabledText = "On")]
        public bool cloakOn = false;

        [KSPField(isPersistant = true)]
        public float visiblilityLevel = UNCLOAKED;

        [KSPField(isPersistant = true)]
        public bool legacyBehavior = false;

        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true),
         UI_FloatRange(controlEnabled = true, scene = UI_Scene.All, minValue = 0.0f, maxValue = 3.0f, stepIncrement = 0.1f)]
        public float areaExponet = 1.0f;
        
        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true),
         UI_FloatRange(controlEnabled = true, scene = UI_Scene.All, minValue = 0.0f, maxValue = 1000.0f)]
        public float ECPerSec = 1.0f; // Electric charge per second
        
        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiUnits = "sec"),
         UI_FloatRange(controlEnabled = true, scene = UI_Scene.All, minValue = 0.0f, maxValue = 30.0f)]
        public float fadeTime = 0.2f; // In seconds
        
        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true),
         UI_FloatRange(controlEnabled = true, scene = UI_Scene.All, minValue = 0.0f, maxValue = 1.0f, stepIncrement = 0.05f)]
        public float maxfade = 0.4f; // invisible:0 to uncloaked:1
        
        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true),
         UI_FloatRange(controlEnabled = true, scene = UI_Scene.All, minValue = 0.0f, maxValue = 1.0f, stepIncrement = 0.1f)]
        public float shadowCutoff = 0.8f;

        [KSPField(isPersistant = true, guiActiveEditor = true, guiActive = true, guiName = "Self Cloak"),
         UI_Toggle(controlEnabled = true, scene = UI_Scene.All, disabledText = "Off", enabledText = "On")]
        public bool selfCloak = true;

        //---------------------------------------------------------------------

        [KSPAction("Cloak Toggle")]
        public void actionToggleDCKCloak(KSPActionParam param)
        {
            cloakOn = !cloakOn;
            UpdateCloak(null, null);
        }

        [KSPAction("Cloak On")]
        public void actionDCKCloakOn(KSPActionParam param)
        {
            cloakOn = true;
            UpdateCloak(null, null);
        }

        [KSPAction("Cloak Off")]
        public void actionDCKCloakOff(KSPActionParam param)
        {
            cloakOn = false;
            UpdateCloak(null, null);
        }

        //---------------------------------------------------------------------

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

        protected void UpdateCloak(BaseField field, object oldValueObj)
        {
            // Update in case its been changed
            calcFadeTime();
            recalcSurfaceArea();
            recalcCloak = true;
        }

        //---------------------------------------------------------------------

        private void ScreenMsg(string msg)
        {
            ScreenMessages.PostScreenMessage(new ScreenMessage(msg, 3, ScreenMessageStyle.UPPER_CENTER));
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

        private void setUI_FieldVisibility(string fieldName, bool state)
        {
            // Control right click menu content based on cfg file input
            Fields[fieldName].uiControlFlight.controlEnabled = state;
            Fields[fieldName].uiControlEditor.controlEnabled = state;
            Fields[fieldName].guiActiveEditor = state;
            Fields[fieldName].guiActive = state;
        }

        private void setGUI_FieldVisibility(string fieldName, bool state)
        {
            Fields[fieldName].guiActiveEditor = state;
            Fields[fieldName].guiActive = state;
        }

        public override void OnStart(StartState state)
        {
            setUI_FieldVisibility("areaExponet", sandboxMode || debugMode);
            setUI_FieldVisibility("ECPerSec", sandboxMode || debugMode);
            setUI_FieldVisibility("fadeTime", sandboxMode || debugMode);
            setUI_FieldVisibility("shadowCutoff", sandboxMode || debugMode);
            setUI_FieldVisibility("hideParticleEffects", sandboxMode || debugMode);
            setUI_FieldVisibility("fullRenderHide", sandboxMode || debugMode);

            setGUI_FieldVisibility("surfaceAreaToCloak", debugMode);
            setGUI_FieldVisibility("ECRequired", debugMode);

            // Sign up for callbacks on vessel changes
            GameEvents.onVesselWasModified.Add(ReconfigureEvent);

            // Toggle callbacks
            BaseField toggleField = Fields[nameof(selfCloak)];
            UI_Toggle editOption = (UI_Toggle)toggleField.uiControlEditor;
            editOption.onFieldChanged = UpdateSelfCloakField;

            toggleField = Fields[nameof(cloakOn)];
            editOption = (UI_Toggle)toggleField.uiControlEditor;
            editOption.onFieldChanged = UpdateCloak;

            // Doesn't work!
            // FloatRange slider callbacks
            //BaseField FloatRangeField = Fields[nameof(fadeTime)];
            //UI_FloatRange sliderOption = (UI_FloatRange)FloatRangeField.uiControlEditor;
            //sliderOption.onFieldChanged = UpdateSelfCloakField;

            //FloatRangeField = Fields[nameof(maxfade)];
            //sliderOption = (UI_FloatRange)FloatRangeField.uiControlEditor;
            //sliderOption.onFieldChanged = UpdateCloak;

            recalcSurfaceArea();
        }

        public void OnDestroy()
        {
            GameEvents.onVesselWasModified.Remove(ReconfigureEvent);
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

                if (hideParticleEffects)
                {
                    EllipsoidParticleEmitter[] EPE = p.GetComponentsInChildren<EllipsoidParticleEmitter>();
                    for (i = 0; i < EPE.GetLength(0); i++)
                    {
                        EPE[i].enabled = renderState;
                    }
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
                ModuleDCKCloak mc = null;
                foreach (Part p in v.parts)
                    if ((p != null) &&
                        ((p != part) || selfCloak))
                    {
                        //p.setOpacity(UNCLOAKED); // 1.1.3
                        p.SetOpacity(UNCLOAKED); // 1.2.2 and up
                        SetRenderAndShadowStates(p, true, true);
                        Debug.Log(modTag + "Uncloak " + p.name);

                        // If the other vessel has a cloak device let it know it needs to do a refresh
                        mc = p.FindModuleImplementing<ModuleDCKCloak>();
                        if (mc != null)
                            mc.recalcCloak = true;
                    }
            }
        }

        public override string GetInfo()
        {
            // Editor "More Info" display
            string st;

            if (sandboxMode || debugMode)
                st = "Fade/sec: User setable\n" +
                     "EC = Area *Fade% * EC/sec ^ Exponent";
            else
                st = "Fade/sec: " + fadePerTime.ToString("F2") + "\n" +
                     "EC = Area * Fade% * " + ECPerSec.ToString("F2") + " ^ " + areaExponet.ToString("F2");
            return st;
        }

        protected void drawEC()
        {
            ECRequired = Time.deltaTime * (1 - visiblilityLevel) * (float)Math.Pow(surfaceAreaToCloak * ECPerSec, areaExponet);

            float ECAcquired = part.RequestResource("ElectricCharge", ECRequired);
            if (ECAcquired < ECRequired * 0.999)
            {
                if (legacyBehavior)
                {
                    // Cloak field turns off if not provided at least 99.9% full charge
                    ScreenMsg("EC low. Req " + (ECRequired / Time.deltaTime).ToString());
                    Events["toggleCloak"].guiName = "Cloak is Off";
                    cloakOn = false;
                }
                else
                    visiblilityLevel = -((ECAcquired / (Time.deltaTime * (float)Math.Pow(surfaceAreaToCloak * ECPerSec, areaExponet))) - 1);
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

        public override void OnUpdate()
        {
            // Get power for the device
            if (cloakOn)
                drawEC();

            // Are we at our desired cloak level?
            if (IsTransitioning())
            {
                recalcCloak = false;
                calcNewCloakLevel();

                foreach (Part p in vessel.parts)
                    if (selfCloak || (p != part))
                    {
                        // If you have multiple cloak devices active on a vessel there are some glitches in cloaking/uncloaking
                        // I'm leaving this as is unless someone asks for this to be fixed
                        // This is were the cloaking happens
                        p.SetOpacity(visiblilityLevel);
                        SetRenderAndShadowStates(p, visiblilityLevel > shadowCutoff, visiblilityLevel > RENDER_THRESHOLD);
                    }
            }
        }

    }
}
