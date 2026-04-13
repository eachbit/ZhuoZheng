using System;
using System.Collections.Generic;

namespace ZhuozhengYuan
{
    public sealed class Chapter02QuizSession
    {
        public readonly struct AnswerResult
        {
            public readonly bool isCorrect;
            public readonly bool isCompleted;
            public readonly int answeredCorrectCount;
            public readonly Chapter02Question question;

            public AnswerResult(bool isCorrectValue, bool isCompletedValue, int answeredCorrectCountValue, Chapter02Question questionValue)
            {
                isCorrect = isCorrectValue;
                isCompleted = isCompletedValue;
                answeredCorrectCount = answeredCorrectCountValue;
                question = questionValue;
            }
        }

        private readonly List<Chapter02Question> _orderedQuestions;
        private int _answeredCorrectCount;

        public int TotalQuestionCount
        {
            get { return _orderedQuestions.Count; }
        }

        public int AnsweredCorrectCount
        {
            get { return _answeredCorrectCount; }
        }

        public bool IsCompleted
        {
            get { return _answeredCorrectCount >= _orderedQuestions.Count && _orderedQuestions.Count > 0; }
        }

        public Chapter02Question CurrentQuestion
        {
            get
            {
                if (_orderedQuestions.Count == 0 || IsCompleted)
                {
                    return null;
                }

                return _orderedQuestions[_answeredCorrectCount];
            }
        }

        public Chapter02QuizSession(IList<Chapter02Question> orderedQuestions, int answeredCorrectCount)
        {
            if (orderedQuestions == null || orderedQuestions.Count == 0)
            {
                throw new ArgumentException("Chapter02 quiz session requires at least one ordered question.", nameof(orderedQuestions));
            }

            _orderedQuestions = new List<Chapter02Question>(orderedQuestions.Count);
            for (int index = 0; index < orderedQuestions.Count; index++)
            {
                Chapter02Question question = orderedQuestions[index];
                if (question == null || !question.IsValid())
                {
                    throw new ArgumentException("Chapter02 quiz session received an invalid question entry.", nameof(orderedQuestions));
                }

                _orderedQuestions.Add(question);
            }

            _answeredCorrectCount = Math.Max(0, Math.Min(answeredCorrectCount, _orderedQuestions.Count));
        }

        public AnswerResult SubmitAnswer(int optionIndex)
        {
            Chapter02Question question = CurrentQuestion;
            if (question == null)
            {
                throw new InvalidOperationException("Cannot submit an answer when the Chapter02 quiz session is complete.");
            }

            bool isCorrect = optionIndex == question.correctOptionIndex;
            if (isCorrect)
            {
                _answeredCorrectCount++;
            }

            return new AnswerResult(isCorrect, IsCompleted, _answeredCorrectCount, question);
        }

        public string[] GetOrderedQuestionIds()
        {
            string[] ids = new string[_orderedQuestions.Count];
            for (int index = 0; index < _orderedQuestions.Count; index++)
            {
                ids[index] = _orderedQuestions[index].questionId;
            }

            return ids;
        }

        public static Chapter02QuizSession CreateRandomized(IList<Chapter02Question> questionBank, int questionCount, int seed)
        {
            List<Chapter02Question> validQuestions = CollectValidQuestions(questionBank);
            if (questionCount <= 0 || validQuestions.Count < questionCount)
            {
                throw new ArgumentException("Not enough valid Chapter02 questions to create a randomized session.", nameof(questionBank));
            }

            List<Chapter02Question> pool = new List<Chapter02Question>(validQuestions);
            List<Chapter02Question> orderedQuestions = new List<Chapter02Question>(questionCount);
            Random random = new Random(seed);

            while (orderedQuestions.Count < questionCount)
            {
                int poolIndex = random.Next(pool.Count);
                orderedQuestions.Add(pool[poolIndex]);
                pool.RemoveAt(poolIndex);
            }

            return new Chapter02QuizSession(orderedQuestions, 0);
        }

        public static Chapter02QuizSession Restore(IList<Chapter02Question> questionBank, IList<string> orderedQuestionIds, int answeredCorrectCount)
        {
            if (orderedQuestionIds == null || orderedQuestionIds.Count == 0)
            {
                return null;
            }

            Dictionary<string, Chapter02Question> lookup = BuildQuestionLookup(questionBank);
            List<Chapter02Question> orderedQuestions = new List<Chapter02Question>(orderedQuestionIds.Count);

            for (int index = 0; index < orderedQuestionIds.Count; index++)
            {
                string questionId = orderedQuestionIds[index];
                if (string.IsNullOrWhiteSpace(questionId))
                {
                    return null;
                }

                if (!lookup.TryGetValue(questionId, out Chapter02Question question))
                {
                    return null;
                }

                orderedQuestions.Add(question);
            }

            return new Chapter02QuizSession(orderedQuestions, answeredCorrectCount);
        }

        private static List<Chapter02Question> CollectValidQuestions(IList<Chapter02Question> questionBank)
        {
            Dictionary<string, Chapter02Question> deduplicated = BuildQuestionLookup(questionBank);
            return new List<Chapter02Question>(deduplicated.Values);
        }

        private static Dictionary<string, Chapter02Question> BuildQuestionLookup(IList<Chapter02Question> questionBank)
        {
            Dictionary<string, Chapter02Question> lookup = new Dictionary<string, Chapter02Question>(StringComparer.Ordinal);
            if (questionBank == null)
            {
                return lookup;
            }

            for (int index = 0; index < questionBank.Count; index++)
            {
                Chapter02Question question = questionBank[index];
                if (question == null || !question.IsValid() || lookup.ContainsKey(question.questionId))
                {
                    continue;
                }

                lookup.Add(question.questionId, question);
            }

            return lookup;
        }
    }
}
