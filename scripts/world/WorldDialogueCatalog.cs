using System.Collections.Generic;

/// <summary>
/// Authored English conversation text for the playable quest slice, keyed by objective ID.
/// Lines pass through <see cref="WorldText"/> at display time, so a translation added later
/// applies automatically. Until then other languages show this English text. These lines are not voiced.
/// </summary>
public static class WorldDialogueCatalog
{
    /// <summary>One conversation: who is speaking and the lines shown in order.</summary>
    public sealed record Conversation(string Speaker, IReadOnlyList<string> Lines);

    private static readonly Dictionary<string, Conversation> Conversations = new()
    {
        ["obj_ok01_1"] = new("Sora", new[] { "The market lost power again. Half the lantern stalls are dark and people are scared to stay after sundown.", "The conduit behind the stalls is cracked. Patch it, and watch for the probes that came through the palace wall." }),
        ["obj_ok01_2"] = new("Field Note", new[] { "The split conduit reseats with a shower of sparks. The lanterns along the row come back one at a time." }),
        ["obj_ok02_1"] = new("Master Gen", new[] { "They are digging under the old walls, looking for our records so they can erase them properly.", "There is a cipher shrine they opened last night. See what they took. I will keep watch here." }),
        ["obj_ok02_2"] = new("Field Note", new[] { "The shrine's cipher plate is scored with extraction marks, but the inner register is intact. Then the drones turn back toward Gen." }),
        ["obj_ok03_1"] = new("Sora", new[] { "Everyone is here. We can afford one project this season, and whichever we pick, the other waits." }),
        ["obj_ok03_2"] = new("District Council", new[] { "Say which comes first. The district will live with the answer." }),
        ["obj_ok03_3"] = new("District Council", new[] { "Then it is decided. We will start at first light, and we will remember who stood here." }),
        ["obj_cw01_1"] = new("Dr. Maya Chen", new[] { "Shelter 04 is out of trauma kits and the courtyard clinic has none at all.", "Take these across. Stay off the open viaduct if you can; Cronos enforcers have started searching carriers." }),
        ["obj_cw01_2"] = new("Field Note", new[] { "The kits are handed through the courtyard gate. A moment later, boots on the stairs: enforcers followed the delivery." }),
        ["obj_cw02_1"] = new("Barik", new[] { "Feel that hum? The pillars are shaking themselves apart. Two dampeners have drifted out of alignment.", "Reset both. If they go, this line goes with them, and so does everyone sleeping under it." }),
        ["obj_cw02_2"] = new("Field Note", new[] { "The first dampener locks back into its seat. The hum drops by half." }),
        ["obj_cw02_3"] = new("Field Note", new[] { "The second dampener settles. For the first time in weeks, the viaduct is quiet." }),
        ["obj_cw03_1"] = new("Dr. Maya Chen", new[] { "The wardens are gathered. There is material for one major project, not two." }),
        ["obj_cw03_2"] = new("Ward Wardens", new[] { "Clinic or workshop. Choose, and we will commit the crews tonight." }),
        ["obj_cw03_3"] = new("Ward Wardens", new[] { "It is ratified. The crews move at dawn." }),
        ["obj_op_0_1"] = new("Kaito", new[] { "My old unit's names are still cut into this stone. Someone has been here, scanning it." }),
        ["obj_op_0_3"] = new("Kaito", new[] { "The datacore still holds our last drill. I remember every step of it. Good." }),
        ["obj_op_2_1"] = new("Kiana", new[] { "We left a marker here for the ones who never made the coast. It has been tampered with." }),
        ["obj_op_2_3"] = new("Kiana", new[] { "The telemetry is whole. Their last positions, every one of them. I will carry these home." }),
    };

    /// <summary>
    /// Returns the conversation for an objective. An objective without authored lines falls
    /// back to its own description, so a catalog addition can never produce an empty panel.
    /// </summary>
    public static Conversation For(QuestObjective objective) =>
        Conversations.TryGetValue(objective.Id, out var conversation)
            ? conversation
            : CityRegistry.FindConversation(objective.Id) ?? new Conversation("Field Note", new[] { objective.Description });

    /// <summary>Whether an objective has authored lines; tests use this to catch unwritten slice content.</summary>
    public static bool HasAuthoredLines(string objectiveId) => Conversations.ContainsKey(objectiveId) || CityRegistry.FindConversation(objectiveId) != null;
}
