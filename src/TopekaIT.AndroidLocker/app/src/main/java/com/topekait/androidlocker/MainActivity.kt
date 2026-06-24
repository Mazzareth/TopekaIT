package com.topekait.androidlocker

import android.app.Activity
import android.nfc.NdefRecord
import android.nfc.NfcAdapter
import android.nfc.Tag
import android.nfc.tech.Ndef
import android.os.Bundle
import android.os.Handler
import android.os.Looper
import android.view.Gravity
import android.view.inputmethod.EditorInfo
import android.widget.Button
import android.widget.EditText
import android.widget.LinearLayout
import android.widget.ScrollView
import android.widget.TextView
import java.nio.charset.Charset
import java.util.Arrays
import java.util.concurrent.Executors

class MainActivity : Activity() {
    private val executor = Executors.newSingleThreadExecutor()
    private val main = Handler(Looper.getMainLooper())
    private var nfcAdapter: NfcAdapter? = null
    private var lastTapKey = ""
    private var lastTapAt = 0L

    private lateinit var backendUrl: EditText
    private lateinit var divisionId: EditText
    private lateinit var readerSerial: EditText
    private lateinit var status: TextView

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        nfcAdapter = NfcAdapter.getDefaultAdapter(this)
        buildUi()
        loadSettings()
    }

    override fun onResume() {
        super.onResume()
        val flags = NfcAdapter.FLAG_READER_NFC_A or
            NfcAdapter.FLAG_READER_NFC_B or
            NfcAdapter.FLAG_READER_NFC_F or
            NfcAdapter.FLAG_READER_NFC_V

        nfcAdapter?.enableReaderMode(this, { tag -> onTagDiscovered(tag) }, flags, null)
        if (nfcAdapter == null) {
            status.text = "NFC is not available on this device."
        } else if (nfcAdapter?.isEnabled == false) {
            status.text = "NFC is turned off."
        } else {
            status.text = "Ready for locker tap."
        }
    }

    override fun onPause() {
        nfcAdapter?.disableReaderMode(this)
        super.onPause()
    }

    override fun onDestroy() {
        executor.shutdownNow()
        super.onDestroy()
    }

    private fun buildUi() {
        val root = LinearLayout(this).apply {
            orientation = LinearLayout.VERTICAL
            setPadding(28, 28, 28, 28)
        }

        root.addView(TextView(this).apply {
            text = "Topeka Locker"
            textSize = 26f
            setTextColor(0xFF17211C.toInt())
        })

        backendUrl = field("Backend URL", EditorInfo.IME_ACTION_NEXT)
        divisionId = field("Division", EditorInfo.IME_ACTION_NEXT)
        readerSerial = field("Device serial or asset tag", EditorInfo.IME_ACTION_DONE)

        root.addView(label("Backend URL"))
        root.addView(backendUrl)
        root.addView(label("Division"))
        root.addView(divisionId)
        root.addView(label("Device serial or asset tag"))
        root.addView(readerSerial)

        root.addView(Button(this).apply {
            text = "Save"
            setOnClickListener {
                saveSettings()
                status.text = "Saved. Ready for locker tap."
            }
        })

        status = TextView(this).apply {
            textSize = 18f
            gravity = Gravity.CENTER_VERTICAL
            setPadding(0, 24, 0, 0)
            setTextColor(0xFF17211C.toInt())
        }
        root.addView(status)

        setContentView(ScrollView(this).apply { addView(root) })
    }

    private fun label(text: String) = TextView(this).apply {
        this.text = text
        textSize = 14f
        setPadding(0, 20, 0, 4)
        setTextColor(0xFF314139.toInt())
    }

    private fun field(hint: String, action: Int) = EditText(this).apply {
        this.hint = hint
        setSingleLine(true)
        imeOptions = action
        textSize = 17f
    }

    private fun loadSettings() {
        val prefs = getSharedPreferences("locker", MODE_PRIVATE)
        backendUrl.setText(prefs.getString("backendUrl", "http://10.36.155.64:5117"))
        divisionId.setText(prefs.getString("divisionId", "6I-A"))
        readerSerial.setText(prefs.getString("readerSerial", DeviceIdentity.bestEffortSerial(this)))
    }

    private fun saveSettings() {
        getSharedPreferences("locker", MODE_PRIVATE).edit()
            .putString("backendUrl", backendUrl.text.toString().trim())
            .putString("divisionId", divisionId.text.toString().trim())
            .putString("readerSerial", readerSerial.text.toString().trim())
            .apply()
    }

    private fun onTagDiscovered(tag: Tag) {
        val payload = readPayload(tag)
        val now = System.currentTimeMillis()
        if (payload == lastTapKey && now - lastTapAt < 1500) return
        lastTapKey = payload
        lastTapAt = now

        main.post {
            status.text = "Read $payload. Recording location..."
            recordLocation(payload)
        }
    }

    private fun recordLocation(payload: String) {
        val baseUrl = backendUrl.text.toString().trim()
        val division = divisionId.text.toString().trim()
        val serial = readerSerial.text.toString().trim()

        if (baseUrl.isBlank() || division.isBlank() || serial.isBlank()) {
            main.post { status.text = "Backend URL, division, and device serial are required." }
            return
        }

        saveSettings()
        executor.execute {
            val result = TopekaMobileApi(baseUrl).recordLocationTap(
                LocationTapRequest(
                    divisionId = division,
                    readerDeviceSerial = serial,
                    tappedTag = payload
                )
            )

            main.post {
                if (result.isSuccess) {
                    val value = result.value
                    val owner = value?.employeeName ?: "unassigned"
                    status.text = "${value?.assetLabel ?: serial} -> Locker ${value?.lockerNumber ?: "?"}\nOwner: $owner"
                } else {
                    status.text = result.error ?: "Location tap failed."
                }
            }
        }
    }

    private fun readPayload(tag: Tag): String {
        val ndef = Ndef.get(tag)
        if (ndef != null) {
            try {
                ndef.connect()
                val records = ndef.ndefMessage?.records ?: ndef.cachedNdefMessage?.records
                val value = records
                    ?.asSequence()
                    ?.mapNotNull { recordToText(it) }
                    ?.firstOrNull { it.isNotBlank() }
                if (!value.isNullOrBlank()) return value.trim()
            } catch (_: Exception) {
            } finally {
                try {
                    ndef.close()
                } catch (_: Exception) {
                }
            }
        }

        return "uid:" + tag.id.joinToString("") { "%02X".format(it) }
    }

    private fun recordToText(record: NdefRecord): String? {
        if (record.tnf == NdefRecord.TNF_WELL_KNOWN &&
            Arrays.equals(record.type, NdefRecord.RTD_TEXT)) {
            val payload = record.payload
            if (payload.isEmpty()) return null
            val languageCodeLength = payload[0].toInt() and 0x3F
            val textStart = 1 + languageCodeLength
            if (textStart >= payload.size) return null
            val charset = if ((payload[0].toInt() and 0x80) == 0) Charsets.UTF_8 else Charset.forName("UTF-16")
            return String(payload, textStart, payload.size - textStart, charset)
        }

        if (record.tnf == NdefRecord.TNF_WELL_KNOWN &&
            Arrays.equals(record.type, NdefRecord.RTD_URI)) {
            return decodeUriRecord(record.payload)
        }

        return record.payload.toString(Charsets.UTF_8).trim().takeIf { it.isNotBlank() }
    }

    private fun decodeUriRecord(payload: ByteArray): String? {
        if (payload.isEmpty()) return null
        val prefixes = arrayOf(
            "",
            "http://www.",
            "https://www.",
            "http://",
            "https://"
        )
        val prefix = prefixes.getOrElse(payload[0].toInt()) { "" }
        return prefix + String(payload, 1, payload.size - 1, Charsets.UTF_8)
    }
}
