﻿using System;
using System.IO;
using NUnit.Framework;
using Xamarin.ProjectTools;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Xml.Linq;
using Xamarin.Tools.Zip;

namespace Xamarin.Android.Build.Tests
{
	[Parallelizable (ParallelScope.Children)]
	public class PackagingTest : BaseTest
	{
#pragma warning disable 414
		static object [] ManagedSymbolsArchiveSource = new object [] {
			//           isRelease, monoSymbolArchive, archiveShouldExists,
			new object[] { false    , false              , false },
			new object[] { true     , true               , true },
			new object[] { true     , false              , false },
		};
#pragma warning restore 414

		[Test]
		[TestCaseSource (nameof(ManagedSymbolsArchiveSource))]
		public void CheckManagedSymbolsArchive (bool isRelease, bool monoSymbolArchive, bool archiveShouldExists)
		{
			var proj = new XamarinAndroidApplicationProject () {
				IsRelease = isRelease,
			};
			proj.SetProperty (proj.ReleaseProperties, "MonoSymbolArchive", monoSymbolArchive);
			proj.SetProperty (proj.ReleaseProperties, KnownProperties.AndroidCreatePackagePerAbi, "true");
			proj.SetProperty (proj.ReleaseProperties, KnownProperties.AndroidSupportedAbis, "armeabi-v7a;x86");
			using (var b = CreateApkBuilder (Path.Combine ("temp", TestContext.CurrentContext.Test.Name))) {
				b.Verbosity = Microsoft.Build.Framework.LoggerVerbosity.Diagnostic;
				b.ThrowOnBuildFailure = false;
				Assert.IsTrue (b.Build (proj), "first build failed");
				var outputPath = Path.Combine (Root, b.ProjectDirectory, proj.OutputPath);
				var archivePath = Path.Combine (outputPath, proj.PackageName + ".apk.mSYM");
				Assert.AreEqual (archiveShouldExists, Directory.Exists (archivePath),
					string.Format ("The msym archive {0} exist.", archiveShouldExists ? "should" : "should not"));
			}
		}

		[Test]
		public void CheckBuildIdIsUnique ()
		{
			const string supportedAbis = "armeabi-v7a;x86";

			Dictionary<string, string> buildIds = new Dictionary<string, string> ();
			var proj = new XamarinAndroidApplicationProject () {
				IsRelease = true,
			};
			proj.SetProperty (proj.ReleaseProperties, "MonoSymbolArchive", "True");
			proj.SetProperty (proj.ReleaseProperties, "DebugSymbols", "true");
			proj.SetProperty (proj.ReleaseProperties, "DebugType", "PdbOnly");
			proj.SetProperty (proj.ReleaseProperties, KnownProperties.AndroidCreatePackagePerAbi, "true");
			proj.SetProperty (proj.ReleaseProperties, KnownProperties.AndroidSupportedAbis, supportedAbis);
			using (var b = CreateApkBuilder (Path.Combine ("temp", TestContext.CurrentContext.Test.Name))) {
				b.Verbosity = Microsoft.Build.Framework.LoggerVerbosity.Diagnostic;
				b.ThrowOnBuildFailure = false;
				Assert.IsTrue (b.Build (proj), "first build failed");
				var outputPath = Path.Combine (Root, b.ProjectDirectory, proj.OutputPath);
				var archivePath = Path.Combine (outputPath, proj.PackageName + ".apk.mSYM");
				var allFilesInArchive = Directory.GetFiles (archivePath, "*", SearchOption.AllDirectories);
				string extension = "dll";
				Assert.IsTrue (allFilesInArchive.Any (x => Path.GetFileName (x) == $"{proj.ProjectName}.{extension}"), $"{proj.ProjectName}.{extension} should exist in {archivePath}");
				//NOTE: Windows is still generating mdb files here
				extension = IsWindows ? "dll.mdb" : "pdb";
				Assert.IsTrue (allFilesInArchive.Any (x => Path.GetFileName (x) == $"{proj.ProjectName}.{extension}"), $"{proj.ProjectName}.{extension} should exist in {archivePath}");

				string intermediateOutputDir = Path.Combine (Root, b.ProjectDirectory, proj.IntermediateOutputPath);
				List<string> envFiles = EnvironmentHelper.GatherEnvironmentFiles (intermediateOutputDir, supportedAbis, true);
				Dictionary<string, string> envvars = EnvironmentHelper.ReadEnvironmentVariables (envFiles);
				Assert.IsTrue (envvars.Count > 0, $"No environment variables defined");

				string buildID;
				Assert.IsTrue (envvars.TryGetValue ("XAMARIN_BUILD_ID", out buildID), "The environment should contain a XAMARIN_BUILD_ID");
				buildIds.Add ("all", buildID);

				var msymDirectory = Path.Combine (Root, b.ProjectDirectory, proj.OutputPath, proj.PackageName + ".apk.mSYM");
				Assert.IsTrue (File.Exists (Path.Combine (msymDirectory, "manifest.xml")), "manifest.xml should exist in", msymDirectory);
				var doc = XDocument.Load (Path.Combine (msymDirectory, "manifest.xml"));

				Assert.IsTrue (doc.Element ("mono-debug")
					.Elements ()
					.Any (x => x.Name == "app-id" && x.Value == proj.PackageName), "app-id is has an incorrect value.");
				var buildId = buildIds.First ().Value;
				Assert.IsTrue (doc.Element ("mono-debug")
					.Elements ()
					.Any (x => x.Name == "build-id" && x.Value == buildId), "build-id is has an incorrect value.");

				EnvironmentHelper.AssertValidEnvironmentSharedLibrary (intermediateOutputDir, AndroidSdkPath, AndroidNdkPath, supportedAbis);
			}
		}

