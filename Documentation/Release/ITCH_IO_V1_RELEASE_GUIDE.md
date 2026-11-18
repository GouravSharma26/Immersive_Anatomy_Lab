# Immersive Anatomy Lab — itch.io v1.0.0 Release & Distribution Guide

**Project:** Immersive Anatomy Lab  
**Version:** v1.0.0  
**Engine:** Unity 6.3 LTS (`6000.3.21f1`), Universal Render Pipeline 17.3.0  
**Release Date:** November 18, 2026  
**Status:** Release Candidates Packaged & Prepared for itch.io Publication  

---

## 1. Release Artifacts & Verification Table

Before distributing or publishing any build files to itch.io, verify the physical files on disk against their cryptographic fingerprints.

| Release Channel | File Name | Target Platform | File Size | SHA-256 Checksum | Signature / Mode | Verification Status |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| **Windows Desktop** | `ImmersiveAnatomyLab-Windows-v1.0.0.zip` | Windows 10/11 x64 PC/Laptop | **91,214,146 bytes** (86.99 MB ZIP; 247.45 MB uncompressed) | `03D75E32F5018D3B33E6206CA945253279EF83800B7A083A8D48972244C85CA9` | Standalone Direct3D 12 Player (No VR headset required) | **VERIFIED PASS** |
| **Meta Quest VR** | `ImmersiveAnatomyLab-Quest-v1.0.0.apk` | Meta Quest 2 / Pro / 3 / 3S | **61,514,996 bytes** (58.66 MB) | `7E80420C11698906D1123176FAE9DC90AD782FCEB67DC00C7C795E27EE2E5397` | Signed APK (v2 Scheme, RSA 2048-bit release keystore) | **BUILD PASS / HARDWARE TEST PENDING** |
| **AI Cloud Tutor** | Cloud Web Service | Web / Client API | Live Endpoint | `https://immersive-anatomy-api.onrender.com` | TLS 1.3 / FastAPI + Gemini 1.5 Flash + RAG + Edge TTS | **ONLINE & HEALTHY** |

---

## 2. Platform Requirements & System Specifications

### 2.1 Windows Desktop Edition
* **Operating System:** Windows 10 (64-bit) or Windows 11 (64-bit)
* **Processor:** Intel Core i5-8400 / AMD Ryzen 5 2600 or higher
* **Memory:** 8 GB RAM minimum (16 GB recommended)
* **Graphics:** NVIDIA GeForce GTX 1060 / AMD Radeon RX 580 or higher with DirectX 12 support
* **Storage:** 500 MB available storage space
* **Input:** Keyboard and Mouse
* **Network:** Broadband Internet connection required for live Dr. Nova AI Tutor and neural text-to-speech (built-in offline curriculum fallback is included when offline)

### 2.2 Meta Quest Standalone VR Edition
* **Headset Family:** Meta Quest 2, Meta Quest Pro, Meta Quest 3, Meta Quest 3S
* **Operating System:** Quest OS v55+ (Android API Level 29 minimum)
* **Architecture:** ARM64 (`arm64-v8a`), IL2CPP with Vulkan Graphics API
* **Tracking:** 6-DoF Roomscale or Stationary Inside-Out Head Tracking
* **Controllers:** Meta Quest Touch / Touch Plus / Touch Pro Controllers with spatial ray interaction
* **Network:** Wi-Fi connection required for real-time cloud AI tutor queries

---

## 3. Platform Installation Guides

### 3.1 Windows Desktop Installation
1. Download `ImmersiveAnatomyLab-Windows-v1.0.0.zip` from the itch.io project page.
2. Right-click the downloaded `.zip` file and select **Extract All...** to an accessible location on your local drive (e.g., `Desktop\ImmersiveAnatomyLab`).
3. Open the extracted folder and double-click `ImmersiveAnatomyLab.exe` to start the application.
4. *Important:* Do **not** move `ImmersiveAnatomyLab.exe` out of its folder. It depends directly on `ImmersiveAnatomyLab_Data/`, `MonoBleedingEdge/`, and `UnityPlayer.dll` located alongside it.

