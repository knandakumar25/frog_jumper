/// <summary>
/// Mumbai, 2157: Arjun's home, opened when Surya Prime joins the squad. Place names are real; the buildings,
/// people and events are invented. Fights happen at works and yards, never at places of worship.
/// </summary>
public static class MumbaiCity
{
    private const string Civic = "Civic Recovery Network", Couriers = "Independent Couriers", Guild = "Reclamation Guild", Signal = "Free Signal Collective";

    /// <summary>The city as data.</summary>
    public static readonly WorldDefinition Definition = CityBuilder.Build(new CitySpec(
        "mumbai", "mum", "Mumbai", "Surya Prime",
        new[]
        {
            new DistrictSpec("Bandra Seaface", "coast", true, new[] { 0, 1, 2 }, "Seaface Sea Wall", "Promenade Palms"),
            new DistrictSpec("Sanjay Gandhi Reserve", "forest", false, new[] { 5, 6, 7 }, "Ranger Watch Tower", "Banyan Hollow"),
            new DistrictSpec("Fort Arcades", "palace", true, new[] { 0, 1, 2 }, "Arcade Clock Tower", "Maidan Green"),
            new DistrictSpec("Dharavi Works", "foundry", true, new[] { 2, 3, 4 }, "Recycling Stacks", "Cooperative Yard")
        },
        new[]
        {
            new PersonSpec("nisha", "Nisha", "Sea Wall Engineer", Guild, "The tide doesn't care who is in charge of the city."),
            new PersonSpec("farhan", "Farhan", "Night Market Organiser", Civic, "If the stalls are open, the street is still ours."),
            new PersonSpec("meera", "Meera", "Reserve Ranger", Guild, "Walk softly. The leopards here have better hearing than the drones."),
            new PersonSpec("guru_das", "Das", "Solar Instructor", Couriers, "Arjun learned to hold a line on this very ridge."),
            new PersonSpec("pooja", "Pooja", "Train Dispatcher", Couriers, "Seven million people ride on my timetable."),
            new PersonSpec("ravi", "Ravi", "Records Clerk", Signal, "They erase a name by losing the file. I don't lose files."),
            new PersonSpec("sunita", "Sunita", "Cooperative Foreman", Guild, "Everything the city throws away comes back through here."),
            new PersonSpec("imran", "Imran", "Workshop Apprentice", Couriers, "Give me an hour and a bin of scrap.")
        },
        new[]
        {
            new QuestSpec(0, 0, "Salt in the Wiring", "Stop the sea wall's pumps from failing before the next high tide.", QuestShape.Repair,
                "Speak with Nisha on the sea wall.", new[] { "Pump three tripped last night, and the tide is due. Something has been eating the insulation.", "Fix the junction. Then look at the yard behind it; I keep seeing lights there." },
                "Repair the pump junction.", "Field Note", new[] { "The junction reseats. Along the wall, the pumps pick up one after another." },
                "Destroy the drones at the pump yard."),
            new QuestSpec(0, 1, "Who Runs the Night Market", "Help the market decide what to do with its one generator.", QuestShape.Vote,
                "Meet Farhan at the night market.", new[] { "One generator, and two good reasons to want it. Everyone has shouted themselves hoarse.", "You aren't from this lane. They'll listen to you." },
                "Decide where the generator goes.", "Market Committee", new[] { "The cold store or the street lights. Say which, and we will carry it there tonight." },
                "Tell the committee your decision.", new[] { "It is done. The men are already lifting it." },
                new[]
                {
                    new QuestBranchChoice("choice_mumbai_cold", "Run the Cold Store", "Keeps the market's food and medicine cold.", "The cold store gets the generator. The lanes stay dark.", "service_mumbai_cold"),
                    new QuestBranchChoice("choice_mumbai_lights", "Light the Lanes", "Powers the street lights through the market.", "The lanes get the generator. The cold store shuts at night.", "service_mumbai_lights")
                }),
            new QuestSpec(1, 0, "Quiet Trails", "Find out why the reserve's camera traps have stopped reporting.", QuestShape.Investigate,
                "Speak with Meera at the ranger post.", new[] { "Every camera on the north trail went dark within the hour.", "Check the relay box. Leave the wildlife alone and it will return the favour." },
                "Examine the cut relay box.", "Field Note", new[] { "The cable has been cut cleanly and spliced into something else, leading uphill." },
                "Destroy the drones at the survey camp."),
            new QuestSpec(1, 1, "Hold the Line", "Arjun returns to the ridge where he trained.", QuestShape.Investigate,
                "Visit Das on the ridge.", new[] { "You still plant your left foot late, Arjun.", "Your old array logs are in the instructor's hut. Someone has been asking after them." },
                "Recover the array logs.", "Arjun", new[] { "Every morning for six years, written down. He kept all of it." },
                "Stop the salvage drones at the array.", ForCityOperative: true),
            new QuestSpec(2, 0, "Late Trains", "Find out who has been changing the suburban timetable.", QuestShape.Investigate,
                "Speak with Pooja at the control room.", new[] { "Three trains were held for no reason this morning. Nobody here held them.", "The signal cabinet by the arcade has been opened. Take a look." },
                "Inspect the signal cabinet.", "Field Note", new[] { "A small unit clipped inside is feeding the line false stops. Its receiver points at the depot." },
                "Destroy the drones at the rail depot."),
            new QuestSpec(2, 1, "The Lost Files", "Get a box of residents' records to safety.", QuestShape.Deliver,
                "Meet Ravi at the records office.", new[] { "These are birth records for a whole ward. There is one copy.", "Take them across the green to the courier. Don't stop." },
                "Hand the records to the courier.", "Field Note", new[] { "The courier takes the box without a word and is gone down the arcade." },
                "Clear the drones that followed you."),
            new QuestSpec(3, 0, "Dead Line", "Restart a stalled recycling line.", QuestShape.Repair,
                "Speak with Sunita at the cooperative gate.", new[] { "No line, no pay, and it is the end of the month.", "The drive has jammed at the sorting shed. Clear it and we are running." },
                "Clear the jammed sorting drive.", "Field Note", new[] { "The belt lurches, catches, and starts to run." },
                "Destroy the drones at the scrap yard."),
            new QuestSpec(3, 1, "Spare Parts", "Carry rebuilt parts to a clinic that needs them.", QuestShape.Deliver,
                "Meet Imran at the workshop.", new[] { "I rebuilt the clinic's oxygen valve out of three broken ones.", "It needs to get there today, and my bicycle has been stolen." },
                "Deliver the valve to the clinic.", "Field Note", new[] { "The nurse tests it twice, nods, and goes straight back inside." },
                "Drive off the drones at the back lane.")
        },
        "The Bombay Hearth (Fort Safehouse)", 2,
        "The Viaduct Grid", "sabotage", 3, "res://scenes/gameplay/mumbai_viaduct_grid.tscn",
        new WorldPoint[] { new(16400, 0), new(15600, 12000), new(16400, 22000), new(15800, 32000) },
        new[] { new SeaEdge(SeaSide.West, 3000) }));
}