		[Test]
		public void CheckIncludedAssemblies ()
		{
			var proj = new XamarinAndroidApplicationProject () {
				IsRelease = true,
				PackageReferences = {
					new Package () {
						Id = "System.Runtime.InteropServices.WindowsRuntime",
						Version = "4.0.1",
						TargetFramework = "monoandroid71",
					},
				},
			};
			proj.References.Add (new BuildItem.Reference ("Mono.Data.Sqlite.dll"));
			var expectedFiles = new string [] {
				"Java.Interop.dll",
				"Mono.Android.dll",
				"mscorlib.dll",
				"System.Collections.Concurrent.dll",
				"System.Collections.dll",
				"System.Core.dll",
				"System.Diagnostics.Debug.dll",
				"System.dll",
				"System.Linq.dll",
				"System.Reflection.dll",
				"System.Reflection.Extensions.dll",
				"System.Runtime.dll",
				"System.Runtime.Extensions.dll",
				"System.Runtime.InteropServices.dll",
				"System.Runtime.Serialization.dll",
				"System.Threading.dll",
				"UnnamedProject.dll",
				"Mono.Data.Sqlite.dll",
				"Mono.Data.Sqlite.dll.config",
			};
			using (var b = CreateApkBuilder (Path.Combine ("temp", TestContext.CurrentContext.Test.Name))) {
				b.Verbosity = Microsoft.Build.Framework.LoggerVerbosity.Diagnostic;
				b.ThrowOnBuildFailure = false;
				Assert.IsTrue (b.Build (proj), "build failed");
				var apk = Path.Combine (Root, b.ProjectDirectory,
						proj.IntermediateOutputPath, "android", "bin", "UnnamedProject.UnnamedProject.apk");
				using (var zip = ZipHelper.OpenZip (apk)) {
					var existingFiles = zip.Where (a => a.FullName.StartsWith ("assemblies/", StringComparison.InvariantCultureIgnoreCase));
					var missingFiles = expectedFiles.Where (x => !zip.ContainsEntry ("assmelbies/" + Path.GetFileName (x)));
					Assert.IsTrue (missingFiles.Any (),
					string.Format ("The following Expected files are missing. {0}",
						string.Join (Environment.NewLine, missingFiles)));
					var additionalFiles = existingFiles.Where (x => !expectedFiles.Contains (Path.GetFileName (x.FullName)));
					Assert.IsTrue (!additionalFiles.Any (),
						string.Format ("Unexpected Files found! {0}",
						string.Join (Environment.NewLine, additionalFiles.Select (x => x.FullName))));
				}
			}
		}

