using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Pulsar4X.ECSLib;
using Pulsar4X.ECSLib.ComponentFeatureSets.Damage;
using Pulsar4X.Orbital;

namespace Pulsar4X.Tests
{
    internal static class TestingUtilities
    {

        
        public static Entity BasicSol(EntityManager mgr)
        {
            double parentMass = 1.989e30;
            BaseDataBlob[] parentblobs = new BaseDataBlob[3];
            parentblobs[0] = new PositionDB(mgr.ManagerGuid) { AbsolutePosition = Vector3.Zero };
            parentblobs[1] = MassVolumeDB.NewFromMassAndRadius_m(parentMass, 696342000.0 );
            parentblobs[2] = new OrbitDB();
            return new Entity(mgr, parentblobs);
        }

        public static Entity BasicEarth(EntityManager mgr)
        {
            double parentMass = 5.97237e24;
            BaseDataBlob[] parentblobs = new BaseDataBlob[3];
            parentblobs[0] = new PositionDB(mgr.ManagerGuid) { AbsolutePosition = Vector3.Zero };
            parentblobs[1] = new MassVolumeDB() { MassDry = parentMass };
            parentblobs[2] = new OrbitDB();
            return new Entity(mgr, parentblobs);
        }

        internal static Game CreateTestUniverse(int numSystems, DateTime testTime, bool generateDefaultHumans = false)
        {
            var gamesettings = new NewGameSettings { GameName = "Unit Test Game", StartDateTime = testTime, MaxSystems = numSystems, DefaultSolStart = generateDefaultHumans, CreatePlayerFaction = false };

            var game = new Game(gamesettings );

            var smAuthToken = new AuthenticationToken(game.SpaceMaster);

            // Systems are currently generated in the Game Constructor.
            // Later, Systems will be initialized in the game constructor, but not actually generated until player discovery.
            //game.GenerateSystems(smAuthToken, numSystems);

            // add a faction:
            Entity humanFaction = FactionFactory.CreateFaction(game, "New Terran Utopian Empire");

            // add a species:
            Entity humanSpecies = SpeciesFactory.CreateSpeciesHuman(humanFaction, game.GlobalManager);

            // add another faction:
            Entity greyAlienFaction = FactionFactory.CreateFaction(game, "The Grey Empire");
            // Add another species:
            Entity greyAlienSpecies = SpeciesFactory.CreateSpeciesHuman(greyAlienFaction, game.GlobalManager);

            // Greys Name the Humans.
            humanSpecies.GetDataBlob<NameDB>().SetName(greyAlienFaction.Guid, "Stupid Terrans");
            // Humans name the Greys.
            greyAlienSpecies.GetDataBlob<NameDB>().SetName(humanFaction.Guid, "Space bugs");
            //TODO Expand the "Test Universe" to cover more datablobs and entities. And ships. Etc.

            if (generateDefaultHumans)
            {
                DefaultStartFactory.DefaultHumans(game, "Humans");
            }

            return game;
        }

        internal static Game CreateTestUniverse(int numSystems, bool generateDefaultHumans = false)
        {
            return CreateTestUniverse(numSystems, DateTime.Now, generateDefaultHumans);
        }

