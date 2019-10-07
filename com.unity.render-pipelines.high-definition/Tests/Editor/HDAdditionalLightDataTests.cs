using UnityEditor.Rendering.TestFramework;
using NUnit.Framework;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using UnityEditor;

namespace UnityEngine.Rendering.HighDefinition.Tests
{
    public class HDAdditionalLightDataTests : MonoBehaviour
    {
        GameObject m_ToClean;
        Light m_Light;
        HDAdditionalLightData m_AdditionalData;
        SerializedProperty builtinType;
        SerializedProperty pointHDType;
        SerializedProperty spotLightShape;
        SerializedProperty areaLightShape;
        SerializedObject serializedLight;
        SerializedObject serializedAdditionalData;

        public enum PointTrueHDType
        {
            Punctual,
            Area
        }

        public class LightTypeDatas
        {
            public LightType builtinLightType;
            public PointTrueHDType pointHDType;
            public SpotLightShape spotLightShape;
            public AreaLightShape areaLightShape;
            public HDLightType correspondingType;
            public HDLightTypeAndShape correspondingLightAndShape;
        }

        static object[] s_LightTypeDatas =
        {
            new LightTypeDatas
            {
                builtinLightType = LightType.Directional,
                pointHDType =  PointTrueHDType.Punctual,
                spotLightShape = SpotLightShape.Cone,
                areaLightShape = AreaLightShape.Rectangle,
                correspondingType = HDLightType.Directional,
                correspondingLightAndShape = HDLightTypeAndShape.Directional,
            },
            new LightTypeDatas
            {
                builtinLightType = LightType.Point,
                pointHDType =  PointTrueHDType.Punctual,
                spotLightShape = SpotLightShape.Cone,
                areaLightShape = AreaLightShape.Rectangle,
                correspondingType = HDLightType.Point,
                correspondingLightAndShape = HDLightTypeAndShape.Point,
            },
            new LightTypeDatas
            {
                builtinLightType = LightType.Spot,
                pointHDType =  PointTrueHDType.Punctual,
                spotLightShape = SpotLightShape.Cone,
                areaLightShape = AreaLightShape.Rectangle,
                correspondingType = HDLightType.Spot,
                correspondingLightAndShape = HDLightTypeAndShape.ConeSpot,
            },
            new LightTypeDatas
            {
                builtinLightType = LightType.Spot,
                pointHDType =  PointTrueHDType.Punctual,
                spotLightShape = SpotLightShape.Box,
                areaLightShape = AreaLightShape.Rectangle,
                correspondingType = HDLightType.Spot,
                correspondingLightAndShape = HDLightTypeAndShape.BoxSpot,
            },
            new LightTypeDatas
            {
                builtinLightType = LightType.Spot,
                pointHDType =  PointTrueHDType.Punctual,
                spotLightShape = SpotLightShape.Pyramid,
                areaLightShape = AreaLightShape.Rectangle,
                correspondingType = HDLightType.Spot,
                correspondingLightAndShape = HDLightTypeAndShape.PyramidSpot,
            },
            new LightTypeDatas
            {
                builtinLightType = LightType.Point,
                pointHDType =  PointTrueHDType.Area,
                spotLightShape = SpotLightShape.Cone,
                areaLightShape = AreaLightShape.Rectangle,
                correspondingType = HDLightType.Area,
                correspondingLightAndShape = HDLightTypeAndShape.RectangleArea,
            },
            new LightTypeDatas
            {
                builtinLightType = LightType.Point,
                pointHDType =  PointTrueHDType.Area,
                spotLightShape = SpotLightShape.Cone,
                areaLightShape = AreaLightShape.Tube,
                correspondingType = HDLightType.Area,
                correspondingLightAndShape = HDLightTypeAndShape.TubeArea,
            },
            new LightTypeDatas
            {
                builtinLightType = LightType.Disc,
                pointHDType =  PointTrueHDType.Area,
                spotLightShape = SpotLightShape.Cone,
                areaLightShape = AreaLightShape.Disc,
                correspondingType = HDLightType.Area,
                correspondingLightAndShape = HDLightTypeAndShape.DiscArea,
            }
        };


        [SetUp]
        public void SetUp()
        {
            m_ToClean = new GameObject("TEST");
            m_Light = m_ToClean.AddComponent<Light>();
            m_AdditionalData = m_ToClean.AddComponent<HDAdditionalLightData>();
            serializedLight = new SerializedObject(m_Light);
            serializedAdditionalData = new SerializedObject(m_AdditionalData);
            builtinType = serializedLight.FindProperty("m_Type");
            pointHDType = serializedAdditionalData.FindProperty("m_PointlightHDType");
            spotLightShape = serializedAdditionalData.FindProperty("m_SpotLightShape");
            areaLightShape = serializedAdditionalData.FindProperty("m_AreaLightShape");
        }

        [TearDown]
        public void TearDown()
        {
            if (m_ToClean != null)
                CoreUtils.Destroy(m_ToClean);
        }

        [Test, TestCaseSource(nameof(s_LightTypeDatas))]
        public void ComputedType(LightTypeDatas lightData)
        {
            builtinType.intValue = (int)lightData.builtinLightType;
            pointHDType.intValue = (int)lightData.pointHDType;
            spotLightShape.intValue = (int)lightData.spotLightShape;
            areaLightShape.intValue = (int)lightData.areaLightShape;
            serializedLight.ApplyModifiedProperties();
            serializedAdditionalData.ApplyModifiedProperties();

            Assert.AreEqual(m_AdditionalData.type, lightData.correspondingType, $"Wrongly recomputed type");
            Assert.AreEqual(m_AdditionalData.GetLightTypeAndShape(), lightData.correspondingLightAndShape, $"Wrongly recomputed type and shape");
        }
    }
}
