namespace Game.Runtime{
    // 当语言切换时，通过事件总线广播
    public readonly struct LanguageChangedEvent{
        public readonly string LanguageCode;

        public LanguageChangedEvent(string languageCode){
            LanguageCode = languageCode;
        }
    }
}