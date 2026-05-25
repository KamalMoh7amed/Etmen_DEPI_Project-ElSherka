namespace Etmen_BLL.Helpers
{
    /// <summary>
    /// Builds the system and user prompts sent to the LLM (GPT-4 / Claude) by AIChatService.
    /// Injects the patient's health context so the model can produce personalised answers.
    /// </summary>
    public static class AIChatHelper
    {
        /// <summary>
        /// Constructs the system prompt that primes the model with medical context.
        /// </summary>
        /// <param name="patientContext">
        /// Serialised patient health summary (risk score, diagnoses, medications, allergies).
        /// </param>
        public static string BuildSystemPrompt(string patientContext)
            => $"""
               أنت مساعد طبي ذكي على منصة اطمئن.
               مهمتك تقديم معلومات صحية مفيدة ودقيقة باللغة العربية.
               لا تضع تشخيصات قاطعة ولا توصي بأدوية بعينها — احرص دائماً على إحالة المريض لطبيبه.

               --- سياق المريض الحالي ---
               {patientContext}
               --------------------------
               """;

        /// <summary>
        /// Wraps the user's raw question together with optional last-assessment context
        /// into the user-turn message body.
        /// </summary>
        public static string BuildUserMessage(string userQuestion, string? lastAssessmentSummary = null)
        {
            if (string.IsNullOrWhiteSpace(lastAssessmentSummary))
                return userQuestion;

            return $"{userQuestion}\n\n[آخر تقييم صحي: {lastAssessmentSummary}]";
        }
    }
}
