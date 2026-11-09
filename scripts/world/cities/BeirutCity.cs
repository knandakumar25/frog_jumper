/// <summary>
/// Beirut, 2157: Maya's home, opened when Aura Phoenicia joins the squad. Place names are real; the buildings,
/// people and events are invented, and the story refers to no real war, disaster or political group.
/// Fights happen at works and yards only.
/// </summary>
public static class BeirutCity
{
    private const string Civic = "Civic Recovery Network", Couriers = "Independent Couriers", Guild = "Reclamation Guild", Signal = "Free Signal Collective";

    /// <summary>The city as data.</summary>
    public static readonly WorldDefinition Definition = CityBuilder.Build(new CitySpec(
        "beirut", "bei", "Beirut", "Aura Phoenicia",
        new[]
        {
            new DistrictSpec("Harbor Works", "foundry", true, new[] { 2, 3, 4 }, "Harbor Crane Row", "Fishers' Basin"),
            new DistrictSpec("Gemmayzeh Steps", "palace", true, new[] { 0, 1, 2 }, "Stairway Arcade", "Jasmine Courtyard"),
            new DistrictSpec("Hamra", "tower", true, new[] { 0, 1, 2 }, "Cinema Marquee", "Bookseller's Corner"),
            new DistrictSpec("Qadisha Road", "highland", false, new[] { 5, 6, 7 }, "Valley Lookout", "Cedar Stand")
        },
        new[]
        {
            new PersonSpec("elias", "Elias", "Hoist Operator", Guild, "Everything this city eats comes past my window."),
            new PersonSpec("rima", "Rima", "Fishers' Union Clerk", Civic, "Ask me anything about nets, tides or who owes who."),
            new PersonSpec("layal", "Layal", "Luthier", Signal, "Every instrument here was rescued from somewhere."),
            new PersonSpec("georges", "Georges", "Café Owner", Civic, "Sit. Nothing is so urgent it can't have coffee first."),
            new PersonSpec("nour", "Nour", "Projectionist", Signal, "I've got film nobody is supposed to have seen."),
            new PersonSpec("sitt_salwa", "Salwa", "Retired Voice Teacher", Civic, "Maya could shatter a glass at nine. I have the bill."),
            new PersonSpec("karim", "Karim", "Mule Driver", Couriers, "No road up there is a road for long."),
            new PersonSpec("hala", "Hala", "Cedar Warden", Guild, "These trees were old when this language was young.")
        },
        new[]
        {
            new QuestSpec(0, 0, "The Stuck Hoist", "Get the harbor's cargo hoist working before the ship leaves.", QuestShape.Repair,
                "Speak with Elias at the hoist.", new[] { "There's a ship with a month of clinic supplies on it, and I can't reach it.", "Reconnect the winch and I will have it ashore by dark." },
                "Reconnect the hoist winch.", "Field Note", new[] { "The cable takes the load, and the first sling comes up swaying." },
                "Destroy the drones on the cargo quay."),
            new QuestSpec(0, 1, "Who Gets the Berths", "Help the union decide how to share the repaired berths.", QuestShape.Vote,
                "Meet Rima at the union office.", new[] { "Two berths are repaired. The fishers want them, and so do the ferry crews.", "They've agreed on only one thing: that you should decide." },
                "Decide who gets the berths.", "Harbor Union", new[] { "The boats or the ferries. Say it plainly." },
                "Tell the union your decision.", new[] { "Written and signed. Nobody is pleased, so it's probably fair." },
                new[]
                {
                    new QuestBranchChoice("choice_beirut_fishers", "Give Berths to the Fishers", "Reserves the berths for the fishing boats.", "The fishing fleet can land its catch. The ferries keep queueing.", "service_beirut_fishers"),
                    new QuestBranchChoice("choice_beirut_ferries", "Give Berths to the Ferries", "Reserves the berths for passenger ferries.", "The ferries run on time. The fishers land less.", "service_beirut_ferries")
                }),
            new QuestSpec(1, 0, "Silent Strings", "Find out who has been breaking into the instrument workshops.", QuestShape.Investigate,
                "Speak with Layal at the workshop.", new[] { "They took nothing but my recordings of old songs.", "Look at the back door. There's something odd about the lock." },
                "Examine the workshop door.", "Field Note", new[] { "The lock has been opened by a machine, and it has left a tracking tag behind." },
                "Destroy the drones in the back lane."),
            new QuestSpec(1, 1, "Coffee for the Stairs", "Carry supplies up to the households on the steps.", QuestShape.Deliver,
                "Meet Georges at the café.", new[] { "The old people at the top of the steps can't come down any more.", "So the shopping goes up. Today, that means you." },
                "Carry the basket up the steps.", "Field Note", new[] { "A door opens at the top, and you are thanked at some length. Then the street goes quiet." },
                "Clear the drones at the top of the steps."),
            new QuestSpec(2, 0, "Forbidden Reel", "Keep a banned film out of the censors' hands.", QuestShape.Deliver,
                "Speak with Nour in the booth.", new[] { "It's the last print. They've offered a reward for it.", "Take it to the bookseller on the corner. He will know what to do." },
                "Deliver the reel to the bookseller.", "Field Note", new[] { "The can vanishes behind a shelf of dictionaries. A censor drone drifts past the window." },
                "Destroy the censor drones in the street."),
            new QuestSpec(2, 1, "A Voice Remembered", "Maya recovers her teacher's archive of recordings.", QuestShape.Investigate,
                "Visit Salwa.", new[] { "Stand up straight, Maya. Breathe from where I told you.", "My recordings are in the conservatory's cellar. They've been coming for voices now." },
                "Recover the recordings.", "Maya", new[] { "Thirty years of students, every one of them nine years old and terrified. I can hear myself." },
                "Stop the collection drones in the courtyard.", ForCityOperative: true),
            new QuestSpec(3, 0, "Rockfall", "Reopen the mountain road's signal line.", QuestShape.Repair,
                "Speak with Karim at the road head.", new[] { "The road's signal line went with the last rockfall. Nobody can warn anybody now.", "Splice it at the junction box up ahead." },
                "Splice the road signal line.", "Field Note", new[] { "A green light comes on, far up the valley." },
                "Destroy the drones at the road camp."),
            new QuestSpec(3, 1, "Marked Trees", "Find out who has been marking the oldest cedars.", QuestShape.Investigate,
                "Meet Hala among the cedars.", new[] { "Someone's been marking the oldest trees with paint I don't recognise.", "Look at the marks by the nursery fence, and tell me I'm imagining it." },
                "Examine the marked trees.", "Field Note", new[] { "The paint hides a survey tag. Each marked tree is listed for removal." },
                "Destroy the survey drones by the nursery.")
        },
        "Beit Yasmine (Hamra Safehouse)", 2,
        "The Broadcast Tower", "recovery", 0, "res://scenes/gameplay/beirut_broadcast_tower.tscn",
        new WorldPoint[] { new(16300, 0), new(15700, 12000), new(16300, 21000), new(15800, 32000) },
        new[] { new SeaEdge(SeaSide.West, 3000) }));
}
