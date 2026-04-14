#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

namespace ZhuozhengYuan.EditorTools
{
    public static class Chapter02QuizSessionVerifier
    {
        private const string RunMenuPath = "Tools/Zhuozhengyuan/Run Chapter02 Quiz Session Verifier";

        [MenuItem(RunMenuPath)]
        public static void RunFromMenu()
        {
            Run();
        }

        public static void Run()
        {
            try
            {
                Chapter02Question[] bank =
                {
                    CreateQuestion("q1", 0),
                    CreateQuestion("q2", 1),
                    CreateQuestion("q3", 2),
                    CreateQuestion("q4", 3),
                    CreateQuestion("q5", 0),
                    CreateQuestion("q6", 1)
                };

                Chapter02QuizSession session = Chapter02QuizSession.CreateRandomized(bank, 4, 12345);
                AssertCondition(session.TotalQuestionCount == 4, "Session should draw exactly four questions.");

                string[] orderedIds = session.GetOrderedQuestionIds();
                AssertCondition(orderedIds.Length == 4, "Ordered question IDs should match the draw count.");
                AssertCondition(HasUniqueIds(orderedIds), "Drawn Chapter02 questions should not repeat.");

                Chapter02Question firstQuestion = session.CurrentQuestion;
                Chapter02QuizSession.AnswerResult wrongResult = session.SubmitAnswer((firstQuestion.correctOptionIndex + 1) % 4);
                AssertCondition(!wrongResult.isCorrect, "Wrong answer should be reported as incorrect.");
                AssertCondition(session.AnsweredCorrectCount == 0, "Wrong answer should not advance progress.");
                AssertCondition(session.CurrentQuestion == firstQuestion, "Wrong answer should repeat the current question.");

                Chapter02QuizSession.AnswerResult correctResult = session.SubmitAnswer(firstQuestion.correctOptionIndex);
                AssertCondition(correctResult.isCorrect, "Correct answer should be reported as correct.");
                AssertCondition(session.AnsweredCorrectCount == 1, "Correct answer should advance progress by one.");
                AssertCondition(session.CurrentQuestion != firstQuestion, "Correct answer should advance to the next question.");

                Chapter02QuizSession restoredSession = Chapter02QuizSession.Restore(bank, orderedIds, 1);
                AssertCondition(restoredSession != null, "Restore should rebuild a valid Chapter02 quiz session.");
                AssertCondition(restoredSession.AnsweredCorrectCount == 1, "Restore should preserve answered count.");
                AssertCondition(restoredSession.CurrentQuestion != null && restoredSession.CurrentQuestion.questionId == orderedIds[1], "Restore should resume on the first unanswered question.");

                Debug.Log("Chapter02QuizSessionVerifier passed.");
            }
            catch (Exception exception)
            {
                Debug.LogError("Chapter02QuizSessionVerifier failed: " + exception.Message);
                throw;
            }
        }

        private static Chapter02Question CreateQuestion(string id, int correctIndex)
        {
            return new Chapter02Question
            {
                questionId = id,
                questionText = "Question " + id,
                options = new[] { "A", "B", "C", "D" },
                correctOptionIndex = correctIndex,
                correctFeedback = "correct",
                wrongFeedback = "wrong"
            };
        }

        private static bool HasUniqueIds(string[] orderedIds)
        {
            for (int outer = 0; outer < orderedIds.Length; outer++)
            {
                for (int inner = outer + 1; inner < orderedIds.Length; inner++)
                {
                    if (string.Equals(orderedIds[outer], orderedIds[inner], StringComparison.Ordinal))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private static void AssertCondition(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(message);
            }
        }
    }
}
#endif
