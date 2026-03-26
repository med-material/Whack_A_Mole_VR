# VR/AR Prism Adaptation Literature Review

## Scope

This review is based on the 21 PDFs currently stored in [Articles](/mnt/c/UNI/P8/Prism-effect-adaptation/Articles). The emphasis is not a full clinical effectiveness meta-analysis; it is a design-and-implementation review focused on:

- terminology and conceptual framing
- what is actually shifted
- how the shift is introduced
- interaction and action-confirmation mechanics
- embodiment and body visibility
- task structure, procedural flow, and feedback
- what each paper leaves underspecified

Important filename/date clarifications:

- `Serino2005.pdf` is the 2006 Neuropsychologia paper.
- `Wahnert&Gerhards2021(VR).pdf` is a 2024 Virtual Reality paper.
- `Patane2025(VR).pdf` is an online-2025 paper in a 2026 issue.
- `Gammeri2018(VR).pdf` appears to reflect 2018 online publication and 2020 journal issue publication.
- `Huygelier2020(VR).pdf` appears in the local corpus under 2020 but the journal article citation in the PDF is 2022.
- `Morse2020(VR).pdf` appears in the local corpus under 2020 but the journal article citation in the PDF is 2022.

## Corpus buckets

### Core prism-adaptation implementation papers

- Frassinetti 2002
- Serino 2006
- Serino 2011
- Carter 2016
- Kim 2017
- Ramos 2019
- Gammeri 2018/2020
- Bourgeois 2021
- Cho 2022
- Ishida and Higa 2023
- Ishida and Higa 2024
- Anan 2025
- Patane 2025/2026

### Adjacent VR sensorimotor-adaptation comparator papers

- Wilf 2021
- Wilf 2022
- Wahnert and Gerhards 2024
- Gerken 2025

### Broader USN/VR context papers

- Huygelier 2022
- Kaiser 2022
- Heyse 2022
- Morse 2022

## Cross-paper answers to your three claims

### 1. Terminology is inconsistent because the same label is used for different perturbations

The corpus does support this claim.

- In the classical clinical papers, `prism adaptation` refers to a full-field optical displacement induced by wedge prisms while the participant points to a target with terminal exposure. This is the framing used by Frassinetti 2002, Serino 2006, and Serino 2011.
- In several VR papers, the term `virtual prism adaptation`, `virtual prisms`, or `VR prism adaptation` is used even though only the virtual effector is shifted relative to a stable target. This is the case in Gammeri, Bourgeois, Cho, Kim, and likely Anan.
- Carter avoids the strongest terminological overreach by calling the phenomenon `visuomotor adaptation` with a `virtual shift of the hand cursor`, which is conceptually cleaner than calling it a direct prism analogue.
- Ramos is closer to a true full-field prism analogue because the viewing frustum itself is skewed or rotated. Patane is closer still, because the entire AR video feed, the real hand, and the virtual target are all shifted together.
- Wilf, Wahnert, and Gerken mainly speak in the language of `sensorimotor adaptation` or `visuomotor rotation`, even when they explicitly relate their systems to prism adaptation.
- Ishida and Higa push the term in a different direction again by extending it from hand adaptation to `body adaptation area`, `symbolic body`, and `head position adaptation`, effectively broadening what counts as the adapted effector.

The largest conceptual fault line is this one:

- `full-field optical shift` papers treat the perturbation as a change in seen world coordinates
- `virtual hand/controller shift` papers treat the perturbation as a change in effector feedback coordinates
- `visuomotor rotation` papers treat the perturbation as a motor-learning transform

These are related, but they are not the same manipulation. Treating them as interchangeable is one of the main sources of ambiguity in the literature.

### 2. There is no shared VR facilitation framework

The corpus strongly supports this claim.

Across papers, the implementation choices vary at almost every layer:

- Hardware: Oculus Rift DK2, Oculus Rift CV1, HTC Vive, Vive Pro, Valve Index, Quest 2, Leap Motion, Vive tracker, Oculus Touch, Kinect v2, haptic robotics, wireless gloves.
- Shift locus: whole visual field, AR video feed, camera/frustum rotation, frustum skew, virtual hand, controller proxy, white rod, cursor, symbolic body, head position.
- Shift schedule: sudden, gradual, stepwise by phase, or completely unspecified.
- Exposure visibility: real terminal exposure under a box, virtual black occluder, visibility threshold near target, no arm but visible controller proxy, real hand visible in AR.
- Input completion: physical touch, controller button press, trigger hold/release, mouse click mapped from arm motion, foot pedal for perceptual judgments, or no discrete endpoint button at all.
- Embodiment: none, cursor only, white rod, hand without arm, hand with haptics, symbolic body, real hand in AR.
- Measurement depth: some papers only report endpoint deviation; others log full trajectories, fNIRS, fMRI, ownership/agency ratings, or recalibration/transfer tests.

Even papers with similar goals do not converge on a shared procedural template. Instead, each paper tends to define its own combination of:

- perturbation type
- exposure visibility rule
- embodiment representation
- target geometry
- action-confirmation rule
- assessment battery

That is exactly the kind of fragmentation your supervisor is pointing to.

### 3. The literature often underspecifies milder USN and subtype-sensitive design

The broader corpus supports this claim, and Kaiser 2022 states it directly.

- Kaiser 2022 explicitly argues that conventional methods fail to detect milder forms and often do not differentiate subtypes, while VR/ET systems are promising precisely because they can capture richer process data.
- Heyse 2022 also emphasizes that USN is heterogeneous across modality and spatial reference frame, but its own proposed tool is not a prism-adaptation implementation and does not solve that subtype problem.
- Morse 2022 uses a more subtype-sensitive screening set in a telerehab context, including Broken Hearts egocentric and allocentric scores, but that paper is about acceptability, not prism adaptation design.
- Most core PA and VRPA implementation papers use healthy participants, not patients. That is true for Kim, Ramos, Gammeri, Bourgeois, Cho, Anan, Patane, Ishida and Higa, Wilf, Wahnert, and Gerken.
- The patient papers that do exist generally treat neglect as a broad syndrome or focus on left neglect after right hemisphere lesions, rather than on subtype-specific recruitment or milder symptom profiles.
- Frassinetti 2002 is broader than most because it reports far, near, and personal-space effects, but it still studies a small chronic-neglect sample, not a subtype-stratified one.
- Serino 2006 and Serino 2011 are mechanistically rich but still frame the patients mainly as neglect patients rather than as allocentric, egocentric, personal, peripersonal, extrapersonal, sensory, or motor-neglect subgroups.

The practical consequence is important for your project:

- the VR implementation literature is much richer in perturbation mechanics than in clinically fine-grained neglect characterization
- the subtype literature is often discussed in reviews and background sections, but much less often operationalized in VR prism-adaptation protocols

## A working taxonomy for your report

If you want a unified language section in the background chapter, the following taxonomy is defensible from this corpus.

### Perturbation class

- `Class A: Full-field optical shift`
  - Real wedge prisms or AR/VR methods that shift the entire seen scene.
  - Examples: Frassinetti 2002, Serino 2006, Serino 2011, Patane 2025/2026, Ramos 2019.

- `Class B: Effector-feedback shift`
  - Only the seen hand, controller, rod, or endpoint proxy is shifted relative to a stable scene/target.
  - Examples: Carter 2016, Kim 2017, Gammeri 2018/2020, Bourgeois 2021, Cho 2022, likely Anan 2025.

- `Class C: Visuomotor rotation`
  - A rotational transform is applied to movement feedback, often without claiming a literal prism analogue.
  - Examples: Wilf 2021, Wilf 2022, Wahnert and Gerhards 2024, Gerken 2025.

- `Class D: Symbolic-body / posture-oriented shift`
  - Adaptation is extended from hand endpoint to symbolic body or head-position control.
  - Examples: Ishida and Higa 2023, Ishida and Higa 2024.

### Exposure-visibility rule

- `Terminal exposure`: only the end of the movement becomes visible
- `Concurrent exposure`: a larger portion of the movement is visible
- `Virtual terminal exposure`: visibility is implemented by software gates or occluders rather than physical boxes
- `Natural real-hand feedback`: AR or real-world variants in which the participant sees the actual hand

### Action-completion rule

- `Physical contact`
- `Endpoint button/trigger press`
- `Mapped click from body movement`
- `Oral response`
- `Foot-pedal response`
- `No discrete press; endpoint inferred from trajectory/timing`

### Representation / embodiment

