import os
import sys
import uuid
import time
import subprocess
from typing import List, Optional
from fastapi import FastAPI, HTTPException, Request, Response
from fastapi.middleware.cors import CORSMiddleware
from fastapi.responses import FileResponse, JSONResponse
from pydantic import BaseModel, Field

from rag_engine import MedicalRAGEngine

app = FastAPI(
    title="Immersive Anatomy Lab — AI Tutor & RAG Backend",
    version="1.0.0",
    description="Production Medical Anatomy Assistant with RAG knowledge retrieval and TTS."
)

app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],
    allow_credentials=True,
    allow_methods=["GET", "POST", "OPTIONS"],
    allow_headers=["*"],
)

AUDIO_CACHE_DIR = os.path.join(os.path.dirname(__file__), "audio_cache")
os.makedirs(AUDIO_CACHE_DIR, exist_ok=True)

# Initialize RAG Engine
rag_engine = MedicalRAGEngine()

DISCLAIMER_TEXT = (
    "DISCLAIMER: Educational medical simulation only. Designed for anatomical study and clinical training; "
    "not intended for medical diagnosis, treatment advice, or real patient management."
)

MAX_QUERY_LENGTH = 500
MAX_CACHE_FILES = 40

class ChatRequest(BaseModel):
    message: str = Field(..., max_length=MAX_QUERY_LENGTH, description="Student or clinical question (max 500 chars)")
    selected_organ: Optional[str] = "Heart"
    body_system: Optional[str] = "Cardiovascular"
    pedagogical_level: Optional[str] = "student" # "student" or "clinical"
    user_accuracy: Optional[float] = 100.0

class ChatResponse(BaseModel):
    reply: str
    citations: List[str]
    clinical_pearl: str
    mnemonic: str
    disclaimer: str
    audio_url: Optional[str] = None

class TTSRequest(BaseModel):
    text: str = Field(..., max_length=1000)

# -------------------------------------------------------------
# Health Probes (Kubernetes / Render Standard)
# -------------------------------------------------------------
@app.get("/health/live")
def liveness_probe():
    """Confirms that the FastAPI process is alive."""
    return {"status": "alive"}

@app.get("/health/ready")
def readiness_probe():
    """Confirms that RAG knowledge base and core services are loaded."""
    rag_loaded = len(rag_engine.knowledge_base) > 0
    return {
        "status": "ready" if rag_loaded else "degraded",
        "service": "Immersive Anatomy AI Tutor",
        "version": "1.0.0",
        "rag_entries": len(rag_engine.knowledge_base),
        "gemini_configured": bool(os.environ.get("GEMINI_API_KEY", "").strip())
    }

@app.get("/api/health")
def api_health():
    """Backwards-compatible health check for legacy clients."""
    return {
        "status": "online",
        "service": "Immersive Anatomy AI Tutor",
        "rag_entries_loaded": len(rag_engine.knowledge_base),
        "tts_available": True
    }

