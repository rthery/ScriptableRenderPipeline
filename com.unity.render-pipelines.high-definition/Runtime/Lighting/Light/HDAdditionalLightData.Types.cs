using System;
using System.Linq;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Rendering;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine.Serialization;

namespace UnityEngine.Rendering.HighDefinition
{
    //[TODO: Migrate]
    // This enum extent the original LightType enum with new light type from HD
    //public enum LightTypeExtent
    //{
    //    Punctual, // Fallback on LightShape type
    //    Rectangle,
    //    Tube,
    //    // Sphere,
    //    // Disc,
    //};
    
    /// <summary>Type of an HDRP Light</summary>
    public enum HDLightType
    {
        /// <summary>Complete this type by setting the SpotLightShape too.</summary>
        Spot = LightType.Spot,
        Directional = LightType.Directional,
        Point = LightType.Point,
        /// <summary>Complete this type by setting the AreaLightShape too.</summary>
        Area = LightType.Area,
    }

    public enum SpotLightShape
    {
        Cone,
        Pyramid,
        Box
    };

    public enum AreaLightShape
    {
        Rectangle,
        Tube,
        Disc,
        // Sphere,
    };

    public enum LightUnit
    {
        Lumen,      // lm = total power/flux emitted by the light
        Candela,    // lm/sr = flux per steradian
        Lux,        // lm/m² = flux per unit area
        Luminance,  // lm/m²/sr = flux per unit area and per steradian
        Ev100,      // ISO 100 Exposure Value (https://en.wikipedia.org/wiki/Exposure_value)
    }

    internal enum DirectionalLightUnit
    {
        Lux = LightUnit.Lux,
    }

    internal enum AreaLightUnit
    {
        Lumen = LightUnit.Lumen,
        Luminance = LightUnit.Luminance,
        Ev100 = LightUnit.Ev100,
    }

    internal enum PunctualLightUnit
    {
        Lumen = LightUnit.Lumen,
        Candela = LightUnit.Candela,
        Lux = LightUnit.Lux,
        Ev100 = LightUnit.Ev100
    }

    /// <summary>
    /// Shadow Update mode
    /// </summary>
    public enum ShadowUpdateMode
    {
        EveryFrame = 0,
        OnEnable,
        OnDemand
    }
    
    // Light layering
    public enum LightLayerEnum
    {
        Nothing = 0,   // Custom name for "Nothing" option
        LightLayerDefault = 1 << 0,
        LightLayer1 = 1 << 1,
        LightLayer2 = 1 << 2,
        LightLayer3 = 1 << 3,
        LightLayer4 = 1 << 4,
        LightLayer5 = 1 << 5,
        LightLayer6 = 1 << 6,
        LightLayer7 = 1 << 7,
        Everything = 0xFF, // Custom name for "Everything" option
    }

    // Note: do not use internally, this enum only exists for the user API to set the light type and shape at once
    /// <summary>
    /// Type of an HDRP Light including shape
    /// </summary>
    public enum HDCondensedLightType
    {
        Point,
        BoxSpot,
        PyramidSpot,
        ConeSpot,
        Directional,
        RectangleArea,
        /// <summary> Runtime Only </summary>
        TubeArea,
        /// <summary> Baking Only </summary>
        DiscArea,
    }

    public static class HDLightTypeExtension
    {
        /// <summary>
        /// Returns true if the hd light type is a spot light
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static bool IsSpot(this HDCondensedLightType type)
            => type == HDCondensedLightType.BoxSpot
            || type == HDCondensedLightType.PyramidSpot
            || type == HDCondensedLightType.ConeSpot;

        /// <summary>
        /// Returns true if the hd light type is an area light
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static bool IsArea(this HDCondensedLightType type)
            => type == HDCondensedLightType.TubeArea
            || type == HDCondensedLightType.RectangleArea
            || type == HDCondensedLightType.DiscArea;

        /// <summary>
        /// Returns true if the hd light type can be used for runtime lighting
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static bool IsRuntime(this HDCondensedLightType type)
            => type != HDCondensedLightType.DiscArea;

        /// <summary>
        /// Returns true if the hd light type can be used for baking
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static bool IsBakeable(this HDCondensedLightType type)
            => type != HDCondensedLightType.TubeArea;
    }
    
    public partial class HDAdditionalLightData
    {
        //Private enum to differentiate built-in LightType.Point that can be Area or Point in HDRP
        //This is due to realtime support and culling behavior in Unity
        private enum PointLightHDType
        {
            Punctual,
            Area
        }

