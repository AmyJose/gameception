using System;
using System.Collections.Generic;
using InputLayer;
using Rhythm;
using UnityEngine;

namespace Gameplay.Choreography
{
    // Manages the state and lifecycle of choreography sequences.
    // Tracks active sequence, generates sequences on interval, and broadcasts events.
    //
    public class ChoreographyQueueState : MonoBehaviour
    {
        [Header("Sequence Generation")]
        [SerializeField] private int sequenceIntervalBeats = 60; // New choreography sequence every 60 beats (around 30 sec at 120 BPM)
        [SerializeField] private int sequenceDurationBeats = 8; //Each sequence is 8 beats long
        [SerializeField] private int promptsPerSequence = 4; // 4 poses to perform

        [Header("Dependencies")]
        [SerializeField] private BeatClock beatClock;

        //Events broadcast to listeners UI, Judge
        public event Action<ChoreographySequence, int> OnSequenceStarted; // (sequence, startBeat)
        public event Action<PromptData> OnPromptActive; // when a prompt enters the "hit zone"
        public event Action<PromptData> OnPromptExpired; // when a prompt passes the hit zone
        public event Action OnSequenceCompleted; // all prompts resolved

        //Data for a prompt at a specific point in time
        public struct PromptData
        {
            public int promptId;
            public ElementPose requiredPose;
            public int beatIndex;
        }

        private ChoreographySequence _activeSequence;
        private int _sequenceStartBeat = 0;
        private int _lastGeneratedSequenceBeat = -999;
        private int _promptIdCounter = 0;

        //tracks which prompts have been judged
        private readonly HashSet<int> _judgedPrompts = new();

        private void OnEnable()
        {
            if (beatClock != null)
                beatClock.OnBeat += HandleBeat;
        }

        private void OnDisable()
        {
            if (beatClock != null)
                beatClock.OnBeat -= HandleBeat;
        }

        private void HandleBeat(BeatInfo beatInfo)
        {
            int currentBeat = beatInfo.beatIndex;

            //Generates new sequence if interval was reached
            if (currentBeat >= _lastGeneratedSequenceBeat + sequenceIntervalBeats)
            {
                GenerateSequence(currentBeat, beatInfo.dspSongTime);
            }

            //checks for active prompts in this beat
            if (_activeSequence != null)
            {
                CheckActivePrompts(currentBeat);
            }
        }

        private void GenerateSequence(int startBeat, double startDspTime)
        {
            _activeSequence = new ChoreographySequence(sequenceDurationBeats);
            _sequenceStartBeat = startBeat;
            _judgedPrompts.Clear();
            _promptIdCounter = 0;

            //Generates random pose sequence
            for (int i = 0; i < promptsPerSequence; i++)
            {
                ElementPose randomPose = GetRandomPose();
                _activeSequence.AddPrompt(i, randomPose, _promptIdCounter++);
            }

            Debug.Log($"[ChoreographyQueue] Generated sequence starting at beat {startBeat}");
            OnSequenceStarted?.Invoke(_activeSequence, startBeat);

            _lastGeneratedSequenceBeat = startBeat;
        }

        private void CheckActivePrompts(int currentBeat)
        {
            foreach (var prompt in _activeSequence.prompts)
            {
                int promptBeat = _sequenceStartBeat + prompt.beatOffset;

                // Reached the hit window for this prompt
                if (currentBeat == promptBeat && !_judgedPrompts.Contains(prompt.promptId))
                {
                    _judgedPrompts.Add(prompt.promptId);
                    
                    var promptData = new PromptData
                    {
                        promptId = prompt.promptId,
                        requiredPose = prompt.requiredPose,
                        beatIndex = promptBeat
                    };
                    OnPromptActive?.Invoke(promptData);
                }

                // Prompt expired
                if (currentBeat > promptBeat + 1 && !_judgedPrompts.Contains(prompt.promptId))
                {
                    _judgedPrompts.Add(prompt.promptId);
                    
                    var promptData = new PromptData
                    {
                        promptId = prompt.promptId,
                        requiredPose = prompt.requiredPose,
                        beatIndex = promptBeat
                    };

                    OnPromptExpired?.Invoke(promptData);
                }
            }

            //Check if sequence completed
            if (_judgedPrompts.Count == _activeSequence.prompts.Count)
            {
                OnSequenceCompleted?.Invoke();
                _activeSequence = null;
            }
        }

        private ElementPose GetRandomPose()
        {
            // Excluding "None" (at index 0)
            int randomIndex = UnityEngine.Random.Range(1, 5); // 1=Earth, 2=Water, 3=Fire, 4=Ice
            return (ElementPose)randomIndex;
        }

        public ChoreographySequence GetActiveSequence() => _activeSequence;
        public int GetSequenceStartBeat() => _sequenceStartBeat;
    }
}