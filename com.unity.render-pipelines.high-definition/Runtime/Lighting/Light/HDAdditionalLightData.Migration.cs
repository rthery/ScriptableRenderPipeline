using System;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Rendering;
using UnityEditor.Rendering.HighDefinition;
#endif
using UnityEngine.Serialization;

namespace UnityEngine.Rendering.HighDefinition
{
    public partial class HDAdditionalLightData : ISerializationCallbackReceiver, IVersionable<HDAdditionalLightData.Version>
    {
        enum Version
        {
            _Unused00,
            _Unused01,
            ShadowNearPlane,
            LightLayer,
            ShadowLayer,
            _Unused02,
            ShadowResolution,
            AreaLightShapeTypeLogicIsolation,
        }

        /// <summary>
        /// Shadow Resolution Tier
        /// </summary>
        [Obsolete]
        enum ShadowResolutionTier
        {
            Low = 0,
            Medium,
            High,
            VeryHigh
        }

        /// <summary>
        /// Type used previous isolation of AreaLightShape as we use Point for realtime area light due to culling
        /// </summary>
        [Obsolete]
        enum LightTypeExtent
        {
            Punctual,
            Rectangle,
            Tube
        };

        Version IVersionable<Version>.version
        {
            get => m_Version;
            set => m_Version = value;
        }

#pragma warning disable 0618, 0612
        [SerializeField]
        private Version m_Version = Version.ShadowResolution;

        private static readonly MigrationDescription<Version, HDAdditionalLightData> k_HDLightMigrationSteps
            = MigrationDescription.New(
                MigrationStep.New(Version.ShadowNearPlane, (HDAdditionalLightData data) =>
                {
                    // Added ShadowNearPlane to HDRP additional light data, we don't use Light.shadowNearPlane anymore
                    // ShadowNearPlane have been move to HDRP as default legacy unity clamp it to 0.1 and we need to be able to go below that
                    data.shadowNearPlane = data.legacyLight.shadowNearPlane;
                }),
                MigrationStep.New(Version.LightLayer, (HDAdditionalLightData data) =>
                {
                    // Migrate HDAdditionalLightData.lightLayer to Light.renderingLayerMask
                    data.legacyLight.renderingLayerMask = LightLayerToRenderingLayerMask((int)data.m_LightLayers, data.legacyLight.renderingLayerMask);
                }),
                MigrationStep.New(Version.ShadowLayer, (HDAdditionalLightData data) =>
                {
                    // Added the ShadowLayer
                    // When we upgrade the option to decouple light and shadow layers will be disabled
                    // so we can sync the shadow layer mask (from the legacyLight) and the new light layer mask
                    data.lightlayersMask = (LightLayerEnum)RenderingLayerMaskToLightLayer(data.legacyLight.renderingLayerMask);
                }),
                MigrationStep.New(Version.ShadowResolution, (HDAdditionalLightData data) =>
                {
                    var additionalShadow = data.GetComponent<AdditionalShadowData>();
                    if (additionalShadow != null)
                    {
                        data.m_ObsoleteCustomShadowResolution = additionalShadow.customResolution;
                        data.m_ObsoleteContactShadows = additionalShadow.contactShadows;

                        data.shadowDimmer = additionalShadow.shadowDimmer;
                        data.volumetricShadowDimmer = additionalShadow.volumetricShadowDimmer;
                        data.shadowFadeDistance = additionalShadow.shadowFadeDistance;
                        data.shadowTint = additionalShadow.shadowTint;
                        data.normalBias = additionalShadow.normalBias;
                        data.constantBias = additionalShadow.constantBias;
                        data.shadowUpdateMode = additionalShadow.shadowUpdateMode;
                        data.shadowCascadeRatios = additionalShadow.shadowCascadeRatios;
                        data.shadowCascadeBorders = additionalShadow.shadowCascadeBorders;
                        data.shadowAlgorithm = additionalShadow.shadowAlgorithm;
                        data.shadowVariant = additionalShadow.shadowVariant;
                        data.shadowPrecision = additionalShadow.shadowPrecision;
                        CoreUtils.Destroy(additionalShadow);
                    }

                    data.shadowResolution.@override = data.m_ObsoleteCustomShadowResolution;
                    switch (data.m_ObsoleteShadowResolutionTier)
                    {
                        case ShadowResolutionTier.Low: data.shadowResolution.level = 0; break;
                        case ShadowResolutionTier.Medium: data.shadowResolution.level = 1; break;
                        case ShadowResolutionTier.High: data.shadowResolution.level = 2; break;
                        case ShadowResolutionTier.VeryHigh: data.shadowResolution.level = 3; break;
                    }
                    data.shadowResolution.useOverride = !data.m_ObsoleteUseShadowQualitySettings;
                    data.useContactShadow.@override = data.m_ObsoleteContactShadows;
                }),
                MigrationStep.New(Version.AreaLightShapeTypeLogicIsolation, (HDAdditionalLightData data) =>
                {
                    // It is now mixed in an other Enum: PointLightHDType
                    // As it is int that live under Enum and used for serialization,
                    // there is no serialization issue but we must move datas where they should be.
                    switch ((LightTypeExtent)data.m_PointlightHDType)
                    {
                        case LightTypeExtent.Punctual:
                            data.m_PointlightHDType = PointLightHDType.Punctual;
                            break;
                        case LightTypeExtent.Rectangle:
                            data.m_PointlightHDType = PointLightHDType.Area;
                            data.m_AreaLightShape = AreaLightShape.Rectangle;
                            break;
                        case LightTypeExtent.Tube:
                            data.m_PointlightHDType = PointLightHDType.Area;
                            data.m_AreaLightShape = AreaLightShape.Tube;
                            break;
                        //No other AreaLight types where supported at this time
                    }
                })
            );
#pragma warning restore 0618, 0612

        void ISerializationCallbackReceiver.OnAfterDeserialize() {}

        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
            UpdateBounds();
        }

        void OnEnable()
        {
            if (shadowUpdateMode == ShadowUpdateMode.OnEnable)
                m_ShadowMapRenderedSinceLastRequest = false;
            SetEmissiveMeshRendererEnabled(true);
        }

        void Awake()
        {
            k_HDLightMigrationSteps.Migrate(this);
#pragma warning disable 0618
            var shadow = GetComponent<AdditionalShadowData>();
            if (shadow != null)
                CoreUtils.Destroy(shadow);
#pragma warning restore 0618
        }

        #region Obsolete fields
        // To be able to have correct default values for our lights and to also control the conversion of intensity from the light editor (so it is compatible with GI)
        // we add intensity (for each type of light we want to manage).
        [Obsolete("Use Light.renderingLayerMask instead")]
        [FormerlySerializedAs("lightLayers")]
        LightLayerEnum m_LightLayers = LightLayerEnum.LightLayerDefault;

        [Obsolete]
        [SerializeField]
        [FormerlySerializedAs("m_ShadowResolutionTier")]
        ShadowResolutionTier m_ObsoleteShadowResolutionTier = ShadowResolutionTier.Medium;
        [Obsolete]
        [SerializeField]
        [FormerlySerializedAs("m_UseShadowQualitySettings")]
        bool m_ObsoleteUseShadowQualitySettings = false;

        [FormerlySerializedAs("m_CustomShadowResolution")]
        [Obsolete]
        [SerializeField]
        int m_ObsoleteCustomShadowResolution = k_DefaultShadowResolution;

        [FormerlySerializedAs("m_ContactShadows")]
        [Obsolete]
        [SerializeField]
        bool m_ObsoleteContactShadows = false;
        #endregion
    }
}
