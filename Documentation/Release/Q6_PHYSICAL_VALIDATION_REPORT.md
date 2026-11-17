# Phase Q6 — Physical Device Validation & Release Candidate Verification Report

**Project:** Immersive Anatomy Lab  
**Engine:** Unity 6.3 LTS (`6000.3.21f1`), Universal Render Pipeline 17.3.0  
**Phase:** Q6 (Dual Platform Release Candidate Verification & Validation)  
**Date:** November 18, 2026  
**Status:** **RELEASE CANDIDATES VERIFIED & PACKAGED**

---

## 1. Executive Summary & Deliverables Matrix

Phase Q6 validates both standalone distribution packages and verifies production cloud backend connectivity for the dual-platform release of *Immersive Anatomy Lab*.

The Android manifest merger conflict (`com.oculus.supportedDevices` attribute collision with `xrmanifest.androidlib`) was resolved using Android build tools namespace overrides (`tools:replace="android:value"`), enabling a clean, unblocked release build for Meta Quest.

Both release packages have been compiled, verified with cryptographic checksums, and stored in their target release locations.

| Deliverable | Target Platform | Format / File Path | File Size | SHA-256 Hash | Signing / Mode | Verification Status |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| **Meta Quest Standalone VR** | Meta Quest 2 / Pro / 3 / 3S | `Builds/Quest/ImmersiveAnatomyLab-Quest-v1.0.0.apk` | **61,514,996 bytes** (58.66 MB) | `7E80420C11698906D1123176FAE9DC90AD782FCEB67DC00C7C795E27EE2E5397` | Signed (APK v2 Scheme, RSA 2048-bit release keystore) | **PASS** |
| **Windows Desktop** | Windows 10/11 x64 Laptop/PC | `Builds/ImmersiveAnatomyLab-Windows-v1.0.0.zip` | **91,214,146 bytes** (86.99 MB ZIP; 247.45 MB uncompressed) | `03D75E32F5018D3B33E6206CA945253279EF83800B7A083A8D48972244C85CA9` | Standalone Player (DirectX 12, No XR runtime required) | **PASS** |
| **Production Cloud Backend** | Cloud Web Service | `https://immersive-anatomy-api.onrender.com` | Live Cloud API | Live HTTPS Endpoint | TLS 1.3 / FastAPI + Gemini 1.5 Flash + RAG + Edge TTS | **PASS** |
| **Physical Quest Headset** | Meta Quest 3 Hardware | Standalone Sideload (`adb install -r`) | N/A | Hardware Test | 72 Hz / 90 Hz real display validation | **NOT TESTED** *(No device connected via ADB)* |

---

## 2. Q6.1 — Release Configuration Audit

All dual-platform settings were verified against the configuration matrix established in Phases Q1 and Q2:

1. **Build Profiles & Scenes:**
   - Primary scene `Assets/Scenes/VR_MainLab.unity` is registered at build index 0 (`enabled: true`).
   - Profile assets exist at `Assets/Settings/BuildProfiles/Quest_Standalone_Release.buildprofile` and `Windows_Desktop_Release.buildprofile`.
2. **Android Player Settings:**
   - Package ID: `com.gourav.immersiveanatomylab`
   - Version: `1.0.0` (Bundle Version Code: `1`)
   - Scripting Backend: `IL2CPP`
   - Target Architecture: `ARM64` (`arm64-v8a`)
   - Minimum API Level: `Android 10.0 (API Level 29)`
   - Target API Level: `Android API Level 36 (Auto)`
   - Graphics API: `Vulkan` (primary), `OpenGLES3` (fallback)
3. **Texture Memory Budget (ASTC Overrides):**
   - All 18 anatomical PBR texture maps feature platform-isolated Android ASTC overrides (`ASTC_4x4` for normal maps, `ASTC_6x6` for albedo, `ASTC_8x8` for occlusion/roughness).
   - Windows desktop maintains full uncompressed BC7 / DXT5 textures for maximum visual clarity on PC displays.
4. **Cloud Backend Endpoint:**
   - `Assets/Scripts/Learning/LLMAnatomyClient.cs` is configured with `BackendEnvironment.ProductionRenderCloud` targeting `https://immersive-anatomy-api.onrender.com`.

---

