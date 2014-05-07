using System;

namespace SimpleWAWS.Code
{
    public class SiteNameGenerator
    {
        private string[] _adjectives = new string[] { "Blue", "Green", "Orange", "Pink", "Purple", "Red", "Yellow", "Adventurous", "Bold", "Brave", "Bright", "Charming", "Colorful", "Cool", "Courageous",
            "Creative", "Dazzling", "Dancing", "Eager", "Fast", "Fearless", "Friendly", "Gentle", "Grazing", "Happy", "Heroic", "Hungry", "Important", "Impressive", "Ingenious", "Insightful", "Intuitive", "Laughing", "Masterful",
            "Merry", "Mighty", "Mild", "Modern", "Outstanding", "Smooth", "Patient", "Peaceful", "Popular", "Precise", "Productive", "Proud", "Resilient", "Resourceful", "Running", "Savvy", "Sociable",
            "Sharp", "Shiny", "Skillful", "Smart", "Speedy", "Spirited", "Strong", "Stunning", "Successful", "Talented", "Technical", "Tenacious", "Thoughtful", "Upbeat", "Valuable", "Victorious",
            "Vigorous", "Visionary", "Vital", "Vivacious", "Winning", "Wise", "Youthful", "Zealous", "Zestful"
        };
        private string[] _nouns = new string[] { "Albatross", "Alligator", "Ant", "Armadillo", "Badger", "Bear", "Bee", "Bird", "Bison", "Bull", "Buffalo", "Caribou", "Cheetah", "Cobra",
            "Coyote", "Crow", "Crab", "Dinosaur", "Dragon", "Dolphin", "Dove", "Eagle", "Elephant", "Elk", "Falcon", "Fish", "Fox", "Frog", "Gazelle", "Panda", "Giraffe", "Gorilla",
            "Hamster", "Hawk", "Heron", "Hippo", "Horse", "Kangaroo", "Koala", "Kudu", "Lemur", "Leopard", "Lion", "Llama", "Lobster", "Manatee", "Meerkat", "Mink", "Mouse",
            "Narwhal", "Octopus", "Okapi", "Oryx", "Ostrich", "Otter", "Oyster", "Panther", "Parrot", "Pelican", "Penguin", "Pony", "Quail", "Rabbit", "Raven", "Rhino", "Robin",
            "Salmon", "Seahorse", "Seal", "Sparrow", "Spider", "Squid", "Squirrel", "Starling", "Stork", "Swan", "Tiger", "Trout", "Turkey", "Turtle", "Unicorn", "Walrus", "Wolf",
            "Cloud", "Galaxy", "Moon", "Mountain", "Prairie", "Star", "Valley", "Wind", "Rocket"
        };
        private Random _random = new Random();

        public string GenerateName(bool includeNumber)
        {
            // Return names like BlueTiger52349
            string name =_adjectives[_random.Next(_adjectives.Length)] +
                _nouns[_random.Next(_nouns.Length)];

            if (includeNumber)
            {
                name += _random.Next(1000);
            }

            return name;
        }
    }
}
