using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Framework;

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
    }
}
