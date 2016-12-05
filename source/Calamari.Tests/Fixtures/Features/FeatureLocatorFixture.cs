﻿using System;
using System.IO;
using Calamari.Extensibility;
using Calamari.Extensibility.Features;
using Calamari.Extensibility.FileSystem;
using Calamari.Features;
using Calamari.Integration.FileSystem;
using Calamari.Integration.Packages;
using Calamari.Tests.Helpers;
using Calamari.Util;
using NUnit.Framework;
#if !NET40
using System.Reflection;
#endif

namespace Calamari.Tests.Fixtures.Features
{
    [TestFixture]
    public class FeatureLocatorFixture
    {
        private static readonly string TestExtensionsDirectoryPath = Path.Combine(TestEnvironment.CurrentWorkingDirectory, "Fixtures", "Extensions");
        private static readonly string RunScriptAssembly = "Calamari.Extensibility.FakeFeatures";
        private static readonly string RunScriptFeatureName = $"Calamari.Extensibility.FakeFeatures.HelloWorldFeature, {RunScriptAssembly}";

        private readonly ICalamariFileSystem fileSystem = CalamariPhysicalFileSystem.GetPhysicalFileSystem();
        private  PackageStore packageStore;

        [SetUp]
        public void SetUp()
        {
            Environment.SetEnvironmentVariable("TentacleHome", Path.GetTempPath());
            packageStore = new PackageStore(new GenericPackageExtractor());
        }

        FeatureLocator BuildLocator(string customDirectory = null)
        {
            return new FeatureLocator(new GenericPackageExtractor(), packageStore, fileSystem, customDirectory);
        }

        [Test]
        public void WarningLoggedIfAttributeMissing()
        {
            var locator = BuildLocator();
            var typename = typeof(MissingAttribute).AssemblyQualifiedName;
            Assert.Throws<InvalidOperationException>(() => locator.Locate(typename),
                $"Feature `{typename}` does not have a FeatureAttribute attribute so is to be used in this operation.");
        }

        [Test]
        public void ConventionReturnsWhenNameSearched()
        {
            var locator = BuildLocator();
            var t = locator.Locate(typeof(IncludesAttribute).AssemblyQualifiedName);
            Assert.AreEqual(typeof(IncludesAttribute), t.Feature);
        }

     

        [Test]
        public void LocateReturnsNullWhenFeatureNotFoundInValidAssembly()
        {
            var randomDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            fileSystem.CopyDirectory(TestExtensionsDirectoryPath, randomDirectory);

            var locator = BuildLocator(randomDirectory);

            var t = locator.Locate($"Calamari.Extensibility.RunScript.FakeFeature, {RunScriptAssembly}");
            Assert.IsNull(t);
        }

        [Test]
        public void LocateReturnsNullWhenAssemblyNotFound()
        {
            var locator = BuildLocator();
            var type = locator.Locate("MyClass, MyAssembly");
            Assert.IsNull(type);
        }

        [Test]
        public void LocateThrowsWhenFeatureTypeNotParseable()
        {
            var locator = BuildLocator();
            Assert.Throws<InvalidOperationException>(() => locator.Locate("FakeName"), "Unable to determine feature from name `FakeName`");
        }

        [Test]
        public void LocateThrowsWhenFeatureHasDodgyModule()
        {
            var locator = BuildLocator();
            Assert.Throws<InvalidOperationException>(() => locator.Locate(typeof(FeatureWithDodgyModule).AssemblyQualifiedName), 
                $"Module `{typeof(ModuleWithNoDefaultConstructor).FullName}` does not have a default parameterless constructor.");
        }

        [Test]
        public void FindsFeatureFromBuiltInDirectory()
        {
            var randomDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            fileSystem.CopyDirectory(TestExtensionsDirectoryPath, randomDirectory);

            var locator = BuildLocator();
            locator.BuiltInExtensionsPath = randomDirectory;

            var t = locator.Locate(RunScriptFeatureName);
            Assert.IsNotNull(t.Feature);
        }

        [Test]
        public void FindsFeatureFromCustomDirectory()
        {
            var randomDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            fileSystem.CopyDirectory(TestExtensionsDirectoryPath, randomDirectory);

            var locator = BuildLocator(randomDirectory);

            var t = locator.Locate(RunScriptFeatureName);
            Assert.IsNotNull(t.Feature);
        }

        [Test]
        public void FindsFeatureFromTentacleHome()
        {
            var randomDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            fileSystem.CopyDirectory(TestExtensionsDirectoryPath, Path.Combine(randomDirectory, "Extensions"));
            Environment.SetEnvironmentVariable("TentacleHome", randomDirectory);

            var locator = BuildLocator();

            var t = locator.Locate(RunScriptFeatureName);
            Assert.IsNotNull(t.Feature);
        }

        [Test]
//        public void FindsFeatureInPacakge()
//        {
//            var randomDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
//            Environment.SetEnvironmentVariable("TentacleHome", randomDirectory);
//
//            packageStore.PackagesDirectory = TestEnvironment.GetTestPath("Fixtures", "Features", "Packages");
//
//            var locator = BuildLocator();
//            
//            var t = locator.Locate(RunScriptFeatureName +", Version=1.0.0.0");
//            Assert.IsNotNull(t.Feature);
//            Assert.AreEqual(new Version(1, 0, 0, 0), t.Feature.GetTypeInfo().Assembly.GetName().Version);
//        }

        
        public void FindsFeatureWithCorrectVersionInPacakge()
        {
            var randomDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Environment.SetEnvironmentVariable("TentacleHome", randomDirectory);

            packageStore.PackagesDirectory = TestEnvironment.GetTestPath("Fixtures", "Features", "Packages");

            var locator = BuildLocator();

            var t = locator.Locate(RunScriptFeatureName);            
            Assert.IsNotNull(t.Feature);
            // Defaults to latest one
            Assert.AreEqual(new Version(6, 6, 6, 0), t.Feature.GetTypeInfo().Assembly.GetName().Version);
        }

        public class MissingAttribute : IFeature
        {
            public void Install(IVariableDictionary variables)
            {
                throw new NotImplementedException();
            }
        }

        [Feature("MyName", "Here Is Info")]
        public class IncludesAttribute : IFeature
        {
            public void Install(IVariableDictionary variables)
            {
                throw new NotImplementedException();
            }
        }


        [Feature("MyName", "Here Is Info", Module = typeof(ModuleWithNoDefaultConstructor))]
        public class FeatureWithDodgyModule : IFeature
        {
            public void Install(IVariableDictionary variables)
            {
                throw new NotImplementedException();
            }
        }

        public class ModuleWithNoDefaultConstructor :IModule
        {
            public ModuleWithNoDefaultConstructor(int value){
                
            }

            public void Register(ICalamariContainer container)
            {
                throw new NotImplementedException();
            }
        }
    }
}