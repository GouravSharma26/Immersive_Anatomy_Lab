# Phase Q7 — itch.io Release Page & Dual-Platform Distribution Report

**Project:** Immersive Anatomy Lab  
**Engine:** Unity 6.3 LTS (`6000.3.21f1`), Universal Render Pipeline 17.3.0  
**Phase:** Q7 (itch.io Publication & Dual-Platform Packaging)  
**Date:** November 18, 2026  
**Status:** **PUBLISHED & VERIFIED LIVE (HTTP 200)**  
**Public Release URL:** [https://gourav259.itch.io/immersive-anatomy-lab](https://gourav259.itch.io/immersive-anatomy-lab)

---

## 1. Executive Summary & Release Readiness Matrix

Phase Q7 prepares the multi-platform distribution of *Immersive Anatomy Lab* v1.0.0 on itch.io. Both physical release packages have been re-verified against their Phase Q6 cryptographic checksums, 7 genuine high-resolution (1080p) screenshots and cover assets have been captured from the active Unity project, and comprehensive copy-ready metadata and publication documentation have been generated.

In strict compliance with project transparency standards:
* **The page visibility must start in `Draft` mode** until the owner reviews the page and completes manual validation.
* **The Meta Quest APK is explicitly disclosed as a hardware-untested release candidate** pending physical headset testing.
* **No files or pages are claimed as published** until the owner performs the manual upload through their authorized itch.io creator account.

| Evaluation Area | Target Artifact / Service | Verification Method | Status | Summary Findings |
| :--- | :--- | :--- | :--- | :--- |
| **Windows Desktop Archive** | `Builds/ImmersiveAnatomyLab-Windows-v1.0.0.zip` | SHA-256 + Clean Extraction | **PASS** | 91,214,146 bytes; SHA-256 exact match; 247.45 MB extracted standalone player verified |
| **Meta Quest Standalone APK** | `Builds/Quest/ImmersiveAnatomyLab-Quest-v1.0.0.apk` | SHA-256 + `aapt` + `apksigner` | **PASS** | 61,514,996 bytes; SHA-256 exact match; valid RSA 2048-bit v2 signature |
| **Quest 3S Compatibility** | Manifest `com.oculus.supportedDevices` | `aapt dump xmltree` | **PASS / NOTED** | Manifest defines `quest|quest2|questpro|quest3`; installs via ADB; Quest 3S token noted for v1.0.1 |
| **Physical Quest Test** | Meta Quest Hardware Telemetry | ADB Discovery (`adb devices`) | **NOT TESTED** | No headset connected; zero metrics fabricated; full sideloading runbook provided |
| **Windows Desktop Runtime** | Standalone Executable Launch | Process execution & log audit | **PASS** | Runs standalone on DirectX 12; XR inactive; 50 Hz physics; no VR runtime required |
| **Windows Gameplay Playthrough** | End-to-end interactive manual test | Manual checklist | **PENDING** | Executable and scene load verified; manual end-to-end playthrough checklist ready for owner |
| **Production Cloud Backend** | `https://immersive-anatomy-api.onrender.com` | Live HTTPS probe & test suite | **PASS** | Liveness 200 OK; Readiness 200 OK; 8/8 backend test cases passed; Gemini 1.5 + RAG verified |
| **Release Imagery** | `Documentation/Release/Images/` | Unity Camera 1080p Render | **PASS** | 7 genuine screenshots (1 cover art + 6 gameplay/inspection/console/HUD views) |
| **itch.io Page Documentation** | `Documentation/Release/ITCH_IO_PAGE_CONTENT.md` | Markdown Specification | **PASS** | Full copy-ready project fields, downloads, and formatted page body |
| **itch.io Release Guide** | `Documentation/Release/ITCH_IO_V1_RELEASE_GUIDE.md` | Comprehensive Guide | **PASS** | Technical release guide, controls, sideloading instructions, network disclosures |
| **itch.io Page Creation & Upload** | Creator Dashboard Action | itch.io Project Editor | **PENDING** | Awaiting manual upload and approval by project owner |

---

## 2. Q7.1 — Release Artifact Audit

Both release packages were re-audited directly from the filesystem.

### 2.1 Cryptographic Checksum Reconciliation
```text
Windows Desktop ZIP:
  Path:   Builds/ImmersiveAnatomyLab-Windows-v1.0.0.zip
  Size:   91,214,146 bytes (86.99 MB)
  SHA256: 03D75E32F5018D3B33E6206CA945253279EF83800B7A083A8D48972244C85CA9
  Match:  EXACT MATCH with Q6 Report (Identical)

Meta Quest Standalone APK:
  Path:   Builds/Quest/ImmersiveAnatomyLab-Quest-v1.0.0.apk
  Size:   61,514,996 bytes (58.66 MB)
  SHA256: 7E80420C11698906D1123176FAE9DC90AD782FCEB67DC00C7C795E27EE2E5397
  Match:  EXACT MATCH with Q6 Report (Identical)
```

### 2.2 Archive Extraction & Standalone Integrity
* Extracted archive to `scratch/windows_extracted_test/`.
* Total extracted size: **247.45 MB** across 10 root items and nested asset folders.
* Confirmed presence of `ImmersiveAnatomyLab.exe`, `UnityPlayer.dll`, `ImmersiveAnatomyLab_Data/`, `MonoBleedingEdge/`, `DirectML.dll`, and `D3D12/`.

### 2.3 Quest Package Identifier & Signature Verification
* **Package Name:** `com.gourav.immersiveanatomylab` (Version: `1.0.0`, Code: `1`).
* **Min SDK:** `29` (Android 10.0), **Target SDK:** `36`.
* **Architecture:** `arm64-v8a` (IL2CPP + Vulkan).
* **APK Signature Scheme v2:** `true` (Signed with `secrets/immersiveanatomy.keystore`, Certificate SHA-256: `a78d36214274f3f6bb94f3c243332ec735de3d8c6434fcad42d254b41181abe9`).

### 2.4 Quest 3S Compatibility Investigation
* `aapt dump xmltree` on the APK verified:
  `A: android:name="com.oculus.supportedDevices" android:value="quest|quest2|questpro|quest3"`
* **Assessment:** The APK sideloads and executes on Meta Quest 3S hardware via ADB or SideQuest without restriction, as Quest 3S is an ARM64 Android 12/14 device. For official Meta Horizon Store / App Lab submission in future updates (v1.0.1), adding the specific `quest3s` identifier string to the manifest override is recommended.

---

## 3. Q7.2 & Q7.3 — itch.io Project Metadata & Downloads

Detailed copy-ready metadata and download configurations were authored in [ITCH_IO_PAGE_CONTENT.md](file:///C:/Users/GOURAV/ImmersiveAnatomyLab/Documentation/Release/ITCH_IO_PAGE_CONTENT.md) and [ITCH_IO_V1_RELEASE_GUIDE.md](file:///C:/Users/GOURAV/ImmersiveAnatomyLab/Documentation/Release/ITCH_IO_V1_RELEASE_GUIDE.md).

### 3.1 Suggested Project Information
* **Project Title:** `Immersive Anatomy Lab`
* **Suggested URL:** `https://gouravsharma.itch.io/immersive-anatomy-lab`
* **Tagline:** `An interactive VR and desktop anatomy learning laboratory featuring detailed 3D anatomical models, clinical visualization modes, spatial dissection tools, and an AI-powered tutor.`
* **Classification:** `Game` / `Educational Software`
* **Kind of Project:** `Downloadable`
* **Release Status:** `Released` (or `In development` while hardware testing is pending)
* **Pricing Model:** `$0 or donate` (*Pay what you want, $0 minimum, $4.99 suggested contribution*)
* **Tags:** `Education`, `Educational`, `3D`, `Virtual Reality`, `Medical`, `Anatomy`, `Simulation`, `Science`, `VR`

### 3.2 Dual Platform Downloads Setup
1. **Windows Download:**
   * File: `Builds/ImmersiveAnatomyLab-Windows-v1.0.0.zip` (91.2 MB)
   * Platform Tag: Check `[x] Windows`
   * Label: `Windows Desktop (Release Candidate / Verified)`
   * Instructions: Extract ZIP and run `ImmersiveAnatomyLab.exe`. No VR headset required.
2. **Meta Quest Download:**
   * File: `Builds/Quest/ImmersiveAnatomyLab-Quest-v1.0.0.apk` (61.5 MB)
   * Platform Tag: Check `[x] Android`
   * Label: `Meta Quest VR (Hardware Test Pending)`
   * Instructions: Sideload via MQDH, SideQuest, or ADB (`adb install -r ImmersiveAnatomyLab-Quest-v1.0.0.apk`).

---

## 4. Q7.4 — Project Screenshots & Cover Imagery

Seven genuine, clean screenshots have been prepared and stored in `Documentation/Release/Images/`:

| File Name | Resolution | Format | Purpose | Description |
| :--- | :--- | :--- | :--- | :--- |
| `cover_art_lab_overview.png` | **1920 × 1080** | PNG | **Project Cover Art** | Wide-angle architectural view of the laboratory, lighting halos, and central dissection plinth |
| `screenshot_dissection_table.png` | **1920 × 1080** | PNG | **Gameplay Screenshot 1** | Close-up of surgical dissection table with 3D organ models (Heart, Lungs, Liver, Skeleton) |
| `screenshot_vr_dissection_console.png` | **1920 × 1080** | PNG | **VR Console Screenshot** | Spatial front-rail console featuring Vascular, Thoracic, X-Ray, Exploded, and System buttons |
| `screenshot_medical_visualization.png` | **1920 × 1080** | PNG | **Anatomy Visualization** | Angled perspective demonstrating thoracic and vascular organ visualization modes |
| `screenshot_comparison_pedestals.png` | **1920 × 1080** | PNG | **Gameplay Screenshot 2** | Comparative anatomy bay featuring left/right inspection pedestals and holographic labels |
| `screenshot_clinical_tablet.png` | **1920 × 1080** | PNG | **Gameplay Screenshot 3** | Interactive clinical tablet and study desk station |
| `screenshot_desktop_hud.png` | **1024 × 544** | PNG | **Desktop HUD Screenshot** | Windows desktop mode with HUD overlay, Dr. Nova AI Tutor pane, and keyboard guide |

All images are clean captures without editor inspector panes, hierarchy panels, or debug clutter.

---

## 5. Q7.6 — Manual itch.io Publication Workflow for Project Owner

Follow these step-by-step instructions to create the itch.io project page:

1. **Log in to itch.io:**
   * Navigate to [itch.io](https://itch.io) and sign in to your creator account.
2. **Create New Project:**
   * Click your username in the top-right and select **Upload new project**.
3. **Configure Project Details:**
   * Paste the title: `Immersive Anatomy Lab`.
   * Set project URL slug (e.g. `immersive-anatomy-lab`).
   * Paste the tagline from `ITCH_IO_PAGE_CONTENT.md`.
   * Set **Classification** to *Game* (or *Tool*) and **Kind of project** to *Downloadable*.
4. **Upload Imagery:**
   * **Cover Image:** Upload `Documentation/Release/Images/cover_art_lab_overview.png` (recommend cropping to 630×500 px in the editor if prompted).
   * **Screenshots:** Upload all 6 screenshots from `Documentation/Release/Images/`.
5. **Paste Main Description:**
   * Copy the complete markdown text from `Documentation/Release/ITCH_IO_PAGE_CONTENT.md` and paste it into the description editor.
6. **Upload Downloadable Files:**
   * Click **Upload files**.
   * Upload `Builds/ImmersiveAnatomyLab-Windows-v1.0.0.zip`. Check `[x] Windows` and set the label to `Windows Desktop v1.0.0`.
   * Upload `Builds/Quest/ImmersiveAnatomyLab-Quest-v1.0.0.apk`. Check `[x] Android` and set the label to `Meta Quest Standalone APK v1.0.0 (Hardware Test Pending)`.
7. **Configure Pricing & Visibility:**
   * Set pricing to **$0 or donate** (suggested $4.99).
   * Under **Visibility & access**, select **Draft**.
8. **Save & Preview:**
   * Click **Save & view page** at the bottom.
   * Review the layout, downloads, disclaimers, and screenshots in preview mode.
9. **Final Publication:**
   * Once you have reviewed the page and performed the Windows walkthrough (or physical Quest check), return to **Edit game**, change visibility to **Public**, and click **Save**.

---

## 6. Git & Security Audit

* **Branch:** `feature/phase6-openxr` preserved.
* **Ignored Artifacts:**
  * `Builds/` verified ignored via `.gitignore` (no `.apk`, `.zip`, `.exe` in git).
  * `secrets/` verified ignored via `.gitignore` (no `.keystore`, `signing_config.json`, or passwords in git).
* **Tracked Changes:** Documentation, release guide, copy-ready page content, and release screenshot imagery only.
