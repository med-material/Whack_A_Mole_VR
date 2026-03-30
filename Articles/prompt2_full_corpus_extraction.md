# Prompt2 Full Corpus Extraction

This file applies `Prompt2` to all 23 local PDFs in `Articles/`.

Rules followed in this extraction:

- Only information explicitly stated in the local PDF is used.
- If a requested detail is not reported, it is marked `not stated`.
- If a Prompt2 item does not apply because the paper is a review, feasibility study, or non-prism system, it is marked `not applicable`.

Filename/date clarifications used below:

- `Serino2005.pdf` is the 2006 paper.
- `Wähnert&Gerhards2021(VR).pdf` is a 2024 paper.
- `Patané2025(VR).pdf` is an online-2025 paper in a 2026 issue.

## Paper 1: Anan et al. 2025 (`Anan2025(VR).pdf`)

### 1. Provide a detailed, step-by-step description of the experimental procedures

- Participants completed three conditions: `control`, `prism-lens (PL)`, and `VR`.
- The order of the three conditions was counterbalanced across participants.
- A one-week washout period separated conditions.
- In each session, participants first completed three pre-tests in a fixed order:
  1. open-loop pointing (OLP)
  2. landmark task
  3. line bisection task
- After the pre-tests, participants completed the adaptation procedure for the session condition.
- In the PL condition, participants performed 96 reaching movements while wearing prism glasses.
- In the VR condition, participants performed 96 reaching movements toward virtual targets.
- The reaching cue in VR was given every 3 s by changing the target color to purple.
- In the PL condition, auditory cues from a metronome paced the reaches every 3 s.
- After the adaptation phase, participants waited with their eyes closed for 10 s while the post-test environment was prepared.
- The same three assessments were then repeated as post-tests.
- The paper states that all participants completed OLP, landmark, and line bisection in that sequence in each session.

### 2. Describe precisely how the visuomotor shift was introduced

#### 2.a Type of shift

- `PL condition`: lateral optical displacement introduced by prism glasses.
- `VR condition`: software-defined prism-equivalent deviation in VR.
- The paper clearly states that the PL condition used prism glasses and the VR condition used a VR-based PA method.
- The exact rendering-layer locus of the VR shift is `not stated` as clearly as in some other VR papers.
- Full visual frustum / optical scene: `yes` for the PL condition; `not stated clearly` for the VR condition.
- Hand representation: a virtual sphere appeared at the believed endpoint in the VR condition.
- Visible arm or body: `not stated clearly`; the paper states trunk and head were held forward.

#### 2.b Magnitude of the shift

- Prism deviation angle: `20 diopters`, approximately `11.3°`.

#### 2.c How the shift was scheduled

- The paper reports a fixed deviation in the PL and VR adaptation phases.
- It does not report a gradual ramp.
- It does not report trial-by-trial increments.
- Prompt2 schedule category: `discrete between exposures/phases`.

#### 2.d Feedback

- PL condition: immediate visual feedback about finger position relative to the target.
- VR condition: immediate visual feedback through a virtual sphere appearing at the believed endpoint.
- If the reached point deviated from the target, the sphere appeared blue.
- If it overlapped exactly, it appeared red.
- Feedback category: `terminal visual feedback`.

### 3. Clarify what the authors mean by “trials”

- In the adaptation procedure, one trial corresponds to one reaching movement.
- The paper explicitly reports `96 reaching movements` in each adaptation condition.
- The paper does not define a larger block as a trial.
- For the landmark task, the centered-line responses were summarized as the percentage of `"right-shifted"` responses among `10 truly centered trials`.

### 4. Use only information explicitly stated in the paper

- This extraction uses only details explicitly stated in the local PDF.
- The paper does not clearly restate the rendering-layer implementation of the VR perturbation, so that part is marked `not stated clearly`.

### 5. What tasks are used for what purposes? (Full Prism-Therapy Pipeline)

#### Stage 1 — USN Assessment (Clinical Neglect Evaluation)

- `Not applicable` as a clinical neglect-diagnosis stage.
- The sample consisted of healthy right-handed adults.
- The paper used OLP, landmark, and line bisection as outcome measures, not as clinical USN inclusion tests.

#### Stage 2 — Baseline Visuomotor Assessment (Pre-Exposure Pointing)

- The baseline task was separate from any clinical USN assessment.
- The baseline visuomotor task was the `OLP` test.
- OLP was used before each condition as the pre-exposure baseline.
- The paper states that the OLP test primarily measures immediate sensorimotor readjustment.
- Number of OLP trials: `not stated` in the abstracted method lines available here.
- The baseline also included the landmark task and line bisection task, but those had different purposes.

#### Stage 3 — Exposure Task (Adaptation Phase)

- Exact exposure task:
  - `PL`: repeated reaching toward three targets while wearing prism glasses.
  - `VR`: repeated reaching toward three virtual spherical targets.
- Target arrangement:
  - the text states three targets arranged in an arc in the PL and VR setups.
- Hand visibility:
  - `PL`: real hand/finger reaching.
  - `VR`: participants used an Oculus Touch controller and pressed the button at the believed target location.
- Trial structure:
  - 96 reaching movements
  - one movement every 3 s
- Shift introduction:
  - fixed 20-diopter / 11.3° deviation
  - not gradual
- What trials mean:
  - one reaching movement = one trial
- What the exposure task measures:
  - the paper emphasizes post-exposure aftereffects, especially OLP, as the main evidence that PA occurred.

#### Stage 4 — Post-Exposure Baseline Repetition (After-Effect Assessment)

- After-effects were assessed with the same three post-tests used at baseline:
  - OLP
  - landmark
  - line bisection
- OLP is the main after-effect task.
- The paper states that a leftward deviation following PA was considered indicative of successful adaptation.
- After-effects were analyzed using `change scores (post-pre)`.
- The paper interprets the OLP shift as the primary evidence that prism adaptation occurred.

#### Stage 5 — (Optional) Post-Therapy USN Re-Assessment

- `Not applicable` as a clinical USN reassessment stage because the sample was healthy.
- The same perceptual tasks were repeated after adaptation, but not as clinical neglect rehabilitation outcomes.

## Paper 2: Bourgeois et al. 2021 (`Bourgeois2021(VR).pdf`)

### 1. Provide a detailed, step-by-step description of the experimental procedures

- Participants were randomly assigned to four groups:
  - visual 0°
  - visual 30°
  - auditory-verbal 0°
  - auditory-verbal 30°
- Each participant completed one session.
- The session consisted of:
  1. baseline test phase
  2. adaptation phase
  3. post-test phase
- Pre- and post-tests evaluated:
  - visual closed-loop pointing
  - visual open-loop pointing
  - line bisection
  - landmark task
- The order of these four tasks was randomized/counterbalanced.
- During adaptation, each trial started with presentation of a black sphere.
- The sphere then turned red and the participant had to reach it with the white rod.
- As soon as the rod contacted the sphere, the sphere turned black and the participant returned to the start position.
- Participants continued until they had completed 100 pointing trials.
- In the visual condition, the target and controller were visible for the first 8 trials.
- The adaptation started after the 15th trial and the deviation increased in very small steps until the maximal 30° deviation was reached after approximately 50-70 trials.
- Participants then continued at maximal deviation until trial 100.
- In the auditory-verbal condition, the procedure started in the same way as the visual condition.
- The target became invisible while the controller was still visible.
- Participants then heard spoken feedback such as `correct`, `more to the left`, or `more to the right`.
- After 15 trials, the controller also became invisible and participants continued based on auditory-verbal feedback only.

### 2. Describe precisely how the visuomotor shift was introduced

#### 2.a Type of shift

- The paper describes `virtual prisms`.
- The actual perturbation was a rightward shift of the virtual controller/white rod relative to the real hand.
- The target remained in stable virtual space.
- Full visual frustum / optical scene: `no`.
- Hand representation: `yes`, through the shifted white-rod/controller representation.
- Visible arm or body: `no`; the paper states that no arm/body was visible.

#### 2.b Magnitude of the shift

- Maximal deviation: `30°`.

#### 2.c How the shift was scheduled

- The shift was introduced gradually.
- It started after the `15th trial`.
- It increased in very small steps `between 0.15 and 0.5 degrees per trial`.
- The maximal shift was reached after approximately `50-70 trials`, depending on pointing speed.
- Participants then continued to point at maximal deviation until `100 trials`.
- Prompt2 schedule category: `gradual during exposure`.

#### 2.d Feedback

- Visual condition:
  - visual target and white rod/controller feedback
  - feedback category: `visual`, initially more concurrent, then mainly endpoint relation to target
- Auditory-verbal condition:
  - spoken trial-by-trial correction messages
  - feedback category: `terminal/corrective verbal feedback`
- The paper does not report haptic feedback.

### 3. Clarify what the authors mean by “trials”

- In the adaptation task, one trial corresponds to one reaching movement toward one sphere.
- Pre- and post-test open-loop tasks each consisted of `five trials`.
- Line bisection used `three trials` for each line length.
- Landmark used `24 trials` total.

### 4. Use only information explicitly stated in the paper

- This extraction uses only the details reported in the local PDF.
- The paper explicitly reports the gradual schedule, the step size, the first 8 and first 15 trial visibility rules, and the test counts.

### 5. What tasks are used for what purposes? (Full Prism-Therapy Pipeline)

#### Stage 1 — USN Assessment (Clinical Neglect Evaluation)

- `Not applicable`.
- The sample consisted of healthy right-handers.

#### Stage 2 — Baseline Visuomotor Assessment (Pre-Exposure Pointing)

- Baseline was separate from any clinical assessment.
- Baseline tasks:
  - visual closed-loop pointing
  - visual open-loop pointing
- Open-loop pointing corresponded to straight-ahead pointing with no visual guidance from the arm/controller.
- The paper notes that `open-loop` is used in the PA literature to designate absence of visual feedback from the arm.
- Closed-loop and open-loop tasks each contained `five trials`.

#### Stage 3 — Exposure Task (Adaptation Phase)

- Exact exposure task:
  - repeated reaching to a red sphere using a white rod/controller representation
- Target arrangement:
  - one sphere among the available pointing stimuli turned red
- Hand visibility:
  - visual condition: controller/rod visible, no arm/body
  - auditory-verbal condition: visibility progressively reduced until controller became invisible
- Trial structure:
  - 100 pointing trials
  - gradual deviation begins after trial 15
- Shift introduction:
  - controller/rod shifted rightward relative to the real hand
  - maximal 30° deviation
  - gradual in steps of 0.15-0.5°/trial
- What trials mean:
  - one reach = one trial
- What the exposure task measures:
  - adaptation expressed as post-test aftereffects in closed-loop/open-loop pointing and transfer tasks.

#### Stage 4 — Post-Exposure Baseline Repetition (After-Effect Assessment)

- After-effects were assessed with:
  - visual closed-loop pointing
  - visual open-loop pointing
  - line bisection
  - landmark task
- Open-loop pointing is the most direct post-exposure sensorimotor after-effect measure.
- The paper reports aftereffects after `30°` visual feedback but not after auditory-verbal feedback.
- The authors link the post-exposure pointing shift to adaptation induced by altered visual feedback, not by verbal correction alone.

#### Stage 5 — (Optional) Post-Therapy USN Re-Assessment