## 3. Q6.2 — Meta Quest APK Build & Signing Verification

### 3.1 Gradle Manifest Merger Conflict Resolution
During initial Gradle compilation, the build failed with:
```text
Manifest merger failed : Attribute meta-data#com.oculus.supportedDevices@value value=(quest|quest2|questpro|quest3) from [:unityLibrary] AndroidManifest.xml:42:13-57
is also present at [:unityLibrary:xrmanifest.androidlib] AndroidManifest.xml:28:13-64 value=(quest|quest2|cambria|eureka|quest3s).
Suggestion: add 'tools:replace="android:value"' to <meta-data> element at AndroidManifest.xml to override.
```
**Resolution:** Updated `Assets/Plugins/Android/AndroidManifest.xml` and `DualPlatformReleaseManager.cs` to declare `xmlns:tools="http://schemas.android.com/tools"` and appended `tools:replace="android:value"` to the `com.oculus.supportedDevices` element. Subsequent Gradle merge succeeded cleanly with zero attribute collisions.

### 3.2 Badging & Package Inspection (`aapt dump badging`)
Inspection of the compiled APK confirmed valid package metadata:
- **Package Name:** `com.gourav.immersiveanatomylab`
- **Version Code:** `1`
- **Version Name:** `1.0.0`
- **SDK Target:** `minSdkVersion: 29`, `targetSdkVersion: 36`
- **Native Architecture:** `arm64-v8a` (IL2CPP)
- **VR Headtracking Feature:** `<uses-feature android:name="android.hardware.vr.headtracking" android:required="true" android:version="1" />`
- **Touchscreen Requirement:** `android.hardware.touchscreen` marked `required="false"`
- **Permissions:** `android.permission.INTERNET`, `android.permission.ACCESS_NETWORK_STATE`, `org.khronos.openxr.permission.OPENXR`
- **Application Label:** `Immersive Anatomy Lab`
- **Launchable Activity:** `com.unity3d.player.UnityPlayerActivity`

### 3.3 Release Signature Verification (`apksigner`)
The APK was signed using `secrets/immersiveanatomy.keystore` and verified with Android SDK `apksigner`:
- **Verification Result:** `Verifies: true`
- **Signature Scheme:** `APK Signature Scheme v2: true`
- **Signer DN:** `CN=Gourav Sharma, OU=Medical Lab, O=Immersive Anatomy Lab, L=Bengaluru, ST=Karnataka, C=IN`
- **Signer Certificate SHA-256 Digest:** `a78d36214274f3f6bb94f3c243332ec735de3d8c6434fcad42d254b41181abe9`
- **Public Key Algorithm:** RSA 2048-bit

---

## 4. Q6.3 — Windows Desktop Package Validation

1. **Clean Directory Extraction:**
   - The production distribution archive `Builds/ImmersiveAnatomyLab-Windows-v1.0.0.zip` was extracted to a clean standalone test directory (`scratch/windows_extracted_test/`).
   - Total uncompressed size: **247.45 MB** across all assemblies and runtime dependencies.
2. **Directory Contents Verification:**
   - Executable: `ImmersiveAnatomyLab.exe` (667,136 bytes)
   - Runtime Engine: `UnityPlayer.dll` (84,319,144 bytes)
   - Data & Assets: `ImmersiveAnatomyLab_Data/`
   - Managed Assemblies & Script Engine: `MonoBleedingEdge/`
   - Graphics & ML Libraries: `DirectML.dll`, `D3D12/D3D12Core.dll`, `UnityCrashHandler64.exe`
3. **Standalone Runtime Launch:**
   - Launching `ImmersiveAnatomyLab.exe` without an XR runtime attached initialized cleanly:
     - DirectX 12 graphics client initialized.
     - New Input System initialized in dual/desktop mode.
     - Physics module fallback backend loaded (50 Hz simulation interval).
     - XR loader gracefully remained inactive on standalone desktop (`InitManagerOnStart: false`), eliminating any VR hardware requirement for laptop/desktop users.

---

## 5. Q6.4 — Meta Quest Physical Device Validation

### 5.1 ADB Hardware Discovery Status
An ADB device interrogation was executed on the host system:
```powershell
adb devices -l
# Result: List of devices attached (empty)
```
**Conclusion:** No physical Meta Quest headset was connected via USB/Wi-Fi ADB at the time of verification.

