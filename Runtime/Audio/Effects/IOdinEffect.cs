using OdinNative.Wrapper.Media;

namespace OdinNative.Unity.Audio
{
    public interface IOdinEffect
    {
        IMedia Media { get; set; }

        T GetMedia<T>() where T : IMedia;

        PiplineEffect GetEffect();
    }
}