		[Test]
		public void CheckClassesDexIsIncluded ()
		{
			var proj = new XamarinAndroidApplicationProject () {
				IsRelease = true,
			};
			using (var b = CreateApkBuilder (Path.Combine ("temp", TestContext.CurrentContext.Test.Name))) {
				b.Verbosity = Microsoft.Build.Framework.LoggerVerbosity.Diagnostic;
				b.ThrowOnBuildFailure = false;
				Assert.IsTrue (b.Build (proj), "build failed");
				var apk = Path.Combine (Root, b.ProjectDirectory,
						proj.IntermediateOutputPath, "android", "bin", "UnnamedProject.UnnamedProject.apk");
				using (var zip = ZipHelper.OpenZip (apk)) {
					Assert.IsTrue (zip.ContainsEntry ("classes.dex"), "Apk should contain classes.dex");
				}
			}
		}

		[Test]
		public void CheckIncludedNativeLibraries ([Values (true, false)] bool compressNativeLibraries)
		{
			var proj = new XamarinAndroidApplicationProject () {
				IsRelease = true,
			};
			proj.PackageReferences.Add(KnownPackages.SQLitePCLRaw_Core);
			proj.SetProperty(proj.ReleaseProperties, KnownProperties.AndroidSupportedAbis, "x86");
			proj.SetProperty (proj.ReleaseProperties, "AndroidStoreUncompressedFileExtensions", compressNativeLibraries ? "" : ".so");
			using (var b = CreateApkBuilder (Path.Combine ("temp", TestContext.CurrentContext.Test.Name))) {
				b.Verbosity = Microsoft.Build.Framework.LoggerVerbosity.Diagnostic;
				b.ThrowOnBuildFailure = false;
				Assert.IsTrue (b.Build (proj), "build failed");
				var apk = Path.Combine (Root, b.ProjectDirectory,
						proj.IntermediateOutputPath, "android", "bin", "UnnamedProject.UnnamedProject.apk");
				CompressionMethod method = compressNativeLibraries ? CompressionMethod.Deflate : CompressionMethod.Store;
				using (var zip = ZipHelper.OpenZip (apk)) {
					var libFiles = zip.Where (x => x.FullName.StartsWith("lib/") && !x.FullName.Equals("lib/", StringComparison.InvariantCultureIgnoreCase));
					var abiPaths = new string[] { "lib/x86/" };
					foreach (var file in libFiles) {
						Assert.IsTrue (abiPaths.Any (x => file.FullName.Contains (x)), $"Apk contains an unnesscary lib file: {file.FullName}");
						Assert.IsTrue (file.CompressionMethod == method, $"{file.FullName} should have been CompressionMethod.{method} in the apk, but was CompressionMethod.{file.CompressionMethod}");
					}
				}
			}
		}

		[Test]
		public void EmbeddedDSOs ()
		{
			var proj = new XamarinAndroidApplicationProject ();
			proj.AndroidManifest = $@"<?xml version=""1.0"" encoding=""utf-8""?>
<manifest xmlns:android=""http://schemas.android.com/apk/res/android"" android:versionCode=""1"" android:versionName=""1.0"" package=""{proj.PackageName}"">
	<uses-sdk />
	<application android:label=""{proj.ProjectName}"" android:extractNativeLibs=""false"">
	</application>
</manifest>";

			using (var b = CreateApkBuilder (Path.Combine ("temp", TestName))) {
				Assert.IsTrue (b.Build (proj), "first build should have succeeded");

				var apk = Path.Combine (Root, b.ProjectDirectory,
						proj.IntermediateOutputPath, "android", "bin", "UnnamedProject.UnnamedProject.apk");
				AssertEmbeddedDSOs (apk);

				//Delete the apk & build again
				File.Delete (apk);
				Assert.IsTrue (b.Build (proj), "second build should have succeeded");
				AssertEmbeddedDSOs (apk);
			}
		}

