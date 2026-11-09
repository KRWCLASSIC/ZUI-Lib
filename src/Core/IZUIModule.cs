namespace ZUI.Core
{
    /// <summary>
    /// Interface for modular extensions or feature modules in ZUI.
    /// Any class implementing IZUIModule is automatically discovered and initialized by ZUI.
    /// </summary>
    public interface IZUIModule
    {
        string Name { get; }
        void Initialize();
    }
}
