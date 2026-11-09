/// <summary>
/// Luanda, 2157: King Tariq's home, opened when Apex Titan joins the squad. Place names are real; the buildings,
/// people and events are invented. Fights happen at works and yards, never at places of worship or memorials.
/// </summary>
public static class LuandaCity
{
    private const string Civic = "Civic Recovery Network", Couriers = "Independent Couriers", Guild = "Reclamation Guild", Signal = "Free Signal Collective";

    /// <summary>The city as data.</summary>
    public static readonly WorldDefinition Definition = CityBuilder.Build(new CitySpec(
        "luanda", "lua", "Luanda", "Apex Titan",
        new[]
        {
            new DistrictSpec("Ilha Causeway", "coast", true, new[] { 0, 1, 7 }, "Causeway Toll Gate", "Sandbar Palms"),
            new DistrictSpec("Viana Works", "foundry", true, new[] { 2, 3, 4 }, "Viana Rail Shed", "Workers' Garden"),
            new DistrictSpec("Baixa", "palace", true, new[] { 0, 1, 2 }, "Marginal Clock Arch", "Bay Promenade"),
            new DistrictSpec("Kissama Edge", "solar", false, new[] { 5, 6, 7 }, "Reserve Lookout", "Baobab Stand")
        },
        new[]
        {
            new PersonSpec("ngola", "Ngola", "Causeway Keeper", Civic, "One road on, one road off. I watch both."),
            new PersonSpec("lucinda", "Lucinda", "Fish Market Elder", Civic, "The sea feeds us, on the days it feels like it."),
            new PersonSpec("mateus", "Mateus", "Rail Shed Mechanic", Guild, "That engine is older than my grandmother and twice as stubborn."),
            new PersonSpec("ana", "Ana", "Shift Steward", Guild, "Nobody here works for free. Not any more."),
            new PersonSpec("domingas", "Domingas", "Radio Host", Signal, "Good evening, Luanda. Still here? Good."),
            new PersonSpec("paulo", "Paulo", "Tram Conductor", Couriers, "Mind the step. Mind the drones."),
            new PersonSpec("mestre_kandimba", "Kandimba", "Wrestling Master", Civic, "Tariq threw me once. He has told everybody."),
            new PersonSpec("zola", "Zola", "Reserve Warden", Guild, "The elephants have come back. Don't tell the machines.")
        },
        new[]
        {
            new QuestSpec(0, 0, "Toll Without a Keeper", "Find out who is operating the causeway gate remotely.", QuestShape.Investigate,
                "Speak with Ngola at the gate.", new[] { "The gate opened by itself last night, and closed again after a convoy.", "Check the control box. I have only got the one good knee." },
                "Inspect the gate control box.", "Field Note", new[] { "A second set of wires leads out of the box and along the sea wall." },
                "Destroy the drones along the sea wall."),
            new QuestSpec(0, 1, "Ice for the Boats", "Help the fish market decide how to use its repaired plant.", QuestShape.Vote,
                "Meet Lucinda at the market.", new[] { "The plant can make ice or clean water this season. It can't manage both.", "The market can't agree. You decide." },
                "Decide what the plant makes.", "Market Women's Council", new[] { "Ice or water. Speak up, we are busy." },
                "Tell the council your decision.", new[] { "Good. It's done, then. Now buy some fish." },
                new[]
                {
                    new QuestBranchChoice("choice_luanda_ice", "Make Ice for the Boats", "Runs the plant for the fishing fleet.", "The catch keeps and the boats earn. Clean water stays scarce.", "service_luanda_ice"),
                    new QuestBranchChoice("choice_luanda_water", "Make Clean Water", "Runs the plant for drinking water.", "The neighbourhood drinks safely. The fleet sells less.", "service_luanda_water")
                }),
            new QuestSpec(1, 0, "Stalled Engine", "Get the old freight engine running.", QuestShape.Repair,
                "Speak with Mateus at the shed.", new[] { "She has to haul the grain tomorrow, and her starter has gone.", "Fit this. Then deal with whatever has been scratching at my doors." },
                "Fit the engine starter.", "Field Note", new[] { "The engine coughs twice, then settles to a heavy, even beat." },
                "Destroy the drones in the rail yard."),
            new QuestSpec(1, 1, "Pay Packets", "Bring the workers' wages safely through the yards.", QuestShape.Deliver,
                "Meet Ana at the union hall.", new[] { "Three hundred pay packets, and they've robbed us twice this year.", "Get them to the pay window. I will walk the other way with an empty bag." },
                "Deliver the wages to the pay window.", "Field Note", new[] { "The queue at the window lets out a long breath. Behind the sheds, engines whine." },
                "Clear the drones behind the sheds."),
            new QuestSpec(2, 0, "On the Air", "Bring a banned radio station back on the air.", QuestShape.Repair,
                "Speak with Domingas in the studio.", new[] { "They cut my transmitter feed in the middle of a song. That is unforgivable.", "Mend the line on the roof, and I can finish it." },
                "Repair the transmitter line.", "Field Note", new[] { "Far below, every radio on the street takes up the same song again." },
                "Destroy the drones hunting the signal."),
            new QuestSpec(2, 1, "Last Tram", "Get a family out on the last tram.", QuestShape.Deliver,
                "Meet Paulo at the terminus.", new[] { "Their papers have been flagged. They need to reach the far stop tonight.", "Carry their bag ahead; it's the bag the scanners look for." },
                "Carry the bag to the far stop.", "Field Note", new[] { "The tram arrives with its passengers safe. The scanner drones arrive just after it." },
                "Destroy the scanner drones at the stop."),
            new QuestSpec(3, 0, "Trophy Hunters", "Stop salvagers trapping the reserve's returning herd.", QuestShape.Investigate,
                "Speak with Zola at the lookout.", new[] { "Someone's been putting sensor stakes along the elephant route.", "Pull one and tell me what it is reporting to." },
                "Examine a sensor stake.", "Field Note", new[] { "The stake is transmitting the herd's position to a camp to the east." },
                "Destroy the drones at the salvage camp."),
            new QuestSpec(3, 1, "The Heavy Hand", "Tariq faces the man who taught him to wrestle.", QuestShape.Investigate,
                "Visit Kandimba.", new[] { "So, the king comes back. Your balance is still terrible.", "The old club's trophies were moved to a shed by the road. Your name is on most of them. Go and see." },
                "Find the club's records.", "Tariq", new[] { "Every bout. Every loss, too. He made sure of that." },
                "Stop the salvage drones on the road.", ForCityOperative: true)
        },
        "Casa da Baía (Baixa Safehouse)", 2,
        "The Power Station", "sabotage", 1, "res://scenes/gameplay/luanda_power_station.tscn",
        new WorldPoint[] { new(15800, 0), new(16400, 13000), new(15700, 23000), new(16300, 32000) },
        new[] { new SeaEdge(SeaSide.West, 3000) }));
}