- `No visible body`
- `Cursor`
- `Rod / controller proxy`
- `Virtual hand without arm`
- `Virtual hand with haptic support`
- `Symbolic body`
- `Real hand visible through AR`

## Paper-by-paper notes

### Anan 2025

- Direct comparison of conventional prism glasses and VR-based PA in 40 healthy right-handed adults under counterbalanced within-subject conditions with a 1-week washout.
- Uses conventional 20-diopter base-left prisms in the physical condition. The VR condition uses a software-defined 20-diopter equivalent, but the exact rendering-layer locus of the deviation is not explained as clearly as in Cho or Gammeri.
- Uses Oculus Touch controllers. Participants press a controller button at the believed target location, and a colored virtual sphere then shows endpoint error.
- Exposure task is repeated reaching to 3 targets every 3 seconds for 96 movements.
- Assessments are OLP, landmark, and line bisection. Assessment order is fixed rather than randomized.
- Main result: VR induced leftward OLP aftereffects in more participants than physical prisms, with comparable OLP magnitude among those who adapted in both conditions.
- Strong for direct physical-vs-VR comparison, but weak on implementation transparency because the exact shift class is underspecified and the endpoint button press may itself perturb measurement.

### Bourgeois 2021

- Healthy-participant study with 48 right-handed volunteers, crossing 0 or 30 degree deviation with visual or auditory-verbal feedback.
- Calls the manipulation `virtual prisms`, but the actual perturbation is a gradual rightward shift of the perceived controller/white-rod position relative to the real hand, not a full-field shift.
- Uses HTC Vive controller in the right hand; open-loop and closed-loop pointings are recorded by controller-button press.
- No arm is shown. The controller is represented as a white rod in a featureless white space.
- Adaptation is gradual and concealed over approximately 50 to 70 trials, then maintained until 100 total pointing trials.
- Transfer tests are line bisection and landmark. Test order is randomized and counterbalanced.
- Main result: only visual 30 degree feedback produced reliable aftereffects and line-bisection transfer; auditory-verbal feedback did not.
- Very useful for your terminology argument because the paper explicitly labels an effector-shift paradigm as `virtual prism adaptation`.

### Carter 2016

- Early bridge paper between classical PA and game-based VR; 7 healthy participants only.
- The paper is more careful than many others and frames the manipulation as `visuomotor adaptation` induced either by optical shift or by `virtual shift` of the hand cursor.
- Non-immersive setup: Microsoft Kinect v2 plus FAAST, PED screen task, and simple online games such as darts or whack-a-mole.
- Endpoint action is effectively a click mapped from a forward reach. The initial arm movement is hidden by tape on goggles or by delaying cursor appearance.
- Procedure is pre-adaptation 30 reaches, adaptation 100 reaches, de-adaptation 30 reaches.
- Session order is fixed rather than randomized.
- Main result: VR virtual-shift training induces a leftward aftereffect similar in direction to optical-shift training.
- Important as an early implementation ancestor for your sandbox, but less useful as a strict prism-therapy analogue because it is screen-based VMA rather than immersive VRPA.

### Cho 2022

- Healthy-participant feasibility study with 14 adults using immersive VR plus fNIRS.
- The paper explicitly states that its VPAT differs from prism goggles because only the virtual hand trajectory is shifted, not the entire visual field.
- Hardware: Oculus Rift DK2 plus Leap Motion hand tracking mounted on the HMD.
- Exposure visibility is controlled by a software-defined visible area near the target. This is a clean virtual equivalent of terminal exposure.
- Experimental phases are pre-VPAT, VPAT-10 degrees, VPAT-20 degrees, and post-VPAT, with alternating pointing and clicking/rest blocks.
- Hand-tracking data are logged at 60 Hz. The authors also note occlusion/self-occlusion problems and explicitly suggest controllers would reduce missing-hand errors at the cost of fatigue.
- Main result: rightward virtual-hand shift induces immediate rightward early errors and leftward post effects, with right frontoparietal cortical activation.
- Strong paper for virtual terminal-exposure design logic and for comparing hand tracking versus controller tradeoffs.

### Frassinetti 2002

