import re
from typing import List, Dict, Any

class MedicalKnowledgeChunk:
    def __init__(self, doc_id: str, title: str, system: str, organ: str, textbook: str, edition: str, chapter: str, text: str, clinical_pearl: str):
        self.doc_id = doc_id
        self.title = title
        self.system = system
        self.organ = organ
        self.textbook = textbook
        self.edition = edition
        self.chapter = chapter
        self.text = text
        self.clinical_pearl = clinical_pearl

    def to_citation(self) -> str:
        return f"{self.textbook} ({self.edition}), {self.chapter} — '{self.title}'"

class MedicalRAGEngine:
    def __init__(self):
        self.knowledge_base: List[MedicalKnowledgeChunk] = []
        self._load_corpus()

    def _load_corpus(self):
        # 1. CARDIOVASCULAR / HEART
        self.knowledge_base.append(MedicalKnowledgeChunk(
            doc_id="CARD-GRAY-01",
            title="Morphology and Orientation of the Cardiac Apex",
            system="Cardiovascular",
            organ="Heart",
            textbook="Gray's Anatomy for Students",
            edition="4th Edition",
            chapter="Chapter 3: Thorax - Middle Mediastinum",
            text="The heart is oriented obliquely in the middle mediastinum, with roughly two-thirds of its mass lying to the left of the median plane. The apex of the heart is formed solely by the inferolateral part of the left ventricle and lies posterior to the left 5th intercostal space, approximately 8-9 cm (one handbreadth) from the midsternal line. The apex is the anatomical site of the point of maximal impulse (PMI), where apical cardiac pulsations are clinically auscultated and palpated.",
            clinical_pearl="Lateral displacement of the PMI past the midclavicular line is a classic sign of left ventricular hypertrophy (LVH) or dilated cardiomyopathy."
        ))

        self.knowledge_base.append(MedicalKnowledgeChunk(
            doc_id="CARD-GUYT-02",
            title="Coronary Hemodynamics and Referred Anginal Pain",
            system="Cardiovascular",
            organ="Heart",
            textbook="Guyton and Hall Textbook of Medical Physiology",
            edition="14th Edition",
            chapter="Chapter 21: Muscle Blood Flow and Cardiac Output During Exercise; the Coronary Circulation",
            text="Coronary blood flow is unique because resting myocardial extraction of oxygen is already near-maximal (~70-80%). The Left Anterior Descending (LAD) artery courses along the anterior interventricular groove, supplying 45-55% of the left ventricle including the apex and anterior septum. When coronary perfusion is impaired by atherothrombosis, myocardial hypoxia stimulates unmyelinated sympathetic C-fiber cardiac afferents. These visceral sensory fibers enter spinal cord levels T1 to T4/T5, synapsing on secondary neurons that also receive somatic afferents from the left arm, neck, and jaw, producing the classic phenomenon of referred anginal pain.",
            clinical_pearl="Dermatomal convergence at the dorsal horn of spinal levels T1-T4 explains why myocardial ischemia produces chest heaviness radiating down the medial aspect of the left arm (T1 dermatome)."
        ))

        # 2. RESPIRATORY / LUNGS & TRACHEA
        self.knowledge_base.append(MedicalKnowledgeChunk(
            doc_id="RESP-MOOR-01",
            title="Tracheobronchial Tree and Carina Bifurcation",
            system="Respiratory",
            organ="Lungs",
            textbook="Moore's Clinically Oriented Anatomy",
            edition="8th Edition",
            chapter="Chapter 1: Thorax - Trachea and Bronchial Tree",
            text="The trachea extends from the inferior margin of the cricoid cartilage (C6 vertebral level) to the sternal angle of Louis (T4/T5 intervertebral disc plane), where it bifurcates at the ridge-like internal cartilage known as the carina. The right main bronchus is wider, shorter (approx. 2.5 cm vs 5 cm), and runs more vertically than the left main bronchus. Consequently, foreign bodies aspirated in children and adults preferentially lodge into the right bronchus and right lower lobe bronchus.",
            clinical_pearl="The carina is the most sensitive area of the entire respiratory tract; contact with aspirated matter triggers a vigorous, explosive cough reflex."
        ))

        self.knowledge_base.append(MedicalKnowledgeChunk(
            doc_id="RESP-ROBB-02",
            title="Pleural Cavity Dynamics and Pneumothorax Pathophysiology",
            system="Respiratory",
            organ="Lungs",
            textbook="Robbins & Cotran Pathologic Basis of Disease",
            edition="10th Edition",
            chapter="Chapter 15: The Lung - Pleural Diseases",
            text="Under normal physiological conditions, the intrapleural space maintains a negative subatmospheric pressure (-3 to -5 cm H2O during quiet expiration; -8 cm H2O during inspiration) which holds the visceral pleura against the parietal pleura through surface tension. Disruption of the visceral pleura (such as rupture of a subpleural apical bleb in tall, young males) or penetrating chest trauma allows atmospheric air into the pleural space. This abolishes the negative transpulmonary pressure, leading to atelectasis (complete elastic recoil collapse of the ipsilateral lung).",
            clinical_pearl="In tension pneumothorax, a one-way 'flutter valve' mechanism causes progressive intrapleural pressure rise, shifting the mediastinum and compressing the vena cava, producing fatal obstructive shock if not decompressed immediately."
        ))

        # 3. NERVOUS / BRAIN
        self.knowledge_base.append(MedicalKnowledgeChunk(
            doc_id="NERV-GRAY-01",
            title="Cerebral Hemispheric Topography and Cortical Localization",
            system="Nervous",
            organ="Brain",
            textbook="Gray's Anatomy for Students",
            edition="4th Edition",
            chapter="Chapter 8: Head and Neck - Cranial Cavity and Brain",
            text="The cerebral cortex is divided into distinct anatomical lobes by prominent fissures. The central sulcus of Rolando demarcates the primary motor cortex (precentral gyrus, Brodmann area 4) in the frontal lobe from the primary somatosensory cortex (postcentral gyrus, Brodmann areas 3, 1, 2) in the parietal lobe. Broca's expressive motor speech area occupies the pars triangularis and pars opercularis of the inferior frontal gyrus (areas 44/45), while Wernicke's receptive sensory speech area resides in the posterior aspect of the superior temporal gyrus (area 22).",
            clinical_pearl="Broca's aphasia presents with non-fluent, halting speech with intact comprehension ('broken speech'). Wernicke's aphasia presents with fluent, grammatical speech devoid of meaning ('word salad') and impaired comprehension."
        ))

        self.knowledge_base.append(MedicalKnowledgeChunk(
            doc_id="NERV-GUYT-02",
            title="Cerebrovascular Circulation and Circle of Willis Anastomosis",
            system="Nervous",
            organ="Brain",
            textbook="Guyton and Hall Textbook of Medical Physiology",
            edition="14th Edition",
            chapter="Chapter 62: Cerebral Blood Flow, Cerebrospinal Fluid, and Brain Metabolism",
            text="The brain receives 15% of resting cardiac output (750 mL/min) despite constituting only 2% of total body mass. The Circle of Willis forms a polygonal arterial anastomotic ring at the base of the brain around the optic chiasm and pituitary stalk, connecting the anterior circulation (internal carotid arteries giving rise to anterior and middle cerebral arteries) with the posterior circulation (vertebral and basilar arteries forming posterior cerebral arteries). This collateral circuit preserves cerebral perfusion if an internal carotid or vertebral artery is gradually stenosed.",
            clinical_pearl="The Middle Cerebral Artery (MCA) is the most frequent site of thrombotic and embolic ischemic stroke, leading to contralateral hemiparesis and hemisensory loss predominantly affecting the face and upper extremity."
        ))

        # 4. DIGESTIVE / LIVER & STOMACH
        self.knowledge_base.append(MedicalKnowledgeChunk(
            doc_id="DIG-MOOR-01",
            title="Hepatic Lobule Microanatomy and Dual Vascular Supply",
            system="Digestive",
            organ="Liver",
            textbook="Moore's Clinically Oriented Anatomy",
            edition="8th Edition",
            chapter="Chapter 2: Abdomen - Liver and Biliary Tract",
            text="The liver possesses a dual blood supply: approximately 75-80% is venous blood from the Hepatic Portal Vein, conveying absorbed nutrients, drugs, and toxins directly from the gastrointestinal tract and spleen; the remaining 20-25% is oxygenated arterial blood from the Proper Hepatic Artery. At the microscopic level, portal triads (containing a branch of the portal vein, hepatic artery, and bile ductule) sit at the periphery of classic hexagonal hepatic lobules. Mixed blood percolates through low-pressure fenestrated sinusoids lined by hepatocytes and resident phagocytic macrophages (Kupffer cells) before draining into the central vein.",
            clinical_pearl="Fibrotic distortion of hepatic sinusoids in cirrhosis dramatically increases vascular resistance, producing Portal Hypertension (splenomegaly, ascites, and life-threatening bleeding from esophageal varices)."
        ))

        self.knowledge_base.append(MedicalKnowledgeChunk(
            doc_id="DIG-GUYT-02",
            title="Gastric Acid Secretion and Intrinsic Factor Synthesis",
            system="Digestive",
            organ="Stomach",
            textbook="Guyton and Hall Textbook of Medical Physiology",
            edition="14th Edition",
            chapter="Chapter 65: Secretory Functions of the Alimentary Tract",
            text="The fundus and body of the stomach contain gastric oxyntic glands housing two specialized cell types: Chief (peptic) cells that secrete pepsinogen, and Parietal (oxyntic) cells that secrete both Hydrochloric Acid (HCl) and Intrinsic Factor. Parietal cells employ an apical H+/K+ ATPase proton pump to transport hydronium ions into the gastric lumen against a million-fold concentration gradient, generating an acidic gastric pH of 1.0 to 2.0. Simultaneously, Intrinsic Factor binds dietary vitamin B12 (cobalamin), protecting it from proteolytic digestion until it reaches the cubam receptor complex in the terminal ileum for receptor-mediated endocytosis.",
            clinical_pearl="Autoimmune destruction of gastric parietal cells results in Achlorhydria (loss of acid) and Pernicious Anemia due to inability to absorb vitamin B12, causing megaloblastic anemia and subacute combined degeneration of the spinal cord."
        ))

        # 5. SKELETAL SYSTEM
        self.knowledge_base.append(MedicalKnowledgeChunk(
            doc_id="SKEL-GRAY-01",
            title="Vertebral Column Curvatures and Thoracic Cage Biomechanics",
            system="Skeletal",
            organ="Skeletal",
            textbook="Gray's Anatomy for Students",
            edition="4th Edition",
            chapter="Chapter 2: Back - Vertebral Column and Bony Framework",
            text="The adult vertebral column comprises 33 vertebrae arranged into four physiological curvatures: primary kyphotic curves (thoracic and sacral) developed during fetal life, and secondary lordotic curves (cervical and lumbar) acquired when an infant lifts its head and begins walking. The 12 thoracic vertebrae articulate with 12 pairs of ribs via costovertebral and costotransverse synovial joints. Ribs 1-7 (true ribs) attach directly to the sternum through individual costal cartilages; ribs 8-10 (false ribs) attach to the cartilage of the rib above; ribs 11-12 (floating ribs) terminate freely in the abdominal musculature.",
            clinical_pearl="The 'bucket-handle' lateral elevation of ribs 7-10 and 'pump-handle' anterior elevation of ribs 1-6 together expand thoracic volume in both anteroposterior and transverse dimensions during inspiration."
        ))

    def retrieve(self, query: str, organ_filter: str = None, top_k: int = 2) -> List[MedicalKnowledgeChunk]:
        """Simple, fast semantic and keyword matching retrieval."""
        query_words = set(re.findall(r'\w+', query.lower()))
        scores = []

        for chunk in self.knowledge_base:
            score = 0
            # Organ match bonus
            if organ_filter:
                if organ_filter.lower() in chunk.organ.lower() or chunk.organ.lower() in organ_filter.lower():
                    score += 5.0
                if organ_filter.lower() in chunk.system.lower() or chunk.system.lower() in organ_filter.lower():
                    score += 3.0

            # Content keyword overlap
            chunk_words = set(re.findall(r'\w+', (chunk.title + " " + chunk.text + " " + chunk.clinical_pearl).lower()))
            overlap = query_words.intersection(chunk_words)
            score += len(overlap) * 1.5

            scores.append((score, chunk))

        scores.sort(key=lambda x: x[0], reverse=True)
        return [chunk for score, chunk in scores[:top_k]]
