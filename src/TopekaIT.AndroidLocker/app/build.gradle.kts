plugins {
    id("com.android.application")
    id("org.jetbrains.kotlin.android")
}

android {
    namespace = "com.topekait.androidlocker"
    compileSdk = 33

    defaultConfig {
        applicationId = "com.topekait.androidlocker"
        minSdk = 22
        targetSdk = 33
        versionCode = 1
        versionName = "0.1.0"
    }
}
