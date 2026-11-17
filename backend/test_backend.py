import os
import pytest
from fastapi.testclient import TestClient

# Ensure GEMINI_API_KEY is unset for deterministic local fallback testing
os.environ.pop("GEMINI_API_KEY", None)

from main import app, rag_engine, MAX_QUERY_LENGTH

client = TestClient(app)

def test_liveness_probe():
    response = client.get("/health/live")
    assert response.status_code == 200
    data = response.json()
    assert data.get("status") == "alive"

def test_readiness_probe():
    response = client.get("/health/ready")
    assert response.status_code == 200
    data = response.json()
    assert data.get("status") in ["ready", "degraded"]
    assert data.get("service") == "Immersive Anatomy AI Tutor"
    assert data.get("rag_entries") > 0
    assert "gemini_configured" in data

def test_legacy_api_health():
    response = client.get("/api/health")
    assert response.status_code == 200
    data = response.json()
    assert data.get("status") == "online"
    assert data.get("rag_entries_loaded") > 0

def test_chat_valid_query_student():
    payload = {
        "message": "Explain blood circulation through the heart chambers.",
        "selected_organ": "Heart",
        "body_system": "Cardiovascular",
        "pedagogical_level": "student"
    }
    response = client.post("/api/chat", json=payload)
    assert response.status_code == 200
    data = response.json()
    assert "reply" in data and len(data["reply"]) > 0
    assert "citations" in data and len(data["citations"]) > 0
    assert "clinical_pearl" in data and len(data["clinical_pearl"]) > 0
    assert "mnemonic" in data and "LAB RAT" in data["mnemonic"]
    assert "disclaimer" in data and "simulation" in data["disclaimer"].lower()

def test_chat_valid_query_clinical():
    payload = {
        "message": "Detail the Circle of Willis and its collateral circulation.",
        "selected_organ": "Brain",
        "body_system": "Nervous",
        "pedagogical_level": "clinical"
    }
    response = client.post("/api/chat", json=payload)
    assert response.status_code == 200
    data = response.json()
    assert "reply" in data
    assert "citations" in data
    assert "mnemonic" in data

def test_chat_empty_query():
    payload = {
        "message": "   ",
        "selected_organ": "Liver"
    }
    response = client.post("/api/chat", json=payload)
    assert response.status_code == 400
    assert "empty" in response.json()["detail"].lower()

def test_chat_exceeds_max_length():
    payload = {
        "message": "A" * (MAX_QUERY_LENGTH + 50),
        "selected_organ": "Lungs"
    }
    response = client.post("/api/chat", json=payload)
    # Pydantic or endpoint validator returns 400 or 422
    assert response.status_code in [400, 422]

def test_rag_retrieval_all_specimens():
    specimens = ["Heart", "Lungs", "Brain", "Liver", "Stomach", "Skeletal"]
    for organ in specimens:
        chunks = rag_engine.retrieve(f"Anatomy and clinical significance of {organ}", organ_filter=organ, top_k=2)
        assert len(chunks) > 0, f"RAG returned no results for organ: {organ}"
        assert any(organ.lower() in c.organ.lower() for c in chunks), f"Retrieved chunk did not match organ {organ}"
