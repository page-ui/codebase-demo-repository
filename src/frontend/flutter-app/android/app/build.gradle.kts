import java.io.File

plugins {
    id("com.android.application")
    id("kotlin-android")
    // The Flutter Gradle Plugin must be applied after the Android and Kotlin Gradle plugins.
    id("dev.flutter.flutter-gradle-plugin")
}

android {
    namespace = "page.ui"
    compileSdk = flutter.compileSdkVersion
    ndkVersion = flutter.ndkVersion

    compileOptions {
        sourceCompatibility = JavaVersion.VERSION_17
        targetCompatibility = JavaVersion.VERSION_17
    }

    kotlinOptions {
        jvmTarget = JavaVersion.VERSION_17.toString()
    }

    defaultConfig {
        // TODO: Specify your own unique Application ID (https://developer.android.com/studio/build/application-id.html).
        applicationId = "page.ui"
        // You can update the following values to match your application needs.
        // For more information, see: https://flutter.dev/to/review-gradle-config.
        minSdk = flutter.minSdkVersion
        targetSdk = flutter.targetSdkVersion
        versionCode = flutter.versionCode
        versionName = flutter.versionName
    }

    buildTypes {
        release {
            // TODO: Add your own signing config for the release build.
            // Signing with the debug keys for now, so `flutter run --release` works.
            signingConfig = signingConfigs.getByName("debug")
        }
    }
}

flutter {
    source = "../.."
}

// Workaround: with modern AGP plugin DSL (versions declared in `settings.gradle(.kts)`),
// Flutter CLI may not find the produced APK under `<project>/build/...` even though Gradle
// creates it under `android/app/build/...`. Sync the APKs to the location Flutter expects.
val flutterApkOutDir = layout.buildDirectory.dir("outputs/flutter-apk")
val flutterCliOutDir = File(rootDir.parentFile, "build/app/outputs/flutter-apk")

val syncFlutterApks =
    tasks.register<Copy>("syncFlutterApks") {
        from(flutterApkOutDir)
        into(flutterCliOutDir)
        doFirst {
            flutterCliOutDir.mkdirs()
        }
    }

tasks.matching { it.name.startsWith("assemble") || it.name.startsWith("package") }.configureEach {
    finalizedBy(syncFlutterApks)
}
