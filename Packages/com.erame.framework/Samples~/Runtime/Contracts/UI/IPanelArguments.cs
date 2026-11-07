namespace __NS__.Runtime{
    /// <summary>
    /// 面板需要“打开参数”时实现此接口。
    /// <see cref="IUIService.OpenPanelAsync(string, object)"/> 会在 <see cref="IUIPanel.OnShowAsync"/>
    /// 之前调用 <see cref="SetArguments"/>；参数为 null 表示清空上一次的值。
    /// </summary>
    public interface IPanelArguments{
        void SetArguments(object args);
    }
}
