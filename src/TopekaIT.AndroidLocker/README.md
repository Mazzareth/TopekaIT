# Topeka IT Android Locker

Native Android first slice for Zebra TC77 and WT6000 devices. The app reads an NFC sticker on a locker, identifies the Android device by its configured serial/asset tag, and posts a location tap to the portal backend.

## Backend Contract

The app posts to:

```text
POST /api/mobile/equipment/location-taps
```

Request:

```json
{
  "divisionId": "6I-A",
  "readerDeviceSerial": "WT6000-123456",
  "tappedTag": "rfid:NTAG-ABC123",
  "platform": "Android",
  "appVersion": "0.1.0"
}
```

Response:

```json
{
  "status": "Recorded",
  "message": "Device recorded at locker A-01.",
  "assetId": "asset-1",
  "assetLabel": "WT6000-123456",
  "lockerId": "locker-1",
  "lockerNumber": "A-01",
  "employeeId": "worker-1",
  "employeeName": "Worker One",
  "readerDeviceSerial": "WT6000-123456",
  "timestamp": "2026-06-23T17:30:00Z",
  "lastSeenLocation": "A-01"
}
```

## Build Notes

This project is a native Android Gradle project wrapped by a `.csproj` so it appears in the .NET solution and normal solution builds stay green.

Command-line build:

```powershell
cd C:\Dev\6IA-IT-Portal\src\TopekaIT.AndroidLocker
.\gradlew.bat --no-daemon assembleDebug
```

Debug APK:

```text
C:\Dev\6IA-IT-Portal\src\TopekaIT.AndroidLocker\app\build\outputs\apk\debug\app-debug.apk
```

The first pilot should use NFC stickers written with the portal locker payload, for example `rfid:NTAG-ABC123`. Use on-metal/ferrite-backed stickers for metal lockers.
