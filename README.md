# Welcome to the Immersive Anatomy Lab

**Immersive Anatomy Lab** is an interactive, multi-platform medical education environment designed for students, healthcare trainees, and anatomy enthusiasts. Experience human anatomy through high-fidelity 3D specimens, advanced clinical visualization shaders, spatial dissection consoles, and an intelligent AI tutor — available for both **Windows Desktop laptops/PCs** and **Meta Quest Standalone VR**.

---

## 🌟 Core Features

* **Detailed 3D Anatomical Organ Systems:**
  Study intricate models including the **Heart, Lungs, Liver, Stomach, Brain, and Complete Skeleton**, with calibrated PBR texturing and anatomical landmarks.

* **Advanced Medical Visualization Modes:**
  * **Vascular Angiogram View:** Pulsatile procedural illumination revealing coronary and vascular flow pathways.
  * **Thoracic Silhouette View:** Contoured anatomical boundary isolation providing depth and spatial relationships in the thoracic cavity.
  * **X-Ray Skeletal Overlay:** Dynamic transparency mode to visualize interior organs aligned within skeletal frames.
  * **Exploded View:** Expand organ groups along anatomical axes to inspect occluded structures.
  * **Comparative Inspection Bay:** Dual pedestals allowing side-by-side comparative inspection, rotation, and scaling of specimens.

* **Dr. Nova — AI Anatomy Tutor:**
  * Powered by **Google Gemini 1.5 Flash** connected to a curated medical Retrieval-Augmented Generation (RAG) knowledge base.
  * Features academic citations from *Gray's Anatomy for Students* and *Guyton & Hall Textbook of Medical Physiology*.
  * Natural neural voice text-to-speech feedback with instant educational offline fallback.

* **Dual Platform Release from One Unified Project:**
  * **Windows Desktop Edition:** Full keyboard and mouse first-person navigation, on-screen interactive HUD, and dedicated inspection controls. No VR hardware required.
  * **Meta Quest Standalone VR Edition:** Full 6-DoF spatial immersion, laser-pointer ray interaction, physical dissection table consoles, and interactive clinical tablets.

---

## 🎮 How to Play / Controls

### Windows Laptop & Desktop Controls
* **Move:** `W` / `A` / `S` / `D` or Arrow Keys
* **Look / Aim:** Mouse
* **Select / Inspect Specimen:** `Left Mouse Click` or `Space` / `E`
* **Rotate Selected Organ:** Hold `Q` or `E`
* **Zoom / Scale Organ:** Mouse Scroll Wheel
* **Orbit Camera Inspection:** Hold `Right Mouse Click`
* **Ask Dr. Nova AI:** Press `T` or `Enter` to open chat
* **Voice Mute:** Press `V`
* **Toggle Shortcuts Guide:** Press `H` or `Tab`
* **Medical Shaders:**
  * `8` — Vascular Angiogram View
  * `9` — Thoracic Silhouette View
  * `X` — X-Ray Skeletal Overlay
  * `Z` — Exploded View
  * `0` — Restore Normal Anatomical PBR
* **Comparative Bay:** Press `C` to send specimen to comparison pedestal
* **Reset Anatomy:** Press `R` to return all organs to dissection table

### Meta Quest VR Controls
* **Laser Ray Selection:** Point your Touch controller ray at any organ and pull the **Trigger** to focus.
* **Spatial Dissection Console:** Aim and click physical console buttons along the table rail to toggle X-Ray, Vascular, Thoracic, Exploded, and System filter modes.
* **Clinical Tablet:** Interact with quiz modules and clinical scenarios in virtual space.
* **Deterministic Snapping:** Organs automatically snap back into place when placed within 35 cm of their anatomical table cavity, or snap onto comparison pedestals within 95 cm.
* **Strict Non-Grabbing Architecture:** Anatomical organs cannot be grabbed or thrown. Spatial selection is restricted exclusively to deliberate laser pointer rays to guarantee clinical precision.

---

## 📥 Installation Instructions

### Windows Desktop:
1. Download `ImmersiveAnatomyLab-Windows-v1.0.0.zip`.
2. Extract the complete `.zip` archive to any local folder.
3. Run `ImmersiveAnatomyLab.exe`. *(Keep all files and folders together)*.

### Meta Quest Standalone VR:
1. Download `ImmersiveAnatomyLab-Quest-v1.0.0.apk`.
2. Connect your Meta Quest (with Developer Mode enabled) to your computer.
3. Sideload via **Meta Quest Developer Hub (MQDH)**, **SideQuest**, or run via ADB:
   `adb install -r ImmersiveAnatomyLab-Quest-v1.0.0.apk`
4. Put on your headset and launch **Immersive Anatomy Lab** from **App Library > Unknown Sources**.

---

## ⚠️ Important Testing & Transparency Disclosures

* **Meta Quest Hardware Verification Status:**
  The Meta Quest APK is a compiled and cryptographically signed ARM64 release candidate (`com.gourav.immersiveanatomylab`). Because verification was performed without physical Quest hardware connected, in-headset frame rate (72 Hz / 90 Hz), thermal performance, and battery telemetry are marked **`NOT TESTED`**.
* **Network & Privacy:**
  Cloud AI features require an active internet connection. No personal user data or personal health identifiers are recorded or stored.

---

## 📜 Credits & Technology Stack
* **Engine:** Unity 6.3 LTS (Universal Render Pipeline)
* **VR Framework:** Unity OpenXR & XR Interaction Toolkit
* **AI Architecture:** FastAPI + Google Gemini 1.5 Flash + Medical RAG Knowledge Base + Edge TTS
* **Developer:** Gourav Sharma