- `Not applicable`.
- Transfer was tested with line bisection and landmark in healthy participants, not as clinical USN reassessment.

## Paper 3: Carter et al. 2016 (`Carter2016(VR).pdf`)

### 1. Provide a detailed, step-by-step description of the experimental procedures

- Seven healthy adults completed four experimental conditions:
  - OS/PED
  - VS/PED
  - OS/Game
  - VS/Game
- `OS` = optical shift with prism goggles.
- `VS` = virtual shift of the hand cursor.
- `PED` = Pointing Error Detection program.
- `Game` = free online games.
- The order was fixed:
  - Day 1: OS/PED followed by VS/PED
  - Day 2: OS/Game followed by VS/Game
- Each condition had three phases:
  1. pre-adaptation: 30 reaches
  2. adaptation: 100 reaches
  3. de-adaptation: 30 reaches
- Each reach constituted a single trial.
- In PED, participants used arm movement to move a cursor to screen targets.
- In the game conditions, the same Kinect/FAAST system mapped reaching to the online games.
- The study assessed the initial portion of the de-adaptation phase as the after-effect.

### 2. Describe precisely how the visuomotor shift was introduced

#### 2.a Type of shift

- `OS`: optical lateral shift produced by prism goggles.
- `VS`: virtual shift of the on-screen hand cursor relative to the participant’s real hand.
- Full visual frustum / optical scene: `yes` for OS through prism goggles; `no` for VS.
- Hand representation: `yes` in VS through the shifted hand cursor.
- Visible arm or body: a tracked skeletal representation existed in the system, but rich embodiment details are `not stated`.

#### 2.b Magnitude of the shift

- The virtual shift was set to mimic about a `12°` prism deviation.

#### 2.c How the shift was scheduled

- The shift was introduced as a fixed shifted adaptation phase.
- The paper does not report a gradual ramp.
- Prompt2 schedule category: `discrete between phases`.

#### 2.d Feedback

- PED provided visual cursor-to-target feedback.
- The online games provided visual and auditory success feedback.
- Endpoint actions were produced by a reach mapped to a mouse click.
- Feedback category: `visual`, with `auditory` success feedback in games.

### 3. Clarify what the authors mean by “trials”

- The paper explicitly states that `each reach constituted a single trial`.
- Pre-adaptation = 30 trials.
- Adaptation = 100 trials.
- De-adaptation = 30 trials.
- For the main outcome, the first `5` de-adaptation reaches were compared with the last `5` pre-adaptation reaches.

### 4. Use only information explicitly stated in the paper

- This extraction uses only explicitly reported details.
- The paper is clearer on phase counts than on fine-grained embodiment mechanics.

### 5. What tasks are used for what purposes? (Full Prism-Therapy Pipeline)

#### Stage 1 — USN Assessment (Clinical Neglect Evaluation)

- `Not applicable`.
- The sample consisted of healthy adults.

#### Stage 2 — Baseline Visuomotor Assessment (Pre-Exposure Pointing)

- Baseline was a separate pre-adaptation reaching phase.
- The same reaching task used later in exposure was used here without shift.
- Baseline involved visually guided reaching with the PED or game environment.
- Trial count: `30 reaches`.

#### Stage 3 — Exposure Task (Adaptation Phase)

- Exact exposure task:
  - repeated reaching in PED or online games
- Input:
  - Kinect v2 plus FAAST mapping body motion to mouse movement and click
- Shift introduction:
  - optical shift with prisms or virtual shift of cursor
  - fixed during the adaptation phase
- Trial count:
  - `100 reaches`
- What trials mean:
  - one reach = one trial
- What the exposure task measures:
  - error correction during adaptation and the after-effect seen immediately after shift removal.

#### Stage 4 — Post-Exposure Baseline Repetition (After-Effect Assessment)

- After-effects were assessed during the de-adaptation phase once the optical or virtual shift was removed.
- The main index used the initial de-adaptation errors.
- Specifically, the first `5` de-adaptation trials were compared with the last `5` pre-adaptation trials.
- The paper interprets the opposite-direction de-adaptation error as evidence of visuomotor adaptation.

#### Stage 5 — (Optional) Post-Therapy USN Re-Assessment

- `Not applicable`.
- The study was a healthy-participant mechanistic feasibility study.

## Paper 4: Cho et al. 2022 (`Cho2022(VR).pdf`)

### 1. Provide a detailed, step-by-step description of the experimental procedures

- Fourteen healthy subjects participated.
- The experiment consisted of four sequential phases:
  1. pre-VPAT
  2. VPAT-10°
  3. VPAT-20°
  4. post-VPAT
- Phase 1 lasted `4 min` and contained `four pointing` and `four clicking` blocks alternating.
- Phase 2 lasted `5 min` and contained `five pointing` and `five resting` blocks alternating.
- Phase 3 lasted `5 min` and contained `five pointing` and `five resting` blocks alternating.
- Phase 4 lasted `5 min` and contained `five pointing` and `five clicking` blocks alternating.
- Each clicking block lasted `30 s` and contained `10 clicks`.
- Each pointing block lasted `30 s` and contained `10 pointings`.
- Targets were presented every `3 s`.
- In the clicking block, subjects clicked as soon as possible when the visual target appeared.
- In the pointing block, subjects pointed as fast as possible with the right index finger.
- Left and right targets were presented in random order.
- The target changed red when pointed correctly.

### 2. Describe precisely how the visuomotor shift was introduced

#### 2.a Type of shift

- The paper explicitly states that, unlike prism goggles, the system shifts only the `virtual hand trajectory`.
- It does not shift the full visual field.
- Full visual frustum / optical scene: `no`.
- Hand representation: `yes`, the virtual hand trajectory is shifted.
- Visible arm or body: `virtual hand only`; the paper does not describe a full arm/body model.

#### 2.b Magnitude of the shift

- `10°` in Phase 2.
- `20°` in Phase 3.

#### 2.c How the shift was scheduled

- The shift changed by phase rather than trial-by-trial.
- Pre-VPAT: no shift.
- VPAT-10°: fixed 10° shift.
- VPAT-20°: fixed 20° shift.
- Post-VPAT: no shift.
- Prompt2 schedule category: `discrete between phases`.

#### 2.d Feedback

- The paper implemented a software-defined visible area near the target.
- The virtual hand was only visible beyond the invisible area.
- The target changed red when pointed correctly.
- Feedback category:
  - `partial visual feedback`
  - `terminal/late visual feedback`

### 3. Clarify what the authors mean by “trials”

- In the pointing task, one trial corresponds to one pointing movement.
- In the clicking task, one trial corresponds to one click.
- Each 30 s block contained 10 trials.
- The first pointing error in each block was used for the main after-effect analysis.

### 4. Use only information explicitly stated in the paper

- This extraction uses only details explicitly reported in the paper.
- The paper is explicit that the VPAT system does not reproduce a full-field prism shift.

### 5. What tasks are used for what purposes? (Full Prism-Therapy Pipeline)

#### Stage 1 — USN Assessment (Clinical Neglect Evaluation)

- `Not applicable`.
- The sample consisted of healthy adults.

#### Stage 2 — Baseline Visuomotor Assessment (Pre-Exposure Pointing)

- Baseline was the `pre-VPAT` pointing phase.
- It used the same pointing task later used during exposure.
- Trial structure:
  - four 30 s pointing blocks
  - 10 pointings per block
- The task involved right-index-finger pointing to left/right targets appearing every 3 s.

#### Stage 3 — Exposure Task (Adaptation Phase)

- Exact exposure task:
  - repeated pointing with the right index finger in immersive VR
- Input:
  - Leap Motion hand tracking mounted on an Oculus Rift DK2
- Hand visibility:
  - the virtual hand appeared only in the visible area near target depth
- Shift introduction:
  - 10° shift phase followed by 20° shift phase
  - fixed within each phase
- What trials mean:
  - one pointing movement = one trial
- What the exposure task measures:
  - pointing error change across blocks
  - the paper reports early rightward errors and later reduction, then leftward post-VPAT errors.

#### Stage 4 — Post-Exposure Baseline Repetition (After-Effect Assessment)

- After-effects were assessed in the `post-VPAT` phase with no shift.
- The same pointing task was repeated.
- The first pointing errors in each block were analyzed.
- The authors interpret the leftward post-VPAT pointing error as an after-effect similar to conventional prism therapy.

#### Stage 5 — (Optional) Post-Therapy USN Re-Assessment

- `Not applicable`.
- The study measured behavioral after-effects and fNIRS in healthy subjects, not clinical neglect recovery.

## Paper 5: Frassinetti et al. 2002 (`Frassinetii2002.pdf`)

### 1. Provide a detailed, step-by-step description of the experimental procedures

- Chronic neglect patients received prism adaptation treatment in `twice-daily sessions` over `2 weeks`.
- Total treatment dose: `20 sessions`.
- Each treatment session lasted about `20 min`.
- Assessments were performed:
  - before treatment
  - 2 days after treatment
  - 1 week after treatment
  - 5 weeks after treatment
- The assessment battery included:
  - BIT
  - bell cancellation
  - reading
  - modified fluff test
  - room description
  - objects reaching
  - motricity index
- For the pointing task, patients sat in front of a wooden box.
- They used the right index finger, starting from the sternum.
- Targets were located at the centre and to the right and left of the body midline at `0°, -21°, +21°`.
- In the pre-exposure condition, patients pointed to `60 targets` presented randomly.
- Half of pre-exposure trials were with visible pointing and half with invisible pointing.
- In the exposure condition, patients pointed to `90 targets` in random order:
  - 30 centre
  - 30 right
  - 30 left
- In the post-exposure condition, patients pointed to `30 targets`:
  - 10 centre
  - 10 right
  - 10 left
- The visible pre-exposure condition was performed only in the first session of each week.

### 2. Describe precisely how the visuomotor shift was introduced

#### 2.a Type of shift

- Classical physical prism adaptation with a full-field optical shift.
- Wide-field prisms shifted the visual field to the right.
- Full visual frustum / optical scene: `yes`.
- Hand representation: real hand viewed in the prism setup.
- Visible arm or body: only the terminal part of the real pointing movement was visible in the exposure condition.

#### 2.b Magnitude of the shift

- `10°` rightward optical shift.

#### 2.c How the shift was scheduled

- Fixed optical deviation during the exposure condition.
- No gradual ramp reported.
- Prompt2 schedule category: `discrete between phases`.

#### 2.d Feedback

- Pre-exposure visible condition: visual feedback available.
- Exposure condition: visible pointing through the box setup.
- Post-exposure condition: invisible pointing.
- Feedback category:
  - `partial visual feedback` during exposure
  - `terminal/late visual feedback` through the classical box setup
  - `absent visual feedback` in invisible pointing phases

### 3. Clarify what the authors mean by “trials”

- One pointing response to one target corresponds to one trial/response.
- Pre-exposure: 60 target responses.
- Exposure: 90 target responses.
- Post-exposure: 30 target responses.
- The paper organizes these as conditions and sessions rather than named blocks.

### 4. Use only information explicitly stated in the paper

- This extraction uses only explicitly reported details from the local PDF.
- Where the paper does not provide a requested detail, it is marked `not stated`.