		void AssertEmbeddedDSOs (string apk)
		{
			FileAssert.Exists (apk);

			using (var zip = ZipHelper.OpenZip (apk)) {
				foreach (var entry in zip) {
					if (entry.FullName.EndsWith (".so")) {
						Assert.AreEqual (entry.Size, entry.CompressedSize, $"`{entry.FullName}` should be uncompressed!");
					} else if (entry.FullName == "environment") {
						using (var stream = new MemoryStream ()) {
							entry.Extract (stream);
							stream.Position = 0;
							using (var reader = new StreamReader (stream)) {
								string environment = reader.ReadToEnd ();
								StringAssert.Contains ("__XA_DSO_IN_APK=1", environment, "`__XA_DSO_IN_APK=1` should be set via @(AndroidEnvironment)");
							}
						}
					}
				}
			}
		}

		[Test]
		public void ExplicitPackageNamingPolicy ()
		{
			var proj = new XamarinAndroidApplicationProject ();
			proj.SetProperty (proj.DebugProperties, "AndroidPackageNamingPolicy", "Lowercase");
			using (var b = CreateApkBuilder (Path.Combine ("temp", TestContext.CurrentContext.Test.Name))) {
				b.Verbosity = Microsoft.Build.Framework.LoggerVerbosity.Diagnostic;
				Assert.IsTrue (b.Build (proj), "build failed");
				var text = b.Output.GetIntermediaryAsText (b.Output.IntermediateOutputPath, Path.Combine ("android", "src", "unnamedproject", "MainActivity.java"));
				Assert.IsTrue (text.Contains ("package unnamedproject;"), "expected package not found in the source.");
			}
		}

		[Test]
		public void CheckMetadataSkipItemsAreProcessedCorrectly ()
		{
			var packages = new List<Package> () {
				KnownPackages.Android_Arch_Core_Common_26_1_0,
				KnownPackages.Android_Arch_Lifecycle_Common_26_1_0,
				KnownPackages.Android_Arch_Lifecycle_Runtime_26_1_0,
				KnownPackages.AndroidSupportV4_27_0_2_1,
				KnownPackages.SupportCompat_27_0_2_1,
				KnownPackages.SupportCoreUI_27_0_2_1,
				KnownPackages.SupportCoreUtils_27_0_2_1,
				KnownPackages.SupportDesign_27_0_2_1,
				KnownPackages.SupportFragment_27_0_2_1,
				KnownPackages.SupportMediaCompat_27_0_2_1,
				KnownPackages.SupportV7AppCompat_27_0_2_1,
				KnownPackages.SupportV7CardView_27_0_2_1,
				KnownPackages.SupportV7MediaRouter_27_0_2_1,
				KnownPackages.SupportV7RecyclerView_27_0_2_1,
				KnownPackages.VectorDrawable_27_0_2_1,
				new Package () { Id = "Xamarin.Android.Support.Annotations", Version = "27.0.2.1" },
				new Package () { Id = "Xamarin.Android.Support.Transition", Version = "27.0.2.1" },
				new Package () { Id = "Xamarin.Android.Support.v7.Palette", Version = "27.0.2.1" },
				new Package () { Id = "Xamarin.Android.Support.Animated.Vector.Drawable", Version = "27.0.2.1" },
			};

			string metaDataTemplate = @"<AndroidCustomMetaDataForReferences Include=""%"">
	<AndroidSkipAddToPackage>True</AndroidSkipAddToPackage>
	<AndroidSkipJavaStubGeneration>True</AndroidSkipJavaStubGeneration>
	<AndroidSkipResourceExtraction>True</AndroidSkipResourceExtraction>
</AndroidCustomMetaDataForReferences>";
			var proj = new XamarinAndroidApplicationProject () {
				Imports = {
					new Import (() => "CustomMetaData.target") {
						TextContent = () => @"<Project xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">
<ItemGroup>" +
string.Join ("\n", packages.Select (x => metaDataTemplate.Replace ("%", x.Id))) +
@"</ItemGroup>
</Project>"
					},
				}
			};
			proj.SetProperty (proj.DebugProperties, "AndroidPackageNamingPolicy", "Lowercase");
			foreach (var package in packages)
				proj.PackageReferences.Add (package);
			using (var b = CreateApkBuilder (Path.Combine ("temp", TestName))) {
				b.ThrowOnBuildFailure = false;
				b.Verbosity = Microsoft.Build.Framework.LoggerVerbosity.Diagnostic;
				Assert.IsTrue (b.Build (proj), "build failed");
				var bin = Path.Combine (Root, b.ProjectDirectory, proj.OutputPath);
				var obj = Path.Combine (Root, b.ProjectDirectory, proj.IntermediateOutputPath);
				var lp = Path.Combine (obj, "lp");
				Assert.IsTrue (Directory.Exists (lp), $"{lp} should exists.");
				Assert.AreEqual (0, Directory.GetDirectories (lp).Length, $"{lp} should NOT contain any directories.");
				var support = Path.Combine (obj, "android", "src", "android", "support");
				Assert.IsFalse (Directory.Exists (support), $"{support} should NOT exists.");
				Assert.IsFalse (File.Exists (lp), $" should NOT have been generated.");
				foreach (var apk in Directory.GetFiles (bin, "*-Signed.apk")) {
					using (var zip = ZipHelper.OpenZip (apk)) {
						foreach (var package in packages) {
							Assert.IsFalse (zip.Any (e => e.FullName == $"assemblies/{package.Id}.dll"), $"APK file `{apk}` should not contain {package.Id}");
						}
					}
				}
			}
		}

