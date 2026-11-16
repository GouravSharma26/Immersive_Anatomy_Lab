using System;
using UnityEngine;

namespace ImmersiveAnatomy.Learning
{
    public enum QuizDifficulty
    {
        Beginner,       // Undergraduate / General
        Intermediate,   // Pre-Med / Physiology
        Advanced        // Clinical Diagnostics & Pathology
    }

    public enum QuestionType
    {
        Identify3DOrgan,   // User clicks the 3D organ directly in the scene
        MultipleChoice,    // Standard 4-option clinical multiple choice
        ClinicalScenario   // Patient case study leading to organ/diagnosis identification
    }

    [Serializable]
    public class QuizQuestion
    {
        public string id;
        public string questionText;
        public string bodySystem; // "Cardiovascular", "Respiratory", "Nervous", "Digestive", "Skeletal"
        public QuizDifficulty difficulty;
        public QuestionType questionType;
        
        // For Multiple Choice / Clinical Scenario
        public string[] options;
        public int correctOptionIndex;

        // For 3D Organ Identification
        public string targetOrganName; // Must match OrganInteractable.organName or keyword

        [TextArea(2, 5)]
        public string clinicalExplanation;
        public string hint;
        public int points = 100;
    }

    [Serializable]
    public class ClinicalCase
    {
        public string caseId;
        public string patientProfile; // e.g., "54-year-old male with chronic hypertension"
        public string chiefComplaint; // e.g., "Crushing substernal chest pain radiating to left jaw and shoulder"
        public string vitalSigns;     // e.g., "BP 165/95, HR 112 bpm, SpO2 93%"
        public string promptQuestion; // "Identify the primary failing organ responsible for this presentation."
        public string targetOrganName;
        public string[] differentialOptions;
        public int correctDifferentialIndex;
        public string pathophysiologicalMechanism;
        public string primaryTreatmentProtocol;
    }
}
