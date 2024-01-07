
using IGUtils;
using KSP.Api;
using KSP.Api.Generic;
using KSP.Sim;
using KSP.Sim.Definitions;
using UnityEngine;

namespace KerbalHeadlamp.Modules
{
    [Serializable]
    public sealed class Data_Headlamp : ModuleData
    {
        public override Type ModuleType => typeof(Module_Headlamp);

        [LocalizedField("PartModules/Headlamp/LightSwitch")]
        [KSPState(CopyToSymmetrySet = true)]
        [PAMDisplayControl(SortIndex = 1)]
        [HideInInspector]
        public ModuleProperty<bool> isLightEnabled = new ModuleProperty<bool>(false);
        [LocalizedField("PartModules/Gimbal/AdvancedSettings")]
        [KSPState]
        [PAMDisplayControl(SortIndex = 2)]
        [HideInInspector]
        public ModuleProperty<bool> IsAdvancedControlsShown = new ModuleProperty<bool>(false);
        [LocalizedField("PartModules/Light/ColorR")]
        [KSPState(CopyToSymmetrySet = true)]
        [PAMDisplayControl(SortIndex = 3)]
        [SteppedRange(0.0f, 1f, 0.01f)]
        [HideInInspector]
        public ModuleProperty<float> lightColorR = new ModuleProperty<float>(1f, false, new ToStringDelegate(Data_Headlamp.GetColorComponentString));
        [LocalizedField("PartModules/Light/ColorG")]
        [KSPState(CopyToSymmetrySet = true)]
        [PAMDisplayControl(SortIndex = 4)]
        [SteppedRange(0.0f, 1f, 0.01f)]
        [HideInInspector]
        public ModuleProperty<float> lightColorG = new ModuleProperty<float>(1f, false, new ToStringDelegate(Data_Headlamp.GetColorComponentString));
        [LocalizedField("PartModules/Light/ColorB")]
        [KSPState(CopyToSymmetrySet = true)]
        [PAMDisplayControl(SortIndex = 5)]
        [SteppedRange(0.0f, 1f, 0.01f)]
        [HideInInspector]
        public ModuleProperty<float> lightColorB = new ModuleProperty<float>(1f, false, new ToStringDelegate(Data_Headlamp.GetColorComponentString));
        [HideInInspector]
        [KSPDefinition]
        public bool isSmoothTransitionEnabled = true;
        [Header("GameObject Config")]
        [KSPDefinition]
        [Tooltip("Name of the mesh renderer for emissive")]
        public string LightMeshRendererName;
        [KSPDefinition]
        public float lightBrightenDuration = 0.3f;
        [KSPDefinition]
        public float lightDimDuration = 0.8f;

        public static string GetColorComponentString(object valueObj)
        {
            return ((float)valueObj).ToString("F2");
        }

        public override void Copy(ModuleData sourceModuleData)
        {
            Data_Headlamp dataHeadlamp = (Data_Headlamp)sourceModuleData;
            IGAssert.IsNotNull((object)dataHeadlamp);
            this.isLightEnabled.SetValue((IProperty<bool>)dataHeadlamp.isLightEnabled);
            this.lightColorR.SetValue((IProperty<float>)dataHeadlamp.lightColorR);
            this.lightColorG.SetValue((IProperty<float>)dataHeadlamp.lightColorG);
            this.lightColorB.SetValue((IProperty<float>)dataHeadlamp.lightColorB);
        }
    }
}