### 3.2 Meta Quest Direct Sideloading Guide
> [!NOTE]
> This APK is a direct standalone distribution file intended for sideloading via developer tools (Meta Quest Developer Hub, SideQuest, or ADB). It is not distributed via the official Meta Horizon Store.

#### Method A: Using Meta Quest Developer Hub (MQDH)
1. Launch **Meta Quest Developer Hub** on your PC.
2. Connect your Meta Quest headset via USB-C and ensure Developer Mode is enabled in the Meta Quest mobile app.
3. Drag and drop `ImmersiveAnatomyLab-Quest-v1.0.0.apk` onto the **Device Manager > Installed Apps** section of MQDH.
4. Put on the headset, open the **App Library**, select the **Unknown Sources** filter from the top-right dropdown, and launch **Immersive Anatomy Lab**.

#### Method B: Using SideQuest
1. Open **SideQuest** (Advanced Installer) and connect your headset.
2. Click the top-bar icon **Install APK file from folder on computer**.
3. Select `ImmersiveAnatomyLab-Quest-v1.0.0.apk` and wait for the installation green indicator.
4. Launch from **App Library > Unknown Sources** in the headset.

#### Method C: Using ADB Command Line
Ensure `adb` is in your system PATH and run:
```bash
# Verify headset is detected and authorized
adb devices -l

# Sideload the release candidate APK
adb install -r ImmersiveAnatomyLab-Quest-v1.0.0.apk

# (Optional) Launch directly via ADB command
adb shell monkey -p com.gourav.immersiveanatomylab -c android.intent.category.LAUNCHER 1
```

---

## 4. User Interaction & Controls Guide

### 4.1 Windows Desktop Keyboard & Mouse Controls
| Input Action | Primary Key / Mouse | Secondary Key | Function |
| :--- | :--- | :--- | :--- |
| **Move Around Lab** | `W` / `A` / `S` / `D` | `Arrow Keys` | First-person laboratory locomotion |
| **Look / Aim** | `Mouse Movement` | — | Camera orientation and cursor aim |
| **Inspect / Select Organ** | `Left Mouse Click` | `Space` / `E` | Select specimen under crosshair/cursor |
| **Rotate Selected Organ** | `Q` / `E` (Hold) | — | Continuous specimen axial rotation |
| **Zoom Isolated Organ** | `Mouse Scroll Wheel` | — | Interactive inspection magnification |
| **Look Orbit Mode** | `Right Mouse Click` (Hold) | — | Smooth camera orbit inspection |
| **Toggle HUD Guide** | `H` | `Tab` | Show/hide keyboard shortcuts overlay |
| **Toggle AI Chat Input** | `T` / `Return` | `/` | Open Dr. Nova text entry field |
| **Send AI Message** | `Enter` | `Keypad Enter` | Transmit clinical query to AI Tutor |
| **Toggle Voice Mute** | `V` | — | Mute/unmute Dr. Nova neural TTS voice |
| **Toggle Lab Lighting** | `L` | — | Switch between full lighting & clinical exam spotlight |
| **X-Ray Skeletal Overlay** | `X` | — | Toggle skeletal transparency overlay |
| **Exploded View Mode** | `Z` | — | Expand organs outward along anatomical axes |
| **Vascular Angiogram View** | `8` | — | Activate pulsating vascular tree visualization |
| **Thoracic Silhouette View** | `9` | — | Activate thoracic cavity boundary silhouette |
| **Restore Normal PBR View** | `0` | — | Reset all visual shaders to standard anatomical PBR |
| **Compare Organs** | `C` | — | Send selected organ to Comparison Pedestal |
| **Reset Table Anatomy** | `R` | — | Return all organs to default dissection plinth |
| **Body System Filters** | `1` to `6` | — | Toggle Skeletal, Cardio, Respiratory, Nervous, Digestive, Body |