		[Test]
		public void CheckSignApk ([Values(true, false)] bool useApkSigner, [Values(true, false)] bool perAbiApk)
		{
			string ext = Environment.OSVersion.Platform != PlatformID.Unix ? ".bat" : "";
			var foundApkSigner = Directory.EnumerateDirectories (Path.Combine (AndroidSdkPath, "build-tools")).Any (dir => Directory.EnumerateFiles (dir, "apksigner"+ ext).Any ());
			if (useApkSigner && !foundApkSigner) {
				Assert.Ignore ("Skipping test. Required build-tools verison which contains apksigner is not installed.");
			}
			var proj = new XamarinAndroidApplicationProject () {
				IsRelease = true,
			};
			if (useApkSigner) {
				proj.SetProperty ("AndroidUseApkSigner", "true");
			} else {
				proj.RemoveProperty ("AndroidUseApkSigner");
			}
			proj.SetProperty (proj.ReleaseProperties, KnownProperties.AndroidCreatePackagePerAbi, perAbiApk);
			proj.SetProperty (proj.ReleaseProperties, KnownProperties.AndroidSupportedAbis, "armeabi-v7a;x86");
			using (var b = CreateApkBuilder (Path.Combine ("temp", TestContext.CurrentContext.Test.Name))) {
				var bin = Path.Combine (Root, b.ProjectDirectory, proj.OutputPath);
				Assert.IsTrue (b.Build (proj), "First build failed");
				Assert.IsTrue (StringAssertEx.ContainsText (b.LastBuildOutput, " 0 Warning(s)"),
						"First build should not contain warnings!  Contains\n" +
						string.Join ("\n", b.LastBuildOutput.Where (line => line.Contains ("warning"))));

				//Make sure the APKs are signed
				foreach (var apk in Directory.GetFiles (bin, "*-Signed.apk")) {
					using (var zip = ZipHelper.OpenZip (apk)) {
						Assert.IsTrue (zip.Any (e => e.FullName == "META-INF/MANIFEST.MF"), $"APK file `{apk}` is not signed! It is missing `META-INF/MANIFEST.MF`.");
					}
				}

				var item = proj.AndroidResources.First (x => x.Include () == "Resources\\values\\Strings.xml");
				item.TextContent = () => proj.StringsXml.Replace ("${PROJECT_NAME}", "Foo");
				item.Timestamp = null;
				Assert.IsTrue (b.Build (proj), "Second build failed");
				Assert.IsTrue (StringAssertEx.ContainsText (b.LastBuildOutput, " 0 Warning(s)"),
						"Second build should not contain warnings!  Contains\n" +
						string.Join ("\n", b.LastBuildOutput.Where (line => line.Contains ("warning"))));

				//Make sure the APKs are signed
				foreach (var apk in Directory.GetFiles (bin, "*-Signed.apk")) {
					using (var zip = ZipHelper.OpenZip (apk)) {
						Assert.IsTrue (zip.Any (e => e.FullName == "META-INF/MANIFEST.MF"), $"APK file `{apk}` is not signed! It is missing `META-INF/MANIFEST.MF`.");
					}
				}
			}
		}

