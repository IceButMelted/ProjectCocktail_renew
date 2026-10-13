using UnityEngine;

public enum SoundChannel { BGM, Ambient, SFX, UiSFX, Voice }

/// <summary>
/// Draws a string field as a dropdown of the ids in SoundData (see SoundIdDrawer).
/// Either a fixed channel, or the name of a sibling <see cref="SoundChannel"/> field.
/// </summary>
public class SoundIdAttribute : PropertyAttribute
{
    public readonly SoundChannel Channel;
    public readonly string ChannelField;    // null = use the fixed Channel

    public SoundIdAttribute(SoundChannel channel) { Channel = channel; }
    public SoundIdAttribute(string channelField) { ChannelField = channelField; }
}
