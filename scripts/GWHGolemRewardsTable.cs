using System.Text;

internal class Program
{
    private static void Main(string[] args)
    {
        var loot = ReadCSV(LootPreview);

        var locationsByRegion = RegionGroups.Split(["\r\n\r\n", "\n\n"], StringSplitOptions.None).Select(group =>
        {
            var lines = group.Split(["\n", "\r\n"], StringSplitOptions.None);
            var regionName = lines[0].Trim();
            var locations = lines[1..].Select(l => l.Trim()).ToList();
            return new KeyValuePair<string, List<string>>(regionName, locations);
        }).ToList();

        var sb = new StringBuilder();
        sb.AppendLine("""
            ==== Golem Rewards ====
            ''Note: The table below does '''not''' include bonus rewards associated with various golem levels as well as the [[Lucky Golden Shield]].''

            <!-- Generated with https://github.com/hymccord/mhwiki-tools/blob/main/scripts/GWHGolemRewardsTable.cs -->
            {| style="margin: 1em; width: 95%; max-width: 95em; text-align: left" class="b_and_p"
            |- class="sticky_table_row" style="background-color: #dddddd"
             ! style="width: 10%" <!--Header-->  | Region
             ! style="width: 15%" <!--Header-->  | Location
             ! style="width: 25%" <!--Header-->  | Area Loot
             ! style="width: 25%" <!--Header-->  | Magical Hat Rewards
             ! style="width: 25%" <!--Header-->  | Magical Scarf Rewards
            """);
        for (int i = 0; i < locationsByRegion.Count; i++)
        {
            var isEven = i % 2 == 0;
            (string region, List<string> locations) = locationsByRegion[i];
            for (int j = 0; j < locations.Count; j++)
            {
                string? location = locations[j];
                var areaLoot = loot.Where(l => l.Location == location && l.LootType == "Area Loot").OrderBy(l => l.Loot).ToList();
                var hatLoot = loot.Where(l => l.Location == location && l.LootType == "Magical Hat").OrderBy(l => l.Loot).ToList();
                var scarfLoot = loot.Where(l => l.Location == location && l.LootType == "Magical Scarf").OrderBy(l => l.Loot).ToList();

                sb.AppendLine(GenerateLocation(j == 0 ? region : null, location, locations.Count, isEven, areaLoot, hatLoot, scarfLoot).Replace("[[SUPER|brie+]]", "{{SB}}"));
            }
        }

        sb.AppendLine("|}");
        var result = sb.ToString();
        Console.WriteLine(result);
    }

    static List<GolemLoot> ReadCSV(string csv)
    {
        var golemLoots = new List<GolemLoot>();
        using (var reader = new StringReader(csv))
        {
            string? line;
            reader.ReadLine(); // Skip header
            while ((line = reader.ReadLine()) != null)
            {
                var values = SplitCsvLine(line);
                if (values.Length < 5) continue;

                var location = values[0].Trim();
                var lootType = values[1].Trim();
                var loot = values[2].Trim();
                var rarity = values[3].Trim();
                var quantity = values[4].Trim();

                golemLoots.Add(new GolemLoot(location, lootType, loot, rarity, quantity));

                // This will read a csv line and account for commas inside quotes
                static string[] SplitCsvLine(string line)
                {
                    var result = new List<string>();
                    var current = new StringBuilder();
                    bool inQuotes = false;

                    for (int i = 0; i < line.Length; i++)
                    {
                        char c = line[i];

                        if (c == '"' && (i == 0 || line[i - 1] != '\\'))
                        {
                            inQuotes = !inQuotes;
                            continue;
                        }

                        if (c == ',' && !inQuotes)
                        {
                            result.Add(current.ToString());
                            current.Clear();
                            continue;
                        }

                        current.Append(c);
                    }

                    result.Add(current.ToString());
                    return result.ToArray();
                }
            }
        }
        return golemLoots;
    }

    private static string GenerateLocation(string? region, string location, int rowSpan, bool isEven,
        List<GolemLoot> area, List<GolemLoot> hat, List<GolemLoot> scarf)
    {
        var sb = new StringBuilder();
        sb.AppendLine(isEven ? "|- style=\"background-color: whitesmoke\"" : "|-");

        if (region is not null)
        {
            sb.AppendLine($"""
              | rowspan="{rowSpan}" <!--Region-->                | '''{region}'''
             """);
        }

        sb.Append($$$"""
             |             <!--Location-->              | {{{location}}}
             |             <!--Area Loot-->             | {{{JoinWithBr(area)}}}
             |             <!--Magical Hat Rewards-->   | {{{JoinWithBr(hat)}}}
             |             <!--Magical Scarf Rewards--> | {{{JoinWithBr(scarf)}}}
            """);
        return sb.ToString();

        static string JoinWithBr(List<GolemLoot> golemLoots) => string.Join("<br>", golemLoots.Select(l => $"{l.Quantity} [[{l.Loot}]]"));
    }