### 5. What tasks are used for what purposes? (Full Prism-Therapy Pipeline)

#### Stage 1 — USN Assessment (Clinical Neglect Evaluation)

- Standardized and ecological neglect measures included:
  - BIT
  - bell cancellation
  - reading
  - modified fluff test
  - room description
  - objects reaching
- The paper explicitly links:
  - room description to `far space`
  - objects reaching to `near space`
  - fluff test to `personal space`
- These assessments were repeated 2 days, 1 week, and 5 weeks after treatment to evaluate improvement.

#### Stage 2 — Baseline Visuomotor Assessment (Pre-Exposure Pointing)

- Baseline pointing was separate from the neglect assessment battery.
- Pre-exposure pointing used the same target arrangement later used in exposure.
- It included:
  - `visible` pointing baseline for the exposure condition
  - `invisible` pointing baseline for the post-exposure condition
- Trial count:
  - 60 total
  - 20 centre, 20 right, 20 left

#### Stage 3 — Exposure Task (Adaptation Phase)

- Exact exposure task:
  - repeated real-hand pointing under a wooden box
- Target arrangement:
  - 90 targets total
  - 30 centre, 30 right, 30 left
- Hand visibility:
  - partial/terminal visual feedback
- Shift introduction:
  - fixed 10° rightward optical shift
- What trials mean:
  - one point to one target
- What the exposure task measures:
  - `adaptation effect` during prism exposure

#### Stage 4 — Post-Exposure Baseline Repetition (After-Effect Assessment)

- After-effects were measured with invisible pointing immediately after prism removal.
- Post-exposure condition:
  - 30 targets total
  - 10 centre, 10 right, 10 left
- The paper defines this as the `after-effect`.
- The authors distinguish the `adaptation effect` during exposure from the `after-effect` after prism removal.

#### Stage 5 — (Optional) Post-Therapy USN Re-Assessment

- The same USN measures were repeated after treatment.
- The paper reports long-lasting neglect improvement up to 5 weeks.
- It further states that improvement was found in patients who showed the adaptation effect and the after-effect.

## Paper 6: Gammeri et al. 2018/2020 (`Gammeri2018(VR).pdf`)

### 1. Provide a detailed, step-by-step description of the experimental procedures

- Healthy adults were assigned to `0°`, `10°`, `20°`, or `30°` virtual-prism groups.
- Each participant completed one experimental session.
- The session included:
  1. baseline
  2. adaptation
  3. post-adaptation testing
  4. recalibration
- During adaptation, participants wore the VR headset and held a controller in the right hand.
- The controller image was replaced with a white rod.
- At the beginning of each trial, one of nine black spheres turned red.
- Participants had to touch the red sphere with the virtual rod as quickly as possible.
- The target position varied randomly across `100 trials`.
- The shift was introduced progressively over `two minutes`.
- Open-loop pointing was evaluated at four time points.
- Transfer effects were examined with:
  - line bisection
  - space bisection
  - line landmark
  - space landmark
- At the end, participants completed an awareness questionnaire.

### 2. Describe precisely how the visuomotor shift was introduced

#### 2.a Type of shift

- The 3D coordinates of the controller were modified in the `xz-plane`.
- The virtual rod appeared radially shifted relative to the real controller/hand.
- Full visual frustum / optical scene: `no`.
- Hand representation: `yes`, via the shifted white rod/controller.
- Visible arm or body: `no`; participants did not see their body or arms.

#### 2.b Magnitude of the shift

- Group-level maximum deviations: `0°`, `10°`, `20°`, `30°`.

#### 2.c How the shift was scheduled

- The deviation increased progressively across approximately `2 min`.
- The virtual rod moved rightward in very small steps of about `0.15-0.5° per trial`.
- The shift progressed across about `50-70` pointing movements and then remained at the assigned final magnitude.
- Prompt2 schedule category: `gradual during exposure`.

#### 2.d Feedback

- During adaptation, the target and virtual rod were visible.
- In open-loop pointing, controller visibility was turned off and there was no body/arm feedback.
- Bisection tasks used visible controller feedback.
- Feedback categories:
  - `concurrent/visual` during adaptation
  - `absent` during open-loop pointing

### 3. Clarify what the authors mean by “trials”

- Adaptation: one reach to one red sphere = one trial.
- Adaptation contained `100 trials`.
- Open-loop pointing contained `5 trials` at each time point.
- Line bisection contained `12 trials`.
- Line landmark contained `27 trials`.
- The paper separates `time points` from `trials`; the time points are not themselves individual trials.

### 4. Use only information explicitly stated in the paper

- This extraction uses only explicitly reported details from the local PDF.
- The paper explicitly states the progressive step size and the counts for adaptation and transfer tasks.

### 5. What tasks are used for what purposes? (Full Prism-Therapy Pipeline)

#### Stage 1 — USN Assessment (Clinical Neglect Evaluation)

- `Not applicable`.
- The sample consisted of healthy adults.

#### Stage 2 — Baseline Visuomotor Assessment (Pre-Exposure Pointing)

- Baseline was measured with open-loop pointing before adaptation.
- Open-loop pointing involved no visual feedback of the controller, body, or arms.
- Participants held the controller at chest height and pointed toward a central target before pressing the controller button.
- Trial count: `5 trials`.

#### Stage 3 — Exposure Task (Adaptation Phase)

- Exact exposure task:
  - repeated VR reaching with a white rod toward a red sphere among nine spheres
- Number and arrangement of targets:
  - `3 x 3` array of spheres at about arm length
- Hand visibility:
  - white rod visible
  - body/arms not visible
- Shift introduction:
  - controller/rod shifted progressively in the xz-plane
  - final magnitude set by group
- What trials mean:
  - one sphere-touch movement = one trial
- What the exposure task measures:
  - direct adaptation via open-loop aftereffect
  - relation of that aftereffect to transfer tasks

#### Stage 4 — Post-Exposure Baseline Repetition (After-Effect Assessment)

- After-effects were measured with open-loop pointing after adaptation.
- The same open-loop task was also repeated later to assess decay.
- The authors interpret the pre-post open-loop difference as the adaptation effect.
- Transfer beyond the pointing task was tested with bisection and landmark variants.

#### Stage 5 — (Optional) Post-Therapy USN Re-Assessment

- `Not applicable`.
- Transfer tasks were perceptual/cognitive probes in healthy subjects, not clinical neglect reassessment.

## Paper 7: Gerken et al. 2025 (`Gerken2025(VR).pdf`)

### 1. Provide a detailed, step-by-step description of the experimental procedures

- Thirty right-handed adults were randomly assigned to:
  - `H group` (virtual hand feedback)
  - `C group` (cursor feedback)
- Each trial started with a starting sphere.
- A target sphere then appeared.
- Participants were instructed to execute rapid, straight center-out reaches.
- The experimental protocol was:
  1. familiarization right and left hand: 45 trials
  2. baseline: 6 trials with veridical feedback
  3. baseline no feedback: 18 trials
  4. baseline: 6 trials with veridical feedback
  5. baseline test targets: 18 trials
  6. baseline: 6 trials with veridical feedback
  7. baseline left hand no feedback: 18 trials
  8. baseline: 6 trials with veridical feedback
  9. adaptation: 144 trials
  10. test targets explicit/implicit: 18 trials
  11. refresh: 18 trials
  12. test targets implicit/explicit: 18 trials
  13. refresh: 18 trials
  14. transfer left hand explicit/implicit: 18 trials
  15. refresh: 18 trials
  16. transfer left hand implicit/explicit: 18 trials
  17. refresh: 18 trials
  18. washout: 18 trials
- Breaks were given every `6 to 18 trials`.
- Break durations ranged from `15 s to 60 s`.

### 2. Describe precisely how the visuomotor shift was introduced

#### 2.a Type of shift

- `40°` visuomotor rotation around the `y-axis`.
- In the H group, the position and orientation of the virtual hand were rotated.
- In the C group, cursor feedback was rotated.
- Full visual frustum / optical scene: `no`.
- Hand representation: `yes` in the H group, `cursor` in the C group.
- Visible arm or body: `no`; real arms were occluded.

#### 2.b Magnitude of the shift

- `40°` counterclockwise.

#### 2.c How the shift was scheduled

- The perturbation was applied during the adaptation block.
- The paper does not report a gradual ramp; it describes a rotated adaptation block of `144 trials`.
- Prompt2 schedule category: `discrete during exposure block`.

#### 2.d Feedback

- Veridical visual feedback during familiarization/baseline feedback blocks.
- No visual feedback in designated `no FB` trials and posttests.
- During no-feedback trials, visual feedback disappeared upon target presentation.
- A semitransparent yellow sphere helped repositioning during the return movement.
- Feedback categories:
  - `concurrent visual feedback` in feedback blocks
  - `absent` in no-feedback blocks

### 3. Clarify what the authors mean by “trials”

- One reach to one target sphere = one trial.
- The paper also bins trials in sets of `18` for analysis.
- The listed protocol blocks are blocks of multiple trials; the trials themselves are individual reaches.

### 4. Use only information explicitly stated in the paper

- This extraction uses only explicitly reported details.
- This is a visuomotor-rotation study, not a clinical prism-adaptation study.

### 5. What tasks are used for what purposes? (Full Prism-Therapy Pipeline)

#### Stage 1 — USN Assessment (Clinical Neglect Evaluation)

- `Not applicable`.
- The sample consisted of healthy adults.

#### Stage 2 — Baseline Visuomotor Assessment (Pre-Exposure Pointing)

- Baseline consisted of veridical and no-feedback reaching blocks with right and left hands and with training/test targets.
- These blocks established pre-rotation reaching accuracy and variability.

#### Stage 3 — Exposure Task (Adaptation Phase)

- Exact exposure task:
  - center-out reaching in VR with either virtual-hand or cursor feedback
- Trial count:
  - `144 adaptation trials`
- Shift introduction:
  - fixed `40°` CCW visuomotor rotation
- What trials mean:
  - one reach = one trial
- What the exposure task measures:
  - adaptation rate
  - effects of visual feedback type

#### Stage 4 — Post-Exposure Baseline Repetition (After-Effect Assessment)

- After-effects were assessed in an `18-trial` block without visual feedback after the refresh/test sequence.
- The paper interprets residual performance change without compensation as aftereffect/implicit learning.

#### Stage 5 — (Optional) Post-Therapy USN Re-Assessment

- `Not applicable`.
- No clinical neglect reassessment was performed.

## Paper 8: Heilman et al. 2000 (`Heilman2000.pdf`)

### 1. Provide a detailed, step-by-step description of the experimental procedures

- This local PDF is a `review article`, not a single prism-adaptation experiment.
- It does not report one unified baseline-exposure-post-exposure protocol.
- Instead, it reviews neglect mechanisms, diagnosis, and treatment approaches.
- It describes bedside and laboratory assessments such as:
  - inattention tests
  - extinction testing
  - line bisection
  - cancellation
  - drawing/copying
  - personal-neglect tests
  - representational tasks

### 2. Describe precisely how the visuomotor shift was introduced

#### 2.a Type of shift

- `Not applicable` as an experimental procedure.
- The review notes that prisms can move visual images and have been used with adaptation procedures to treat neglect.
- It does not specify one implemented shift.

#### 2.b Magnitude of the shift