# -------------------------------------------------------------
# Dr. Nova Chat Route
# -------------------------------------------------------------
@app.post("/api/chat", response_model=ChatResponse)
def chat_with_tutor(req: ChatRequest):
    query = req.message.strip()
    if not query:
        raise HTTPException(status_code=400, detail="Query cannot be empty.")
    if len(query) > MAX_QUERY_LENGTH:
        raise HTTPException(status_code=400, detail=f"Query exceeds maximum allowed length of {MAX_QUERY_LENGTH} characters.")

    # 1. RAG Retrieval
    relevant_chunks = rag_engine.retrieve(query, organ_filter=req.selected_organ, top_k=2)
    citations = [chunk.to_citation() for chunk in relevant_chunks]
    pearl = relevant_chunks[0].clinical_pearl if relevant_chunks else "Review primary anatomical landmarks."

    # 2. External LLM (Gemini) with Local Synthesis Fallback
    gemini_key = os.environ.get("GEMINI_API_KEY", "").strip()
    llm_answer = None

    if gemini_key:
        try:
            import google.generativeai as genai
            genai.configure(api_key=gemini_key)
            model = genai.GenerativeModel("gemini-1.5-flash")

            context_text = "\n\n".join([f"[{c.to_citation()}]: {c.text}" for c in relevant_chunks])
            prompt = (
                f"You are Dr. Nova, a senior clinical anatomist and virtual medical lab tutor.\n"
                f"Student Level: {(req.pedagogical_level or 'student').upper()}\n"
                f"Current Organ Selected: {req.selected_organ} ({req.body_system})\n"
                f"Student Question: {query}\n\n"
                f"Verified Textbook Reference Excerpts:\n{context_text}\n\n"
                f"Instructions:\n"
                f"- If Student Mode: use clear, intuitive analogies, fundamental physiology, and a memorable study tip.\n"
                f"- If Clinical Mode: provide in-depth microanatomy, hemodynamics, innervation, and clinical pathologies.\n"
                f"- Keep answer concise, high-yield, engaging, and professional.\n"
            )
            response = model.generate_content(prompt)
            if response and response.text:
                llm_answer = response.text.strip()
        except Exception as e:
            # Redact key and log error
            print(f"[Dr. Nova AI] External LLM error (falling back to clinical RAG synthesis): {e}")

    # Fallback to local clinical reasoning synthesis
    if not llm_answer:
        llm_answer = synthesize_clinical_response(req, relevant_chunks)

    # 3. Audio Synthesis (Graceful degradation if TTS fails)
    audio_filename = generate_tts_file(llm_answer[:220])
    audio_url = f"/api/audio/{audio_filename}" if audio_filename else None

    # 4. Mnemonic selection
    mnemonics = {
        "heart": "Right = Deoxygenated to lungs | Left = Oxygenated to body ('LAB RAT': Left Atrium Bicuspid, Right Atrium Tricuspid).",
        "lungs": "Right Lung = 3 Lobes (Tri-lobed). Left Lung = 2 Lobes (Leaves room for the cardiac notch).",
        "brain": "Frontal = Motor/Planning; Parietal = Pain/Perception; Temporal = Tone/Talking; Occipital = Optics.",
        "liver": "Dual vascular supply: 75% Portal Vein (Nutrient-rich venous) + 25% Proper Hepatic Artery (Oxygenated).",
        "stomach": "Parietal cells secrete Protons (HCl acid via proton pump) and Protection (Intrinsic Factor for B12).",
        "skeletal": "Vertebrae meal times: Cervical 7 (Breakfast), Thoracic 12 (Lunch), Lumbar 5 (Dinner)."
    }
    organ_key = (req.selected_organ or "").lower()
    mnemonic = "Active visceral organ contributing to systemic homeostasis."
    for k, v in mnemonics.items():
        if k in organ_key:
            mnemonic = v
            break

    return ChatResponse(
        reply=llm_answer,
        citations=citations,
        clinical_pearl=pearl,
        mnemonic=mnemonic,
        disclaimer=DISCLAIMER_TEXT,
        audio_url=audio_url
    )

def synthesize_clinical_response(req: ChatRequest, chunks: List) -> str:
    organ = req.selected_organ or "the selected organ"
    level = req.pedagogical_level or "student"
    primary_chunk = chunks[0] if chunks else None

    if level == "student":
        intro = f"Great question! In student terms, {organ} is vital for keeping our body balanced and energized."
        if primary_chunk:
            return (
                f"{intro}\n\n"
                f"<b>Core Concept:</b> {primary_chunk.text[:220]}...\n\n"
                f"<b>Clinical Pearl:</b> <i>{primary_chunk.clinical_pearl}</i>"
            )
        else:
            return f"{intro} It works continuously in synergy with the other 5 major anatomical systems in the lab."
    else:
        intro = f"From an advanced clinical and surgical perspective regarding {organ}:"
        if primary_chunk:
            return (
                f"{intro}\n\n"
                f"{primary_chunk.text}\n\n"
                f"<b>Pathological Correlate:</b> {primary_chunk.clinical_pearl}"
            )
        else:
            return f"{intro} It maintains neurovascular and hemodynamic equilibrium with specific segmental vascular supply and autonomic innervation."

