using BepInEx.Logging;
using KSP.Sim.Definitions;
using UnityEngine;
using KSP.Modules;
using KSP.Sim;
using I2.Loc;
using Logger = BepInEx.Logging.Logger;
using UnityEngine.Serialization;
using KSP.UI;
using KSP.Game;
using KSP.UI.Flight;
using SpaceWarp.API.Game.Extensions;
using KSP.Api.CoreTypes;
using KSP.Sim.impl;

namespace KerbalHeadlamp.Modules
{
    public class Module_Headlamp : PartBehaviourModule
    {
        private static readonly ManualLogSource _LOGGER = Logger.CreateLogSource("KerbalHeadlamp.Modules");

        [FormerlySerializedAs("data")]
        [SerializeField]
        protected KSPLight Headlamp_Light = null;
        protected Data_Headlamp dataHeadlamp;
        public MeshRenderer lightMeshRenderer;
        public float emissionHDRIntensity = 6f;
        public float maxLightIntensity = 2f;

        private Color _lightColor = Color.white;
        private Color _currentLightColor = Color.white;
        private float _elapsedTransitionTime;
        private float _currentIntensity;
        private bool _isBrightening;
        private bool _isDimming;
        private bool _currentLightState;
        private KSPPartAudioManager _kspPartAudioManager;

        public override Type PartComponentModuleType => typeof(PartComponentModule_Headlamp);

        private bool IsTransitioningToDisabled
        {
            get
            {
                return !this.dataHeadlamp.isLightEnabled.GetValue() && this.dataHeadlamp.isSmoothTransitionEnabled && this._isDimming && (double)this._currentIntensity > 0.0;
            }
        }

        public void AddHeadlamp(GameObject kerbal_gameobject)
        {
            if (Headlamp_Light == null)
            {
                Transform kerbal_helmet_t = kerbal_gameobject.transform.FindChildRecursive("helm_spacesuit_01(Clone)");
                if (kerbal_helmet_t != null)
                {
                    Texture2D EmissionMap_Texture;
                    SpaceWarp.API.Assets.AssetManager.TryGetAsset("KerbalHeadlamp/images/kerbal_spacesuit_01_e.png", out EmissionMap_Texture);
                    if (EmissionMap_Texture != null)
                    {
                        this.lightMeshRenderer = kerbal_helmet_t.GetComponentInChildren<MeshRenderer>(true);
                        this.lightMeshRenderer?.material.SetTexture("_EmissionMap", EmissionMap_Texture);
                        this.lightMeshRenderer?.material.SetColor("_EmissionColor", this.CurrentHDREmissionColor);
                    }

                    GameObject light_go = Instantiate(new GameObject("kerbalheadlight"), kerbal_helmet_t);
                    light_go.transform.localEulerAngles = new Vector3(270f, 0, 0);
                    light_go.transform.localPosition = new Vector3(-.75f, 0f, 0f);


                    Light light = light_go.AddComponent<Light>();
                    light.enabled = true;
                    light.type = LightType.Spot;
                    light.intensity = this._currentIntensity;
                    light.range = 50;
                    light.spotAngle = 110;

                    Headlamp_Light = new KSPLight(light, 1.0f);
                    //_LOGGER.LogInfo("Added Headlamp");
                }
                else
                {
                    //_LOGGER.LogWarning("Could not find Kerbal helmet");
                }
            }
            else
            {
                //_LOGGER.LogDebug("Headlamp already exists");
            }
        }
        public override void AddDataModules()
        {
            base.AddDataModules();
            //this.DataModules.TryAddUnique<Data_Headlamp>(this.dataHeadlamp, out this.dataHeadlamp);
            this.dataHeadlamp = new Data_Headlamp();
            this.DataModules.Add(typeof(Data_Headlamp), dataHeadlamp);
        }

