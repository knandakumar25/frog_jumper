/// <summary>Controller identity and physical face positions are testable without native Godot objects.</summary>
public class InputPromptFamilyTests
{
    [TestCase("Xbox Wireless Controller", PromptControllerFamily.Xbox)]
    [TestCase("Microsoft X-Box 360 pad", PromptControllerFamily.Xbox)]
    [TestCase("XINPUT Controller", PromptControllerFamily.Xbox)]
    [TestCase("Sony DualSense Wireless Controller", PromptControllerFamily.PlayStation)]
    [TestCase("DualShock 4", PromptControllerFamily.PlayStation)]
    [TestCase("PS5 Controller", PromptControllerFamily.PlayStation)]
    [TestCase("Nintendo Switch Pro Controller", PromptControllerFamily.Nintendo)]
    [TestCase("Joy-Con (L)", PromptControllerFamily.Nintendo)]
    [TestCase("Logitech F310", PromptControllerFamily.Unknown)]
    [TestCase("Generic USB Joystick", PromptControllerFamily.Unknown)]
    [TestCase("", PromptControllerFamily.Unknown)]
    /// <summary>Reported identities select only recognized families; generic devices stay neutral.</summary>
    public void IdentitySelectsRecognizedFamilyAndKeepsUnknownNeutral(string identity, PromptControllerFamily expected) =>
        Assert.That(InputPromptService.DetectFamily(identity), Is.EqualTo(expected));

    [TestCase(PromptControllerFamily.Xbox, "A", "B", "X", "Y")]
    [TestCase(PromptControllerFamily.PlayStation, "Cross", "Circle", "Square", "Triangle")]
    [TestCase(PromptControllerFamily.Nintendo, "B", "A", "Y", "X")]
    [TestCase(PromptControllerFamily.Unknown, "South face", "East face", "West face", "North face")]
    /// <summary>Family naming changes never move the four physical face-button functions.</summary>
    public void LabelsFollowPhysicalPositionsAcrossFamilies(PromptControllerFamily family, string south, string east, string west, string north)
    {
        string[] expected = { south, east, west, north };
        for (int position = 0; position < expected.Length; position++)
            Assert.That(InputPromptService.ButtonLabel(position, family), Is.EqualTo(expected[position]));
    }
}