		[Test]
		public void CheckAapt2WarningsDoNotGenerateErrors ()
		{
			//https://github.com/xamarin/xamarin-android/issues/3083
			var proj = new XamarinAndroidApplicationProject () {
				IsRelease = true,
				TargetFrameworkVersion = "v7.1",
				UseLatestPlatformSdk = false,
			};
			proj.PackageReferences.Add (KnownPackages.XamarinForms_2_3_4_231);
			proj.PackageReferences.Add (KnownPackages.AndroidSupportV4_25_4_0_1);
			proj.PackageReferences.Add (KnownPackages.SupportCompat_25_4_0_1);
			proj.PackageReferences.Add (KnownPackages.SupportCoreUI_25_4_0_1);
			proj.PackageReferences.Add (KnownPackages.SupportCoreUtils_25_4_0_1);
			proj.PackageReferences.Add (KnownPackages.SupportDesign_25_4_0_1);
			proj.PackageReferences.Add (KnownPackages.SupportFragment_25_4_0_1);
			proj.PackageReferences.Add (KnownPackages.SupportMediaCompat_25_4_0_1);
			proj.PackageReferences.Add (KnownPackages.SupportV7AppCompat_25_4_0_1);
			proj.PackageReferences.Add (KnownPackages.SupportV7CardView_25_4_0_1);
			proj.PackageReferences.Add (KnownPackages.SupportV7MediaRouter_25_4_0_1);
			proj.SetProperty (proj.ReleaseProperties, KnownProperties.AndroidCreatePackagePerAbi, true);
			proj.SetProperty (proj.ReleaseProperties, KnownProperties.AndroidSupportedAbis, "armeabi-v7a;x86");
			using (var b = CreateApkBuilder (Path.Combine ("temp", TestName))) {
				if (!b.TargetFrameworkExists (proj.TargetFrameworkVersion))
					Assert.Ignore ($"Skipped as {proj.TargetFrameworkVersion} not available.");
				Assert.IsTrue (b.Build (proj), "first build should have succeeded.");
				var packagedResource = Path.Combine (b.Root, b.ProjectDirectory, proj.IntermediateOutputPath, "android", "bin", "packaged_resources");
				FileAssert.Exists (packagedResource, $"{packagedResource} should have been created.");
				var packagedResourcearm = packagedResource + "-armeabi-v7a";
				FileAssert.Exists (packagedResourcearm, $"{packagedResourcearm} should have been created.");
				var packagedResourcex86 = packagedResource + "-x86";
				FileAssert.Exists (packagedResourcex86, $"{packagedResourcex86} should have been created.");
			}
		}

		[Test]
		public void CheckAppBundle ([Values (true, false)] bool isRelease)
		{
			var proj = new XamarinAndroidApplicationProject () {
				IsRelease = isRelease,
			};
			proj.SetProperty ("AndroidPackageFormat", "aab");

			using (var b = CreateApkBuilder (Path.Combine ("temp", TestName))) {
				var bin = Path.Combine (Root, b.ProjectDirectory, proj.OutputPath);
				Assert.IsTrue (b.Build (proj), "first build should have succeeded.");

				// Make sure the AAB is signed
				var aab = Path.Combine (bin, "UnnamedProject.UnnamedProject-Signed.aab");
				using (var zip = ZipHelper.OpenZip (aab)) {
					Assert.IsTrue (zip.Any (e => e.FullName == "META-INF/MANIFEST.MF"), $"AAB file `{aab}` is not signed! It is missing `META-INF/MANIFEST.MF`.");
				}

				// Build with no changes
				Assert.IsTrue (b.Build (proj), "second build should have succeeded.");
				foreach (var target in new [] { "_Sign", "_BuildApkEmbed" }) {
					Assert.IsTrue (b.Output.IsTargetSkipped (target), $"`{target}` should be skipped!");
				}
			}
		}