        public override void OnInitialize()
        {
            base.OnInitialize();
            this.InitProperties();
            this.SetLightIntensity(0.0f);
            this.SetLightState(this.dataHeadlamp.isLightEnabled.GetValue());
            this.UpdateLightColors();
            this.UpdateFlightPAMControlVisibility();
            this.AddActionGroupAction(new Action<bool>(this.SetLightState), KSPActionGroup.Lights, LocalizationManager.GetTranslation("PartModules/Light/LightEnabled/Toggle", true, 0, true, false, (GameObject)null, (string)null, true), this.dataHeadlamp.isLightEnabled);
            this.AddActionGroupAction(new Action(this.SetLightStateOn), KSPActionGroup.None, LocalizationManager.GetTranslation("PartModules/Light/LightEnabled/Enable", true, 0, true, false, (GameObject)null, (string)null, true));
            this.AddActionGroupAction(new Action(this.SetLightStateOff), KSPActionGroup.None, LocalizationManager.GetTranslation("PartModules/Light/LightEnabled/Disable", true, 0, true, false, (GameObject)null, (string)null, true));
            this.moduleIsEnabled = true;
            this._kspPartAudioManager = this.simulationObject?.Part?.PartAudioManager;
            _LOGGER.LogInfo($"OnInitialize");
        }