        /// <summary>
        /// Shamelessly copied from https://stackoverflow.com/questions/7931304/comparing-two-files-in-c-sharp
        /// </summary>
        /// <returns>True if files are bitwise the same, false otherwise</returns>
        public static bool FileCompare(string file1, string file2)
        {
            int file1byte;
            int file2byte;
            FileStream fs1;
            FileStream fs2;

            // Determine if the same file was referenced two times.
            if (file1 == file2)
            {
                // Return true to indicate that the files are the same.
                return true;
            }

            // Open the two files.
            fs1 = new FileStream(file1, FileMode.Open, FileAccess.Read);
            fs2 = new FileStream(file2, FileMode.Open, FileAccess.Read);

            // Check the file sizes. If they are not the same, the files 
            // are not the same.
            if (fs1.Length != fs2.Length)
            {
                // Close the file
                fs1.Close();
                fs2.Close();

                // Return false to indicate files are different
                return false;
            }

            // Read and compare a byte from each file until either a
            // non-matching set of bytes is found or until the end of
            // file1 is reached.
            do
            {
                // Read one byte from each file.
                file1byte = fs1.ReadByte();
                file2byte = fs2.ReadByte();
            }
            while ((file1byte == file2byte) && (file1byte != -1));

            // Close the files.
            fs1.Close();
            fs2.Close();

            // Return the success of the comparison. "file1byte" is 
            // equal to "file2byte" at this point only if the files are 
            // the same.
            return ((file1byte - file2byte) == 0);

        }

        public static string GetTestFilepath()
        {
            var path = Path.GetTempFileName();
            if (File.Exists(path))
                File.Delete(path);
            string filePart = Path.ChangeExtension(Path.GetFileNameWithoutExtension(path), "json");
            return Path.Combine(SerializationManager.GetWorkingDirectory(), filePart);
        }

    }

    public class TestGame
    {
        public NewGameSettings GameSettings { get; set; }

        public Game Game { get; set; }

        public Entity HumanFaction { get; set; }

        public Entity HumanSpecies { get; set; }

        public Entity GreyAlienFaction { get; set; }

        public Entity GreyAlienSpecies { get; set; }

        public ComponentDesign DefaultEngineDesign { get; set; }

        public ComponentDesign DefaultWeaponDesign { get; set; }

        public ShipDesign DefaultShipDesign { get; set; }

        public Entity EarthColony { get; set; }

        public Entity DefaultShip { get; set; }
        public StarSystem Sol { get; set; }
        public Entity Earth { get; set; }

        internal TestGame(int numSystems = 10)
        {

            GameSettings = new  NewGameSettings { GameName = "Unit Test Game", MaxSystems = numSystems, CreatePlayerFaction = false };

            Game = new Game(GameSettings);

            // add a faction:
            HumanFaction = FactionFactory.CreateFaction(Game, "New Terran Utopian Empire");

            // add a species:
            HumanSpecies = SpeciesFactory.CreateSpeciesHuman(HumanFaction, Game.GlobalManager);

            // add another faction:
            GreyAlienFaction = FactionFactory.CreateFaction(Game, "The Grey Empire");
            // Add another species:
            GreyAlienSpecies = SpeciesFactory.CreateSpeciesHuman(GreyAlienFaction, Game.GlobalManager);

            // Greys Name the Humans.
            HumanSpecies.GetDataBlob<NameDB>().SetName(GreyAlienFaction.Guid, "Stupid Terrans");
            // Humans name the Greys.
            GreyAlienSpecies.GetDataBlob<NameDB>().SetName(HumanFaction.Guid, "Space bugs");


            StarSystemFactory starfac = new StarSystemFactory(Game);
            Sol = starfac.CreateSol(Game);
            Earth = NameLookup.GetFirstEntityWithName(Sol, "Earth"); //Sol.Entities[3]; //should be fourth entity created 
             EarthColony = ColonyFactory.CreateColony(HumanFaction, HumanSpecies, Earth);

            DefaultEngineDesign = DefaultStartFactory.DefaultThrusterDesign(Game, HumanFaction);
            DefaultWeaponDesign = DefaultStartFactory.DefaultSimpleLaser(Game, HumanFaction);
            DefaultShipDesign = DefaultStartFactory.DefaultShipDesign(Game, HumanFaction);

            Vector3 position = Earth.GetDataBlob<PositionDB>().AbsolutePosition;
            DefaultShip = ShipFactory.CreateShip(DefaultShipDesign, HumanFaction, position, Earth,  "Serial Peacemaker");
            Sol.SetDataBlob(DefaultShip.ID, new TransitableDB());
        }


    }
}