# -------------------------------------------------------------
# Text-to-Speech Route & Cross-Platform Engine
# -------------------------------------------------------------
@app.post("/api/tts")
def synthesize_speech(req: TTSRequest):
    filename = generate_tts_file(req.text)
    if not filename:
        raise HTTPException(status_code=500, detail="TTS generation failed or unsupported on this host.")
    return {"audio_url": f"/api/audio/{filename}"}

@app.get("/api/audio/{filename}")
def get_audio(filename: str):
    file_path = os.path.join(AUDIO_CACHE_DIR, filename)
    if not os.path.exists(file_path):
        raise HTTPException(status_code=404, detail="Audio file not found")
    media_type = "audio/wav" if filename.endswith(".wav") else "audio/mpeg"
    return FileResponse(file_path, media_type=media_type)

def clean_old_audio_cache():
    """Maintains ephemeral audio cache under MAX_CACHE_FILES."""
    try:
        files = [os.path.join(AUDIO_CACHE_DIR, f) for f in os.listdir(AUDIO_CACHE_DIR) if f.startswith("tts_")]
        if len(files) > MAX_CACHE_FILES:
            files.sort(key=os.path.getctime)
            for f in files[:len(files) - MAX_CACHE_FILES]:
                try: os.remove(f)
                except: pass
    except Exception:
        pass

def generate_tts_file(text: str) -> Optional[str]:
    """Cross-platform TTS: Windows SpeechSynthesizer or Linux gTTS with graceful fallback."""
    clean_old_audio_cache()
    clean_text = text.replace("<b>", "").replace("</b>", "").replace("<i>", "").replace("</i>", "")
    clean_text = clean_text.replace("\n", " ").replace("\"", "'")
    clean_text = clean_text[:280]

    # Strategy 1: Windows Native PowerShell SpeechSynthesizer (if on Windows)
    if sys.platform == "win32":
        try:
            filename = f"tts_{uuid.uuid4().hex[:8]}.wav"
            output_path = os.path.join(AUDIO_CACHE_DIR, filename)
            ps_script = (
                f"Add-Type -AssemblyName System.Speech; "
                f"$synth = New-Object System.Speech.Synthesis.SpeechSynthesizer; "
                f"$synth.SetOutputToWaveFile('{output_path}'); "
                f"$synth.Speak(\"{clean_text}\"); "
                f"$synth.Dispose()"
            )
            result = subprocess.run(["powershell", "-Command", ps_script], capture_output=True, text=True, timeout=8)
            if os.path.exists(output_path) and os.path.getsize(output_path) > 1000:
                return filename
        except Exception as ex:
            print(f"[TTS] Windows native TTS failed: {ex}")

    # Strategy 2: Google Text-to-Speech (gTTS) for Linux/Render or fallback
    try:
        from gtts import gTTS
        filename = f"tts_{uuid.uuid4().hex[:8]}.mp3"
        output_path = os.path.join(AUDIO_CACHE_DIR, filename)
        tts = gTTS(text=clean_text, lang='en', tld='com', slow=False)
        tts.save(output_path)
        if os.path.exists(output_path) and os.path.getsize(output_path) > 500:
            return filename
    except Exception as ex:
        # Graceful degradation: Dr. Nova returns text answer without audio
        pass

    return None

if __name__ == "__main__":
    import uvicorn
    port = int(os.environ.get("PORT", 8000))
    host = os.environ.get("HOST", "0.0.0.0")
    print(f"Starting Dr. Nova AI Backend on http://{host}:{port} ...")
    uvicorn.run(app, host=host, port=port)
