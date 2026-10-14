using System;
using System.Collections.Generic;

namespace MyFutureCity.Core
{
    public enum StepOutcome
    {
        /// <summary>The best action was chosen.</summary>
        Correct,
        /// <summary>A wrong action was chosen; an alternative explanation is offered and the child tries again.</summary>
        WrongRetry,
        /// <summary>Second miss: the best action is shown with its explanation and the lesson moves on (never a dead end).</summary>
        Revealed
    }

    public sealed class StepResult
    {
        public StepOutcome Outcome;
        /// <summary>Feedback for the chosen option (or the success text when correct).</summary>
        public string FeedbackFa;
        public string RetryHintFa;
        public string CorrectTextFa;
        /// <summary>True only when the very first choice was the best one.</summary>
        public bool FirstTry;
        public int Attempts;
        public bool IsFinished { get { return Outcome != StepOutcome.WrongRetry; } }
    }

    /// <summary>
    /// One teaching decision being played. Options are shuffled every time so the best answer is never
    /// in a predictable place. A wrong choice removes that option and shows another way to explain it;
    /// after two attempts the answer is revealed so the child is never stuck.
    /// Pure C#, fully unit-tested.
    /// </summary>
    public sealed class TeachingSession
    {
        public const int MaxAttempts = 2;

        private readonly LessonStep step;
        private readonly List<int> remaining = new List<int>();   // original option indexes, in display order

        public TeachingSession(LessonStep step, Random rng)
        {
            if (step == null) throw new ArgumentNullException("step");
            if (step.options == null || step.options.Count < 2) throw new ArgumentException("step needs at least two options");
            if (step.correctIndex < 0 || step.correctIndex >= step.options.Count) throw new ArgumentException("correctIndex out of range");
            this.step = step;
            for (int i = 0; i < step.options.Count; i++) remaining.Add(i);
            Shuffle(remaining, rng ?? new Random());
        }

        public LessonStep Step { get { return step; } }
        public int Attempts { get; private set; }
        public bool Finished { get; private set; }
        /// <summary>True once the child needed the alternative explanation (a hint) for this step.</summary>
        public bool UsedHint { get; private set; }
        public bool FirstTryCorrect { get; private set; }

        public int OptionCount { get { return remaining.Count; } }

        public string OptionText(int displayIndex)
        {
            return step.options[remaining[displayIndex]];
        }

        public string CorrectText { get { return step.options[step.correctIndex]; } }

        /// <summary>Position (in the current display order) of the best answer. Used by tests and analytics only.</summary>
        public int CorrectDisplayIndex
        {
            get { return remaining.IndexOf(step.correctIndex); }
        }

        public StepResult Choose(int displayIndex)
        {
            if (Finished || displayIndex < 0 || displayIndex >= remaining.Count) return null;
            int original = remaining[displayIndex];
            Attempts++;

            if (original == step.correctIndex)
            {
                Finished = true;
                FirstTryCorrect = Attempts == 1;
                return new StepResult
                {
                    Outcome = StepOutcome.Correct,
                    FeedbackFa = step.successFa,
                    CorrectTextFa = CorrectText,
                    FirstTry = FirstTryCorrect,
                    Attempts = Attempts
                };
            }

            UsedHint = true;
            string feedback = step.FeedbackFor(original);
            remaining.RemoveAt(displayIndex);

            // Out of attempts, or only the best answer is left: show it and carry on.
            if (Attempts >= MaxAttempts || remaining.Count <= 1)
            {
                Finished = true;
                FirstTryCorrect = false;
                return new StepResult
                {
                    Outcome = StepOutcome.Revealed,
                    FeedbackFa = feedback,
                    RetryHintFa = step.retryHintFa,
                    CorrectTextFa = CorrectText,
                    FirstTry = false,
                    Attempts = Attempts
                };
            }

            return new StepResult
            {
                Outcome = StepOutcome.WrongRetry,
                FeedbackFa = feedback,
                RetryHintFa = step.retryHintFa,
                CorrectTextFa = CorrectText,
                FirstTry = false,
                Attempts = Attempts
            };
        }

        private static void Shuffle(List<int> list, Random rng)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                int tmp = list[i]; list[i] = list[j]; list[j] = tmp;
            }
        }
    }
}