- Foundational clinical paper: repeated PA in chronic left neglect after right hemisphere lesion, with a control group receiving non-specific rehab.
- Classical full-field optical-shift framing with physical wedge prisms and repeated pointing.
- Distinguishes `adaptation effect` during exposure from `after-effect` after prism removal.
- Demonstrates transfer beyond the pointing task into conventional and behavioral neglect measures, and across far, near, and to some extent personal space.
- Shows that the clinical improvement can outlast the visuo-motor aftereffect by weeks.
- Strong benchmark for your background chapter because it is one of the clearest examples of the difference between short-lived sensorimotor aftereffects and longer-lasting neglect improvement.
- Weakness for implementation extraction: the VR-level details you care about obviously do not apply, and hardware reporting is minimal by modern standards.

### Gammeri 2018/2020

- Healthy-participant dose-response study with 48 participants assigned to 0, 10, 20, or 30 degree rightward deviation.
- Uses HTC Vive; the tracked controller is visually replaced with a wooden rod.
- The perturbation is a gradual rightward displacement of the controller representation in the xz plane, not a full-field shift.
- The authors explicitly acknowledge that the principle differs from wedge prisms because the target remains in stable virtual coordinates while only the effector representation is shifted.
- Open-loop pointing is recorded by pressing the controller button; line bisection uses a red beam from the controller tip; landmark uses verbal judgment.
- Procedure includes baseline, adaptation, post-adaptation testing, and recalibration. Bisection tasks are randomized.
- Main result: only the 30 degree group shows transfer on bisection tasks; most participants stay unaware because the shift is gradual.
- One of the most useful papers in the corpus for your matrix because it is explicit about shift locus, shift schedule, test battery, awareness, and threshold effects.

### Gerken 2025

- 30 healthy participants, randomized to virtual-hand versus cursor feedback.
- Not a prism paper in the clinical sense. This is a VR visuomotor-rotation study using a 40 degree rotation.
- Hardware: Valve Index plus wireless data gloves.
- Clean non-button design: trial starts by dwelling in a start sphere; several test blocks run without visual feedback.
- Embodiment manipulation is strong: hand without arm versus cursor at the wrist; real arms fully occluded.
- Includes familiarization, baseline, adaptation, refresh blocks, explicit/implicit generalization and transfer tests, aftereffects, and agency/ownership/proprioception questionnaires.
- Main result: hand feedback speeds adaptation and increases agency, but aftereffects and transfer are similar across hand and cursor.
- Highly relevant for your embodiment and input-design discussion, especially because it avoids endpoint clicking entirely.

### Heyse 2022

- Not a prism-adaptation implementation paper. It proposes a multisensory music-based VR rehabilitation tool for USN.
- Includes one of the clearest background summaries of USN heterogeneity in the local corpus, distinguishing modality-based and space-based subtypes.
- Small pilot with four patients and four non-clinical users.
- HMD plus controllers used as mallets in a music interaction metaphor.
- The value for your project is conceptual rather than procedural: it supports the claim that current rehabilitation often ignores heterogeneity and that personalization is needed.
- Useful in the background chapter for subtype framing and multimodal rehabilitation logic, not for prism-effect mechanics.

### Huygelier 2020

- Feasibility study of an immersive VR neglect-rehabilitation game using Oculus Rift.
- 15 healthy controls and 7 stroke patients.
- No prism manipulation; instead the system uses patient-tailored multisensory cues in the neglected field.
- The paper is useful because it reports controller onboarding, button-learning procedures, patient-tailored target distributions, and user-experience/cybersickness issues.
- It explicitly positions VR as a way to improve ecological validity, automated feedback, and personalization.
- Strong support paper for your claim that VR neglect systems still vary widely in hardware, input devices, and task logic.

### Ishida and Higa 2023

- VR paper focused on expanding prism adaptation from the hand to whole-body or head-orientation related adaptation.
- Uses symbolic rather than realistic body representation.
- The system measures body landmarks with controllers and uses those measurements to place targets and symbolic-body feedback.
- The conceptual novelty is not a better hand-pointing paradigm; it is the claim that VR can create adaptation in head orientation and posture-related units.
- This is not standard prism-therapy terminology and arguably extends the term `prism adaptation` beyond its classical scope.
- Very relevant if your sandbox includes posture, target-visible-area control, or symbolic rather than realistic embodiment modes.

### Ishida and Higa 2024

