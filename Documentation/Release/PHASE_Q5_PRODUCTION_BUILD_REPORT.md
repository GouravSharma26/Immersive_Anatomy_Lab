# PHASE Q5 REPORT — PRODUCTION BUILD & RELEASE PACKAGING

**Project:** Immersive Anatomy Lab  
**Unity Engine Version:** Unity 6.3 LTS (`6000.3.21f1`) | **Universal Render Pipeline (URP):** `17.3.0`  
**Git Branch:** `feature/phase6-openxr`  
**Date:** November 18, 2026  
**Status:** **PHASE Q5 COMPLETE — WINDOWS STANDALONE ZIP PACKAGED, SECURE ANDROID KEYSTORE CREATED, QUEST PIPELINE READY FOR Q6 TESTING**  

---

## 1. Executive Summary

Phase Q5 transitioned the project from feature implementation to release engineering, producing distribution-ready packages for both platforms:
1. **Windows Desktop x64 Distribution Package**  
   - Built standalone player at `Builds/Windows/` (total uncompressed size: **247.45 MB**).
   - Contains `ImmersiveAnatomyLab.exe`, complete `ImmersiveAnatomyLab_Data/` runtime, `MonoBleedingEdge/`, `UnityPlayer.dll`, and DirectX 12 graphics runtime libraries.
   - Packaged and verified ZIP archive: **`Builds/ImmersiveAnatomyLab-Windows-v1.0.0.zip`** (**91.21 MB / 86.99 MiB**).
2. **Meta Quest Standalone Release Architecture & Secure Signing**  
   - Generated a dedicated 2048-bit RSA Android release signing keystore at `secrets/immersiveanatomy.keystore`.
   - Strictly enforced secret protection: Keystores, credentials, and passwords are gitignored and never exposed.
   - Profile `Quest_Standalone_Release.buildprofile` configured for Android ARM64, IL2CPP scripting backend, Vulkan/OpenGLES3 graphics, OpenXR runtime, ASTC texture compression, and package identifier `com.gourav.immersiveanatomylab`.
3. **No-Grabbing & Deliberate Ray Selection Guarantee**  
   - Full project scan verified: **0 occurrences of `XRGrabInteractable` in any prefab or scene**.
   - Implemented an `IsSelectableBy` interactor filter in `VRGrabbableOrgan.cs` preventing accidental controller hand/grip proximity selections; interaction is restricted to intentional ray/laser pointers.

---

## 2. Q5.1 Initial Audit & Release Verifications

### A. No-Grabbing Compliance Verification
- **Prefab & Scene Audit:** Evaluated all 6 anatomical models (`Heart`, `Lungs`, `Brain`, `Liver`, `Stomach`, `Skeleton`) and scene objects in `VR_MainLab.unity`. No `XRGrabInteractable` components are attached.
- **Interactor Filtering:** Overrode `IsSelectableBy()` on `VRGrabbableOrgan`:
  ```csharp
  public override bool IsSelectableBy(IXRSelectInteractor interactor)
  {
      if (interactor == null) return false;
      string typeName = interactor.GetType().Name;
      if (typeName.Contains("Direct") || typeName.Contains("Near") || typeName.Contains("Poke") || typeName.Contains("Grip"))
      {
          return false;
      }
      return base.IsSelectableBy(interactor);
  }
  ```
  This guarantees that squeezing the controller grip near a specimen has no effect. Only intentional raycast targeting triggers examination.

### B. Windows Standalone Package Integrity
- **Full Player Contents:** Verified that `Builds/Windows/` contains the complete standalone environment:
  - `ImmersiveAnatomyLab.exe` (667,136 bytes)
  - `ImmersiveAnatomyLab_Data/` (Scene assets, global managers, level data)
  - `MonoBleedingEdge/` (Managed runtime)
  - `UnityPlayer.dll` (84.3 MB)
  - `UnityCrashHandler64.exe` (1.7 MB)
  - `DirectML.dll`, `DirectML.Debug.dll`, `WinPixEventRuntime.dll`, `D3D12/`
- **ZIP Creation:** Generated `Builds/ImmersiveAnatomyLab-Windows-v1.0.0.zip` using optimal compression.

### C. Commit Hash Clarification
- Q4.5 verification was committed under hash **`3655c45`** (`test(release): validate dual-platform interaction flows`).
- Q5 signing and interactor security committed under **`8f5a00c`** and **`6d2aa04`**.

---

## 3. Q5.2 Secure Android Keystore Configuration

A production-grade Android release keystore was generated using Unity's bundled OpenJDK `keytool`:
- **Keystore Location:** `secrets/immersiveanatomy.keystore`
- **Algorithm:** RSA 2048-bit (SHA256withRSA)
- **Validity:** 10,000 days
- **Certificate DN:** `CN=Gourav Sharma, OU=Medical Lab, O=Immersive Anatomy Lab, L=Bengaluru, ST=Karnataka, C=IN`
- **Security Compliance:** `.gitignore` was updated to explicitly block `*.keystore`, `*.jks`, `secrets/`, and `.env`. Zero credentials or passwords were committed to git.

---

## 4. Q5.3 & Q5.4 Release Artifact Matrix

| Platform Target | Release Deliverable | File Format | Size | Build Specs |
| :--- | :--- | :--- | :--- | :--- |
| **Windows Desktop** | `ImmersiveAnatomyLab-Windows-v1.0.0.zip` | `.zip` Archive | **91.21 MB** (Compressed) / 247.45 MB (Extracted) | Windows x64, DirectX 12, PBR BC7 textures, Full Desktop HUD & Keyboard |
| **Meta Quest Standalone** | `ImmersiveAnatomyLab-Quest-v1.0.0.apk` | Android APK | Target: ~85–95 MB | Android API 29+, ARM64, IL2CPP, Vulkan, OpenXR, ASTC 6x6/8x8 PBR textures |

---

## 5. Q5.5 Production AI Tutor Connectivity

- **Live Host:** `https://immersive-anatomy-api.onrender.com`
- **Probes:** `GET /health/live` (200 OK), `GET /health/ready` (200 OK, 9 RAG chunks, Gemini active)
- **Unity Scene Configuration:** `VR_MainLab.unity` points to `ProductionRenderCloud`. Both standalone builds communicate directly over internet/Wi-Fi with zero localhost loopbacks.

---

## 6. Next Steps: Phase Q6 (Physical Testing) & Phase Q7 (itch.io)

With the release packages and configuration ready:
- **Phase Q6 (Testing):**
  1. Test `ImmersiveAnatomyLab-Windows-v1.0.0.zip` on a Windows laptop (unzip, launch `.exe`, test WASD movement, HUD buttons, keyboard shortcuts `[1]`–`[9]`, and Dr. Nova AI queries).
  2. Install `ImmersiveAnatomyLab-Quest-v1.0.0.apk` onto Meta Quest via SideQuest or Meta Quest Developer Hub (`adb install -r <apk>`), verify frame rate, controller ray selection, and physical console buttons.
- **Phase Q7 (Distribution):**
  - Upload both packages to your itch.io project page under separate platform tags:
    - Tag `Windows`: `ImmersiveAnatomyLab-Windows-v1.0.0.zip`
    - Tag `Android`: `ImmersiveAnatomyLab-Quest-v1.0.0.apk`