        public override void OnModuleUpdate(float deltaTime) => this.UpdateLight(deltaTime);
        public override void OnShutdown()
        {
            _LOGGER.LogDebug($"OnShutdown triggered.");
            this.dataHeadlamp.lightColorR.OnChangedValue -= new Action<float>(this.OnLightColorChanged);
            this.dataHeadlamp.lightColorG.OnChangedValue -= new Action<float>(this.OnLightColorChanged);
            this.dataHeadlamp.lightColorB.OnChangedValue -= new Action<float>(this.OnLightColorChanged);
            this.dataHeadlamp.isLightEnabled.OnChangedValue -= new Action<bool>(this.OnLightEnabledChanged);
            this.dataHeadlamp.IsAdvancedControlsShown.OnChanged -= new Action(this.OnIsAdvancedSettingsShownChanged);
        }
        private void UpdateLight(float deltaTime)
        {
            if (!this.dataHeadlamp.isLightEnabled.GetValue() && !this.IsTransitioningToDisabled)
                this.UpdateHDRColorIntensity(0.0f, Color.black);
            else
            {
                if (this.dataHeadlamp.isSmoothTransitionEnabled)
                {
                    this._elapsedTransitionTime += deltaTime;
                    if (this._isBrightening && (double)this._elapsedTransitionTime < (double)this.dataHeadlamp.lightBrightenDuration)
                    {
                        this._currentIntensity = this._elapsedTransitionTime / Mathf.Max(this.dataHeadlamp.lightBrightenDuration, 1f / 1000f);
                        this._currentLightColor = Color.Lerp(Color.black, this._lightColor, this._currentIntensity);
                        this._isBrightening = (double)this._currentIntensity < (double)this.maxLightIntensity;
                    }
                    if (this._isDimming && (double)this._elapsedTransitionTime < (double)this.dataHeadlamp.lightDimDuration)
                    {
                        this._currentIntensity = Mathf.Abs((this.dataHeadlamp.lightDimDuration - this._elapsedTransitionTime) / Mathf.Max(this.dataHeadlamp.lightDimDuration, 1f / 1000f));
                        if (this._currentIntensity < 0.1f)
                        {
                            this._currentIntensity = 0.0f;
                        }
                        this._currentLightColor = Color.Lerp(Color.black, this._lightColor, this._currentIntensity);
                        this._isDimming = (double)this._currentIntensity > 0.0;
                    }
                }
                else
                {
                    this._currentIntensity = this.maxLightIntensity;
                    this._currentLightColor = this._lightColor;
                }
                this.UpdateHDRColorIntensity(this._currentIntensity, this._currentLightColor);
                if ((double)this._currentIntensity != 0.0)
                    return;
                this.SetLightState(false);
            }
        }
        private void SetLightStateOn() => this.SetLightState(true);
        private void SetLightStateOff() => this.SetLightState(false);
        private void SetLightState(bool state)
        {
            this.dataHeadlamp.isLightEnabled.SetValue(state);

            if (this._kspPartAudioManager == null || this._currentLightState == state)
                return;
            if (state)
            {
                KSP.Audio.KSPAudioEventManager.PostAKEvent("Play_light_ON");
            }
            else
            {
                KSP.Audio.KSPAudioEventManager.PostAKEvent("Play_light_OFF");
            }
            this._currentLightState = state;
            //_LOGGER.LogDebug($"Headlamp new State: {(state ? "Enabled" : "Disabled")}");
        }
        private void UpdateLightColors()
        {
            this._lightColor.r = this.dataHeadlamp.lightColorR.GetValue();
            this._lightColor.g = this.dataHeadlamp.lightColorG.GetValue();
            this._lightColor.b = this.dataHeadlamp.lightColorB.GetValue();
            this._currentLightColor = this._lightColor;
            this.lightMeshRenderer?.material.SetColor("_EmissionColor", this.CurrentHDREmissionColor);
            if(Headlamp_Light != null)
                Headlamp_Light.light.color = this._lightColor;
        }
        private Color CurrentHDREmissionColor
        {
            get
            {
                return !this.IsIlluminated ? Color.black : this._lightColor * this.emissionHDRIntensity;
            }
        }
        private bool IsIlluminated
        {
            get
            {
                return this.dataHeadlamp.isLightEnabled.GetValue();
            }
        }
        private void UpdateHDRColorIntensity(float intensity, Color color)
        {
            this.SetLightIntensity(intensity);
            this.lightMeshRenderer?.material.SetColor("_EmissionColor", color * this.emissionHDRIntensity);
        }
        private void SetLightIntensity(float brightValue)
        {
            if (Headlamp_Light != null)
            {
                float num = Mathf.Min(brightValue, Headlamp_Light.brightness);
                Headlamp_Light.light.intensity = num;
            }
        }
        protected void InitProperties()
        {
            this.dataHeadlamp.isLightEnabled.OnChangedValue += new Action<bool>(this.OnLightEnabledChanged);
            this.dataHeadlamp.lightColorR.OnChangedValue += new Action<float>(this.OnLightColorChanged);
            this.dataHeadlamp.lightColorG.OnChangedValue += new Action<float>(this.OnLightColorChanged);
            this.dataHeadlamp.lightColorB.OnChangedValue += new Action<float>(this.OnLightColorChanged);
            this.dataHeadlamp.IsAdvancedControlsShown.OnChanged += new Action(this.OnIsAdvancedSettingsShownChanged);
        }
        private void OnLightEnabledChanged(bool bValue)
        {
            this._elapsedTransitionTime = 0.0f;
            this._isBrightening = bValue;
            this._isDimming = !this._isBrightening;
            this.SetLightState(this.dataHeadlamp.isLightEnabled.GetValue());
        }
        private void OnLightColorChanged(float cValue) => this.UpdateLightColors();
        private void OnIsAdvancedSettingsShownChanged() => this.UpdateFlightPAMControlVisibility();
        private void UpdateFlightPAMControlVisibility()
        {
            this.dataHeadlamp.SetVisible((IModuleDataContext)this.dataHeadlamp.lightColorR, this.dataHeadlamp.IsAdvancedControlsShown.GetValue());
            this.dataHeadlamp.SetVisible((IModuleDataContext)this.dataHeadlamp.lightColorG, this.dataHeadlamp.IsAdvancedControlsShown.GetValue());
            this.dataHeadlamp.SetVisible((IModuleDataContext)this.dataHeadlamp.lightColorB, this.dataHeadlamp.IsAdvancedControlsShown.GetValue());
            //_LOGGER.LogInfo($"UpdateFlightPAMVisibility Done");
        }
    }
}