    #region Golem Data
    // Thanks to Brad: https://docs.google.com/spreadsheets/d/1K7q4bzOn_f-bmzHhCHETrE6sMaepSQZSWLP3_aYNWSY/edit?gid=1073293788#gid=1073293788
    const string LootPreview = """
Environment,Category,Item Name,Rarity,Quantity
Acolyte Realm,Area Loot,Rune,Common,5
Acolyte Realm,Area Loot,Ancient Potion,Uncommon,2
Acolyte Realm,Area Loot,Stale Cheese,Uncommon,10
Acolyte Realm,Area Loot,Runic Potion,Rare,1
Acolyte Realm,Magical Hat,Rune,Common,25
Acolyte Realm,Magical Hat,Runic Potion,Uncommon,10
Acolyte Realm,Magical Scarf,Rune,Guaranteed,100
Balack's Cove,Area Loot,Pinch of Annoyance,Uncommon,5
Balack's Cove,Area Loot,Raisins of Wrath,Uncommon,5
Balack's Cove,Area Loot,Bottled-Up Rage,Uncommon,5
Balack's Cove,Area Loot,Vengeful Vanilla Stilton Cheese,Uncommon,5
Balack's Cove,Area Loot,Vanilla Bean,Rare,10
Balack's Cove,Area Loot,Vanilla Stilton Cheese,Rare,10
Balack's Cove,Area Loot,Empowered Anchor Charm,Rare,10
Balack's Cove,Area Loot,Scrap Metal,Very rare,10
Balack's Cove,Magical Hat,Vengeful Vanilla Stilton Cheese,Guaranteed,10
Balack's Cove,Magical Scarf,Vengeful Vanilla Stilton Cheese,Guaranteed,100
Bazaar,Area Loot,Gilded Cheese,Common,1
Bazaar,Area Loot,Gold,Uncommon,"50,000"
Bazaar,Magical Hat,Gilded Cheese,Guaranteed,10
Bazaar,Magical Scarf,Gilded Cheese,Guaranteed,100
Bountiful Beanstalk,Area Loot,Royal Ruby Bean,Uncommon,24
Bountiful Beanstalk,Area Loot,Golden Harp String,Uncommon,24
Bountiful Beanstalk,Area Loot,Lavish Lapis Bean,Uncommon,24
Bountiful Beanstalk,Area Loot,Fabled Fertilizer,Uncommon,1
Bountiful Beanstalk,Area Loot,Condensed Creativity,Very rare,1
Bountiful Beanstalk,Area Loot,Magic Bean,Very rare,24
Bountiful Beanstalk,Magical Hat,Fabled Fertilizer,Uncommon,10
Bountiful Beanstalk,Magical Hat,Golden Harp String,Uncommon,256
Bountiful Beanstalk,Magical Hat,Royal Ruby Bean,Uncommon,256
Bountiful Beanstalk,Magical Hat,Condensed Creativity,Rare,10
Bountiful Beanstalk,Magical Hat,Lavish Lapis Bean,Rare,256
Bountiful Beanstalk,Magical Hat,Magic Bean,Very rare,256
Bountiful Beanstalk,Magical Hat,Giant's Golden Key,Very rare,1
Bountiful Beanstalk,Magical Hat,Golden Goose Feather,Very rare,1
Bountiful Beanstalk,Magical Hat,Golden Goose,Extremely rare,1
Bountiful Beanstalk,Magical Scarf,Fabled Fertilizer,Common,25
Bountiful Beanstalk,Magical Scarf,Golden Harp String,Uncommon,"2,048"
Bountiful Beanstalk,Magical Scarf,Royal Ruby Bean,Uncommon,"2,048"
Bountiful Beanstalk,Magical Scarf,Magic Bean,Very rare,"2,048"
Bountiful Beanstalk,Magical Scarf,Lavish Lapis Bean,Very rare,"2,048"
Bountiful Beanstalk,Magical Scarf,Giant's Golden Key,Very rare,1
Bountiful Beanstalk,Magical Scarf,Golden Goose Feather,Extremely rare,1
Bountiful Beanstalk,Magical Scarf,Golden Goose,Extremely rare,1
Bristle Woods Rift,Area Loot,Tiny Sprocket,Uncommon,10
Bristle Woods Rift,Area Loot,Rift Ultimate Luck Charm,Uncommon,1
Bristle Woods Rift,Area Loot,Rift Ultimate Power Charm,Uncommon,1
Bristle Woods Rift,Area Loot,Rift Ultimate Lucky Power Charm,Uncommon,1
Bristle Woods Rift,Area Loot,Quantum Quartz,Very rare,3
Bristle Woods Rift,Area Loot,Timesplit Charm,Very rare,1
Bristle Woods Rift,Area Loot,Clockwork Cog,Very rare,1
Bristle Woods Rift,Area Loot,Rift Antiskele Charm,Very rare,10
Bristle Woods Rift,Area Loot,Portal Scrambler,Very rare,1
Bristle Woods Rift,Magical Hat,Timesplit Charm,Uncommon,2
Bristle Woods Rift,Magical Hat,Rift Ultimate Luck Charm,Uncommon,15
Bristle Woods Rift,Magical Hat,Rift Ultimate Power Charm,Uncommon,15
Bristle Woods Rift,Magical Hat,Quantum Quartz,Uncommon,15
Bristle Woods Rift,Magical Hat,Rift Ultimate Lucky Power Charm,Uncommon,10
Bristle Woods Rift,Magical Hat,Clockwork Cog,Rare,2
Bristle Woods Rift,Magical Hat,Timesplit Rune,Very rare,1
Bristle Woods Rift,Magical Scarf,Timesplit Rune,Guaranteed,1
Burroughs Rift,Area Loot,Terre Ricotta Potion,Uncommon,1
Burroughs Rift,Area Loot,Mist Canister,Uncommon,15
Burroughs Rift,Area Loot,Polluted Parmesan Potion,Uncommon,1
Burroughs Rift,Area Loot,Rift Vacuum Charm,Uncommon,20
Burroughs Rift,Area Loot,Calcified Rift Mist,Uncommon,5
Burroughs Rift,Magical Hat,Magical String Cheese,Common,15
Burroughs Rift,Magical Hat,Polluted Parmesan Potion,Common,10
Burroughs Rift,Magical Scarf,Polluted Parmesan Potion,Guaranteed,50
Calm Clearing,Area Loot,Cherry Potion,Guaranteed,1
Calm Clearing,Magical Hat,Cherry Potion,Guaranteed,2
Calm Clearing,Magical Scarf,Cherry Potion,Guaranteed,50
Cantera Quarry,Area Loot,Ultimate Power Charm,Uncommon,2
Cantera Quarry,Area Loot,Magic Essence,Uncommon,3
Cantera Quarry,Area Loot,Wild Tonic,Uncommon,2
Cantera Quarry,Area Loot,Ember Stone,Rare,1
Cantera Quarry,Magical Hat,Ember Stone,Common,10
Cantera Quarry,Magical Hat,Wild Tonic,Uncommon,10
Cantera Quarry,Magical Hat,Ultimate Power Charm,Uncommon,15
Cantera Quarry,Magical Scarf,Ember Stone,Guaranteed,30
Cape Clawed,Area Loot,Seashell,Uncommon,15
Cape Clawed,Area Loot,Savoury Vegetables,Uncommon,15
Cape Clawed,Area Loot,Delicious Stone,Uncommon,15
Cape Clawed,Magical Hat,Seashell,Uncommon,60
Cape Clawed,Magical Hat,Savoury Vegetables,Uncommon,60
Cape Clawed,Magical Hat,Delicious Stone,Uncommon,60
Cape Clawed,Magical Scarf,Seashell,Uncommon,400
Cape Clawed,Magical Scarf,Savoury Vegetables,Uncommon,400
Cape Clawed,Magical Scarf,Delicious Stone,Uncommon,400
Catacombs,Area Loot,Crescent Cheese,Uncommon,10
Catacombs,Area Loot,Stale Cheese,Uncommon,10
Catacombs,Area Loot,Radioactive Blue Potion,Uncommon,5
Catacombs,Area Loot,Ancient Potion,Uncommon,5
Catacombs,Area Loot,Undead Emmental Potion,Uncommon,1
Catacombs,Area Loot,Antiskele Charm,Rare,5
Catacombs,Area Loot,Moon Cheese,Rare,5
Catacombs,Magical Hat,Moon Cheese,Common,10
Catacombs,Magical Hat,Undead Emmental Potion,Uncommon,10
Catacombs,Magical Scarf,Undead Emmental Potion,Guaranteed,35
Claw Shot City,Area Loot,Prospector's Charm,Uncommon,10
Claw Shot City,Area Loot,Fool's Gold,Uncommon,5
Claw Shot City,Area Loot,Cactus Charm,Uncommon,10
Claw Shot City,Area Loot,Super Cactus Charm,Uncommon,10
Claw Shot City,Area Loot,Sheriff's Badge Charm,Rare,1
Claw Shot City,Area Loot,Rare Map Dust,Extremely rare,1
Claw Shot City,Magical Hat,Fool's Gold,Common,30
Claw Shot City,Magical Hat,Sheriff's Badge Charm,Uncommon,1
Claw Shot City,Magical Hat,Rare Map Dust,Uncommon,1
Claw Shot City,Magical Scarf,Rare Map Dust,Guaranteed,1
Crystal Library,Area Loot,Library Points,Common,5
Crystal Library,Area Loot,Flawed Orb,Uncommon,5
Crystal Library,Area Loot,Simple Orb,Rare,5
Crystal Library,Area Loot,Wealth Charm,Rare,10
Crystal Library,Area Loot,Flawless Orb,Very rare,3
Crystal Library,Area Loot,Divine Orb,Very rare,2
Crystal Library,Magical Hat,Library Points,Uncommon,50
Crystal Library,Magical Hat,Fusion Fondue,Uncommon,2
Crystal Library,Magical Hat,Red Tome of Wisdom,Uncommon,1
Crystal Library,Magical Hat,Silver Tome of Wisdom,Rare,1
Crystal Library,Magical Scarf,Fusion Fondue,Guaranteed,50
Derr Dunes,Area Loot,Delicious Stone,Common,10
Derr Dunes,Area Loot,Red Pepper Seed,Common,10
Derr Dunes,Area Loot,Derr Power Charm,Very rare,10
Derr Dunes,Magical Hat,Delicious Stone,Guaranteed,30
Derr Dunes,Magical Scarf,Delicious Stone,Guaranteed,300
Dojo,Area Loot,Token of the Cheese Belt,Uncommon,3
Dojo,Area Loot,Token of the Cheese Claw,Uncommon,3
Dojo,Area Loot,Token of the Cheese Fang,Uncommon,3
Dojo,Area Loot,Master Belt Shard,Very rare,3
Dojo,Area Loot,Master Claw Shard,Very rare,3
Dojo,Area Loot,Master Fang Shard,Very rare,3
Dojo,Magical Hat,Token of the Cheese Belt,Uncommon,15
Dojo,Magical Hat,Token of the Cheese Claw,Uncommon,15
Dojo,Magical Hat,Token of the Cheese Fang,Uncommon,15
Dojo,Magical Scarf,Token of the Cheese Belt,Uncommon,120
Dojo,Magical Scarf,Token of the Cheese Claw,Uncommon,120
Dojo,Magical Scarf,Token of the Cheese Fang,Uncommon,120
Dracano,Area Loot,Dragonbane Charm,Common,1
Dracano,Area Loot,Fire Salt,Uncommon,3
Dracano,Area Loot,Inferno Pepper,Uncommon,3
Dracano,Magical Hat,Dragonbane Charm,Guaranteed,5
Dracano,Magical Scarf,Super Dragonbane Charm,Guaranteed,50
Draconic Depths,Area Loot,Dragonhide Sliver,Uncommon,100
Draconic Depths,Area Loot,Dragon Ember,Uncommon,30
Draconic Depths,Area Loot,Fire Grub,Uncommon,25
Draconic Depths,Area Loot,Poison Grub,Uncommon,25
Draconic Depths,Area Loot,Ice Grub,Uncommon,25
Draconic Depths,Area Loot,Magic Essence,Rare,2
Draconic Depths,Area Loot,Super Dragonbane Charm,Rare,3
Draconic Depths,Area Loot,Dragonbane Charm,Rare,3
Draconic Depths,Area Loot,Extreme Dragonbane Charm,Very rare,3
Draconic Depths,Area Loot,Condensed Creativity,Very rare,1
Draconic Depths,Area Loot,Dragon Ember Potion,Extremely rare,1
Draconic Depths,Area Loot,Ful'mina's Charged Toothlet,Extremely rare,1
Draconic Depths,Area Loot,Spore Charm,Extremely rare,5
Draconic Depths,Area Loot,Super Spore Charm,Extremely rare,5
Draconic Depths,Area Loot,Extreme Spore Charm,Extremely rare,5
Draconic Depths,Area Loot,Ultimate Spore Charm,Extremely rare,5
Draconic Depths,Area Loot,Rainbow Spore Charm,Extremely rare,5
Draconic Depths,Magical Hat,Elemental Emmental Cheese,Uncommon,1
Draconic Depths,Magical Hat,Fire Grub,Uncommon,250
Draconic Depths,Magical Hat,Ice Grub,Uncommon,250
Draconic Depths,Magical Hat,Poison Grub,Uncommon,250
Draconic Depths,Magical Hat,Magic Essence,Uncommon,25
Draconic Depths,Magical Hat,Dragon Ember,Uncommon,500
Draconic Depths,Magical Hat,Dragonhide Sliver,Rare,"1,000"
Draconic Depths,Magical Hat,Condensed Creativity,Rare,10
Draconic Depths,Magical Hat,Dragon Ember Potion,Extremely rare,3
Draconic Depths,Magical Hat,Mythical Dragon Heart,Extremely rare,1
Draconic Depths,Magical Hat,Dragon's Skull,Extremely rare,1
Draconic Depths,Magical Scarf,Elemental Emmental Cheese,Uncommon,5
Draconic Depths,Magical Scarf,Dragon Ember,Uncommon,"1,000"
Draconic Depths,Magical Scarf,Fire Grub,Uncommon,500
Draconic Depths,Magical Scarf,Ice Grub,Uncommon,500
Draconic Depths,Magical Scarf,Poison Grub,Uncommon,500
Draconic Depths,Magical Scarf,Magic Essence,Rare,50
Draconic Depths,Magical Scarf,Dragonhide Sliver,Rare,"2,500"
Draconic Depths,Magical Scarf,Condensed Creativity,Very rare,15
Draconic Depths,Magical Scarf,Dragon Ember Potion,Very rare,5
Draconic Depths,Magical Scarf,Mythical Dragon Heart,Extremely rare,1
Draconic Depths,Magical Scarf,Dragon's Skull,Extremely rare,1
Elub Shore,Area Loot,Seashell,Common,10
Elub Shore,Area Loot,Blue Pepper Seed,Common,10
Elub Shore,Area Loot,Elub Power Charm,Very rare,10
Elub Shore,Magical Hat,Seashell,Guaranteed,30
Elub Shore,Magical Scarf,Seashell,Guaranteed,300
Fiery Warpath,Area Loot,Desert Horseshoe,Uncommon,1
Fiery Warpath,Area Loot,Heatproof Mage Cloth,Uncommon,1
Fiery Warpath,Area Loot,Flameshard,Uncommon,15
Fiery Warpath,Area Loot,Super Warpath Warrior Charm,Uncommon,1
Fiery Warpath,Area Loot,Super Warpath Scout Charm,Uncommon,1
Fiery Warpath,Area Loot,Super Warpath Archer Charm,Uncommon,1
Fiery Warpath,Area Loot,Warpath Commander's Charm,Rare,1
Fiery Warpath,Magical Hat,Flameshard,Common,50
Fiery Warpath,Magical Hat,Warpath Portal Core,Rare,1
Fiery Warpath,Magical Hat,Warpath Portal Console,Rare,1
Fiery Warpath,Magical Scarf,Warpath Portal Core,Uncommon,1
Fiery Warpath,Magical Scarf,Warpath Portal Console,Uncommon,1
Fiery Warpath,Magical Scarf,Artillery Strike Launch Box,Uncommon,1
Fiery Warpath,Magical Scarf,Sandblasted Metal,Very rare,1
Floating Islands,Area Loot,Sky Ore,Rare,25
Floating Islands,Area Loot,Sky Glass,Rare,25
Floating Islands,Area Loot,Cloud Curd,Rare,50
Floating Islands,Area Loot,Empyrean Seal,Rare,100
Floating Islands,Area Loot,Low Altitude Treasure Trove,Rare,1
Floating Islands,Area Loot,Storm Cell,Very rare,1
Floating Islands,Area Loot,Ultimate Lucky Power Charm,Very rare,2
Floating Islands,Area Loot,Ultimate Luck Charm,Very rare,3
Floating Islands,Area Loot,Ultimate Power Charm,Very rare,3
Floating Islands,Area Loot,Magic Essence,Very rare,2
Floating Islands,Area Loot,Rainbow Charm,Very rare,2
Floating Islands,Area Loot,Bottled Wind,Very rare,2
Floating Islands,Area Loot,Cyclone Stone,Very rare,1
Floating Islands,Area Loot,Corsair's Curd,Very rare,15
Floating Islands,Area Loot,High Altitude Treasure Trove,Very rare,1
Floating Islands,Area Loot,Ember Charm,Very rare,1
Floating Islands,Area Loot,Ultimate Ancient Charm,Very rare,5
Floating Islands,Area Loot,Adorned Empyrean Jewel,Extremely rare,1
Floating Islands,Magical Hat,Bottled Wind,Uncommon,25
Floating Islands,Magical Hat,Storm Cell,Uncommon,10
Floating Islands,Magical Hat,Corsair's Curd,Rare,50
Floating Islands,Magical Hat,Adorned Empyrean Jewel,Rare,1
Floating Islands,Magical Hat,Compressed Jet Stream,Rare,1
Floating Islands,Magical Hat,Empyrean Seal,Very rare,"1,000"
Floating Islands,Magical Hat,Ember Charm,Very rare,1
Floating Islands,Magical Hat,Rainbow Charm,Very rare,50
Floating Islands,Magical Hat,High Altitude Treasure Trove,Very rare,2
Floating Islands,Magical Hat,Ultimate Power Charm,Very rare,25
Floating Islands,Magical Hat,Ultimate Luck Charm,Very rare,25
Floating Islands,Magical Hat,Ultimate Lucky Power Charm,Very rare,15
Floating Islands,Magical Scarf,Empyrean Treasure Trove,Very likely,1
Floating Islands,Magical Scarf,Compressed Jet Stream,Uncommon,1
Forbidden Grove,Area Loot,Ancient Potion,Common,5
Forbidden Grove,Area Loot,Rune,Uncommon,3
Forbidden Grove,Area Loot,Stale Cheese,Uncommon,10
Forbidden Grove,Area Loot,Runic Potion,Rare,1
Forbidden Grove,Area Loot,Realm Ripper Charm,Extremely rare,1
Forbidden Grove,Magical Hat,Ancient Potion,Very likely,20
Forbidden Grove,Magical Hat,Realm Ripper Charm,Rare,3
Forbidden Grove,Magical Scarf,Ancient Potion,Guaranteed,100
Foreword Farm,Area Loot,Papyrus Seed,Uncommon,15
Foreword Farm,Area Loot,Mythical Mulch,Uncommon,5
Foreword Farm,Area Loot,Parable Papyrus,Uncommon,65
Foreword Farm,Area Loot,Crop Coin,Rare,10
Foreword Farm,Area Loot,Spore Charm,Rare,4
Foreword Farm,Area Loot,Super Spore Charm,Rare,4
Foreword Farm,Area Loot,Magic Essence,Very rare,2
Foreword Farm,Area Loot,Extreme Spore Charm,Very rare,4
Foreword Farm,Area Loot,Ultimate Spore Charm,Very rare,4
Foreword Farm,Magical Hat,Parable Papyrus,Common,"1,000"
Foreword Farm,Magical Hat,Condensed Creativity,Uncommon,10
Foreword Farm,Magical Hat,Ultimate Spore Charm,Very rare,20
Foreword Farm,Magical Hat,Rainbow Spore Charm,Very rare,10
Foreword Farm,Magical Scarf,Parable Papyrus,Guaranteed,"5,000"
Fort Rox,Area Loot,Meteorite Piece,Common,25
Fort Rox,Area Loot,Crescent Cheese,Uncommon,10
Fort Rox,Area Loot,Moon Cheese,Uncommon,5
Fort Rox,Area Loot,Tower Mana,Rare,3
Fort Rox,Area Loot,Silver Bolt,Extremely rare,1
Fort Rox,Area Loot,Animatronic Bird,Extremely rare,1
Fort Rox,Magical Hat,Tower Mana,Very likely,15
Fort Rox,Magical Hat,Silver Bolt,Rare,1
Fort Rox,Magical Hat,Fort Rox Portal Core,Very rare,1
Fort Rox,Magical Hat,Fort Rox Portal Console,Very rare,1
Fort Rox,Magical Hat,Animatronic Bird,Extremely rare,1
Fort Rox,Magical Scarf,Fort Rox Portal Core,Uncommon,1
Fort Rox,Magical Scarf,Fort Rox Portal Console,Uncommon,1
Fort Rox,Magical Scarf,Tower Mana,Uncommon,150
Fungal Cavern,Area Loot,Cavern Fungus,Common,15
Fungal Cavern,Area Loot,Nightshade,Uncommon,10
Fungal Cavern,Area Loot,Gemstone,Uncommon,2
Fungal Cavern,Area Loot,Mineral,Rare,30
Fungal Cavern,Area Loot,Diamond,Extremely rare,1
Fungal Cavern,Magical Hat,Gemstone,Common,10
Fungal Cavern,Magical Hat,Diamond,Uncommon,5
Fungal Cavern,Magical Scarf,Diamond,Guaranteed,100
Furoma Rift,Area Loot,Chi Belt Token,Uncommon,5
Furoma Rift,Area Loot,Chi Fang Token,Uncommon,5
Furoma Rift,Area Loot,Chi Claw Token,Uncommon,5
Furoma Rift,Area Loot,Enerchi,Rare,20
Furoma Rift,Area Loot,Chi Belt Heirloom,Rare,10
Furoma Rift,Area Loot,Chi Fang Heirloom,Rare,10
Furoma Rift,Area Loot,Chi Claw Heirloom,Rare,10
Furoma Rift,Area Loot,Null Onyx Stone,Rare,1
Furoma Rift,Area Loot,Calcified Rift Mist,Very rare,15
Furoma Rift,Magical Hat,Rift Rumble Cheese,Uncommon,10
Furoma Rift,Magical Hat,Chi Belt Heirloom,Uncommon,10
Furoma Rift,Magical Hat,Chi Fang Heirloom,Uncommon,10
Furoma Rift,Magical Hat,Chi Claw Heirloom,Uncommon,10
Furoma Rift,Magical Hat,Enerchi,Uncommon,200
Furoma Rift,Magical Hat,Null Onyx Stone,Rare,10
Furoma Rift,Magical Scarf,Null Onyx Stone,Common,50
Furoma Rift,Magical Scarf,Enerchi,Uncommon,"3,600"
Gnawnia Rift,Area Loot,Riftiago Potion,Common,1
Gnawnia Rift,Area Loot,Rift Curd,Rare,30
Gnawnia Rift,Area Loot,Ionized Salt,Rare,50
Gnawnia Rift,Area Loot,Magic Seed,Rare,3
Gnawnia Rift,Area Loot,Riftgrass,Rare,3
Gnawnia Rift,Area Loot,Rift Dust,Rare,3
Gnawnia Rift,Area Loot,Bag of Living Essences,Very rare,1
Gnawnia Rift,Magical Hat,Magical String Cheese,Common,10
Gnawnia Rift,Magical Hat,Magic Seed,Uncommon,10
Gnawnia Rift,Magical Hat,Riftgrass,Uncommon,10
Gnawnia Rift,Magical Hat,Rift Dust,Uncommon,10
Gnawnia Rift,Magical Hat,Bag of Living Essences,Rare,3
Gnawnia Rift,Magical Scarf,Bag of Living Essences,Common,50
Gnawnia Rift,Magical Scarf,Resonator Cheese,Uncommon,75
Gnawnian Express Station,Area Loot,Supply Schedule Charm,Uncommon,10
Gnawnian Express Station,Area Loot,Roof Rack Charm,Uncommon,10
Gnawnian Express Station,Area Loot,Door Guard Charm,Uncommon,10
Gnawnian Express Station,Area Loot,Greasy Glob Charm,Uncommon,10
Gnawnian Express Station,Area Loot,Dusty Coal Charm,Uncommon,10
Gnawnian Express Station,Area Loot,Black Powder Charm,Rare,5
Gnawnian Express Station,Area Loot,Copper Bead,Very rare,5
Gnawnian Express Station,Area Loot,Tin Scrap,Very rare,5
Gnawnian Express Station,Area Loot,Iron Pellet,Very rare,5
Gnawnian Express Station,Magical Hat,Black Powder Charm,Common,5
Gnawnian Express Station,Magical Hat,Magmatic Crystal Charm,Common,5
Gnawnian Express Station,Magical Scarf,Magmatic Crystal Charm,Guaranteed,30
Great Gnarled Tree,Area Loot,Gnarled Potion,Guaranteed,1
Great Gnarled Tree,Magical Hat,Gnarled Potion,Guaranteed,10
Great Gnarled Tree,Magical Scarf,Gnarled Potion,Guaranteed,50
Harbour,Area Loot,Gold,Common,"3,500"
Harbour,Area Loot,Swiss Cheese,Uncommon,40
Harbour,Area Loot,Brie Cheese,Uncommon,20
Harbour,Magical Hat,SUPER|brie+,Guaranteed,10
Harbour,Magical Scarf,SUPER|brie+,Guaranteed,15
Iceberg,Area Loot,War Scrap,Uncommon,5
Iceberg,Area Loot,Wax Charm,Uncommon,10
Iceberg,Area Loot,Sticky Charm,Uncommon,10
Iceberg,Area Loot,Ultimate Luck Charm,Uncommon,1
Iceberg,Area Loot,Wire Spool,Rare,1
Iceberg,Area Loot,Heating Oil,Rare,1
Iceberg,Area Loot,Frosty Metal,Rare,1
Iceberg,Area Loot,Bottled Cold Fusion,Very rare,1
Iceberg,Area Loot,Drilling Dorblu Cheese,Extremely rare,1
Iceberg,Magical Hat,Drill Charge,Common,2
Iceberg,Magical Hat,Ultimate Luck Charm,Uncommon,10
Iceberg,Magical Hat,Bottled Cold Fusion,Uncommon,5
Iceberg,Magical Hat,Drilling Dorblu Cheese,Uncommon,2
Iceberg,Magical Scarf,Drill Charge,Guaranteed,8
Jungle of Dread,Area Loot,Spicy Havarti Cheese,Uncommon,5
Jungle of Dread,Area Loot,Pungent Havarti Cheese,Uncommon,5
Jungle of Dread,Area Loot,Creamy Havarti Cheese,Uncommon,5
Jungle of Dread,Area Loot,Magical Havarti Cheese,Uncommon,5
Jungle of Dread,Area Loot,Crunchy Havarti Cheese,Uncommon,5
Jungle of Dread,Area Loot,Sweet Havarti Cheese,Uncommon,5
Jungle of Dread,Area Loot,Fire Salt,Very rare,10
Jungle of Dread,Area Loot,Dreaded Charm,Very rare,5
Jungle of Dread,Magical Hat,Spicy Havarti Cheese,Uncommon,15
Jungle of Dread,Magical Hat,Pungent Havarti Cheese,Uncommon,15
Jungle of Dread,Magical Hat,Creamy Havarti Cheese,Uncommon,15
Jungle of Dread,Magical Hat,Magical Havarti Cheese,Uncommon,15
Jungle of Dread,Magical Hat,Crunchy Havarti Cheese,Uncommon,15
Jungle of Dread,Magical Hat,Sweet Havarti Cheese,Uncommon,15
Jungle of Dread,Magical Scarf,Fire Salt,Guaranteed,200
King's Arms,Area Loot,Regal Charm,Uncommon,5
King's Arms,Area Loot,Super Regal Charm,Uncommon,5
King's Arms,Area Loot,King's Credit,Uncommon,3
King's Arms,Area Loot,Extreme Regal Charm,Uncommon,5
King's Arms,Area Loot,Royal Loot Crate,Rare,1
King's Arms,Magical Hat,Royal Loot Crate,Common,1
King's Arms,Magical Hat,King's Credit,Uncommon,10
King's Arms,Magical Hat,Extreme Regal Charm,Uncommon,20
King's Arms,Magical Scarf,Royal Loot Crate,Guaranteed,10
King's Gauntlet,Area Loot,Gauntlet Potion Tier 6,Uncommon,2
King's Gauntlet,Area Loot,Gauntlet Potion Tier 4,Uncommon,5
King's Gauntlet,Area Loot,Gauntlet Potion Tier 2,Uncommon,10
King's Gauntlet,Area Loot,Gauntlet Potion Tier 3,Uncommon,10
King's Gauntlet,Area Loot,Gauntlet Potion Tier 5,Uncommon,5
King's Gauntlet,Area Loot,Gauntlet Potion Tier 7,Very rare,1
King's Gauntlet,Magical Hat,Gauntlet Potion Tier 7,Common,3
King's Gauntlet,Magical Hat,Gauntlet Potion Tier 8,Uncommon,1
King's Gauntlet,Magical Scarf,Gauntlet Potion Tier 8,Guaranteed,3
Laboratory,Area Loot,Radioactive Blue Potion,Common,3
Laboratory,Area Loot,Greater Radioactive Blue Potion,Uncommon,2
Laboratory,Area Loot,Radioactive Sludge,Uncommon,5
Laboratory,Area Loot,Scientist's Charm,Very rare,3
Laboratory,Magical Hat,Greater Radioactive Blue Potion,Common,15
Laboratory,Magical Hat,SUPER|brie+,Uncommon,15
Laboratory,Magical Scarf,Greater Radioactive Blue Potion,Guaranteed,100
Labyrinth,Area Loot,Cavern Fungus,Uncommon,10
Labyrinth,Area Loot,Lantern Oil,Uncommon,3
Labyrinth,Area Loot,Nightshade,Uncommon,8
Labyrinth,Area Loot,Shuffler's Cube,Uncommon,1
Labyrinth,Area Loot,Nightshade Farming Charm,Very rare,10
Labyrinth,Magical Hat,Lantern Oil,Guaranteed,10
Labyrinth,Magical Scarf,Lantern Oil,Guaranteed,100
Lagoon,Area Loot,Wicked Gnarly Potion,Common,2
Lagoon,Area Loot,Greater Wicked Gnarly Potion,Uncommon,2
Lagoon,Area Loot,Gnarled Potion,Uncommon,2
Lagoon,Area Loot,Scrap Metal,Rare,5
Lagoon,Magical Hat,Wicked Gnarly Potion,Common,10
Lagoon,Magical Hat,Scrap Metal,Common,50
Lagoon,Magical Scarf,Greater Wicked Gnarly Potion,Guaranteed,30
Living Garden,Area Loot,Ber Essence,Uncommon,5
Living Garden,Area Loot,Dewthief Petal,Uncommon,15
Living Garden,Area Loot,Graveblossom Petal,Uncommon,8
Living Garden,Area Loot,Dreamfluff Herbs,Rare,15
Living Garden,Area Loot,Duskshade Petal,Rare,15
Living Garden,Area Loot,Cynd Essence,Rare,5
Living Garden,Area Loot,Dol Essence,Very rare,5
Living Garden,Area Loot,Plumepearl Herbs,Very rare,5
Living Garden,Area Loot,Lunaria Petal,Very rare,5
Living Garden,Magical Hat,Plumepearl Herbs,Uncommon,30
Living Garden,Magical Hat,Lunaria Petal,Uncommon,30
Living Garden,Magical Hat,Fel Essence,Uncommon,1
Living Garden,Magical Hat,Est Essence,Uncommon,1
Living Garden,Magical Hat,Gur Essence,Uncommon,1
Living Garden,Magical Hat,Hix Essence,Rare,1
Living Garden,Magical Hat,Icuri Essence,Very rare,1
Living Garden,Magical Scarf,Plumepearl Herbs,Common,60
Living Garden,Magical Scarf,Lunaria Petal,Common,60
Living Garden,Magical Scarf,Icuri Essence,Rare,1
Lost City,Area Loot,Ber Essence,Uncommon,5
Lost City,Area Loot,Dreamfluff Herbs,Uncommon,15
Lost City,Area Loot,Dewthief Petal,Uncommon,15
Lost City,Area Loot,Graveblossom Petal,Uncommon,8
Lost City,Area Loot,Cynd Essence,Rare,5
Lost City,Area Loot,Plumepearl Herbs,Rare,5
Lost City,Area Loot,Dol Essence,Very rare,5
Lost City,Magical Hat,Plumepearl Herbs,Uncommon,30
Lost City,Magical Hat,Fel Essence,Uncommon,1
Lost City,Magical Hat,Est Essence,Uncommon,1
Lost City,Magical Hat,Gur Essence,Uncommon,1
Lost City,Magical Hat,Hix Essence,Rare,1
Lost City,Magical Hat,Icuri Essence,Very rare,1
Lost City,Magical Scarf,Plumepearl Herbs,Guaranteed,60
Meadow,Area Loot,Gold,Common,"1,250"
Meadow,Area Loot,Swiss Cheese,Uncommon,40
Meadow,Area Loot,Brie Cheese,Uncommon,20
Meadow,Magical Hat,SUPER|brie+,Guaranteed,10
Meadow,Magical Scarf,SUPER|brie+,Guaranteed,15
Meditation Room,Area Loot,Master Belt Shard,Uncommon,2
Meditation Room,Area Loot,Master Claw Shard,Uncommon,2
Meditation Room,Area Loot,Master Fang Shard,Uncommon,2
Meditation Room,Area Loot,Rumble Cheese,Extremely rare,1
Meditation Room,Magical Hat,Master Belt Shard,Uncommon,10
Meditation Room,Magical Hat,Master Claw Shard,Uncommon,10
Meditation Room,Magical Hat,Master Fang Shard,Uncommon,10
Meditation Room,Magical Scarf,Master Belt Shard,Uncommon,90
Meditation Room,Magical Scarf,Master Claw Shard,Uncommon,90
Meditation Room,Magical Scarf,Master Fang Shard,Uncommon,90
Mountain,Area Loot,Chedd-Ore Cheese,Common,15
Mountain,Area Loot,Faceted Sugar,Uncommon,3
Mountain,Area Loot,Iced Curd,Uncommon,3
Mountain,Area Loot,Extreme Power Charm,Rare,25
Mountain,Area Loot,Abominable Asiago Cheese,Rare,3
Mountain,Magical Hat,SUPER|brie+,Common,10
Mountain,Magical Hat,Abominable Asiago Cheese,Uncommon,10
Mountain,Magical Scarf,Abominable Asiago Cheese,Guaranteed,50
Mousoleum,Area Loot,Cemetery Slat,Common,10
Mousoleum,Area Loot,Crimson Curd,Uncommon,3
Mousoleum,Area Loot,Radioactive Blue Potion,Uncommon,3
Mousoleum,Magical Hat,Cemetery Slat,Common,30
Mousoleum,Magical Hat,Crimson Curd,Common,6
Mousoleum,Magical Scarf,Cemetery Slat,Common,"1,000"
Mousoleum,Magical Scarf,Crimson Curd,Common,300
Moussu Picchu,Area Loot,Shadowvine,Uncommon,6
Moussu Picchu,Area Loot,Arcanevine,Uncommon,6
Moussu Picchu,Area Loot,Dragonbane Charm,Uncommon,3
Moussu Picchu,Area Loot,Windy Potion,Rare,5
Moussu Picchu,Area Loot,Rainy Potion,Rare,5
Moussu Picchu,Area Loot,Super Dragonbane Charm,Rare,3
Moussu Picchu,Area Loot,Cavern Fungus,Rare,15
Moussu Picchu,Area Loot,Nightshade,Very rare,8
Moussu Picchu,Area Loot,Dragon Scale,Very rare,10
Moussu Picchu,Area Loot,Mineral,Very rare,30
Moussu Picchu,Area Loot,Extreme Dragonbane Charm,Very rare,3
Moussu Picchu,Area Loot,Ful'mina's Charged Toothlet,Very rare,5
Moussu Picchu,Magical Hat,Fire Bowl Fuel,Common,15
Moussu Picchu,Magical Hat,Dragon Scale,Uncommon,30
Moussu Picchu,Magical Hat,Extreme Dragonbane Charm,Uncommon,10
Moussu Picchu,Magical Hat,Ful'Mina's Tooth,Very rare,1
Moussu Picchu,Magical Scarf,Ful'Mina's Tooth,Guaranteed,1
Muridae Market,Area Loot,Limestone Brick,Uncommon,10
Muridae Market,Area Loot,Coconut Timber,Uncommon,10
Muridae Market,Area Loot,Artisan Charm,Uncommon,10
Muridae Market,Area Loot,Flameshard,Uncommon,15
Muridae Market,Area Loot,Shard of Glass,Uncommon,10
Muridae Market,Area Loot,Molten Glass,Rare,5
Muridae Market,Magical Hat,Artisan Charm,Guaranteed,25
Muridae Market,Magical Scarf,Flameshard,Guaranteed,300
Nerg Plains,Area Loot,Savoury Vegetables,Common,10
Nerg Plains,Area Loot,Yellow Pepper Seed,Common,10
Nerg Plains,Area Loot,Nerg Power Charm,Very rare,10
Nerg Plains,Magical Hat,Savoury Vegetables,Guaranteed,30
Nerg Plains,Magical Scarf,Savoury Vegetables,Guaranteed,300
Pinnacle Chamber,Area Loot,Rumble Cheese,Uncommon,1
Pinnacle Chamber,Area Loot,Master Belt Shard,Uncommon,1
Pinnacle Chamber,Area Loot,Master Claw Shard,Uncommon,1
Pinnacle Chamber,Area Loot,Master Fang Shard,Uncommon,1
Pinnacle Chamber,Magical Hat,Rumble Cheese,Guaranteed,10
Pinnacle Chamber,Magical Scarf,Rumble Cheese,Guaranteed,30
Prickly Plains,Area Loot,Hot Spice Leaf,Uncommon,5
Prickly Plains,Area Loot,Mild Spice Leaf,Uncommon,10
Prickly Plains,Area Loot,Medium Spice Leaf,Uncommon,10
Prickly Plains,Area Loot,Ember Root,Rare,1
Prickly Plains,Area Loot,Flamin' Spice Leaf,Very rare,5
Prickly Plains,Magical Hat,Flamin' Spice Leaf,Common,10
Prickly Plains,Magical Hat,Ember Root,Common,10
Prickly Plains,Magical Scarf,Ember Root,Guaranteed,30
Prologue Pond,Area Loot,Ingenuity Grub,Uncommon,15
Prologue Pond,Area Loot,Cleverness Clam,Uncommon,15
Prologue Pond,Area Loot,Inspiration Ink,Uncommon,65
Prologue Pond,Area Loot,Pond Penny,Rare,15
Prologue Pond,Area Loot,Spore Charm,Rare,4
Prologue Pond,Area Loot,Super Spore Charm,Rare,4
Prologue Pond,Area Loot,Magic Essence,Very rare,2
Prologue Pond,Area Loot,Extreme Spore Charm,Very rare,4
Prologue Pond,Area Loot,Ultimate Spore Charm,Very rare,4
Prologue Pond,Magical Hat,Inspiration Ink,Common,"1,000"
Prologue Pond,Magical Hat,Condensed Creativity,Uncommon,10
Prologue Pond,Magical Hat,Ultimate Spore Charm,Very rare,20
Prologue Pond,Magical Hat,Rainbow Spore Charm,Very rare,10
Prologue Pond,Magical Scarf,Inspiration Ink,Guaranteed,"5,000"
Queso Geyser,Area Loot,Cork Bark,Uncommon,10
Queso Geyser,Area Loot,Solidified Amber Queso,Rare,10
Queso Geyser,Area Loot,Dragonbane Charm,Rare,3
Queso Geyser,Area Loot,Super Dragonbane Charm,Rare,3
Queso Geyser,Area Loot,Extreme Dragonbane Charm,Rare,3
Queso Geyser,Area Loot,Bland Queso,Very rare,"1,750"
Queso Geyser,Area Loot,Mild Spice Leaf,Very rare,35
Queso Geyser,Area Loot,Medium Spice Leaf,Very rare,20
Queso Geyser,Area Loot,Hot Spice Leaf,Very rare,10
Queso Geyser,Area Loot,Flamin' Spice Leaf,Very rare,5
Queso Geyser,Area Loot,Queso Pump Charm,Very rare,10
Queso Geyser,Magical Hat,Wild Tonic,Uncommon,15
Queso Geyser,Magical Hat,Super Dragonbane Charm,Uncommon,15
Queso Geyser,Magical Hat,Bland Queso,Uncommon,"3,000"
Queso Geyser,Magical Hat,Ember Root,Uncommon,10
Queso Geyser,Magical Hat,Ember Stone,Uncommon,10
Queso Geyser,Magical Hat,Extreme Dragonbane Charm,Rare,15
Queso Geyser,Magical Hat,Magic Nest Dust,Very rare,1
Queso Geyser,Magical Hat,Queso Thermal Spring,Very rare,1
Queso Geyser,Magical Hat,Thermal Chisel,Extremely rare,1
Queso Geyser,Magical Hat,Geyser Smolder Stone,Extremely rare,1
Queso Geyser,Magical Hat,Kalor'ignis Rib,Extremely rare,1
Queso Geyser,Magical Scarf,Super Dragonbane Charm,Uncommon,30
Queso Geyser,Magical Scarf,Ember Root,Uncommon,30
Queso Geyser,Magical Scarf,Ember Stone,Uncommon,30
Queso Geyser,Magical Scarf,Bland Queso,Uncommon,"100,000"
Queso Geyser,Magical Scarf,Extreme Dragonbane Charm,Rare,30
Queso Geyser,Magical Scarf,Ultimate Dragonbane Charm,Extremely rare,30
Queso Geyser,Magical Scarf,Kalor'ignis Rib,Extremely rare,1
Queso River,Area Loot,Bland Queso,Very likely,500
Queso River,Area Loot,Queso Pump Charm,Rare,5
Queso River,Area Loot,Unstable Ember Gadget,Extremely rare,1
Queso River,Magical Hat,Bland Queso,Very likely,"3,000"
Queso River,Magical Hat,Unstable Ember Gadget,Uncommon,1
Queso River,Magical Scarf,Bland Queso,Guaranteed,"100,000"
S.S. Huntington IV,Area Loot,Gold,Common,"15,000"
S.S. Huntington IV,Area Loot,Brie Cheese,Uncommon,25
S.S. Huntington IV,Area Loot,Galleon Gouda,Uncommon,1
S.S. Huntington IV,Magical Hat,Galleon Gouda,Guaranteed,5
S.S. Huntington IV,Magical Scarf,Galleon Gouda,Guaranteed,50
Sand Dunes,Area Loot,Ber Essence,Uncommon,5
Sand Dunes,Area Loot,Duskshade Petal,Uncommon,15
Sand Dunes,Area Loot,Dewthief Petal,Uncommon,15
Sand Dunes,Area Loot,Graveblossom Petal,Uncommon,8
Sand Dunes,Area Loot,Cynd Essence,Rare,5
Sand Dunes,Area Loot,Lunaria Petal,Rare,5
Sand Dunes,Area Loot,Dol Essence,Very rare,5
Sand Dunes,Magical Hat,Lunaria Petal,Uncommon,30
Sand Dunes,Magical Hat,Fel Essence,Uncommon,1
Sand Dunes,Magical Hat,Est Essence,Uncommon,1
Sand Dunes,Magical Hat,Gur Essence,Uncommon,1
Sand Dunes,Magical Hat,Hix Essence,Rare,1
Sand Dunes,Magical Hat,Icuri Essence,Very rare,1
Sand Dunes,Magical Scarf,Lunaria Petal,Guaranteed,60
School of Sorcery,Area Loot,Master Magus Mimetite,Uncommon,30
School of Sorcery,Area Loot,Shadow Moonstone,Uncommon,20
School of Sorcery,Area Loot,Arcane Sunstone,Uncommon,20
School of Sorcery,Area Loot,Apprentice Alchemic Amber,Uncommon,12
School of Sorcery,Area Loot,Condensed Creativity,Rare,1
School of Sorcery,Area Loot,Magic Essence,Very rare,2
School of Sorcery,Area Loot,Super Spore Charm,Very rare,5
School of Sorcery,Area Loot,Spore Charm,Very rare,5
School of Sorcery,Area Loot,Extreme Spore Charm,Very rare,5
School of Sorcery,Area Loot,Ultimate Spore Charm,Extremely rare,5
School of Sorcery,Magical Hat,Condensed Creativity,Uncommon,10
School of Sorcery,Magical Hat,Master Mimolette Cheese,Uncommon,30
School of Sorcery,Magical Hat,Master Magus Wand,Uncommon,5
School of Sorcery,Magical Hat,Magic Essence,Rare,20
School of Sorcery,Magical Hat,Thousandth Draft Derby Cheese,Very rare,1
School of Sorcery,Magical Hat,Ultimate Spore Charm,Extremely rare,20
School of Sorcery,Magical Hat,Rainbow Spore Charm,Extremely rare,10
School of Sorcery,Magical Scarf,Master Mimolette Cheese,Uncommon,60
School of Sorcery,Magical Scarf,Master Magus Wand,Uncommon,10
School of Sorcery,Magical Scarf,Condensed Creativity,Uncommon,30
School of Sorcery,Magical Scarf,Magic Essence,Uncommon,30
School of Sorcery,Magical Scarf,Rainbow Spore Charm,Very rare,50
School of Sorcery,Magical Scarf,Thousandth Draft Derby Cheese,Very rare,1
Seasonal Garden,Area Loot,Amplifier Charm,Guaranteed,5
Seasonal Garden,Magical Hat,Amplifier Charm,Guaranteed,20
Seasonal Garden,Magical Scarf,Amplifier Charm,Guaranteed,600
Slushy Shoreline,Area Loot,War Scrap,Common,20
Slushy Shoreline,Area Loot,Softserve Charm,Uncommon,10
Slushy Shoreline,Area Loot,Wire Spool,Rare,1
Slushy Shoreline,Area Loot,Heating Oil,Rare,1
Slushy Shoreline,Area Loot,Frosty Metal,Rare,1
Slushy Shoreline,Magical Hat,War Scrap,Guaranteed,35
Slushy Shoreline,Magical Scarf,War Scrap,Guaranteed,500
Sunken City,Area Loot,Empowered Anchor Charm,Uncommon,1
Sunken City,Area Loot,Oxygen Canister,Uncommon,10
Sunken City,Area Loot,Sand Dollar,Uncommon,5
Sunken City,Area Loot,Water Jet Charm,Uncommon,1
Sunken City,Area Loot,Barnacle,Very rare,10
Sunken City,Area Loot,Mouse Scale,Very rare,10
Sunken City,Area Loot,Damaged Coral Fragment,Very rare,10
Sunken City,Area Loot,Scrap Metal,Very rare,10
Sunken City,Area Loot,Ultimate Anchor Charm,Extremely rare,1
Sunken City,Magical Hat,Sand Dollar,Common,15
Sunken City,Magical Hat,Water Jet Charm,Uncommon,10
Sunken City,Magical Hat,Ultimate Anchor Charm,Rare,1
Sunken City,Magical Hat,Predatory Processor,Extremely rare,1
Sunken City,Magical Scarf,Predatory Processor,Guaranteed,1
Table of Contents,Area Loot,Draft Derby Curd,Common,50
Table of Contents,Area Loot,Unstable Manuscript,Uncommon,1
Table of Contents,Area Loot,Spore Charm,Rare,4
Table of Contents,Area Loot,Super Spore Charm,Rare,4
Table of Contents,Area Loot,Condensed Creativity,Very rare,1
Table of Contents,Area Loot,Magic Essence,Very rare,2
Table of Contents,Area Loot,Extreme Spore Charm,Very rare,4
Table of Contents,Area Loot,Ultimate Spore Charm,Very rare,4
Table of Contents,Magical Hat,Draft Derby Curd,Common,"1,200"
Table of Contents,Magical Hat,Condensed Creativity,Uncommon,10
Table of Contents,Magical Hat,Magic Essence,Rare,20
Table of Contents,Magical Hat,Ultimate Spore Charm,Rare,20
Table of Contents,Magical Hat,Rainbow Spore Charm,Rare,10
Table of Contents,Magical Scarf,Unstable Magnum Opus,Common,1
Table of Contents,Magical Scarf,Rainbow Spore Charm,Uncommon,50
Tournament Hall,Area Loot,Runny Cheese,Uncommon,5
Tournament Hall,Area Loot,Tournament Token,Uncommon,3
Tournament Hall,Area Loot,Champion Charm,Uncommon,25
Tournament Hall,Area Loot,MEGA Tournament Token,Uncommon,1
Tournament Hall,Magical Hat,Tournament Token,Common,5
Tournament Hall,Magical Hat,MEGA Tournament Token,Uncommon,3
Tournament Hall,Magical Scarf,Tournament Token,Guaranteed,100
Town of Digby,Area Loot,Radioactive Sludge,Common,5
Town of Digby,Area Loot,Living Shard,Common,5
Town of Digby,Area Loot,Mining Charm,Rare,5
Town of Digby,Magical Hat,Limelight Cheese,Guaranteed,20
Town of Digby,Magical Scarf,Limelight Cheese,Guaranteed,250
Town of Gnawnia,Area Loot,Gold,Common,"2,250"
Town of Gnawnia,Area Loot,Swiss Cheese,Uncommon,40
Town of Gnawnia,Area Loot,Brie Cheese,Uncommon,20
Town of Gnawnia,Magical Hat,SUPER|brie+,Guaranteed,10
Town of Gnawnia,Magical Scarf,SUPER|brie+,Guaranteed,15
Toxic Spill,Area Loot,Radioactive Curd,Uncommon,25
Toxic Spill,Area Loot,Pollutinum,Uncommon,5
Toxic Spill,Area Loot,Radioactive Blue Potion,Uncommon,10
Toxic Spill,Area Loot,Radioactive Sludge,Uncommon,10
Toxic Spill,Area Loot,Scrap Metal,Very rare,10
Toxic Spill,Area Loot,Soap Charm,Very rare,10
Toxic Spill,Area Loot,Canister Ring,Extremely rare,1
Toxic Spill,Magical Hat,Pollutinum,Common,15
Toxic Spill,Magical Hat,Magical Rancid Radioactive Blue Cheese,Uncommon,10
Toxic Spill,Magical Scarf,Pollutinum,Guaranteed,200
Training Grounds,Area Loot,Token of the Cheese Belt,Uncommon,6
Training Grounds,Area Loot,Token of the Cheese Claw,Uncommon,6
Training Grounds,Area Loot,Token of the Cheese Fang,Uncommon,6
Training Grounds,Area Loot,Maki Cheese,Extremely rare,3
Training Grounds,Magical Hat,Maki Cheese,Guaranteed,10
Training Grounds,Magical Scarf,Maki Cheese,Guaranteed,15
Valour Rift,Area Loot,Rift Power Charm,Uncommon,5
Valour Rift,Area Loot,Rift Ultimate Lucky Power Charm,Uncommon,5
Valour Rift,Area Loot,Rift Ultimate Power Charm,Uncommon,5
Valour Rift,Area Loot,Gauntlet Elixir,Uncommon,10
Valour Rift,Area Loot,Rift Super Power Charm,Rare,5
Valour Rift,Area Loot,Rift Extreme Power Charm,Rare,5
Valour Rift,Area Loot,Champion's Fire,Very rare,5
Valour Rift,Magical Hat,Rift Ultimate Lucky Power Charm,Common,50
Valour Rift,Magical Hat,Rift Ultimate Power Charm,Uncommon,50
Valour Rift,Magical Hat,Champion's Fire,Uncommon,50
Valour Rift,Magical Scarf,Rift Ultimate Lucky Power Charm,Guaranteed,100
Whisker Woods Rift,Area Loot,Calcified Rift Mist,Common,5
Whisker Woods Rift,Area Loot,Rift Cherries,Rare,2
Whisker Woods Rift,Area Loot,Rift-torn Roots,Rare,2
Whisker Woods Rift,Area Loot,Sap-filled Thorns,Rare,2
Whisker Woods Rift,Area Loot,Tasty Spider Mould,Rare,1
Whisker Woods Rift,Area Loot,Creamy Gnarled Sap,Rare,1
Whisker Woods Rift,Area Loot,Crumbly Rift Salts,Rare,1
Whisker Woods Rift,Area Loot,Widow's Web,Very rare,1
Whisker Woods Rift,Area Loot,Taunting Charm,Extremely rare,1
Whisker Woods Rift,Magical Hat,Tasty Spider Mould,Uncommon,1
Whisker Woods Rift,Magical Hat,Creamy Gnarled Sap,Uncommon,1
Whisker Woods Rift,Magical Hat,Crumbly Rift Salts,Uncommon,1
Whisker Woods Rift,Magical Hat,Taunting Charm,Uncommon,1
Whisker Woods Rift,Magical Hat,Widow's Web,Rare,1
Whisker Woods Rift,Magical Scarf,Taunting Charm,Guaranteed,3
Windmill,Area Loot,Gold,Common,"3,250"
Windmill,Area Loot,Packet of Flour,Common,20
Windmill,Magical Hat,Grilled Cheese,Guaranteed,20
Windmill,Magical Scarf,Grilled Cheese,Guaranteed,300
Zokor,Area Loot,Plate of Fealty,Uncommon,5
Zokor,Area Loot,Scholar Scroll,Uncommon,5
Zokor,Area Loot,Tech Power Core,Uncommon,5
Zokor,Area Loot,Unstable Crystal,Rare,1
Zokor,Area Loot,Cavern Fungus,Rare,10
Zokor,Area Loot,Ultimate Power Charm,Rare,1
Zokor,Area Loot,Ultimate Luck Charm,Rare,1
Zokor,Area Loot,Unstable Charm,Rare,10
Zokor,Area Loot,Rift Ultimate Lucky Power Charm,Very rare,1
Zokor,Area Loot,Nightshade,Very rare,8
Zokor,Area Loot,Sacred Script,Very rare,1
Zokor,Area Loot,Infused Plate,Very rare,1
Zokor,Area Loot,Powercore Hammer,Very rare,1
Zokor,Area Loot,"Really, Really Shiny Precious Gold",Extremely rare,1
Zokor,Magical Hat,Ultimate Power Charm,Uncommon,20
Zokor,Magical Hat,Ultimate Luck Charm,Uncommon,20
Zokor,Magical Hat,Rift Ultimate Lucky Power Charm,Uncommon,15
Zokor,Magical Hat,Sacred Script,Uncommon,1
Zokor,Magical Hat,Infused Plate,Uncommon,1
Zokor,Magical Hat,Powercore Hammer,Uncommon,1
Zokor,Magical Hat,Ultimate Charm,Rare,1
Zokor,Magical Hat,"Really, Really Shiny Precious Gold",Extremely rare,1
Zokor,Magical Hat,Enigmatic Core,Extremely rare,1
Zokor,Magical Hat,Essence of Destruction,Extremely rare,1
Zokor,Magical Hat,Temporal Shadow Plate,Extremely rare,1
Zokor,Magical Scarf,Ultimate Charm,Common,1
Zokor,Magical Scarf,Ultimate Lucky Power Charm,Common,100
Zokor,Magical Scarf,Enigmatic Core,Very rare,1
Zokor,Magical Scarf,Essence of Destruction,Extremely rare,1
Zokor,Magical Scarf,Temporal Shadow Plate,Extremely rare,1
Zugzwang's Tower,Area Loot,Amplifier Charm,Common,5
Zugzwang's Tower,Area Loot,Checkmate Cheese,Uncommon,2
Zugzwang's Tower,Area Loot,Rook Crumble Charm,Rare,3
Zugzwang's Tower,Magical Hat,Checkmate Cheese,Guaranteed,5
Zugzwang's Tower,Magical Scarf,Checkmate Cheese,Guaranteed,30
""";
    const string RegionGroups = """
        Gnawnia
        Meadow
        Town of Gnawnia
        Windmill
        Harbour
        Mountain

        Valour
        King's Arms
        Tournament Hall
        King's Gauntlet

        Whisker Woods
        Calm Clearing
        Great Gnarled Tree
        Lagoon

        Burroughs
        Laboratory
        Town of Digby
        Mousoleum
        Bazaar
        Toxic Spill

        Furoma
        Training Grounds
        Dojo
        Meditation Room
        Pinnacle Chamber

        Bristle Woods
        Catacombs
        Forbidden Grove
        Acolyte Realm

        Tribal Isles
        Cape Clawed
        Elub Shore
        Nerg Plains
        Derr Dunes
        Jungle of Dread
        Dracano
        Balack's Cove

        Varmint Valley
        Claw Shot City
        Gnawnian Express Station
        Fort Rox

        Queso Canyon
        Queso River
        Prickly Plains
        Cantera Quarry
        Queso Geyser

        Rodentia
        S.S. Huntington IV
        Slushy Shoreline
        Iceberg
        Seasonal Garden
        Zugzwang's Tower
        Crystal Library
        Sunken City

        Sandtail Desert
        Fiery Warpath
        Muridae Market
        Living Garden
        Lost City
        Sand Dunes

        Hollow Heights
        Fungal Cavern
        Labyrinth
        Zokor
        Moussu Picchu
        Floating Islands

        Folklore Forest
        Foreword Farm
        Prologue Pond
        Table of Contents
        Bountiful Beanstalk
        School of Sorcery
        Draconic Depths

        Rift Plane
        Gnawnia Rift
        Burroughs Rift
        Whisker Woods Rift
        Furoma Rift
        Bristle Woods Rift
        Valour Rift
        """;
    #endregion
}

record GolemLoot(string Location, string LootType, string Loot, string Rarity, string Quantity);
