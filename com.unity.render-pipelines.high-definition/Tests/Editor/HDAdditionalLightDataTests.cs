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

        public class LightTypeDatas : TestCaseData
        {
            public LightType builtinLightType;
            public PointTrueHDType pointHDType;
            public SpotLightShape spotLightShape;
            public AreaLightShape areaLightShape;
            public HDLightType correspondingType;
            public HDLightTypeAndShape correspondingLightAndShape;
        }

        static TestCaseData[] s_LightTypeDatas =
        {
            new TestCaseData(LightType.Directional, PointTrueHDType.Punctual, SpotLightShape.Cone, AreaLightShape.Rectangle)
                .Returns((HDLightType.Directional, HDLightTypeAndShape.Directional))
                .SetName("Directional"),
            new TestCaseData(LightType.Point, PointTrueHDType.Punctual, SpotLightShape.Cone, AreaLightShape.Rectangle)
                .Returns((HDLightType.Point, HDLightTypeAndShape.Point))
                .SetName("Point"),
            new TestCaseData(LightType.Spot, PointTrueHDType.Punctual, SpotLightShape.Cone, AreaLightShape.Rectangle)
                .Returns((HDLightType.Spot, HDLightTypeAndShape.ConeSpot))
                .SetName("Spot with cone shape"),
            new TestCaseData(LightType.Spot, PointTrueHDType.Punctual, SpotLightShape.Box, AreaLightShape.Rectangle)
                .Returns((HDLightType.Spot, HDLightTypeAndShape.BoxSpot))
                .SetName("Spot with box shape"),
            new TestCaseData(LightType.Spot, PointTrueHDType.Punctual, SpotLightShape.Pyramid, AreaLightShape.Rectangle)
                .Returns((HDLightType.Spot, HDLightTypeAndShape.PyramidSpot))
                .SetName("Spot with pyramid shape"),
            new TestCaseData(LightType.Point, PointTrueHDType.Area, SpotLightShape.Cone, AreaLightShape.Rectangle)
                .Returns((HDLightType.Area, HDLightTypeAndShape.RectangleArea))
                .SetName("Area with rectangle shape"),
            new TestCaseData(LightType.Point, PointTrueHDType.Area, SpotLightShape.Cone, AreaLightShape.Tube)
                .Returns((HDLightType.Area, HDLightTypeAndShape.TubeArea))
                .SetName("Area with tube shape"),
            new TestCaseData(LightType.Disc, PointTrueHDType.Area, SpotLightShape.Cone, AreaLightShape.Disc)
                .Returns((HDLightType.Area, HDLightTypeAndShape.DiscArea))
                .SetName("Area with disc shape"),
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
        public (HDLightType, HDLightTypeAndShape) ComputedType(LightType builtinLightType, PointTrueHDType pointHDType, SpotLightShape spotLightShape, AreaLightShape areaLightShape)
        {
            builtinType.intValue = (int)builtinLightType;
            this.pointHDType.intValue = (int)pointHDType;
            this.spotLightShape.intValue = (int)spotLightShape;
            this.areaLightShape.intValue = (int)areaLightShape;
            serializedLight.ApplyModifiedProperties();
            serializedAdditionalData.ApplyModifiedProperties();

            return (m_AdditionalData.type, m_AdditionalData.GetLightTypeAndShape());
        }
    }
}