- `Not stated` in a single experimental protocol.

#### 2.c How the shift was scheduled

- `Not applicable`.

#### 2.d Feedback

- `Not applicable`.

### 3. Clarify what the authors mean by “trials”

- The review does not define trials for a single experiment.
- Trial structure is therefore `not applicable`.

### 4. Use only information explicitly stated in the paper

- This extraction only uses material explicitly present in the local review PDF.
- Because the paper is a review, most Prompt2 procedural items are `not applicable`.

### 5. What tasks are used for what purposes? (Full Prism-Therapy Pipeline)

#### Stage 1 — USN Assessment (Clinical Neglect Evaluation)

- The review describes multiple standardized/clinical neglect assessments, including:
  - line bisection
  - target/cancellation tasks
  - drawing and copying
  - personal-neglect tests involving body parts
  - extinction and inattention testing
  - representational imagery tasks
- The review explicitly discusses subtype distinctions such as:
  - personal neglect
  - representational neglect
  - viewer-centered/environmental reference frame differences
- The review therefore contributes strongly to Stage 1 framing, but not to a single experimental protocol.

#### Stage 2 — Baseline Visuomotor Assessment (Pre-Exposure Pointing)

- `Not applicable` for a single study protocol.

#### Stage 3 — Exposure Task (Adaptation Phase)

- `Not applicable`.
- The paper mentions prism treatment conceptually but does not report one exposure procedure.

#### Stage 4 — Post-Exposure Baseline Repetition (After-Effect Assessment)

- `Not applicable`.

#### Stage 5 — (Optional) Post-Therapy USN Re-Assessment

- `Not applicable` as a single reported experiment.

## Paper 9: Heyse et al. 2022 (`Heyse2022(VR).pdf`)

### 1. Provide a detailed, step-by-step description of the experimental procedures

- This paper presents a multisensory VR rehabilitation tool for spatial neglect based on music-making.
- The study involved `four` patients with USN and `four` non-clinical users.
- The VR therapy module required the patient to wear an HMD and use `two controllers` representing mallets.
- The system contained four tasks:
  - Assessment
  - Scales
  - Memory
  - Free-to-Play
- In the `Assessment` task, the patient was asked to freely play the virtual xylophone; the design also added falling balls and target prompts to keep the patient engaged.
- In the `Scales` task, the patient had to sequentially hit keys from the non-neglected side toward the neglected side.
- In the `Memory` task, the patient had to memorize and reproduce note sequences.
- In `Free-to-Play`, the patient could play freely while still being prompted to explore space.
- A therapist dashboard displayed head direction over time and played-key information during the session.
- The paper does not report one fixed baseline-exposure-post-exposure prism protocol.

### 2. Describe precisely how the visuomotor shift was introduced

#### 2.a Type of shift

- `Not applicable`.
- The system is not a prism-adaptation setup.
- The paper uses audiovisual and motor prompts to encourage exploration of the neglected side.

#### 2.b Magnitude of the shift

- `Not applicable`.

#### 2.c How the shift was scheduled

- `Not applicable`.

#### 2.d Feedback

- Feedback was multisensory:
  - visual
  - auditory
  - motor interaction with the virtual instrument
- The paper explicitly frames the tool as multi-sensory VR rehabilitation.

### 3. Clarify what the authors mean by “trials”

- The paper does not define a classical prism-adaptation `trial`.
- It describes therapy `tasks`, `sessions`, and observed behavior rather than a single repeated-trial exposure paradigm.

### 4. Use only information explicitly stated in the paper

- This extraction uses only the details explicitly stated in the paper.
- Because this is not a prism-adaptation experiment, most shift-specific Prompt2 items are `not applicable`.

### 5. What tasks are used for what purposes? (Full Prism-Therapy Pipeline)

#### Stage 1 — USN Assessment (Clinical Neglect Evaluation)

- The paper gives a broad overview of USN assessment methods, including:
  - line bisection
  - cancellation tests
  - behavioral tests
- It also reviews neglect subtypes:
  - sensory
  - motor
  - representational
  - personal
  - peripersonal
  - extrapersonal
- Within the VR tool itself, the `Assessment` task was intended to assess severity/progress through head direction and responses during musical interaction.

#### Stage 2 — Baseline Visuomotor Assessment (Pre-Exposure Pointing)

- `Not applicable` as a prism-style pre-exposure pointing stage.

#### Stage 3 — Exposure Task (Adaptation Phase)

- `Not applicable` as a prism-adaptation exposure task.
- The active training tasks were the musical interaction tasks described above.

#### Stage 4 — Post-Exposure Baseline Repetition (After-Effect Assessment)

- `Not applicable`.
- The paper does not report prism after-effects.

#### Stage 5 — (Optional) Post-Therapy USN Re-Assessment

- The paper reports behavioral observations from the VR therapy tool, including correction of head direction toward the neglected side.
- It does not report a classical prism-therapy clinical reassessment pipeline.

## Paper 10: Huygelier et al. 2022 (`Huygelier2020(VR).pdf`)

### 1. Provide a detailed, step-by-step description of the experimental procedures

- This paper describes an immersive VR game to train spatial attention orientation after stroke.
- It is a feasibility study, not a prism-adaptation experiment.
- The system used an Oculus Rift CV1.
- Patients used the right Oculus Touch controller; the left controller was disabled.
- Before entering the rehabilitation module, patients completed an assessment module.
- The assessment module consisted of `3 levels`.
- Each assessment level finished after a fixed number of `75 trials`.
- The rehabilitation game contained `18 levels`, presented twice during the game.
- The game could be played in `active` and `placebo` conditions.
- In Phase 1 and Phase 2, participants played a single session.
- In Phase 3, patients played `6 sessions` spread across `6` different days.

### 2. Describe precisely how the visuomotor shift was introduced

#### 2.a Type of shift

- `Not applicable`.
- The system used patient-tailored target placement and audiovisual cueing, not a prism or rotation shift.

#### 2.b Magnitude of the shift

- `Not applicable`.

#### 2.c How the shift was scheduled

- `Not applicable`.

#### 2.d Feedback

- Feedback included:
  - audiovisual looming cue
  - green checkmark for correct responses
  - red cross for wrong responses
  - blue exclamation mark and sound for no response within the time window
  - audiovisual reward on 25% of accurate trials
- Cueing was present in 50% of trials and predicted the target location.

### 3. Clarify what the authors mean by “trials”

- In the assessment module, a trial was one cue-target-response event.
- Each assessment level contained `75 trials`.
- The game also updated performance every `10 trials`.
- The paper uses `levels` as groups of multiple trials rather than equating a level with a single trial.

### 4. Use only information explicitly stated in the paper

- This extraction uses only details explicitly stated in the local PDF.
- Because the paper is not a prism-adaptation study, shift-specific items are marked `not applicable`.

### 5. What tasks are used for what purposes? (Full Prism-Therapy Pipeline)

#### Stage 1 — USN Assessment (Clinical Neglect Evaluation)

- The paper reports neuropsychological characterization with:
  - Dutch OCS confrontation task / hearts tasks
  - BIT letter cancellation
  - BIT figure copy
  - computerized cancellation
- The computerized cancellation task used `12 trials` and included one practice trial.
- For each computerized cancellation trial, `50 targets` and `100 distractors` were presented.
- A break occurred every `three trials`.

#### Stage 2 — Baseline Visuomotor Assessment (Pre-Exposure Pointing)

- `Not applicable` as a prism-style baseline pointing stage.

#### Stage 3 — Exposure Task (Adaptation Phase)

- `Not applicable` as a prism-adaptation exposure task.
- The active training was the VR rehabilitation game with patient-tailored cueing and target sampling.

#### Stage 4 — Post-Exposure Baseline Repetition (After-Effect Assessment)

- `Not applicable`.

#### Stage 5 — (Optional) Post-Therapy USN Re-Assessment

- The study evaluated feasibility, correspondence between VR and computerized neglect performance, and cueing effects.
- It did not report a prism-style after-effect or prism-therapy clinical reassessment pipeline.

## Paper 11: Ishida and Higa 2023 (`Ishida&Higa2023(VR).pdf`)

### 1. Provide a detailed, step-by-step description of the experimental procedures

- The paper develops a virtual prism-adaptation system intended to extend adaptation beyond the hand to posture/head orientation.
- The system uses a standalone Oculus Quest 2 with inside-out tracking.
- Body measurements were first taken to determine target coordinates.
- A symbolic body (`SB`) was used instead of a realistic avatar.
- The paper describes two tasks:
  - `hand adaptation (HaA)`
  - `head adaptation (HeA)`
- In the hand adaptation task:
  1. one of `10` target positions was selected
  2. the target was displayed
  3. the subject moved the controller to the target location
  4. the subject pulled the trigger
  5. the SB was shown while the trigger was held
  6. when the trigger was released, SB and target disappeared
- This sequence was counted as `one task`.
- The task was performed `20 times` as a set.
- Sets `1-3` were `no-shift`.
- Sets `4-6` were `shift`.
- Sets `7-9` were `no-shift`.
- The head adaptation task followed the same overall no-shift / shift / no-shift organization, but used head direction rather than hand movement.

### 2. Describe precisely how the visuomotor shift was introduced

#### 2.a Type of shift

- The perturbation was applied to the symbolic-body representation, not to the full visual scene.
- In the no-shift condition, SB was displayed at the controller position.
- In the shift condition, SB was displayed at a position rotated around the origin from the controller position.
- Full visual frustum / optical scene: `no`.
- Hand representation: `yes`, via SB.
- Visible arm or body: `symbolic body`, not a realistic hand/arm.

#### 2.b Magnitude of the shift

- Shift angle `θ = -15°`.

#### 2.c How the shift was scheduled

- The perturbation was organized by sets:
  - no-shift
  - shift
  - no-shift
- The shift was fixed within the shift sets.
- Prompt2 schedule category: `discrete between sets`.

#### 2.d Feedback

- Visual feedback was provided by showing the symbolic body and target while the trigger was held.
- The paper does not report auditory or haptic feedback as the main adaptation feedback.
- Feedback category: `visual`, with the error relation visible while the SB was displayed.

### 3. Clarify what the authors mean by “trials”

- The paper refers to `tasks` and `sets`.
- One task corresponds to one complete target-directed action sequence ending when the trigger is released.
- `20` tasks make one set.
- The set structure is central to the analysis.

### 4. Use only information explicitly stated in the paper

- This extraction uses only paper-reported information.
- The paper substantially redefines prism adaptation around symbolic-body control; this is described without inferring equivalence to classical optical prisms.

### 5. What tasks are used for what purposes? (Full Prism-Therapy Pipeline)

#### Stage 1 — USN Assessment (Clinical Neglect Evaluation)

- `Not applicable` as a participant-screening stage in this paper.
- The experiments were run in healthy subjects.
- The paper discusses USN posture and head-orientation problems as the rehabilitation target.

#### Stage 2 — Baseline Visuomotor Assessment (Pre-Exposure Pointing)

- Baseline corresponded to the initial `no-shift` sets.
- For HaA, the baseline index was the controller deflection angle.
- For HeA, the baseline index was the camera deflection angle.

#### Stage 3 — Exposure Task (Adaptation Phase)

- Exact exposure task:
  - symbolic-body hand adaptation or head adaptation