### 4.2 Meta Quest Spatial VR Controls
* **Laser Pointer Selection:** Point the right or left controller ray at any anatomical organ specimen and pull the **Trigger** to select and focus.
* **VR Dissection Console:** Physical buttons along the front rail of the dissection plinth can be triggered by pointing and clicking with your controller ray:
  * `X-Ray Mode`
  * `Vascular Angiogram`
  * `Thoracic Silhouette`
  * `Exploded View`
  * `Reset Anatomy`
  * Body System filter toggles (Skeletal, Cardio, Respiratory, Nervous, Digestive)
* **Clinical Tablet:** Held in the virtual space or docked on the study station; provides interactive quizzes, diagnostic prompts, and synchronized visual mode buttons.
* **Deterministic Snapping:** Organs automatically snap back into place when placed within 35 cm of their anatomical table cavity, and snap onto comparison pedestals within 95 cm.
* **Strict Non-Grabbing Architecture:** Anatomical organs cannot be grabbed or thrown. Spatial selection is restricted exclusively to deliberate laser pointer rays to guarantee clinical precision.

---

## 5. Network & Production Backend Disclosure

* **Production Service URL:** `https://immersive-anatomy-api.onrender.com`
* **Protocol:** HTTPS (TLS 1.3)
* **AI Model:** Google Gemini 1.5 Flash via FastAPI microservice
* **RAG Clinical Database:** 9 comprehensive medical anatomy modules (incorporating Gray's Anatomy and Guyton & Hall Physiology citations)
* **Text-to-Speech:** Microsoft Edge Neural TTS streaming MP3 voice responses
* **Privacy & Data Security:** No patient health information (PHI) or personal user identifiers are collected, stored, or transmitted. Chat interactions are used solely for real-time anatomical tutoring during the active session.
* **Offline Resilience:** If network connectivity is unavailable or the cloud service times out, an educational offline fallback database provides instant anatomical responses to prevent disruption of student study.

---

## 6. Known Limitations & Hardware Testing Disclosures

1. **Meta Quest Physical Testing Caveat:**
   * The release APK (`ImmersiveAnatomyLab-Quest-v1.0.0.apk`) has been compiled for ARM64 IL2CPP, verified with package inspection, and cryptographically signed.
   * **Physical headset testing remains pending.** Actual in-headset frame rate (72 Hz vs 90 Hz), thermal performance, and battery consumption on Quest 2/3 hardware have not yet been measured with physical hardware connected.
   * Users sideloading this APK should treat it as a release candidate.
2. **Quest 3S Device Identifier Notice:**
   * The release APK manifest explicitly lists `quest|quest2|questpro|quest3` under `com.oculus.supportedDevices`.
   * The APK installs and executes on Quest 3S via ADB/sideloading. Native Quest OS launcher categorization on Quest 3S hardware will be updated in v1.0.1 to include the specific `quest3s` identifier token.
3. **Windows Laptop Mode:**
   * Desktop mode does not require SteamVR, Oculus Link, or any VR runtime. It runs directly as a standard standalone desktop application.

---

## 7. Asset Licenses & Acknowledgments

* **3D Anatomical Specimen Models:** Created and optimized for the Immersive Anatomy Lab project under standard educational project use.
* **Render Pipeline:** Universal Render Pipeline (URP 17.3.0), Unity Technologies.
* **OpenXR & VR Framework:** Unity XR Interaction Toolkit 3.3.2 and Unity OpenXR Plugin 1.16.1.
* **AI Integration:** Google Gemini 1.5 Flash API via Google Cloud.
* **Medical References:** Citations curated from *Gray's Anatomy for Students* and *Guyton and Hall Textbook of Medical Physiology*.
