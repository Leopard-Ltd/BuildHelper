namespace BuildHelper.Workflows
{
    [System.Serializable]
    public class TelegramInformation
    {
        public string TelegramBotToken       = "";
        public string TelegramChatId         = "";
        public string TelegramThreadId       = "";
        public bool   ShouldUploadToTelegram = false;
    }
}