- Target arrangement:
  - one of 10 candidate target positions
- Hand/body visibility:
  - symbolic body rather than realistic hand/body
- Shift introduction:
  - fixed `-15°` symbolic-body rotation in shift sets
- What trials mean:
  - one complete target-directed task = one trial/task
- What the exposure task measures:
  - controller/camera deflection changes across sets

#### Stage 4 — Post-Exposure Baseline Repetition (After-Effect Assessment)

- After-effects were assessed in the final `no-shift` sets after the shift sets.
- The paper interprets continued leftward deflection in the no-shift sets as shift adaptation/de-adaptation behavior similar to conventional PA.

#### Stage 5 — (Optional) Post-Therapy USN Re-Assessment

- `Not applicable`.
- The study was conducted in healthy subjects and did not repeat standardized clinical neglect tests.

## Paper 12: Ishida and Higa 2024 (`Ishida&Higa2024(VR).pdf`)

### 1. Provide a detailed, step-by-step description of the experimental procedures

- The paper develops a VR system that generates prism adaptation for multiple motor units.
- It studies:
  - hand position adaptation
  - head position adaptation
- In the hand position adaptation task:
  1. the subject moved the controller to a home position
  2. one of `10` candidate targets was presented
  3. the subject moved quickly to overlap SB and target
  4. when the controller left the home position, the target disappeared
  5. pulling the trigger displayed SB and target
  6. releasing the trigger hid SB and target
  7. if `n > 0`, the next task started; if `n = 0`, the set ended
- In the head position adaptation task, the same logic was applied using the headset/head position.
- The experiments included a continuous series of:
  - `6 no-shift sets`
  - `6 shift sets`
  - `6 no-shift sets`

### 2. Describe precisely how the visuomotor shift was introduced

#### 2.a Type of shift

- The perturbation acted on `symbolic body` position for hand and head adaptation.
- It did not shift the whole scene.
- Full visual frustum / optical scene: `no`.
- Hand representation: `yes`, via symbolic body.
- Visible arm or body: `symbolic body only`.

#### 2.b Magnitude of the shift

- Shift angle: `-15°`.

#### 2.c How the shift was scheduled

- Fixed shift within the `6 shift sets`.
- No-shift and shift were alternated in a blockwise sequence of sets.
- Prompt2 schedule category: `discrete between sets`.

#### 2.d Feedback

- Visual feedback was given by the visible relation between SB and target while the trigger was held.
- The paper does not report auditory or haptic feedback as the core adaptation feedback.

### 3. Clarify what the authors mean by “trials”

- The paper describes `sets of trials`.
- One trial/task is one complete action cycle from target appearance to trigger release.
- A set is a group of such trials; the paper analyzes behavior across sets.

### 4. Use only information explicitly stated in the paper

- This extraction uses only explicitly stated details.
- The paper reports the set structure, the `-15°` shift, and the symbolic-body logic directly.

### 5. What tasks are used for what purposes? (Full Prism-Therapy Pipeline)

#### Stage 1 — USN Assessment (Clinical Neglect Evaluation)

- `Not applicable`.
- Healthy subjects were used.

#### Stage 2 — Baseline Visuomotor Assessment (Pre-Exposure Pointing)

- Baseline corresponds to the initial `6 no-shift sets`.
- The paper analyzes `θpos` and `θor` in these sets as pre-shift reference behavior.

#### Stage 3 — Exposure Task (Adaptation Phase)

- Exact exposure task:
  - hand position adaptation task or head position adaptation task with symbolic body
- Target arrangement:
  - 10 candidate targets
- Shift introduction:
  - fixed `-15°` symbolic-body shift
  - 6 shift sets
- What trials mean:
  - one complete action cycle = one trial
- What the exposure task measures:
  - adaptation and de-adaptation of position/orientation indices across sets

#### Stage 4 — Post-Exposure Baseline Repetition (After-Effect Assessment)

- After-effects/de-adaptation were assessed in the final `6 no-shift sets`.
- The paper explicitly discusses the de-adaptation process between sets `12` and `13`.
- It links persistence/decay across sets to adaptation awareness and rate.

#### Stage 5 — (Optional) Post-Therapy USN Re-Assessment

- `Not applicable`.
- No standardized clinical neglect reassessment was performed.

## Paper 13: Kaiser et al. 2022 (`Kaiser2022(VR).pdf`)

### 1. Provide a detailed, step-by-step description of the experimental procedures

- This local PDF is a `systematic review`.
- It does not report one single experiment with baseline, exposure, and post-exposure phases.
- Instead, it reviews immersive VR and eye-tracking approaches to USN assessment and treatment.

### 2. Describe precisely how the visuomotor shift was introduced

#### 2.a Type of shift

- `Not applicable` as a single experimental manipulation.
- The review includes discussion of prism adaptation and notes that in VR a prismatic shift can be obtained by manipulating alignment between the participant and the VR training game.

#### 2.b Magnitude of the shift

- `Not applicable` for one protocol.

#### 2.c How the shift was scheduled

- `Not applicable`.

#### 2.d Feedback

- `Not applicable` as a single experimental protocol.

### 3. Clarify what the authors mean by “trials”

- The review does not define a single trial structure for one study.
- `Not applicable`.

### 4. Use only information explicitly stated in the paper

- This extraction uses only details explicitly stated in the local review PDF.
- Because this is a review, most procedural items are `not applicable`.

### 5. What tasks are used for what purposes? (Full Prism-Therapy Pipeline)

#### Stage 1 — USN Assessment (Clinical Neglect Evaluation)

- This paper is highly relevant to Stage 1.
- It explicitly states that conventional assessment methods often:
  - fail to discover milder forms
  - cannot differentiate USN subtypes
  - lack ecological validity
- It reviews subtypes including:
  - egocentric
  - allocentric
  - personal
  - peripersonal
  - extrapersonal
- It argues that subtype-sensitive diagnostics are needed.

#### Stage 2 — Baseline Visuomotor Assessment (Pre-Exposure Pointing)

- `Not applicable` as a single study protocol.

#### Stage 3 — Exposure Task (Adaptation Phase)

- `Not applicable`.
- The review discusses how VR could implement prismatic shift, but does not report one exposure protocol.

#### Stage 4 — Post-Exposure Baseline Repetition (After-Effect Assessment)

- `Not applicable`.

#### Stage 5 — (Optional) Post-Therapy USN Re-Assessment

- `Not applicable` as a single experiment.
- The review instead argues at a framework level that subtype-specific treatment and assessment are needed.

## Paper 14: Kim et al. 2017 (`Kim2017(VR).pdf`)

### 1. Provide a detailed, step-by-step description of the experimental procedures

- The system was applied to `4 healthy people`.
- The experiment consisted of four phases:
  1. non-prism
  2. 5-min prism, 10° deviation
  3. 5-min prism, 20° deviation
  4. 5-min non-prism (post-adaptation)
- Each phase was composed of `30-second blocks`.
- Subjects were instructed to point to the target object in immersive VR every `3 s`.
- The paper plots median pointing deviations for each 30-second block.

### 2. Describe precisely how the visuomotor shift was introduced

#### 2.a Type of shift

- The visualized virtual hand was rendered in a transformed position.
- The real hand and virtual hand trajectories differed.
- Full visual frustum / optical scene: `no`.
- Hand representation: `yes`, the virtual hand was shifted.
- Visible arm or body: `not stated`.

#### 2.b Magnitude of the shift

- `10°` phase.
- `20°` phase.

#### 2.c How the shift was scheduled

- The shift changed by phase:
  - non-prism
  - 10°
  - 20°
  - post-adaptation no shift
- Prompt2 schedule category: `discrete between phases`.

#### 2.d Feedback

- The feedback was visual through the transformed virtual hand.
- The paper does not describe additional auditory or haptic feedback.

### 3. Clarify what the authors mean by “trials”

- The paper does not define classical numbered trials.
- It reports `30-second blocks` and one pointing action every `3 s`.
- On that basis, each block appears to contain repeated individual pointing movements, but the paper itself summarizes by block.

### 4. Use only information explicitly stated in the paper

- This extraction uses only explicitly stated details.
- The paper is very brief on exact trial counts and endpoint-confirmation mechanics; those details are therefore `not stated`.

### 5. What tasks are used for what purposes? (Full Prism-Therapy Pipeline)

#### Stage 1 — USN Assessment (Clinical Neglect Evaluation)

- `Not applicable`.
- Healthy subjects were used.

#### Stage 2 — Baseline Visuomotor Assessment (Pre-Exposure Pointing)

- The `non-prism` phase functioned as the baseline.
- The task was immersive VR target pointing.

#### Stage 3 — Exposure Task (Adaptation Phase)

- Exact exposure task:
  - repeated target pointing in immersive VR
- Shift introduction:
  - 10° then 20° transformed virtual-hand deviation
- What trials mean:
  - the paper reports block-based summaries rather than an explicit trial definition
- What the exposure task measures:
  - rightward errors during prism phases and their change over time by block

#### Stage 4 — Post-Exposure Baseline Repetition (After-Effect Assessment)

- After-effects were measured in the final `5-min non-prism` phase.
- The paper reports leftward deviations in the post-adaptation phase, which it interprets as similar to conventional prism therapy.

#### Stage 5 — (Optional) Post-Therapy USN Re-Assessment

- `Not applicable`.

## Paper 15: Morse et al. 2022 (`Morse2020(VR).pdf`)

### 1. Provide a detailed, step-by-step description of the experimental procedures

- This paper explored perspectives on VR as a precursor to telerehabilitation for spatial neglect post-stroke.
- Participants included:
  - `7` stroke survivors
  - `3` carers
  - `6` clinicians
- Stroke survivors were assessed before the focus groups/interviews.
- Participants then trialled non-immersive VR rehabilitation tools:
  - VirtualRehab exergames
  - c-SIGHT
- Each participant used the VR telerehabilitation for approximately `5-10 min` in the session described.
- Focus groups/interviews were then conducted.
- Afterward, usability and acceptance questionnaires were completed.

### 2. Describe precisely how the visuomotor shift was introduced

#### 2.a Type of shift

- `Not applicable`.
- This is not a prism-adaptation experiment.

#### 2.b Magnitude of the shift

- `Not applicable`.

#### 2.c How the shift was scheduled

- `Not applicable`.

#### 2.d Feedback

- The paper discusses VR feedback broadly:
  - auditory prompts
  - visual progress
  - objective performance feedback
- It does not describe a single adaptation-feedback protocol.

### 3. Clarify what the authors mean by “trials”

- The paper does not define a prism-style trial structure.
- `Not applicable`.

### 4. Use only information explicitly stated in the paper

- This extraction uses only details explicitly stated in the local PDF.
- Because this is an acceptability/qualitative study, most shift-specific Prompt2 items are `not applicable`.

### 5. What tasks are used for what purposes? (Full Prism-Therapy Pipeline)

#### Stage 1 — USN Assessment (Clinical Neglect Evaluation)

- Stroke survivors were assessed with:
  - Behavioural Inattention Test (BIT)
  - line bisection
  - paper version of the Broken Hearts task
- The paper reports egocentric and allocentric Broken Hearts scores and cut-offs.
- This makes the paper relevant to subtype-sensitive assessment framing.