- Follow-up paper making the conceptual expansion even more explicit: `multiple motor units`, `symbolic body`, and `head position adaptation`.
- The paper proposes that VR makes it possible to adapt body parts outside the visible field of physical prisms.
- Interaction uses controller and headset positions plus trigger presses; target visibility and symbolic-body visibility are deliberately staged to implement open-loop control.
- Hand-position and head-position adaptation tasks are described as repeated no-shift and shift sets.
- Terminology is especially important here because the paper partially replaces standard PA terms with `shift` and `no-shift` condition language.
- High value for your taxonomy chapter because it shows how far the field has drifted from a shared operational definition of prism adaptation.

### Kaiser 2022

- Systematic review of VR and eye-tracking for USN assessment and treatment.
- One of the strongest papers in the corpus for your third claim.
- Explicitly states that conventional assessment often misses milder forms, lacks subtype differentiation, and has limited ecological validity.
- Also explicitly notes that few VR/ET studies distinguish subtypes even though the technology should allow it.
- Not a prism-implementation paper, but essential as a high-level evidence source for your problem framing.

### Kim 2017

- Early proof-of-concept conference paper; only 4 healthy participants.
- Hardware: Oculus Rift DK2, Leap Motion, synchronized with fNIRS infrastructure for future work.
- The manipulation is a transformed virtual-hand position with real and virtual trajectories diverging under the prism condition.
- Four phases: non-prism, 10 degree prism, 20 degree prism, and post-adaptation.
- Very sparse on procedure details. Action completion, target geometry, and embodiment details are only briefly described.
- Main result: expected rightward errors during prism phases and leftward post-adaptation deviation.
- Useful as an early prototype marker, but too underspecified to serve as a stand-alone design reference.

### Morse 2022

- Qualitative telerehabilitation acceptability study, not an implementation paper for PA.
- Includes 7 stroke survivors, 3 carers, and 6 clinicians.
- Formal neglect assessment in the study includes BIT, line bisection, and Broken Hearts egocentric/allocentric metrics.
- Important for your project because it moves closer to subtype-sensitive description than most PA implementation papers, even though it is not about prism mechanics.
- Also valuable for practical deployment constraints: instructions, home hardware, monitoring, fatigue, and perceived ownership of rehabilitation.

### Patane 2025/2026

- One of the strongest ecological analogues to classical PA in the corpus.
- Uses augmented reality rather than purely virtual reality. The full camera feed, real environment, real hand, and virtual target are all shifted by 15 degrees.
- Hardware: Vive Pro plus Leap Motion. Participants are seated with the head on a chinrest.
- Exposure uses real index-finger pointing to projected virtual targets on a real surface. A virtual black rectangle hides the first third of the movement. This is a clean AR implementation of terminal exposure.
- Critically, no endpoint hand-button press is used during adaptation. Landmark judgments are collected by left and right foot pedals specifically to avoid using the right hand and inducing de-adaptation.
- Multiple post-adaptation blocks track persistence up to approximately 40 minutes.
- Main result: robust sensorimotor aftereffects for both adaptation directions; immediate visuospatial effect only after leftward ARPA; aftereffects generalize from AR to real open-loop pointing.
- This paper is probably your best design reference if you want a high-fidelity, low-Heisenberg-effect benchmark.

### Ramos 2019

- Within-subject comparison of 20 healthy participants across three conditions: real prism goggles plus PC, VR frustum rotation, and VR frustum skew.
- Strong design paper because it compares two distinct rendering-level implementations of simulated prisms.
- Uses a 5-step structure in each condition: pre-test, baseline, exposure, post-test, reset.
- The VR controller menu button is used to define fingertip position, and the virtual arm is hidden behind a virtual black box. The PCP condition uses a touch screen, wooden screen, and beeping feedback.
- A randomized sequence across conditions is used, with at least 10-minute breaks between conditions.
- Main result: both VR conditions produce larger aftereffects than the physical-prism PC condition; no difference between rotate and skew.
- Excellent for your sandbox design because it foregrounds reset procedures, terminal feedback control, fatigue, and implementation-level perturbation differences.

### Serino 2006

- Mechanistic clinical follow-up to Frassinetti.
- 16 chronic neglect patients receive 10 daily PA sessions; 8 control patients receive non-specific rehab.
- Classical physical setup: target pen, wooden box, wide-field prisms inducing a 10 degree rightward shift, pre-exposure visible and invisible pointing, exposure visible pointing, post-exposure invisible pointing.
- The paper makes a strong conceptual distinction between `error reduction` during prism exposure and `after-effect` after prism removal.
- Main result: error reduction in the first week predicts neglect recovery better than aftereffect; oculomotor leftward deviation is linked to recovery; occipital lesions predict poor response.
- This paper is central for your terminology table because it shows a careful multi-level framing of PA rather than treating `aftereffect` as the only meaningful index.

