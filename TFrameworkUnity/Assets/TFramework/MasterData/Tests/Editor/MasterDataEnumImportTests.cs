using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace TFramework.MasterData.Editor.Tests
{
    public class MasterDataEnumImportTests
    {
        private string _outputPath;

        private enum EffectType
        {
            fire_power,
            _2nd_Wave,
            Heal_Mode
        }

        [SetUp]
        public void SetUp()
        {
            _outputPath = Path.Combine(Path.GetTempPath(), $"TFrameworkMasterDataTests_{Guid.NewGuid():N}");
            Directory.CreateDirectory(_outputPath);
            CodeGenerator.ResetSharedEnumDefinitions();
        }

        [TearDown]
        public void TearDown()
        {
            CodeGenerator.ResetSharedEnumDefinitions();

            if (Directory.Exists(_outputPath))
            {
                Directory.Delete(_outputPath, true);
            }
        }

        [Test]
        public void GenerateSharedEnumFile_NormalizedEnumMember()
        {
            List<string[]> csvData = new List<string[]>
            {
                new[] { "Id", "Effect" },
                new[] { "識別子", "効果種別" },
                new[] { "int", "enum:EffectType" },
                new[] { "1", "fire-power" },
                new[] { "2", " 2nd Wave" },
                new[] { "3", "Heal Mode" }
            };

            CodeGenerator.GenerateClass("SkillMaster", csvData, _outputPath);
            CodeGenerator.GenerateSharedEnumFile(_outputPath);

            string enumCode = File.ReadAllText(Path.Combine(_outputPath, "MasterDataEnums.generated.cs"));
            string dataCode = File.ReadAllText(Path.Combine(_outputPath, "SkillMaster.generated.cs"));

            Assert.That(enumCode, Does.Contain("public enum EffectType"));
            Assert.That(enumCode, Does.Contain("fire_power,"));
            Assert.That(enumCode, Does.Contain("_2nd_Wave,"));
            Assert.That(enumCode, Does.Contain("Heal_Mode,"));
            Assert.That(dataCode, Does.Contain("public EffectType Effect;"));
        }

        [Test]
        public void RegisterSharedEnumCatalog_CatalogOnlyValues_GeneratesOnlySharedEnum()
        {
            List<string[]> catalogData = new List<string[]>
            {
                new[] { "value", "description" },
                new[] { "fire-power", "火力" },
                new[] { " 2nd Wave", "第二波" }
            };

            bool registered = CodeGenerator.RegisterSharedEnumCatalog("Effect Type", catalogData);
            CodeGenerator.GenerateSharedEnumFile(_outputPath);

            string enumCode = File.ReadAllText(Path.Combine(_outputPath, "MasterDataEnums.generated.cs"));
            Assert.That(registered, Is.True);
            Assert.That(enumCode, Does.Contain("public enum Effect_Type"));
            Assert.That(enumCode, Does.Contain("fire_power,"));
            Assert.That(enumCode, Does.Contain("_2nd_Wave,"));
            Assert.That(File.Exists(Path.Combine(_outputPath, "Effect Type.generated.cs")), Is.False);
            Assert.That(File.Exists(Path.Combine(_outputPath, "Effect TypeContainer.cs")), Is.False);
            Assert.That(File.Exists(Path.Combine(_outputPath, "MasterDataServiceExtensions.Generated.cs")), Is.False);
        }

        [Test]
        public void RegisterSharedEnumCatalog_TableValues_MergesAndDeduplicatesValues()
        {
            List<string[]> catalogData = new List<string[]>
            {
                new[] { "value", "description" },
                new[] { "catalog-only", "カタログのみ" },
                new[] { "fire-power", "重複値1" },
                new[] { "fire-power", "重複値2" }
            };
            List<string[]> tableData = new List<string[]>
            {
                new[] { "Id", "Effect" },
                new[] { "識別子", "効果種別" },
                new[] { "int", "enum:EffectType" },
                new[] { "1", "table-only" },
                new[] { "2", "fire-power" }
            };

            CodeGenerator.RegisterSharedEnumCatalog("EffectType", catalogData);
            CodeGenerator.GenerateClass("SkillMaster", tableData, _outputPath);
            CodeGenerator.GenerateSharedEnumFile(_outputPath);

            string enumCode = File.ReadAllText(Path.Combine(_outputPath, "MasterDataEnums.generated.cs"));
            Assert.That(enumCode, Does.Contain("catalog_only,"));
            Assert.That(enumCode, Does.Contain("table_only,"));
            Assert.That(CountOccurrences(enumCode, "fire_power,"), Is.EqualTo(1));
        }

        [Test]
        public void RegisterSharedEnumCatalog_InvalidHeaderOrValue_ReturnsFalse()
        {
            List<string[]> invalidHeader = new List<string[]>
            {
                new[] { "", "description" },
                new[] { "fire", "火" }
            };
            List<string[]> invalidValue = new List<string[]>
            {
                new[] { "value", "description" },
                new[] { "", "空値" }
            };

            LogAssert.Expect(LogType.Error, "[TFramework] [MasterData] Enumカタログのヘッダーが無効である: EffectType");
            bool headerResult = CodeGenerator.RegisterSharedEnumCatalog("EffectType", invalidHeader);
            LogAssert.Expect(LogType.Error, "[TFramework] [MasterData] Enumカタログの値が空である: EffectType row=2");
            bool valueResult = CodeGenerator.RegisterSharedEnumCatalog("EffectType", invalidValue);
            CodeGenerator.GenerateSharedEnumFile(_outputPath);

            Assert.That(headerResult, Is.False);
            Assert.That(valueResult, Is.False);
            Assert.That(File.Exists(Path.Combine(_outputPath, "MasterDataEnums.generated.cs")), Is.False);
        }

        [Test]
        public void ParseValue_EnumValue_NormalizedValue()
        {
            MethodInfo parseValue = typeof(MasterDataAssetUpdater).GetMethod(
                "ParseValue",
                BindingFlags.Static | BindingFlags.NonPublic);

            Assert.That(parseValue, Is.Not.Null);

            object hyphenValue = parseValue.Invoke(null, new object[] { "fire-power", "enum:EffectType", typeof(EffectType) });
            object numberValue = parseValue.Invoke(null, new object[] { " 2nd Wave", "enum:EffectType", typeof(EffectType) });

            Assert.That(hyphenValue, Is.EqualTo(EffectType.fire_power));
            Assert.That(numberValue, Is.EqualTo(EffectType._2nd_Wave));
        }

        private static int CountOccurrences(string source, string value)
        {
            int count = 0;
            int index = 0;
            while ((index = source.IndexOf(value, index, StringComparison.Ordinal)) >= 0)
            {
                count++;
                index += value.Length;
            }

            return count;
        }
    }
}