#### Stage 2 — Baseline Visuomotor Assessment (Pre-Exposure Pointing)

- `Not applicable`.
- The trialled systems were not used as prism-style pre-exposure pointing measures.

#### Stage 3 — Exposure Task (Adaptation Phase)

- `Not applicable` as prism adaptation.
- Participants trialled existing VR/telerehabilitation systems for usability and acceptability.

#### Stage 4 — Post-Exposure Baseline Repetition (After-Effect Assessment)

- `Not applicable`.

#### Stage 5 — (Optional) Post-Therapy USN Re-Assessment

- `Not applicable` as a reported intervention outcome pipeline.
- The paper is about acceptability and design constraints for future deployment.

## Paper 16: Patané et al. 2025/2026 (`Patané2025(VR).pdf`)

### 1. Provide a detailed, step-by-step description of the experimental procedures

- Participants underwent either `leftward` or `rightward` ARPA.
- The procedure included:
  1. one `Pre` block
  2. one `Post 1` block immediately after ARPA
  3. ensuing `Post` blocks up to `Post 9`
- In each experimental block, open-loop pointing was always performed before the landmark task.
- `Pre`, `Post 1`, and `Post 9` each included two open-loop blocks:
  - AR-based open-loop pointing
  - real open-loop pointing
- The order of the two open-loop tasks in these three blocks was counterbalanced.
- The ARPA session lasted approximately `10-15 min`.
- Each assessment block lasted about `5 min`.
- During ARPA, participants followed verbal instructions through the headset.
- They performed `150 pointing movements` toward left or right virtual targets.

### 2. Describe precisely how the visuomotor shift was introduced

#### 2.a Type of shift

- The paper describes a genuine `augmented reality prism adaptation`.
- The shift was applied to the visual field and to the virtual elements of the scene.
- The ARPA system displaced the viewed real environment, the seen real hand, and the virtual target together.
- Full visual frustum / optical scene: `yes`, within the AR display.
- Hand representation: `yes`, the seen real hand in AR was shifted.
- Visible arm or body: the real right hand interacted with the real surface in AR.

#### 2.b Magnitude of the shift

- `15°` leftward or rightward.

#### 2.c How the shift was scheduled

- Fixed leftward or rightward shift during the ARPA session.
- No gradual ramp is reported.
- Prompt2 schedule category: `discrete during exposure block`.

#### 2.d Feedback

- ARPA provided the same tactile feedback as physical PA because participants touched the same real surface.
- A virtual black rectangle hid the first part of the movement.
- Feedback category:
  - `partial visual feedback`
  - `terminal/late visual feedback`
  - `tactile/proprioceptive feedback` from the real surface

### 3. Clarify what the authors mean by “trials”

- In the ARPA session, one trial corresponds to one pointing movement.
- The exposure phase consisted of `150 pointing movements`.
- In open-loop tasks, participants performed `six` pointing movements toward the central target.
- Landmark consisted of `66 trials` and was preceded by about `20` practice trials.

### 4. Use only information explicitly stated in the paper

- This extraction uses only explicitly reported details from the local PDF.
- The paper clearly reports the ARPA block structure, open-loop counts, landmark count, and the 15° shift.

### 5. What tasks are used for what purposes? (Full Prism-Therapy Pipeline)

#### Stage 1 — USN Assessment (Clinical Neglect Evaluation)

- `Not applicable`.
- The sample consisted of healthy participants.

#### Stage 2 — Baseline Visuomotor Assessment (Pre-Exposure Pointing)

- Baseline was assessed in the `Pre` block.
- Baseline tasks:
  - AR open-loop pointing
  - real open-loop pointing
  - landmark task
- In the real open-loop task, participants pointed six times to the central physical target with eyes closed.
- In the AR open-loop task, participants pointed six times to the central virtual target in the headset.

#### Stage 3 — Exposure Task (Adaptation Phase)

- Exact exposure task:
  - repeated right-index-finger pointing in AR toward left or right virtual targets
- Target arrangement:
  - targets at `+10°` and `-10°`
- Hand visibility:
  - first part of movement hidden by a virtual black rectangle
- Shift introduction:
  - fixed `15°` leftward or rightward AR shift
- What trials mean:
  - one pointing movement = one trial
- What the exposure task measures:
  - the authors focus on immediate and time-course sensorimotor and visuospatial aftereffects.

#### Stage 4 — Post-Exposure Baseline Repetition (After-Effect Assessment)

- After-effects were assessed with repeated AR open-loop pointing and landmark blocks from `Post 1` to `Post 9`.
- Real open-loop pointing was also repeated in `Pre`, `Post 1`, and `Post 9` to test generalization outside AR.
- The sensorimotor aftereffect was the shift in landing position relative to baseline.
- The visuospatial aftereffect was the shift in landmark-task PSE.
- The paper interprets sensorimotor aftereffects as robust for both directions, while the landmark shift appeared only after leftward ARPA.

#### Stage 5 — (Optional) Post-Therapy USN Re-Assessment

- `Not applicable`.
- The repeated post blocks tested sensorimotor and visuospatial persistence in healthy participants, not clinical USN recovery.

## Paper 17: Ramos et al. 2019 (`Ramos2019(VR).pdf`)

### 1. Provide a detailed, step-by-step description of the experimental procedures

- Twenty healthy subjects completed three conditions in randomized order:
  - `PCP` physical prism condition
  - `VRR` frustum rotation condition
  - `VRS` frustum skew condition
- There was a `10-min` break between conditions.
- Each condition followed the same five-step protocol:
  1. `Pre-test`: 9 trials
  2. `Baseline (pre-exposure)`: 30 trials
  3. `Exposure`: 90 trials
  4. `Post-test (post-exposure)`: 60 trials
  5. `Reset`: 30 trials
- The paper states that the `Pre-test` includes sample trials both with and without visible fingertip.
- The `Reset` step was inserted to ensure that each subject left the condition in an unadapted state.
- In the VR conditions, subjects wore an HTC Vive headset and controller.
- In the physical condition, subjects pointed on a touchscreen behind a wooden screen.

### 2. Describe precisely how the visuomotor shift was introduced

#### 2.a Type of shift

- `PCP`: physical prism exposure.
- `VRS`: skewing of the viewing frustum.
- `VRR`: rotation of the viewing frustum.
- Full visual frustum / optical scene: `yes` in VRS and VRR.
- Hand representation: `yes`, through the virtual or physical fingertip.
- Visible arm or body: `no`; in VR a black box concealed arm movements.

#### 2.b Magnitude of the shift

- VR conditions: `10°`.
- PCP: actual `8.7°`, transformed to `10°` in analysis.

#### 2.c How the shift was scheduled

- The perturbation was fixed within the exposure phase of each condition.
- No gradual ramp is reported.
- Prompt2 schedule category: `discrete between phases`.

#### 2.d Feedback

- The paper explicitly states whether fingertip feedback was visible in each phase.
- Exposure and reset used feedback; baseline and post-test used no feedback.
- In VR, the fingertip was visible right below the top of the virtual black box regardless of vertical pointing.
- The HTC Vive controller rumbled slightly when the white wall was touched.
- In PCP, a beep was emitted on touch.
- Feedback categories:
  - `terminal/late visual feedback`
  - `tactile/haptic` in VR via rumble
  - `auditory` in PCP via beep

### 3. Clarify what the authors mean by “trials”

- One trial corresponds to one pointing movement.
- The five-step protocol gives explicit trial counts for each phase:
  - 9
  - 30
  - 90
  - 60
  - 30
- The paper uses `step`/`phase` for collections of trials and `trial` for individual pointings.

### 4. Use only information explicitly stated in the paper

- This extraction uses only explicitly reported details from the local PDF.
- The paper is one of the clearest in the corpus about rendering-level shift implementation.

### 5. What tasks are used for what purposes? (Full Prism-Therapy Pipeline)

#### Stage 1 — USN Assessment (Clinical Neglect Evaluation)

- `Not applicable`.
- The sample consisted of healthy subjects.

#### Stage 2 — Baseline Visuomotor Assessment (Pre-Exposure Pointing)

- Baseline consisted of:
  - `Pre-test` with 9 trials
  - `Baseline` with 30 trials and no feedback
- These tasks established pre-exposure pointing behavior.

#### Stage 3 — Exposure Task (Adaptation Phase)

- Exact exposure task:
  - repeated pointing under PCP, VRR, or VRS
- Number and arrangement of targets:
  - the paper reports center and lateral target positions, with equal-frequency/pseudorandom presentation
- Hand visibility:
  - only fingertip visible in VR
  - black box hid the arm
- Shift introduction:
  - fixed physical prism, frustum rotation, or frustum skew
- What trials mean:
  - one pointing movement = one trial
- What the exposure task measures:
  - adaptation strength across conditions, especially aftereffects.

#### Stage 4 — Post-Exposure Baseline Repetition (After-Effect Assessment)

- After-effects were assessed in the `Post-test` phase with `60` no-feedback trials.
- The paper also included a `Reset` phase to return subjects toward baseline before the next condition.
- The authors interpret the post-test deviation as prismatic after-effect and directly compare VRR, VRS, and PCP on that basis.

#### Stage 5 — (Optional) Post-Therapy USN Re-Assessment

- `Not applicable`.

## Paper 18: Rossetti et al. 1998 (`Rosetti1998.pdf`)

### 1. Provide a detailed, step-by-step description of the experimental procedures

- The paper reports `two experiments`.
- Experiment 1 included `8` neglect patients and `5` controls.
- In Experiment 1:
  1. subjects sat in front of a horizontal box
  2. for straight-ahead tests they were blindfolded
  3. they performed `10` pre-test straight-ahead pointing trials without goggles
  4. they then underwent prism exposure
  5. exposure consisted of `50` pointing responses to visual targets presented `10°` to the right or left of the objective body midline
  6. head alignment was maintained with a chin rest
  7. exposure lasted `2-5 min`
  8. immediately after prism removal, a post-test of `10` straight-ahead pointing trials was run
- Experiment 1 randomly exposed subjects to two prism conditions:
  - base left
  - base right
- The order of these two exposure conditions was counterbalanced, with at least `2 days` between them.
- Experiment 2 included `12` neglect patients randomly assigned to:
  - prism group
  - control group
- In Experiment 2:
  1. patients completed a pre-test battery of neuropsychological tests
  2. they then performed the same elementary pointing task as in Experiment 1
  3. prism-group patients wore 10° right-shifting prisms
  4. control-group patients wore neutral goggles
  5. immediately after removing the goggles, the same neuropsychological battery was repeated
  6. patients were tested again about `2 h` later in a late test

### 2. Describe precisely how the visuomotor shift was introduced

#### 2.a Type of shift

- Physical wedge prisms induced a wide-field optical shift of the visual field.
- During exposure subjects could see the target, the second half of their pointing trajectory, and their terminal error through the prisms.
- Full visual frustum / optical scene: `yes`.
- Hand representation: real hand seen through prisms.
- Visible arm or body: partial real movement visibility only.

#### 2.b Magnitude of the shift

- `10°`.

#### 2.c How the shift was scheduled

- Fixed prism deviation during exposure.
- No gradual schedule is reported.
- Prompt2 schedule category: `discrete between exposures/phases`.

#### 2.d Feedback

