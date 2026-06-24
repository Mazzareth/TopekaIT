package com.topekait.androidlocker

import org.json.JSONObject
import java.io.BufferedReader
import java.io.InputStreamReader
import java.net.HttpURLConnection
import java.net.URL

data class LocationTapRequest(
    val divisionId: String,
    val readerDeviceSerial: String,
    val tappedTag: String,
    val platform: String = "Android",
    val appVersion: String = "0.1.0"
)

data class LocationTapResponse(
    val status: String,
    val message: String,
    val assetLabel: String?,
    val lockerNumber: String?,
    val employeeName: String?
)

data class ApiResult<T>(
    val isSuccess: Boolean,
    val value: T? = null,
    val error: String? = null
)

class TopekaMobileApi(private val baseUrl: String) {
    fun recordLocationTap(request: LocationTapRequest): ApiResult<LocationTapResponse> {
        val url = URL(baseUrl.trimEnd('/') + "/api/mobile/equipment/location-taps")
        val body = JSONObject()
            .put("divisionId", request.divisionId)
            .put("readerDeviceSerial", request.readerDeviceSerial)
            .put("tappedTag", request.tappedTag)
            .put("platform", request.platform)
            .put("appVersion", request.appVersion)
            .toString()

        val connection = (url.openConnection() as HttpURLConnection).apply {
            requestMethod = "POST"
            connectTimeout = 10000
            readTimeout = 15000
            doOutput = true
            setRequestProperty("Accept", "application/json")
            setRequestProperty("Content-Type", "application/json; charset=utf-8")
        }

        return try {
            connection.outputStream.use { stream ->
                stream.write(body.toByteArray(Charsets.UTF_8))
            }

            val responseText = readResponse(connection)
            val json = if (responseText.isBlank()) JSONObject() else JSONObject(responseText)
            val response = LocationTapResponse(
                status = json.optString("status"),
                message = json.optString("message"),
                assetLabel = json.optNullableString("assetLabel"),
                lockerNumber = json.optNullableString("lockerNumber"),
                employeeName = json.optNullableString("employeeName")
            )

            if (connection.responseCode in 200..299) {
                ApiResult(isSuccess = true, value = response)
            } else {
                ApiResult(isSuccess = false, error = response.message.ifBlank { "HTTP ${connection.responseCode}" })
            }
        } catch (ex: Exception) {
            ApiResult(isSuccess = false, error = ex.message ?: ex.javaClass.simpleName)
        } finally {
            connection.disconnect()
        }
    }

    private fun readResponse(connection: HttpURLConnection): String {
        val stream = if (connection.responseCode in 200..299) {
            connection.inputStream
        } else {
            connection.errorStream
        } ?: return ""

        BufferedReader(InputStreamReader(stream, Charsets.UTF_8)).use { reader ->
            return reader.readText()
        }
    }

    private fun JSONObject.optNullableString(name: String): String? {
        if (!has(name) || isNull(name)) return null
        return optString(name).takeIf { it.isNotBlank() }
    }
}