### 5.2 Strict Non-Fabrication Disclosure
> [!IMPORTANT]
> In accordance with project integrity standards, **zero headset performance metrics (72 Hz / 90 Hz FPS, frame timing, battery drain, or thermal telemetry) have been fabricated**.
> Physical Quest hardware tests are officially classified as **`NOT TESTED`**.

### 5.3 Physical Device Deployment & Validation Runbook
When a physical Meta Quest 2, 3, 3S, or Pro headset is connected, run the following steps:
```bash
# 1. Verify ADB connection and developer mode authorization
adb devices -l

# 2. Sideload the release candidate APK
adb install -r "Builds/Quest/ImmersiveAnatomyLab-Quest-v1.0.0.apk"

# 3. Launch application on headset
adb shell monkey -p com.gourav.immersiveanatomylab -c android.intent.category.LAUNCHER 1

# 4. Stream real-time XR compositor performance & FPS
adb shell setprop debug.oculus.fps 1
adb logcat -s Unity OpenXR-Loader OculusXRPlugin
```

---

## 6. Q6.5 — Automated Regression & Security Validation

### 6.1 Strict Zero Organ Grabbing Scan
A full recursive AST scan across all scenes, prefabs, and ScriptableObject assets in `Assets/` was executed:
- **`XRGrabInteractable` instances found:** **`0`**
- **Architecture:** Anatomical specimens utilize `XRSimpleInteractable` with an `IsSelectableBy` guard override that explicitly rejects `Direct`, `Near`, `Poke`, and `Grip` interactors—restricting interaction exclusively to deliberate ray/laser pointers.

### 6.2 Deterministic Snap Distances
- **Anatomical Socket Snap Radius:** `VRGrabbableOrgan.cs` line 22 enforces `snapDistance = 0.35f` (35 cm threshold to dissection table cavity).
- **Inspection Pedestal Snap Radius:** `OrganComparisonController.cs` line 15 enforces `snapDistance = 0.95f` (95 cm threshold to left/right comparison pedestals).

### 6.3 Production Cloud Backend Verification
The live cloud service at `https://immersive-anatomy-api.onrender.com` was queried directly:
- **Liveness Probe (`/health/live`):** `200 OK` (`{"status":"alive"}`)
- **Readiness Probe (`/health/ready`):** `200 OK` (`{"status":"ready", "gemini_configured": true, "rag_entries": 9}`)
- **Live AI Tutor Inference (`/api/chat`):** Tested with clinical coronary circulation query. Returned `200 OK` with medically accurate response powered by Gemini 1.5 Flash, 2 literature citations (Guyton & Hall, Gray's Anatomy), and valid MP3 audio URL (`/api/audio/tts_cd071dba.mp3`).
- **Backend Test Suite (`backend/test_backend.py`):** **8 / 8 tests passed** (100% test pass rate).

### 6.4 Security & Secrets Isolation
- `secrets/immersiveanatomy.keystore` and `secrets/signing_config.json` are isolated and protected by `.gitignore`.
- No credentials, passwords, or keystores are tracked in git history.
- `Builds/` directory containing binaries is verified gitignored (`git status --ignored -s Builds/`).

---

## 7. Q6.6 — Dual Platform Release Candidate Status

Both platforms have met all automated compilation, packaging, signing, and security requirements:

| Platform Release Package | Distribution Target | File Status | Ready for itch.io Publication |
| :--- | :--- | :--- | :--- |
| `ImmersiveAnatomyLab-Quest-v1.0.0.apk` | Meta Quest (Standalone VR) | Signed APK (61.5 MB) | **READY (PENDING PHYSICAL HEADSET VERIFICATION)** |
| `ImmersiveAnatomyLab-Windows-v1.0.0.zip` | Windows 10/11 x64 (Desktop) | Standalone ZIP (91.2 MB) | **READY FOR IMMEDIATE PUBLICATION** |
| `https://immersive-anatomy-api.onrender.com` | Production AI Cloud Service | Healthy & Live | **ACTIVE IN PRODUCTION** |

**Recommendation:** Proceed to **Phase Q7 (itch.io Publication)** to create the project release page and prepare download files.