### Serino 2011

- Direct comparison of terminal prism adaptation (TPA), concurrent prism adaptation (CPA), and neutral pointing (NP) in 30 neglect patients.
- All groups complete 10 daily sessions over 2 weeks with 90 pointing movements per session.
- TPA uses only the final part of the movement as visible; CPA exposes the latter half of the movement; NP uses the TPA visibility schedule but neutral goggles.
- Classical wooden-box physical setup with 10 degree rightward prisms.
- Main result: all treatments help somewhat, but TPA produces stronger neglect amelioration than CPA, along with larger initial pointing error and stronger error-reduction dynamics.
- One of the most important papers for your task-analysis chapter because it shows that visibility schedule is not a trivial implementation detail.

### Wilf 2021

- VR plus haptic-robotics system designed to reproduce PA-like sensorimotor adaptation while flexibly varying space and movement mode.
- 60 healthy participants across active near/far training, sham, and robot-guided conditions.
- The perturbation is described as a rotational shift between real and virtual hand positions. This is PA-inspired, but operationally closer to a visuomotor-rotation paradigm than to literal wedge-prism displacement.
- Uses a robotic arm support, haptic feedback, virtual hand, and trigger-based object interaction.
- Measures open-loop reaching, reaching straight ahead, landmark, and embodiment/awareness questionnaires.
- Main result: reliable aftereffects across trained spaces and even under robot-guided passive exposure; manipulation awareness is lower in the guided condition.
- Highly relevant for your embodiment and guidance variables, but it is not a direct one-to-one substitute for physical prism exposure.

### Wilf 2022

- Neuroimaging follow-up using a VRPA-like training before and after fMRI.
- 45 healthy participants assigned to rightward adaptation, leftward adaptation, or sham.
- Uses the same general VRPA logic to show that brief adaptation alters large-scale connectivity and naturalistic visual processing.
- Valuable mainly as evidence that VRPA can have effects beyond immediate endpoint error and aftereffect measures.
- Not especially helpful for controller-level interaction questions.

### Wahnert and Gerhards 2024

- Healthy-participant VR sensorimotor-adaptation paper studying instructions and body representation.
- Uses a Valve HMD, chinrest, HTC Vive tracker attached to the hand, and no endpoint button press.
- Procedure is close to a modern VR prism-adaptation analogue: familiarization, baseline, exposure, and de-exposure.
- The whole visual environment is displaced by 11.31 degrees during exposure. Participants receive terminal feedback, and the first de-exposure trial uses a novel target to quantify aftereffect magnitude.
- Two manipulations are tested: misinformation about additional random error, and body representation change from virtual hand to 3D arrow.
- Main result: misinformation reduces persistence of aftereffects; the arrow representation does not clearly reduce aftereffects.
- Strong paper for your design chapter because it directly links instructions, error attribution, and embodiment to aftereffect magnitude and persistence without relying on endpoint clicking.

## Practical synthesis for the sandbox

If the sandbox is meant to compare rather than silently privilege one implementation, the corpus suggests exposing at least these switches:

- perturbation class: full-field, effector-only, visuomotor rotation, symbolic-body
- shift schedule: sudden, gradual, stepwise by phase
- shift magnitude
- exposure visibility: terminal, concurrent, AR natural hand, none
- embodiment: none, cursor, rod, hand, symbolic body, real hand
- input completion: physical touch, button press, dwell/inferred endpoint, pedal, oral
- task family: exposure pointing, open-loop pointing, line bisection, landmark
- guidance mode: active, guided/passive
- logging depth: endpoint only vs full trajectory and phase segmentation

The most defensible default recommendations from this corpus would be:

- keep perturbation class explicit in the UI and report it in logs
- separate `full-field shift` from `virtual hand/controller shift`
- log whether endpoint required a button press
- treat exposure visibility as a first-class variable, not a UI detail
- document whether the participant saw a hand, controller, rod, cursor, symbolic body, or nothing
- distinguish `error reduction`, `aftereffect`, and `transfer` in both code and reporting
