using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace ImmersiveAnatomy.Learning
{
    public enum TutorPedagogicalLevel
    {
        SimplifiedStudent,
        AdvancedClinical
    }

    [Serializable]
    public class OrganTutorData
    {
        public string organKey;
        public string simplifiedExplanation;
        public string advancedClinicalExplanation;
        public string clinicalCorrelates;
        public string highYieldMnemonic;
    }

    public class AIAnatomyTutor : MonoBehaviour
    {
        [Header("Tutor Settings")]
        public TutorPedagogicalLevel pedagogicalLevel = TutorPedagogicalLevel.SimplifiedStudent;
        public string tutorName = "Dr. Nova (AI Anatomy Assistant)";

        [Header("API Integration (Optional)")]
        public string customApiEndpoint = "";
        public string apiKey = "";

        private Dictionary<string, OrganTutorData> knowledgeBase = new Dictionary<string, OrganTutorData>(StringComparer.OrdinalIgnoreCase);

        private void Awake()
        {
            InitializeKnowledgeBase();
        }

        private void InitializeKnowledgeBase()
        {
            // 1. HEART
            knowledgeBase["Heart"] = new OrganTutorData
            {
                organKey = "Heart",
                simplifiedExplanation = "The heart is the master mechanical pump of the body! It beats approximately 100,000 times a day, circulating 2,000 gallons of blood to supply oxygen and nutrients to every cell while carrying away waste.",
                advancedClinicalExplanation = "A four-chambered muscular organ tilted at 45° with apex oriented anteroinferiorly toward the 5th left intercostal space. Deoxygenated venous blood returns via IVC/SVC to the right atrium, pumped via the pulmonary artery to the alveolar capillary beds. Oxygenated blood returns through 4 pulmonary veins to the left atrium and powerful left ventricle, which generates systolic pressures of ~120 mmHg against systemic vascular resistance.",
                clinicalCorrelates = "Coronary artery occlusion results in Acute Myocardial Infarction (AMI). Referred angina to the left arm and jaw occurs due to shared T1-T4 dermatomal afferent pathways in the spinal cord.",
                highYieldMnemonic = "Right Side = Deoxygenated to Lungs | Left Side = Oxygenated to Body. 'LAB RAT': Left Atrium = Bicuspid (Mitral), Right Atrium = Tricuspid."
            };

            // 2. LUNGS
            knowledgeBase["Lungs"] = new OrganTutorData
            {
                organKey = "Lungs",
                simplifiedExplanation = "Your lungs act like specialized biological air filters and sponges. Every time you breathe, they transfer life-giving oxygen directly into your bloodstream and expel carbon dioxide exhaust gas.",
                advancedClinicalExplanation = "Asymmetric bilateral respiratory organs. The right lung has 3 lobes (Superior, Middle, Inferior) separated by horizontal and oblique fissures. The left lung has 2 lobes and features the cardiac notch and lingula to accommodate the pericardial sac. Trachea bifurcates at the carina (T4/T5 vertebral plane) into primary bronchi; the right bronchus is wider, shorter, and more vertical, predisposing it to foreign body aspiration.",
                clinicalCorrelates = "Chronic Obstructive Pulmonary Disease (COPD) causes alveolar wall destruction (emphysema) and mucus hypersecretion (chronic bronchitis). Pneumothorax disrupts intrapleural negative pressure, causing lung collapse.",
                highYieldMnemonic = "Right Lung = 3 Lobes (Tri-lobed). Left Lung = 2 Lobes (Leaves room for the heart)."
            };

            // 3. BRAIN
            knowledgeBase["Brain"] = new OrganTutorData
            {
                organKey = "Brain",
                simplifiedExplanation = "The brain is the supreme biological supercomputer of the body. It weighs roughly 3 pounds, uses 20% of all your energy, and controls all thoughts, sensations, emotions, speech, and heartbeat regulation.",
                advancedClinicalExplanation = "Consists of the cerebrum, cerebellum, and brainstem. Cerebrum features distinct frontal (motor/executive), parietal (somatosensory), temporal (auditory/memory), and occipital (visual) lobes separated by the central and lateral sulci. Arterial supply is safeguarded by the Circle of Willis anastomosing the internal carotid and vertebrobasilar systems. The brainstem (midbrain, pons, medulla oblongata) governs autonomic cardiovascular and respiratory homeostasis.",
                clinicalCorrelates = "Middle Cerebral Artery (MCA) strokes classically cause contralateral hemiparesis and aphasia (Broca's in frontal lobe or Wernicke's in temporal lobe). Subarachnoid hemorrhage presents as a sudden 'thunderclap headache'.",
                highYieldMnemonic = "Frontal = Future planning & Motor; Parietal = Perception & Pain; Temporal = Tone & Talking; Occipital = Optics."
            };

            // 4. LIVER
            knowledgeBase["Liver"] = new OrganTutorData
            {
                organKey = "Liver",
                simplifiedExplanation = "The liver is your body's personal chemical factory and detox center. It purifies your blood, metabolizes nutrients from food, synthesizes bile to break down fats, and stores emergency glucose.",
                advancedClinicalExplanation = "The largest internal visceral gland (~1.5 kg), situated in the right hypochondriac and epigastric regions beneath the diaphragm. Receives dual blood supply: 75% nutrient-rich venous blood from the Hepatic Portal Vein and 25% oxygenated arterial blood from the Proper Hepatic Artery. Functional units are microscopic hepatic lobules lined with hepatocytes and phagocytic Kupffer cells surrounding a central vein.",
                clinicalCorrelates = "Hepatic cirrhosis produces portal hypertension, manifesting with esophageal varices, ascites, splenomegaly, and caput medusae. Elevated unconjugated bilirubin reflects hemolysis or impaired conjugation; conjugated elevation signals biliary obstruction.",
                highYieldMnemonic = "Dual vascular input: 75% Portal Vein (Nutrients) + 25% Hepatic Artery (Oxygen)."
            };

            // 5. STOMACH & INTESTINES
            knowledgeBase["Stomach"] = new OrganTutorData
            {
                organKey = "Stomach",
                simplifiedExplanation = "The stomach is a muscular churning pouch that baths your food in powerful hydrochloric acid and digestive enzymes, breaking meals down into a fluid liquid called chyme before passing it to the intestines.",
                advancedClinicalExplanation = "J-shaped distensible muscular reservoir bounded by the gastroesophageal lower esophageal sphincter (LES) and the pyloric sphincter. Possesses three smooth muscle tunics: longitudinal, circular, and inner oblique. Parietal cells in gastric oxyntic glands synthesize hydrochloric acid (HCl) via H+/K+ ATPase proton pumps and secrete Intrinsic Factor essential for Vitamin B12 ileal absorption.",
                clinicalCorrelates = "Loss of parietal cells or autoimmune destruction of intrinsic factor leads to Pernicious Anemia. Helicobacter pylori colonization and NSAID overuse disrupt the mucosal bicarbonate barrier, leading to peptic ulcer disease.",
                highYieldMnemonic = "Parietal cells pump Protons (HCl) and Protection factor (Intrinsic Factor for B12)."
            };

            // 6. SKELETON
            knowledgeBase["Skeletal"] = new OrganTutorData
            {
                organKey = "Skeletal",
                simplifiedExplanation = "The skeletal system provides structural framework, protects vital internal organs (like the brain inside the skull and heart within the ribcage), and houses red marrow to generate millions of new blood cells daily.",
                advancedClinicalExplanation = "Composed of 206 bones divided into axial and appendicular divisions. Axial spine features 7 cervical (lordosis), 12 thoracic (kyphosis with rib articulations), 5 lumbar (lordosis), fused sacrum, and coccyx. Thoracic cage comprises 12 pairs of ribs (1-7 true, 8-10 false, 11-12 floating) linked by hyaline costal cartilage to the sternum (manubrium, body, xiphoid process). Bone serves as the primary reservoir for calcium and phosphate ions regulated by PTH and calcitonin.",
                clinicalCorrelates = "Osteoporosis results from an imbalance of osteoclastic bone resorption over osteoblastic synthesis, predisposing to compression fractures. Kyphosis ('dowager's hump') reflects anterior vertebral wedging.",
                highYieldMnemonic = "Cervical = 7 (Eat breakfast at 7 AM); Thoracic = 12 (Lunch at 12 PM); Lumbar = 5 (Dinner at 5 PM)."
            };
        }

        public string GetExplanation(string organKey)
        {
            if (string.IsNullOrEmpty(organKey)) return "Select any anatomical organ to receive clinical AI tutoring.";

            // Normalize organ name
            string key = MatchKey(organKey);
            if (knowledgeBase.TryGetValue(key, out OrganTutorData data))
            {
                if (pedagogicalLevel == TutorPedagogicalLevel.SimplifiedStudent)
                {
                    return $"<b>[AI Tutor - Student Mode]</b>\n{data.simplifiedExplanation}\n\n<b>Mnemonic:</b> <i>{data.highYieldMnemonic}</i>";
                }
                else
                {
                    return $"<b>[AI Tutor - Clinical Level]</b>\n{data.advancedClinicalExplanation}\n\n<b>Pathology:</b> {data.clinicalCorrelates}\n\n<b>High-Yield Mnemonic:</b> {data.highYieldMnemonic}";
                }
            }

            return $"<b>[AI Tutor]</b> Detailed clinical insights for {organKey}: Active vascular organ contributing to systemic physiological homeostasis.";
        }

        public string GetQuizHint(QuizQuestion question)
        {
            if (question == null) return "No active question.";
            if (!string.IsNullOrEmpty(question.hint)) return $"<b>[Tutor Hint]:</b> {question.hint}";

            string key = MatchKey(question.targetOrganName);
            if (knowledgeBase.TryGetValue(key, out OrganTutorData data))
            {
                return $"<b>[Tutor Hint]:</b> Consider this organ's role: {data.highYieldMnemonic}";
            }

            return "<b>[Tutor Hint]:</b> Review the vascular supply and anatomical landmarks in the 3D lab.";
        }

        public string GetCaseDiscussion(ClinicalCase clinicalCase)
        {
            if (clinicalCase == null) return "";
            string key = MatchKey(clinicalCase.targetOrganName);
            string baseInfo = "";
            if (knowledgeBase.TryGetValue(key, out OrganTutorData data))
            {
                baseInfo = data.clinicalCorrelates;
            }

            return $"<b>[Clinical Debrief - {clinicalCase.caseId}]:</b>\n" +
                   $"<b>Pathophysiology:</b> {clinicalCase.pathophysiologicalMechanism}\n" +
                   $"<b>Recommended Management:</b> {clinicalCase.primaryTreatmentProtocol}\n" +
                   $"<b>Clinical Note:</b> {baseInfo}";
        }

        private string MatchKey(string organName)
        {
            if (string.IsNullOrEmpty(organName)) return "Heart";
            if (organName.IndexOf("Heart", StringComparison.OrdinalIgnoreCase) >= 0 || organName.IndexOf("Cardio", StringComparison.OrdinalIgnoreCase) >= 0) return "Heart";
            if (organName.IndexOf("Lung", StringComparison.OrdinalIgnoreCase) >= 0 || organName.IndexOf("Resp", StringComparison.OrdinalIgnoreCase) >= 0 || organName.IndexOf("Trachea", StringComparison.OrdinalIgnoreCase) >= 0) return "Lungs";
            if (organName.IndexOf("Brain", StringComparison.OrdinalIgnoreCase) >= 0 || organName.IndexOf("Nerv", StringComparison.OrdinalIgnoreCase) >= 0) return "Brain";
            if (organName.IndexOf("Liver", StringComparison.OrdinalIgnoreCase) >= 0) return "Liver";
            if (organName.IndexOf("Stomach", StringComparison.OrdinalIgnoreCase) >= 0 || organName.IndexOf("Digest", StringComparison.OrdinalIgnoreCase) >= 0 || organName.IndexOf("Intestine", StringComparison.OrdinalIgnoreCase) >= 0) return "Stomach";
            if (organName.IndexOf("Skelet", StringComparison.OrdinalIgnoreCase) >= 0 || organName.IndexOf("Bone", StringComparison.OrdinalIgnoreCase) >= 0 || organName.IndexOf("Rib", StringComparison.OrdinalIgnoreCase) >= 0) return "Skeletal";
            return organName;
        }

        public void TogglePedagogicalLevel()
        {
            if (pedagogicalLevel == TutorPedagogicalLevel.SimplifiedStudent)
                pedagogicalLevel = TutorPedagogicalLevel.AdvancedClinical;
            else
                pedagogicalLevel = TutorPedagogicalLevel.SimplifiedStudent;
        }
    }
}
