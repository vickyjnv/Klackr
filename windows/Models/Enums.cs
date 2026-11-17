namespace Klackr.Models
{
    public enum KeyType
    {
        Regular,
        Space,
        Enter,
        Backspace,
        Modifier
    }

    public enum KeyEvent
    {
        Press,
        Release
    }

    public enum SoundPackType
    {
        CherryMXBrown,
        CherryMXBlue,
        CherryMXRed,
        CherryMXBlack,
        ToprePurple,
        EverglideOreo
    }

    public enum MouseButton
    {
        Left,
        Right,
        Middle
    }

    public enum MouseEventType
    {
        Press,
        Release
    }

    public enum MouseSoundPackType
    {
        ClassicOmron,
        LogitechSuperlight,
        RazerOptical,
        GloriousModelO,
        SilentOffice
    }
}
