/// <summary>
/// Dubai, 2157: Zayd's home, opened when Mirage Blade joins the squad. Place names are real; the buildings,
/// people and events are invented, and no real institution appears. Fights happen at works and yards only.
/// </summary>
public static class DubaiCity
{
    private const string Civic = "Civic Recovery Network", Couriers = "Independent Couriers", Guild = "Reclamation Guild", Signal = "Free Signal Collective";

    /// <summary>The city as data.</summary>
    public static readonly WorldDefinition Definition = CityBuilder.Build(new CitySpec(
        "dubai", "dub", "Dubai", "Mirage Blade",
        new[]
        {
            new DistrictSpec("Jebel Ali Docks", "foundry", true, new[] { 2, 3, 4 }, "Dock Crane Row", "Mangrove Basin"),
            new DistrictSpec("Deira Souks", "palace", true, new[] { 0, 1, 2 }, "Wind Tower Arcade", "Spice Lane"),
            new DistrictSpec("Al Marmoom Dunes", "desert", false, new[] { 5, 6, 7 }, "Dune Survey Tower", "Oryx Springs"),
            new DistrictSpec("Creekside Towers", "tower", true, new[] { 0, 1, 2 }, "Creek Span", "Dhow Quay")
        },
        new[]
        {
            new PersonSpec("salma", "Salma", "Crane Operator", Guild, "From up there, everyone looks like they're late."),
            new PersonSpec("yousef", "Yousef", "Stevedore Foreman", Couriers, "We move it, whatever it is, whoever owns it."),
            new PersonSpec("hessa", "Hessa", "Textile Trader", Civic, "A city is a market that decided to stay."),
            new PersonSpec("omar", "Omar", "Spice Merchant", Couriers, "Smell that? That's four hundred kilometres of smuggling."),
            new PersonSpec("rashid", "Rashid", "Desert Guide", Guild, "The dune you crossed yesterday isn't there today."),
            new PersonSpec("uncle_nabil", "Nabil", "Old Falconer", Civic, "Zayd never could keep still long enough for the birds."),
            new PersonSpec("aisha", "Aisha", "Water Clerk", Signal, "Every litre in this city has a line in my ledger."),
            new PersonSpec("hamad", "Hamad", "Abra Skipper", Couriers, "One dirham to cross. It has been one dirham for a century.")
        },
        new[]
        {
            new QuestSpec(0, 0, "Dead Cranes", "Get the dock cranes moving again.", QuestShape.Repair,
                "Speak with Salma at crane row.", new[] { "All six froze at the same second. That isn't a fault, that's a command.", "Reset the control bus and they'll answer to us again." },
                "Reset the crane control bus.", "Field Note", new[] { "One by one the booms shudder and swing back under their operators' hands." },
                "Destroy the drones in the container yard."),
            new QuestSpec(0, 1, "Unlisted Cargo", "Find out what the occupation is shipping through the docks.", QuestShape.Investigate,
                "Meet Yousef on the quay.", new[] { "There is a container on my manifest that weighs nothing and has four guards.", "Have a look at it for me. Quietly." },
                "Inspect the unlisted container.", "Field Note", new[] { "Inside are racks of blank identity cards, thousands of them, ready to be issued." },
                "Destroy the guard drones on the quay."),
            new QuestSpec(1, 0, "Price of Water", "Help the souk decide how to spend its reserve.", QuestShape.Vote,
                "Speak with Hessa in the arcade.", new[] { "The traders' fund can buy one thing this season, and there are two things we need.", "You've seen how other places do it. Choose." },
                "Decide how the fund is spent.", "Traders' Council", new[] { "Water tanks or cooling for the arcades. Name it." },
                "Tell the council your decision.", new[] { "So be it. We will place the order tonight." },
                new[]
                {
                    new QuestBranchChoice("choice_dubai_tanks", "Buy Water Tanks", "Buys reserve tanks for the souk's households.", "The tanks are filled before summer. The arcades stay hot.", "service_dubai_tanks"),
                    new QuestBranchChoice("choice_dubai_cooling", "Cool the Arcades", "Repairs the cooling in the covered arcades.", "The arcades are bearable this summer. The tanks wait.", "service_dubai_cooling")
                }),
            new QuestSpec(1, 1, "Sealed Sacks", "Get a smuggled shipment of medicine past the checkpoint.", QuestShape.Deliver,
                "Meet Omar on Spice Lane.", new[] { "Nobody searches a sack of cardamom twice. The smell gets into their filters.", "Take these to the clinic door at the end of the lane." },
                "Deliver the sacks to the clinic.", "Field Note", new[] { "A hand takes the sacks through the door. Further up the lane, a checkpoint drone turns." },
                "Clear the checkpoint drones."),
            new QuestSpec(2, 0, "Moved Markers", "Find out who moved the desert route markers.", QuestShape.Investigate,
                "Speak with Rashid at the camp.", new[] { "Two caravans lost this month. Somebody has been moving the markers at night.", "Go and look at the last one that I trust." },
                "Examine the route marker.", "Field Note", new[] { "Its base is freshly dug, and a broadcast unit has been buried beneath it." },
                "Destroy the drones at the survey tower."),
            new QuestSpec(2, 1, "The Falcon's Ledger", "Zayd recovers the record of his family's lost land.", QuestShape.Investigate,
                "Visit Nabil at the mews.", new[] { "Stand still, Zayd. You frighten the birds.", "Your father's ledger is in the buried store by the springs. Bring it before the wind or the machines do." },
                "Recover the ledger.", "Zayd", new[] { "Every well and every boundary. They said none of this was ever written down." },
                "Stop the salvage drones in the dunes.", ForCityOperative: true),
            new QuestSpec(3, 0, "Short Measures", "Find who has been skimming the district's water.", QuestShape.Repair,
                "Speak with Aisha at the water office.", new[] { "The towers are drawing a tenth more than the meters report.", "Fix the bypass valve in the basement and the numbers will be honest again." },
                "Close the bypass valve.", "Field Note", new[] { "The needle on the main settles. Somewhere above you, an alarm starts." },
                "Destroy the drones in the service yard."),
            new QuestSpec(3, 1, "Across the Creek", "See a family's documents safely across the water.", QuestShape.Deliver,
                "Meet Hamad at the quay.", new[] { "This family needs its papers on the other side before the office closes.", "I can take the boat. You take the papers over the span." },
                "Carry the papers to the far quay.", "Field Note", new[] { "The papers reach the far side with an hour to spare, and so does something else." },
                "Drive off the drones on the far quay.")
        },
        "The Barjeel House (Creekside Safehouse)", 3,
        "The Salt Plant", "infiltration", 0, "res://scenes/gameplay/dubai_salt_plant.tscn",
        new WorldPoint[] { new(16300, 0), new(15700, 10000), new(16400, 21000), new(15700, 32000) },
        new[] { new SeaEdge(SeaSide.North, 3000) }));
}
