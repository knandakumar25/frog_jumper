/// <summary>
/// Honolulu, 2157: Kiana's home, opened when Oceanus joins the squad. Place names are real; the buildings,
/// people and events are invented. Fights happen at works and yards, never at sacred sites.
/// </summary>
public static class HonoluluCity
{
    private const string Civic = "Civic Recovery Network", Couriers = "Independent Couriers", Guild = "Reclamation Guild", Signal = "Free Signal Collective";

    /// <summary>The city as data.</summary>
    public static readonly WorldDefinition Definition = CityBuilder.Build(new CitySpec(
        "honolulu", "hon", "Honolulu", "Oceanus",
        new[]
        {
            new DistrictSpec("Kalihi Works", "foundry", true, new[] { 2, 3, 4 }, "Kalihi Pump Hall", "Stream Cut Gardens"),
            new DistrictSpec("Manoa Valley", "forest", false, new[] { 5, 6, 7 }, "Valley Weather Mast", "Rain Fern Walk"),
            new DistrictSpec("Chinatown Harbor", "harbor", true, new[] { 0, 1, 2 }, "Harbor Light Tower", "Lei Sellers' Lane"),
            new DistrictSpec("Waikiki Strand", "coast", true, new[] { 0, 1, 7 }, "Strand Sea Gate", "Outrigger Beach")
        },
        new[]
        {
            new PersonSpec("keoni", "Keoni", "Pump Hall Engineer", Guild, "The valley sends the water down. We make sure it goes somewhere."),
            new PersonSpec("leilani", "Leilani", "Yard Mechanic", Couriers, "If it floats or rolls, I can probably get it moving."),
            new PersonSpec("makani", "Makani", "Weather Watcher", Signal, "The rain here keeps better records than the government did."),
            new PersonSpec("tutu_hale", "Tutu Hale", "Retired Navigator", Civic, "Kiana learned her stars on my porch."),
            new PersonSpec("akira", "Akira", "Harbor Pilot", Couriers, "Every ship in this harbor owes me a favour."),
            new PersonSpec("mei", "Mei", "Lane Shopkeeper", Civic, "My family has kept this counter for two hundred years."),
            new PersonSpec("kai", "Kai", "Beach Patrol Captain", Civic, "Nobody drowns on my watch. Nobody."),
            new PersonSpec("noelani", "Noelani", "Canoe Builder", Guild, "A good hull remembers the tree.")
        },
        new[]
        {
            new QuestSpec(0, 0, "High Water", "Keep the Kalihi pumps running through the rains.", QuestShape.Repair,
                "Speak with Keoni at the pump hall.", new[] { "We are one storm from having the stream in people's kitchens.", "The main feed has shorted. Fix it, and then see what is prowling the intake." },
                "Repair the main pump feed.", "Field Note", new[] { "The feed holds. Deep under the floor, the big pumps begin to turn." },
                "Destroy the drones at the intake."),
            new QuestSpec(0, 1, "Parts Run", "Get salvaged parts across the yards.", QuestShape.Deliver,
                "Meet Leilani in the yard.", new[] { "I have the bearings the ferry needs. The ferry crew has the fuel I need.", "Run these over to them, would you?" },
                "Deliver the bearings to the ferry crew.", "Field Note", new[] { "The crew unloads a drum of fuel in exchange and points down the road, frowning." },
                "Clear the drones on the yard road."),
            new QuestSpec(1, 0, "False Forecast", "Find out why the valley's rain gauges are lying.", QuestShape.Investigate,
                "Speak with Makani at the weather mast.", new[] { "My gauges say dry. My boots say otherwise.", "Somebody is rewriting the numbers before they reach the town. Check the line." },
                "Examine the rain gauge line.", "Field Note", new[] { "A clip on the line replaces every reading with a neat, false zero." },
                "Destroy the drones at the survey post."),
            new QuestSpec(1, 1, "The Star Book", "Kiana recovers her teacher's navigation notes.", QuestShape.Investigate,
                "Visit Tutu Hale.", new[] { "You came back thinner, Kiana. Sit down.", "My star book is in the old boathouse store. Fetch it before the collectors do." },
                "Find the star book.", "Kiana", new[] { "Every swell and every star, in her small square hand. I will carry this home." },
                "Stop the collection drones up the valley.", ForCityOperative: true),
            new QuestSpec(2, 0, "Dark Light", "Restore the harbor light before the fishing fleet returns.", QuestShape.Repair,
                "Speak with Akira at the pilot's office.", new[] { "The light went out at sundown and the fleet is due by midnight.", "The lamp feed is at the foot of the tower. Please be quick." },
                "Repair the harbor lamp feed.", "Field Note", new[] { "The light swings back across the water. Far out, a horn answers." },
                "Destroy the drones at the pier sheds."),
            new QuestSpec(2, 1, "The Last Shipment", "Help the lane decide who receives the last shipment of rice.", QuestShape.Vote,
                "Meet Mei at her counter.", new[] { "One shipment. The school kitchen wants it and so do the old people's homes.", "We've all known each other too long to decide this fairly." },
                "Decide who receives the shipment.", "Lane Association", new[] { "School or elders. We will abide by it." },
                "Tell the association your decision.", new[] { "It is agreed. The carts go at first light." },
                new[]
                {
                    new QuestBranchChoice("choice_honolulu_school", "Feed the School Kitchen", "Sends the rice to the school kitchen.", "The children eat this month. The elders' homes run short.", "service_honolulu_school"),
                    new QuestBranchChoice("choice_honolulu_elders", "Supply the Elders' Homes", "Sends the rice to the elders' homes.", "The elders eat this month. The school kitchen runs short.", "service_honolulu_elders")
                }),
            new QuestSpec(3, 0, "Warning Lines", "Bring the beach warning sirens back online.", QuestShape.Repair,
                "Speak with Kai at the patrol hut.", new[] { "A siren that doesn't sound is worse than none, because people trust it.", "The line is down by the sea gate." },
                "Repair the siren line.", "Field Note", new[] { "A short test note rolls down the strand and fades." },
                "Destroy the drones at the sea gate yard."),
            new QuestSpec(3, 1, "Hull Timber", "Bring timber safely to the canoe sheds.", QuestShape.Deliver,
                "Meet Noelani at the shed.", new[] { "This is the last of the koa I will see in my life.", "Take the marked planks to the far shed. Watch the corners." },
                "Deliver the planks to the far shed.", "Field Note", new[] { "The planks are stacked with care. Behind you, something hums over the sand." },
                "Drive off the drones at the far shed.")
        },
        "Kapa Hale (Chinatown Safehouse)", 2,
        "The Pumping Station", "defense", 0, "res://scenes/gameplay/honolulu_pumping_station.tscn",
        new WorldPoint[] { new(15600, 0), new(16400, 11000), new(15600, 22000), new(16200, 32000) },
        new[] { new SeaEdge(SeaSide.South, 3000) }));
}