        [System.NonSerialized]
        static Dictionary<int, LightUnit[]> supportedLightTypeCache = new Dictionary<int, LightUnit[]>();

        //[TODO: migrate all area shape to PointLightHDType.Area
        [SerializeField, FormerlySerializedAs("lightTypeExtent"), FormerlySerializedAs("m_LightTypeExtent")]
        PointLightHDType m_PointlightHDType = PointLightHDType.Punctual;

        // Only for Spotlight, should be hide for other light
        [SerializeField, FormerlySerializedAs("spotLightShape")]
        SpotLightShape m_SpotLightShape = SpotLightShape.Cone;

        // Only for Spotlight, should be hide for other light
        [SerializeField]
        AreaLightShape m_AreaLightShape = AreaLightShape.Rectangle;
        
        /// <summary>
        /// The type of light used.
        /// This handle some internal conversion in Light component for culling purpose.
        /// </summary>
        public HDLightType type
        {
            get
            {
                switch (legacyLight.type)
                {
                    case LightType.Spot:        return HDLightType.Spot;
                    case LightType.Directional: return HDLightType.Directional;
                    case LightType.Point:
                        switch(m_PointlightHDType)
                        {
                            case PointLightHDType.Punctual: return HDLightType.Point;
                            case PointLightHDType.Area:     return HDLightType.Area;
                            default:
                                Debug.Assert(false, $"Unknown {typeof(PointLightHDType).Name} {m_PointlightHDType}. Fallback on Punctual");
                                return HDLightType.Point;
                        }
                    //case LightType.Area: <- same than LightType.Rectangle
                    case LightType.Rectangle: 
                        // not supported directly. Convert now to equivalent:
                        legacyLight.type = LightType.Point;
                        m_PointlightHDType = PointLightHDType.Area;
                        areaLightShape = AreaLightShape.Rectangle;

                        //sanitycheck on the baking mode first time we add additionalLightData
#if UNITY_EDITOR
                        legacyLight.lightmapBakeType = LightmapBakeType.Realtime;
#endif
                        return HDLightType.Area;
                    case LightType.Disc:
                        areaLightShape = AreaLightShape.Disc;

                        //sanitycheck on the baking mode
#if UNITY_EDITOR
                        legacyLight.lightmapBakeType = LightmapBakeType.Baked;
#endif
                        return HDLightType.Area;
                    default:
                        Debug.Assert(false, $"Unknown {typeof(LightType).Name} {legacyLight.type}. Fallback on Point");
                        return HDLightType.Point;
                }
            }
            set
            {
                if (type != value)
                {
                    switch (value)
                    {
                        case HDLightType.Directional:
                            legacyLight.type = LightType.Directional;
                            break;
                        case HDLightType.Spot:
                            legacyLight.type = LightType.Spot;
                            break;
                        case HDLightType.Point:
                            legacyLight.type = LightType.Point;
                            m_PointlightHDType = PointLightHDType.Punctual;
                            break;
                        case HDLightType.Area:
                            if (areaLightShape == AreaLightShape.Disc)
                                legacyLight.type = LightType.Disc;
                            else
                            {
                                legacyLight.type = LightType.Point;
                                m_PointlightHDType = PointLightHDType.Area;
                            }
                            break;
                        default:
                            Debug.Assert(false, $"Unknown {typeof(HDLightType).Name} {value}.");
                            break;
                    }

                    // If the current light unit is not supported by the new light type, we change it
                    var supportedUnits = GetSupportedLightUnits(value, m_SpotLightShape);
                    if (!supportedUnits.Any(u => u == lightUnit))
                        lightUnit = supportedUnits.First();
                    UpdateAllLightValues();
                }
            }
        }

        /// <summary>
        /// Control the shape of the spot light.
        /// </summary>
        public SpotLightShape spotLightShape
        {
            get => m_SpotLightShape;
            set
            {
                if (m_SpotLightShape == value)
                    return;

                m_SpotLightShape = value;
                UpdateAllLightValues();
            }
        }

        /// <summary>
        /// Control the shape of the spot light.
        /// </summary>
        public AreaLightShape areaLightShape
        {
            get => m_AreaLightShape;
            set
            {
                if (m_AreaLightShape == value)
                    return;

                m_AreaLightShape = value;
                UpdateAllLightValues();
            }
        }

