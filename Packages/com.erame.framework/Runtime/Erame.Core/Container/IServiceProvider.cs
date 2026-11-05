namespace Erame.Core{
    public interface IServiceProvider{
        T Get<T>() where T : class;
        bool TryGet<T>(out T service) where T : class;
    }
}