- Subjects saw:
  - the target
  - the second half of the pointing trajectory
  - the terminal error
- Feedback category:
  - `partial concurrent visual feedback`
  - `terminal visual feedback`

### 3. Clarify what the authors mean by “trials”

- In the straight-ahead tests, one trial corresponds to one pointing movement.
- The paper explicitly reports `10` straight-ahead pointing trials.
- In the exposure phase, the paper reports `50 pointing responses`; it does not subdivide these into blocks.

### 4. Use only information explicitly stated in the paper

- This extraction uses only details explicitly stated in the local PDF.
- The paper refers to supplementary information not present in the local file, so no absent supplementary detail is added here.

### 5. What tasks are used for what purposes? (Full Prism-Therapy Pipeline)

#### Stage 1 — USN Assessment (Clinical Neglect Evaluation)

- In Experiment 2, the paper used:
  - line bisection
  - line cancellation
  - copying a simple drawing made of five items
  - drawing a daisy from memory
  - reading a simple text
- These tests were repeated at pre-test, post-test, and late test.

#### Stage 2 — Baseline Visuomotor Assessment (Pre-Exposure Pointing)

- In Experiment 1, baseline was a separate blindfolded straight-ahead pointing task.
- It used `10` trials.
- This was an open-loop straight-ahead judgment task.

#### Stage 3 — Exposure Task (Adaptation Phase)

- Exact exposure task:
  - repeated real-hand pointing to targets 10° right or left of body midline
- Trial count:
  - `50` pointing responses
- Hand visibility:
  - second half of trajectory visible
- Shift introduction:
  - fixed 10° optical shift from prism lenses
- What trials mean:
  - one pointing response = one movement
- What the exposure task measures:
  - adaptation to the visual-proprioceptive discrepancy created by the prisms.

#### Stage 4 — Post-Exposure Baseline Repetition (After-Effect Assessment)

- After-effects were measured with the same blindfolded straight-ahead pointing task.
- Post-test count:
  - `10` trials
- The paper computes the after-effect from the change in straight-ahead pointing after prism removal.
- The authors interpret this as evidence of a genuine active central adaptation process rather than a passive visual effect.

#### Stage 5 — (Optional) Post-Therapy USN Re-Assessment

- In Experiment 2, the same neuropsychological neglect tests were repeated immediately after exposure and about 2 h later.
- The paper links the improvement to prism adaptation induced by the exposure task.

## Paper 19: Serino et al. 2006 (`Serino2005.pdf`)

### 1. Provide a detailed, step-by-step description of the experimental procedures

- The paper included:
  - `16` neglect patients in the experimental PA group
  - `8` control patients receiving general cognitive stimulation
- Experimental patients received `10 daily sessions` of PA over `2 weeks`.
- Each session lasted about `20 min`.
- The pointing task was performed in three conditions:
  1. pre-exposure
  2. exposure
  3. post-exposure
- In pre-exposure:
  - patients pointed to `60` targets
  - half with visible pointing
  - half with invisible pointing
- In exposure:
  - patients pointed to `90` targets in random order
  - `30` centre, `30` right, `30` left
- In post-exposure:
  - patients pointed to `30` targets
  - `10` centre, `10` right, `10` left
- The targets were presented at the centre and at `-21°` and `+21°`.
- Both groups also underwent neglect testing and a reading/eye-movement assessment.
- Neglect was assessed before treatment, and `1 week`, `1 month`, and `3 months` after treatment.

### 2. Describe precisely how the visuomotor shift was introduced

#### 2.a Type of shift

- Classical wide-field physical prism adaptation.
- Full visual frustum / optical scene: `yes`.
- Hand representation: real hand seen in the box setup.
- Visible arm or body: final part of the real movement was visible during exposure.

#### 2.b Magnitude of the shift

- `10°` rightward optical shift.

#### 2.c How the shift was scheduled

- Fixed prism shift during exposure.
- No gradual schedule reported.
- Prompt2 schedule category: `discrete between phases`.

#### 2.d Feedback

- Exposure used visible pointing through the wooden box.
- Pre-exposure and post-exposure invisible conditions had no hand visibility.
- Feedback category:
  - `partial/terminal visual feedback` during exposure
  - `absent visual feedback` in invisible conditions

### 3. Clarify what the authors mean by “trials”

- One target-pointing response corresponds to one trial/response.
- Pre-exposure = `60` responses.
- Exposure = `90` responses.
- Post-exposure = `30` responses.
- The reading task also had its own trial structure:
  - `48` letter strings
  - four blocks of `12` trials

### 4. Use only information explicitly stated in the paper

- This extraction uses only explicitly stated details from the local PDF.
- The paper explicitly separates `error reduction` during exposure from `after-effect` after prism removal.

### 5. What tasks are used for what purposes? (Full Prism-Therapy Pipeline)

#### Stage 1 — USN Assessment (Clinical Neglect Evaluation)

- The paper used standardized neglect tests including the BIT.
- It also measured reading performance and eye movements during reading.
- The neglect/reading assessments were repeated after treatment and at follow-ups.

#### Stage 2 — Baseline Visuomotor Assessment (Pre-Exposure Pointing)

- Baseline pointing was separate from the clinical neglect battery.
- It used the pre-exposure pointing task.
- Visible pre-exposure served as baseline for the exposure condition.
- Invisible pre-exposure served as baseline for the post-exposure condition.

#### Stage 3 — Exposure Task (Adaptation Phase)

- Exact exposure task:
  - repeated real-hand pointing under the wooden-box setup
- Target arrangement:
  - 90 targets
  - 30 centre, 30 right, 30 left
- Hand visibility:
  - terminal part visible
- Shift introduction:
  - fixed 10° rightward prism shift
- What trials mean:
  - one pointing movement = one trial
- What the exposure task measures:
  - `error reduction`
- The paper emphasizes that error reduction in the first week predicted neglect improvement.

#### Stage 4 — Post-Exposure Baseline Repetition (After-Effect Assessment)

- After-effects were measured with invisible post-exposure pointing.
- Post-exposure count:
  - `30` targets
- The after-effect was the change from invisible pre-exposure to post-exposure.
- The authors argue that after-effect alone was less predictive of clinical recovery than error reduction.

#### Stage 5 — (Optional) Post-Therapy USN Re-Assessment

- Neglect assessments were repeated `1 week`, `1 month`, and `3 months` after treatment.
- The paper attributes the longer-term improvement primarily to the mechanisms indexed by error reduction and associated oculomotor change.

## Paper 20: Serino et al. 2011 (`Serino2011.pdf`)

### 1. Provide a detailed, step-by-step description of the experimental procedures

- Thirty neglect patients were assigned to three groups:
  - terminal prism adaptation (`TPA`)
  - concurrent prism adaptation (`CPA`)
  - neutral pointing (`NP`)
- All treatments consisted of `10 daily sessions` over `2 weeks`.
- Each session took about `30 min`.
- The pointing task had three experimental conditions:
  1. pre-exposure
  2. exposure
  3. post-exposure
- In pre-exposure:
  - `60` targets were presented in the right field, left field, and centre
  - half of the trials were visible
  - half were invisible
- In exposure:
  - `90` targets were presented in random order
  - `30` centre, `30` right, `30` left
- In TPA, only the final part of the movement was visible.
- In CPA, the entire latter half of the movement was visible.
- In NP, the same pointing procedure as TPA was used, but with neutral goggles.
- Neglect was assessed at baseline and again `one week after the end of treatment`.
- TPA and CPA groups also underwent eye-movement recording during a reading task.

### 2. Describe precisely how the visuomotor shift was introduced

#### 2.a Type of shift

- Classical physical prism adaptation with a manipulation of exposure visibility.
- Full visual frustum / optical scene: `yes` for the prism groups.
- Hand representation: real hand seen in the box setup.
- Visible arm or body:
  - TPA: final part visible
  - CPA: latter half visible

#### 2.b Magnitude of the shift

- `10°` rightward optical shift.

#### 2.c How the shift was scheduled

- Fixed prism shift during exposure.
- No gradual ramp reported.
- Prompt2 schedule category: `discrete between phases`.

#### 2.d Feedback

- TPA:
  - `partial/terminal visual feedback`
- CPA:
  - `partial` but more concurrent visual feedback because half the movement was visible
- NP:
  - neutral goggles, same pointing structure without prism displacement

### 3. Clarify what the authors mean by “trials”

- One pointing movement to one target corresponds to one trial/response.
- Pre-exposure: `60` responses.
- Exposure: `90` responses.
- The paper additionally analyzes initial trials `1-9` and final trials `81-90` within each exposure session.

### 4. Use only information explicitly stated in the paper

- This extraction uses only details explicitly reported in the local PDF.
- The paper is explicit about the TPA/CPA visibility manipulation and about the pre/exposure/post counts.

### 5. What tasks are used for what purposes? (Full Prism-Therapy Pipeline)

#### Stage 1 — USN Assessment (Clinical Neglect Evaluation)

- The BIT was used at baseline and post-treatment.
- A reading task with eye-movement recording was used to assess oculomotor consequences.

#### Stage 2 — Baseline Visuomotor Assessment (Pre-Exposure Pointing)

- Baseline pointing was separate from the BIT.
- Pre-exposure had:
  - visible pointing baseline for exposure
  - invisible pointing baseline for post-exposure

#### Stage 3 — Exposure Task (Adaptation Phase)

- Exact exposure task:
  - repeated real-hand pointing under TPA, CPA, or NP conditions
- Target arrangement:
  - 90 targets in random order
- Hand visibility:
  - TPA final segment only
  - CPA latter half
- Shift introduction:
  - fixed 10° prism shift for TPA and CPA
- What trials mean:
  - one point to one target
- What the exposure task measures:
  - `error reduction`
- The paper explicitly compares initial and final trials to quantify error reduction.

#### Stage 4 — Post-Exposure Baseline Repetition (After-Effect Assessment)

- After-effects were assessed with post-exposure invisible pointing after each session.
- The paper reports a significant leftward post-exposure deviation in the prism groups.
- It states that the total amount of after-effect did not differ between TPA and CPA, even though error reduction differed.
- The authors therefore distinguish adaptation mechanisms during exposure from the total after-effect measured afterward.

#### Stage 5 — (Optional) Post-Therapy USN Re-Assessment

- BIT and reading/eye-movement outcomes were reassessed after treatment.
- The paper concludes that TPA outperformed CPA and neutral pointing in neglect recovery, linking this to the stronger prism-adaptation procedure.

## Paper 21: Wilf et al. 2021 (`Wilf2021(VR).pdf`)

### 1. Provide a detailed, step-by-step description of the experimental procedures

- Experiment 1 involved active VR-PA in near or far space, with experimental and sham groups.
- Participants first performed practice:
  - `20` near-space reaching trials
  - `20` far-space reach-and-roll trials
  - if needed, an additional batch of `25` practice trials
- After practice, one reaching-straight-ahead movement in darkness was performed.
- Participants then completed four pre-tests:
  - open-loop near
  - open-loop far
  - landmark near
  - landmark far
- The order of these experiments was counterbalanced.
- Participants then completed VR-PA or sham training.
- In the training task, participants used the right arm to move a natural-looking virtual hand grabbing a black bowling ball.
- Near condition:
  - push a bowling pin off a nearby table with haptic feedback