        /// <summary>
        /// Set the type of the light ansd its shape.
        /// Note: this will also change the unit of the light if the current one is not supported by the new light type.
        /// </summary>
        /// <param name="condensedType"></param>
        public void SetLightType(HDCondensedLightType condensedType)
        {
            switch (condensedType)
            {
                case HDCondensedLightType.Point:
                    type = HDLightType.Point;
                    break;
                case HDCondensedLightType.Directional:
                    type = HDLightType.Directional;
                    break;
                case HDCondensedLightType.ConeSpot:
                    type = HDLightType.Spot;
                    spotLightShape = SpotLightShape.Cone;
                    break;
                case HDCondensedLightType.PyramidSpot:
                    type = HDLightType.Spot;
                    spotLightShape = SpotLightShape.Pyramid;
                    break;
                case HDCondensedLightType.BoxSpot:
                    type = HDLightType.Spot;
                    spotLightShape = SpotLightShape.Box;
                    break;
                case HDCondensedLightType.RectangleArea:
                    type = HDLightType.Area;
                    areaLightShape = AreaLightShape.Rectangle;
                    break;
                case HDCondensedLightType.TubeArea:
                    type = HDLightType.Area;
                    areaLightShape = AreaLightShape.Tube;
                    break;
                case HDCondensedLightType.DiscArea:
                    type = HDLightType.Area;
                    areaLightShape = AreaLightShape.Disc;
                    break;
            }
        }

        /// <summary>
        /// Get the HD condensed light type and its shape.
        /// </summary>
        /// <returns></returns>
        public HDCondensedLightType GetLightType()
        {
            switch (type)
            {
                case HDLightType.Directional:   return HDCondensedLightType.Directional;
                case HDLightType.Point:         return HDCondensedLightType.Point;
                case HDLightType.Spot:
                    switch (spotLightShape)
                    {
                        case SpotLightShape.Cone: return HDCondensedLightType.ConeSpot;
                        case SpotLightShape.Box: return HDCondensedLightType.BoxSpot;
                        case SpotLightShape.Pyramid: return HDCondensedLightType.PyramidSpot;
                        default: throw new Exception($"Unknown {typeof(SpotLightShape)}: {spotLightShape}");
                    }
                case HDLightType.Area:
                    switch (areaLightShape)
                    {
                        case AreaLightShape.Rectangle: return HDCondensedLightType.RectangleArea;
                        case AreaLightShape.Tube: return HDCondensedLightType.TubeArea;
                        case AreaLightShape.Disc: return HDCondensedLightType.DiscArea;
                        default: throw new Exception($"Unknown {typeof(AreaLightShape)}: {areaLightShape}");
                    }
                default: throw new Exception($"Unknown {typeof(HDLightType)}: {type}");
            }
        }
        
        string GetLightTypeName()
        {
            if (isAreaLight)
                return $"{areaLightShape}AreaLight";
            else
            {
                if (legacyLight.type == LightType.Spot)
                    return $"{spotLightShape}SpotLight";
                else
                    return $"{legacyLight.type}Light";
            }
        }

        internal bool isAreaLight
            => m_PointlightHDType == PointLightHDType.Area;

        bool isPointLight
            => legacyLight.type == LightType.Point
            && m_PointlightHDType == PointLightHDType.Punctual;


        //[TODO: remove argument if not used static]
        LightUnit[] GetSupportedLightUnits(HDLightType type, SpotLightShape spotLightShape)
        {
            LightUnit[] supportedTypes;

            // Combine the two light types to access the dictionary
            int cacheKey = ((int)type & 0xFF) << 0;
            cacheKey |= ((int)spotLightShape & 0xFF) << 8;

            // We cache the result once they are computed, it avoid garbage generated by Enum.GetValues and Linq.
            if (supportedLightTypeCache.TryGetValue(cacheKey, out supportedTypes))
                return supportedTypes;

            if (type == HDLightType.Area)
                supportedTypes = Enum.GetValues(typeof(AreaLightUnit)).Cast<LightUnit>().ToArray();
            else if (type == HDLightType.Directional || (type == HDLightType.Spot && spotLightShape == SpotLightShape.Box))
                supportedTypes = Enum.GetValues(typeof(DirectionalLightUnit)).Cast<LightUnit>().ToArray();
            else
                supportedTypes = Enum.GetValues(typeof(PunctualLightUnit)).Cast<LightUnit>().ToArray();

            supportedLightTypeCache[cacheKey] = supportedTypes;

            return supportedTypes;
        }

        //[TODO: remove argument if not used static]
        bool IsValidLightUnitForType(HDLightType type, SpotLightShape spotLightShape, LightUnit unit)
        {
            LightUnit[] allowedUnits = GetSupportedLightUnits(type, spotLightShape);

            return allowedUnits.Any(u => u == unit);
        }
    }
}
