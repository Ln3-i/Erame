namespace __NS__.Runtime{
    public readonly struct SaveProgressEvent{
        public readonly float Progress; // 0.0 到 1.0
        public readonly bool IsLoading; // true 代表在读档，false 代表在存档

        public SaveProgressEvent(float progress, bool isLoading){
            Progress = progress;
            IsLoading = isLoading;
        }
    }
}