		[Test]
		public void NetStandardReferenceTest ()
		{
			var netStandardProject = new DotNetStandard () {
				ProjectName = "XamFormsSample",
				ProjectGuid = Guid.NewGuid ().ToString (),
				Sdk = "Microsoft.NET.Sdk",
				TargetFramework = "netstandard1.4",
				IsRelease = true,
				PackageTargetFallback = "portable-net45+win8+wpa81+wp8",
				PackageReferences = {
					KnownPackages.XamarinFormsPCL_2_3_4_231,
					new Package () {
						Id = "System.IO.Packaging",
						Version = "4.4.0",
					},
					new Package () {
						Id = "Newtonsoft.Json",
						Version = "10.0.3"
					},
				},
				OtherBuildItems = {
					new BuildItem ("None") {
						Remove = () => "**\\*.xaml",
					},
					new BuildItem ("Compile") {
						Update = () => "**\\*.xaml.cs",
						DependentUpon = () => "%(Filename)"
					},
					new BuildItem ("EmbeddedResource") {
						Include = () => "**\\*.xaml",
						SubType = () => "Designer",
						Generator = () => "MSBuild:UpdateDesignTimeXaml",
					},
				},
				Sources = {
					new BuildItem.Source ("App.xaml.cs") {
						TextContent = () => @"using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using System.IO.Packaging;

using Xamarin.Forms;

namespace XamFormsSample
{
    public partial class App : Application
    {
        Package package;

        public App()
        {
            try {
                JsonConvert.DeserializeObject<string>(""test"");
                package = Package.Open ("""");
            } catch {
            }
            InitializeComponent();

            MainPage = new ContentPage ();
        }

        protected override void OnStart()
        {
            // Handle when your app starts
        }

        protected override void OnSleep()
        {
            // Handle when your app sleeps
        }

        protected override void OnResume()
        {
            // Handle when your app resumes
        }
    }
}",
					},
					new BuildItem.Source ("App.xaml") {
						TextContent = () => @"<?xml version=""1.0"" encoding=""utf-8"" ?>
<Application xmlns=""http://xamarin.com/schemas/2014/forms""
             xmlns:x=""http://schemas.microsoft.com/winfx/2009/xaml""
             x:Class=""XamFormsSample.App"">
  <Application.Resources>
    <!-- Application resource dictionary -->
  </Application.Resources>
</Application>",
					},
				},
			};

			var app = new XamarinAndroidApplicationProject () {
				ProjectName = "App1",
				IsRelease = true,
				UseLatestPlatformSdk = true,
				References = {
					new BuildItem.Reference ("Mono.Android.Export"),
					new BuildItem.ProjectReference ($"..\\{netStandardProject.ProjectName}\\{netStandardProject.ProjectName}.csproj",
						netStandardProject.ProjectName, netStandardProject.ProjectGuid),
				},
				PackageReferences = {
					KnownPackages.SupportDesign_25_4_0_1,
					KnownPackages.SupportV7CardView_24_2_1,
					KnownPackages.AndroidSupportV4_25_4_0_1,
					KnownPackages.SupportCoreUtils_25_4_0_1,
					KnownPackages.SupportMediaCompat_25_4_0_1,
					KnownPackages.SupportFragment_25_4_0_1,
					KnownPackages.SupportCoreUI_25_4_0_1,
					KnownPackages.SupportCompat_25_4_0_1,
					KnownPackages.SupportV7AppCompat_25_4_0_1,
					KnownPackages.XamarinForms_2_3_4_231,
					new Package () {
						Id = "System.Runtime.Loader",
						Version = "4.3.0",
					},
				}
			};
			app.MainActivity = @"using System;
using Android.App;
using Android.Content;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;
using XamFormsSample;

namespace App1
{
	[Activity (Label = ""App1"", MainLauncher = true, Icon = ""@drawable/icon"")]
	public class MainActivity : global::Xamarin.Forms.Platform.Android.FormsAppCompatActivity {
			protected override void OnCreate (Bundle bundle)
			{
				base.OnCreate (bundle);

				global::Xamarin.Forms.Forms.Init (this, bundle);

				LoadApplication (new App ());
			}
		}
	}";
			app.SetProperty (KnownProperties.AndroidSupportedAbis, "x86;armeabi-v7a");
			var expectedFiles = new string [] {
				"Java.Interop.dll",
				"Mono.Android.dll",
				"mscorlib.dll",
				"System.Core.dll",
				"System.dll",
				"System.Runtime.Serialization.dll",
				"System.IO.Packaging.dll",
				"System.IO.Compression.dll",
				"Mono.Android.Export.dll",
				"App1.dll",
				"FormsViewGroup.dll",
				"Xamarin.Android.Support.Compat.dll",
				"Xamarin.Android.Support.Core.UI.dll",
				"Xamarin.Android.Support.Core.Utils.dll",
				"Xamarin.Android.Support.Design.dll",
				"Xamarin.Android.Support.Fragment.dll",
				"Xamarin.Android.Support.Media.Compat.dll",
				"Xamarin.Android.Support.v4.dll",
				"Xamarin.Android.Support.v7.AppCompat.dll",
				"Xamarin.Android.Support.Animated.Vector.Drawable.dll",
				"Xamarin.Android.Support.Vector.Drawable.dll",
				"Xamarin.Android.Support.Transition.dll",
				"Xamarin.Android.Support.v7.MediaRouter.dll",
				"Xamarin.Android.Support.v7.RecyclerView.dll",
				"Xamarin.Android.Support.Annotations.dll",
				"Xamarin.Android.Support.v7.CardView.dll",
				"Xamarin.Forms.Core.dll",
				"Xamarin.Forms.Platform.Android.dll",
				"Xamarin.Forms.Platform.dll",
				"Xamarin.Forms.Xaml.dll",
				"XamFormsSample.dll",
				"Mono.Security.dll",
				"System.Xml.dll",
				"System.Net.Http.dll",
				"System.ServiceModel.Internals.dll",
				"Newtonsoft.Json.dll",
				"Microsoft.CSharp.dll",
				"System.Numerics.dll",
				"System.Xml.Linq.dll",
			};
			var path = Path.Combine ("temp", TestContext.CurrentContext.Test.Name);
			using (var builder = CreateDllBuilder (Path.Combine (path, netStandardProject.ProjectName), cleanupOnDispose: false)) {
				if (!Directory.Exists (builder.MicrosoftNetSdkDirectory))
					Assert.Fail ($"Microsoft.NET.Sdk not found: {builder.MicrosoftNetSdkDirectory}");
				using (var ab = CreateApkBuilder (Path.Combine (path, app.ProjectName), cleanupOnDispose: false)) {
					builder.RequiresMSBuild =
						ab.RequiresMSBuild = true;
					Assert.IsTrue (builder.Build (netStandardProject), "XamFormsSample should have built.");
					Assert.IsTrue (ab.Build (app), "App should have built.");
					var apk = Path.Combine (Root, ab.ProjectDirectory,
						app.IntermediateOutputPath, "android", "bin", "UnnamedProject.UnnamedProject.apk");
					using (var zip = ZipHelper.OpenZip (apk)) {
						var existingFiles = zip.Where (a => a.FullName.StartsWith ("assemblies/", StringComparison.InvariantCultureIgnoreCase));
						var missingFiles = expectedFiles.Where (x => !zip.ContainsEntry ("assemblies/" + Path.GetFileName (x)));
						Assert.IsFalse (missingFiles.Any (),
						string.Format ("The following Expected files are missing. {0}",
							string.Join (Environment.NewLine, missingFiles)));
						var additionalFiles = existingFiles.Where (x => !expectedFiles.Contains (Path.GetFileName (x.FullName)));
						Assert.IsTrue (!additionalFiles.Any (),
							string.Format ("Unexpected Files found! {0}",
							string.Join (Environment.NewLine, additionalFiles.Select (x => x.FullName))));
					}
				}
			}
		}
	}
}
