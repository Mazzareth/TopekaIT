package com.topekait.androidlocker

import android.os.Build
import android.provider.Settings
import android.content.Context

object DeviceIdentity {
    @Suppress("DEPRECATION")
    fun bestEffortSerial(context: Context): String {
        val serial = try {
            if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.O) Build.getSerial() else Build.SERIAL
        } catch (_: SecurityException) {
            ""
        } catch (_: RuntimeException) {
            ""
        }

        if (!serial.isNullOrBlank() && serial != Build.UNKNOWN) {
            return serial.trim()
        }

        return Settings.Secure.getString(context.contentResolver, Settings.Secure.ANDROID_ID)
            ?.trim()
            .orEmpty()
    }
}