- Far condition:
  - roll the ball toward a distant bowling pin without target haptic feedback
- Total planned training was `50` trials.
- The shift was introduced at `trial 10`.
- Participants who had not yet adapted by the end of 50 trials received extra batches of `25` trials.
- Near participants completed between `50 and 100` trials, median `72.5`.
- Far participants completed between `50 and 125` trials, median `100`.
- Experiment 2 used robot-guided VR-PA in near space.
- In Experiment 2, guided VR-PA consisted of `75` replayed reaching movements, with shift introduced on the `11th` trial.

### 2. Describe precisely how the visuomotor shift was introduced

#### 2.a Type of shift

- The paper explicitly states that it used a `rotation` between the real hand and the virtual ball/hand trajectory.
- Full visual frustum / optical scene: `no`.
- Hand representation: `yes`, via the virtual hand/ball.
- Visible arm or body: during actual adaptation only the ball was visible; the virtual hand remained invisible.

#### 2.b Magnitude of the shift

- `25°` rightward rotational shift.

#### 2.c How the shift was scheduled

- Training began with `10` no-shift trials.
- The `25°` rightward rotational shift was induced at `trial 10`/from `trial 11`.
- Sham training used the same procedure without any shift.
- Prompt2 schedule category: `discrete during exposure`.

#### 2.d Feedback

- During VR-PA training only the ball represented hand position.
- The ball became visible only after the hand moved more than `10 cm` from the origin.
- Successful hits produced reward feedback:
  - sound
  - visual lights
- Near space additionally provided naturalistic haptic feedback from the bowling pin.
- Open-loop tests provided no visual or haptic feedback.

### 3. Clarify what the authors mean by “trials”

- One movement toward one bowling-pin target corresponds to one trial.
- Open-loop task: `20 trials`.
- Landmark task: `50 trials`.
- Training trials are individual reaches/rolls; extra batches are added in sets of `25`.

### 4. Use only information explicitly stated in the paper

- This extraction uses only explicitly stated details from the local PDF.
- The paper is explicit that it is a VR-PA setup based on visuomotor rotation.

### 5. What tasks are used for what purposes? (Full Prism-Therapy Pipeline)

#### Stage 1 — USN Assessment (Clinical Neglect Evaluation)

- `Not applicable`.
- The sample consisted of neurologically intact participants.

#### Stage 2 — Baseline Visuomotor Assessment (Pre-Exposure Pointing)

- Baseline tasks included:
  - open-loop reaching near and far
  - landmark near and far
  - reaching straight ahead in darkness
- Open-loop task:
  - `20 trials`
  - no visual or haptic feedback

#### Stage 3 — Exposure Task (Adaptation Phase)

- Exact exposure task:
  - near-space push or far-space roll with the bowling-ball representation
- Target arrangement:
  - target at the edge of the table in near or far space
- Hand visibility:
  - only ball visible after 10 cm
- Shift introduction:
  - fixed `25°` rightward rotation introduced after 10 initial no-shift trials
- What trials mean:
  - one push/roll attempt = one trial
- What the exposure task measures:
  - trial-by-trial target error and adaptation across active or guided training.

#### Stage 4 — Post-Exposure Baseline Repetition (After-Effect Assessment)

- After-effects were measured with:
  - open-loop reaching
  - reaching straight ahead
  - landmark task
- The paper interprets opposite-direction open-loop biases after shift removal as sensorimotor aftereffects.

#### Stage 5 — (Optional) Post-Therapy USN Re-Assessment

- `Not applicable`.

## Paper 22: Wilf et al. 2022 (`Wilf2022(VR).pdf`)

### 1. Provide a detailed, step-by-step description of the experimental procedures

- Forty-five healthy participants were divided into:
  - rightward VRPA
  - leftward VRPA
  - sham VRPA
- A session began with `baseline VR training` without shift.
- The paper explicitly states `60 trials` of baseline VR training.
- Participants then performed behavioral aftereffect tests.
- They underwent pre-adaptation fMRI.
- They then performed the VRPA session outside the scanner.
- The VRPA session consisted of `160 trials` of the tennis-game training with either leftward, rightward, or sham shift.
- The shift was introduced on the `11th` trial.
- Behavioral aftereffect tests were repeated immediately afterward.
- Participants then underwent post-adaptation fMRI.
- Finally, a subset of the aftereffect tests was repeated about `40 min` after adaptation.

### 2. Describe precisely how the visuomotor shift was introduced

#### 2.a Type of shift

- The paper describes a visuomotor rotation / VRPA-like training.
- It introduced a shift between the real hand and the virtual hand.
- Full visual frustum / optical scene: `no`.
- Hand representation: `yes`, natural-looking virtual hand.
- Visible arm or body: the virtual hand was invisible near the origin and became visible only in the last segment of the reach.

#### 2.b Magnitude of the shift

- `20°` rightward or leftward rotational shift.

#### 2.c How the shift was scheduled

- Baseline had no shift.
- During adaptation, the `20°` shift was induced on `trial 11`.
- Sham involved no shift.
- Prompt2 schedule category: `discrete during exposure`.

#### 2.d Feedback

- During training, when a ball was caught, participants received:
  - wind-chime sound
  - controller vibration
  - glitter visual effect
- Visual feedback of hand trajectory was partial because the hand was invisible near the origin and visible only later in the movement.

### 3. Clarify what the authors mean by “trials”

- One catch attempt to one tennis ball corresponds to one trial.
- Baseline training: `60 trials`.
- Adaptation training: `160 trials`.
- Behavioral aftereffect tests used separate trial-based tasks, such as straight-ahead pointing and open-loop pointing, but this paper does not restate every count in full.

### 4. Use only information explicitly stated in the paper

- This extraction uses only explicitly stated details from the local PDF.
- Where the paper refers back to an earlier VRPA setup rather than restating all mechanics, those missing details are left as `not restated`.

### 5. What tasks are used for what purposes? (Full Prism-Therapy Pipeline)

#### Stage 1 — USN Assessment (Clinical Neglect Evaluation)

- `Not applicable`.
- The sample consisted of healthy participants.

#### Stage 2 — Baseline Visuomotor Assessment (Pre-Exposure Pointing)

- Baseline consisted of no-shift VR tennis-ball catching and behavioral aftereffect tests before adaptation.
- The paper explicitly includes:
  - pointing straight ahead with eyes closed
  - open-loop pointing to visual targets in VR
  - an additional open-loop test outside VR

#### Stage 3 — Exposure Task (Adaptation Phase)

- Exact exposure task:
  - tennis-ball catching with a virtual hand in VR
- Trial count:
  - `160` trials
- Shift introduction:
  - fixed `20°` leftward or rightward shift induced on trial 11
- What trials mean:
  - one catch attempt = one trial
- What the exposure task measures:
  - the study uses behavioral aftereffects and brain imaging changes as the main outcomes.

#### Stage 4 — Post-Exposure Baseline Repetition (After-Effect Assessment)

- After-effects were assessed immediately post-training and again at the end of the session about `40 min` later.
- Straight-ahead pointing with eyes closed was the main reported behavioral index.
- The paper interprets these post-training pointing biases as VRPA-induced aftereffects.

#### Stage 5 — (Optional) Post-Therapy USN Re-Assessment

- `Not applicable`.

## Paper 23: Wähnert and Gerhards 2024 (`Wähnert&Gerhards2021(VR).pdf`)

### 1. Provide a detailed, step-by-step description of the experimental procedures

- Thirty healthy right-handed individuals were assigned to:
  - control group
  - misinformation group
  - arrow group
- Participants sat at a table in VR.
- A Vive tracker was attached to the right hand.
- Acoustic signals indicated start and end of the movement.
- The familiarization phase consisted of `30 trials`:
  - `10` with concurrent feedback
  - `20` with only terminal feedback
- The baseline phase consisted of `10 trials`.
- The exposure phase consisted of `35 trials`.
- The de-exposure phase consisted of `30 trials`.
- A ten-second rest period was inserted after a maximum of `15 trials`.
- The whole experiment lasted `45-60 min`.
- The VR part lasted no longer than `20 min`.
- In the misinformation group, participants were told that an additional random error component would be built into the movement during exposure.
- In the arrow group, a 3D arrow replaced the virtual hand.

### 2. Describe precisely how the visuomotor shift was introduced

#### 2.a Type of shift

- The paper states that the virtual environment was visually displaced to the right.
- Full visual frustum / optical scene: `yes`, the paper describes visual displacement of the environment.
- Hand representation:
  - control and misinformation groups: virtual hand
  - arrow group: 3D arrow
- Visible arm or body:
  - representation of the moving right hand as virtual hand or arrow

#### 2.b Magnitude of the shift

- `11.31°`, equivalent to `20 dioptres`.

#### 2.c How the shift was scheduled

- The displacement was applied in the exposure phase only.
- The paper does not report a gradual ramp.
- The shift was removed in the de-exposure phase.
- Prompt2 schedule category: `discrete between phases`.

#### 2.d Feedback

- Familiarization included concurrent and terminal feedback.
- Subsequent phases used terminal-feedback logic.
- Magnitude was measured from the first de-exposure trial without prior de-exposure feedback.
- The remaining de-exposure trials were used to measure persistence.

### 3. Clarify what the authors mean by “trials”

- One pointing movement corresponds to one trial.
- Familiarization = `30 trials`.
- Baseline = `10 trials`.
- Exposure = `35 trials`.
- De-exposure = `30 trials`.
- Magnitude of the aftereffect was based on the `first trial` of de-exposure.
- Persistence was based on the remaining `29` de-exposure trials.

### 4. Use only information explicitly stated in the paper

- This extraction uses only explicitly stated details from the local PDF.
- The paper explicitly reports how magnitude and persistence were operationalized.

### 5. What tasks are used for what purposes? (Full Prism-Therapy Pipeline)

#### Stage 1 — USN Assessment (Clinical Neglect Evaluation)

- `Not applicable`.
- Healthy participants were used.

#### Stage 2 — Baseline Visuomotor Assessment (Pre-Exposure Pointing)

- Baseline consisted of `10` pointing trials without the experimental displacement.
- The first `30` familiarization trials preceded this and established task understanding.

#### Stage 3 — Exposure Task (Adaptation Phase)

- Exact exposure task:
  - repeated VR pointing with right-hand tracking
- Target arrangement:
  - nine possible target positions with ±3 cm deviations from the centre position
- Hand visibility:
  - virtual hand or arrow, depending on group
- Shift introduction:
  - fixed `11.31°` rightward displacement in the exposure phase
- What trials mean:
  - one point = one trial
- What the exposure task measures:
  - subsequent aftereffect magnitude and persistence

#### Stage 4 — Post-Exposure Baseline Repetition (After-Effect Assessment)

- After-effects were measured in the `de-exposure` phase after removal of the visual displacement.
- Magnitude:
  - horizontal deviation in the first baseline-corrected de-exposure trial
- Persistence:
  - mean absolute baseline-corrected horizontal deviation across the remaining 29 de-exposure trials
- The paper interprets these measures as aftereffect magnitude and persistence following sensorimotor adaptation.

#### Stage 5 — (Optional) Post-Therapy USN Re-Assessment

- `Not applicable`.
