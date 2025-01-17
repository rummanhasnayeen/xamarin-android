
### adbxxxx Adb Tooling

+ [adb0000](adb0000.md): Generic `adb` Error/Warning.

### andXXxxxx Generic Android Tooling

+ [andas0000](andas0000.md): Generic `apksigner` Error/Warning.
+ [andjs0000](andjs0000.md): Generic `jarsigner` Error/Warning.
+ [andkt0000](andkt0000.md): Generic `keytool` Error/Warning.
+ [andza0000](andza0000.md): Generic `zipalign` Error/Warning.

### aptxxxx Aapt Tooling

+ [apt0000](apt0000.md): Generic `aapt` Error/Warning.
+ [apt0001](apt0001.md): unknown option -- `{option}` . This is the result of using `aapt` command line arguments with `aapt2`. The arguments are not compatible.

### XA0xxx Environment/Missing Tooling

+ [XA0000](xa0000.md): Could not determine $(AndroidApiLevel) or $(TargetFrameworkVersion).
+ [XA0001](xa0001.md): Invalid or unsupported `$(TargetFrameworkVersion)` value.
+ [XA0118](xa0002.md): Could not find mono.android.jar

+ [XA0030](xa0030.md): Building with JDK Version `{versionNumber}` is not supported.
+ [XA0031](xa0031.md): Java SDK {requiredJavaForFrameworkVersion} or above is required when targeting FrameworkVersion {targetFrameworkVersion}.
+ [XA0032](xa0032.md): Java SDK {requiredJavaForBuildTools} or above is required when using build-tools {buildToolsVersion}.
+ [XA0033](xa0033.md): Failed to get the Java SDK version as it does not appear to contain a valid version number.
+ [XA0034](xa0034.md): Failed to get the Java SDK version. 

+ XA0100: EmbeddedNativeLibrary is invalid in Android Application project. Please use AndroidNativeLibrary instead.
+ [XA0101](xa0101.md): warning XA0101: @(Content) build action is not supported.
+ [XA0102](xa0102.md): Generic `lint` Warning.
+ [XA0103](xa0103.md): Generic `lint` Error.
+ XA0104: Invalid Sequence Point mode.
+ [XA0105](xa0105.md): The $(TargetFrameworkVersion) for a dll is greater than the $(TargetFrameworkVersion) for your project.
+ [XA0107](xa0107.md): `{Assmebly}` is a Reference Assembly.
+ [XA0108](xa0108.md): Could not get version from `lint`.
+ [XA0109](xa0109.md): Unsupported or invalid `$(TargetFrameworkVersion)` value of 'v4.5'.
+ [XA0110](xa0110.md): Disabling $(AndroidExplicitCrunch) as it is not supported by `aapt2`. If you wish to use $(AndroidExplicitCrunch) please set $(AndroidUseAapt2) to false.
+ [XA0111](xa0111.md): Could not get the `aapt2` version. Please check it is installed correctly.
+ [XA0112](xa0112.md): `aapt2` is not installed. Disabling `aapt2` support. Please check it is installed correctly.
+ [XA0113](xa0113.md): Google Play requires that new applications and updates must use a TargetFrameworkVersion of v8.0 (API level 26) or above.
+ [XA0114](xa0114.md): Google Play requires that application updates must use a `$(TargetFrameworkVersion)` of v8.0 (API level 26) or above.
+ [XA0115](xa0115.md): Invalid value 'armeabi' in $(AndroidSupportedAbis). This ABI is no longer supported. Please update your project properties to remove the old value. If the properties page does not show an 'armeabi' checkbox, un-check and re-check one of the other ABIs and save the changes.
+ [XA0116](xa0116.md): Unable to find `EmbeddedResource` of name `{ResourceName}`.
+ [XA0117](xa0117.md): The TargetFrameworkVersion {TargetFrameworkVersion} is deprecated. Please update it to be v4.4 or higher.
+ [XA0118](xa0118.md): Could not parse '{TargetMoniker}'

### XA1xxx Project Related

+ [XA1000](xa1000.md): There was an problem parsing {file}. This is likely due to incomplete or invalid xml.
+ [XA1001](xa1001.md): AndroidResgen: Warning while updating Resource XML '{filename}': {Message}
+ [XA1002](xa1002.md): We found a matching key '{Key}' for '{Item}'. But the casing was incorrect. Please correct the casing
+ [XA1003](xa1003.md): '{zip}' does not exist. Please rebuild the project.
+ [XA1004](xa1004.md): There was an error opening {filename}. The file is probably corrupt. Try deleting it and building again.
+ [XA1005](xa1005.md): Attempting naive type name fixup for element with ID '{id}' and type '{managedType}'
+ [XA1006](xa1006.md): Your application is running on a version of Android ({compileSdk}) that is more recent than your targetSdkVersion specifies ({targetSdk}). Set your targetSdkVersion to the highest version of Android available to match your TargetFrameworkVersion ({compileSdk}).
+ [XA1007](xa1007.md): The minSdkVersion ({minSdk}) is greater than targetSdkVersion. Please change the value such that minSdkVersion is less than or equal to targetSdkVersion ({targetSdk}).
+ [XA1008](xa1008.md): The TargetFrameworkVersion ({compileSdk}) should not be lower than targetSdkVersion ({targetSdk})
+ [XA1009](xa1009.md): The {assembly} is Obsolete. Please upgrade to {assembly} {version}

### XA2xxx Linker

### XA3xxx AOT

### XA4xxx Code Generation

+ [XA4214](xa4214.md): The managed type \`Library1.Class1\` exists in multiple assemblies: Library1, Library2. Please refactor the managed type names in these assemblies so that they are not identical.
+ [XA4215](xa4215.md): The Java type \`com.contoso.library1.Class1\` is generated by more than one managed type. Please change the \[Register\] attribute so that the same Java type is not emitted.
+ [XA4301](xa4301.md): Apk already contains the item `xxx`.

### XA5xxx GCC and toolchain

+ [XA5205](xa5205.md): Cannot find `{ToolName}` in the Android SDK.
+ [XA5207](xa5207.md): Could not find android.jar for API Level `{compileSdk}`.
+ [XA5300](xa5300.md): The Android/Java SDK Directory could not be found.

### XA6xxx Internal Tools

### XA7xxx	Reserved

### XA8xxx	Reserved

### XA9xxx	Licensing
